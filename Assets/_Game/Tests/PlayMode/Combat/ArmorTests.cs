using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DungeonCrawler.Combat;
using DungeonCrawler.Enemies;
using DungeonCrawler.Loot;
using DungeonCrawler.Player;
using DungeonCrawler.Rooms;

namespace DungeonCrawler.Tests
{
    // Play Mode coverage for armor: what wearing it does, the leather chest, picking it up, and maze enemies dropping chain mail.
    //
    // Armor has two states (not worn, worn), so the first group checks that change and that it only
    // happens once. The drop rule is a small decision table: armor only drops when the player isn't
    // wearing any and there isn't a set already on the floor.
    public class ArmorTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f; // PauseMenu can leave this at 0
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned)
                if (go != null) Object.Destroy(go);
            spawned.Clear();

            foreach (var pickup in Object.FindObjectsByType<ArmorPickup>(FindObjectsSortMode.None))
                Object.Destroy(pickup.gameObject);
            foreach (var pickup in Object.FindObjectsByType<HealthPickup>(FindObjectsSortMode.None))
                Object.Destroy(pickup.gameObject);
        }

        // ---------- wearing it ----------

        [UnityTest]
        public IEnumerator Player_StartsWithoutArmor()
        {
            var armor = SpawnPlayer();
            yield return null;

            Assert.IsNotNull(armor, "PlayerController should add the armor component itself");
            Assert.IsFalse(armor.HasArmor);
        }

        [UnityTest]
        public IEnumerator Equip_AddsOneHeart_AndFillsIt()
        {
            var armor = SpawnPlayer();
            var health = armor.GetComponent<Health>();
            yield return null;
            int maxBefore = health.Max, currentBefore = health.Current;

            armor.Equip();

            Assert.IsTrue(armor.HasArmor);
            Assert.AreEqual(maxBefore + Health.HpPerHeart, health.Max, "armor should add exactly one heart");
            Assert.AreEqual(currentBefore + Health.HpPerHeart, health.Current, "the new heart should start full");
        }

        [UnityTest]
        public IEnumerator Equip_Twice_OnlyAddsTheHeartOnce()
        {
            var armor = SpawnPlayer();
            var health = armor.GetComponent<Health>();
            yield return null;

            armor.Equip();
            int maxAfterFirst = health.Max;
            armor.Equip();

            Assert.AreEqual(maxAfterFirst, health.Max);
        }

        // ---------- chain mail ----------

        [UnityTest]
        public IEnumerator ChainMail_ReplacesLeather_AndAddsOneMoreHeart()
        {
            var armor = SpawnPlayer();
            var health = armor.GetComponent<Health>();
            yield return null;
            int baseMax = health.Max;

            armor.Equip(ArmorTier.Leather);
            Assert.IsTrue(armor.Equip(ArmorTier.ChainMail), "chain mail is an upgrade over leather");

            Assert.AreEqual(ArmorTier.ChainMail, armor.Tier);
            Assert.AreEqual(2, armor.ExtraHearts);
            Assert.AreEqual(baseMax + 2 * Health.HpPerHeart, health.Max, "chain mail gives two hearts in total, not three");
        }

        [UnityTest]
        public IEnumerator Leather_DoesNotReplaceChainMail()
        {
            var armor = SpawnPlayer();
            var health = armor.GetComponent<Health>();
            yield return null;

            armor.Equip(ArmorTier.ChainMail);
            int maxWithChain = health.Max;

            Assert.IsFalse(armor.Equip(ArmorTier.Leather), "putting on worse armor should do nothing");
            Assert.AreEqual(ArmorTier.ChainMail, armor.Tier);
            Assert.AreEqual(maxWithChain, health.Max);
        }

        [UnityTest]
        public IEnumerator ChainMailPickup_UpgradesThePlayer()
        {
            var armor = SpawnPlayer();
            yield return new WaitForFixedUpdate();
            armor.Equip(ArmorTier.Leather);

            LootDropper.SpawnArmor(Vector2.zero, null, ArmorTier.ChainMail);
            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();

            Assert.AreEqual(ArmorTier.ChainMail, armor.Tier);
        }

        [UnityTest]
        public IEnumerator EnemyInTheMaze_DropsChainMail_EvenWhenWearingLeather()
        {
            var armor = SpawnPlayer();
            var dropper = SpawnEnemy(new Vector2(5f, 0f));
            dropper.GetComponent<Enemy>().Room = SpawnRoom(ArmorTier.ChainMail);
            yield return null;
            armor.Equip(ArmorTier.Leather);

            var drop = dropper.TryDropArmor();

            Assert.IsNotNull(drop, "the maze's chain mail is an upgrade over leather");
            Assert.AreEqual(ArmorTier.ChainMail, drop.GetComponent<ArmorPickup>().Tier);
        }

        [UnityTest]
        public IEnumerator EnemyInTheMaze_DropsNothing_OnceTheyHaveChainMail()
        {
            var armor = SpawnPlayer();
            var dropper = SpawnEnemy(new Vector2(5f, 0f));
            dropper.GetComponent<Enemy>().Room = SpawnRoom(ArmorTier.ChainMail);
            yield return null;
            armor.Equip(ArmorTier.ChainMail);

            Assert.IsNull(dropper.TryDropArmor());
        }

        // ---------- picking it up ----------

        [UnityTest]
        public IEnumerator ArmorPickup_PutsArmorOnThePlayer_AndDisappears()
        {
            var armor = SpawnPlayer();
            yield return new WaitForFixedUpdate();

            var pickup = LootDropper.SpawnArmor(Vector2.zero, null);
            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();

            Assert.IsTrue(armor.HasArmor);
            Assert.IsTrue(pickup == null, "the pickup should be gone once taken");
        }

        // ---------- enemies dropping it ----------

        [UnityTest]
        public IEnumerator Enemy_InANormalRoom_DropsNoArmor()
        {
            SpawnPlayer();
            var dropper = SpawnEnemy(new Vector2(5f, 0f));
            dropper.GetComponent<Enemy>().Room = SpawnRoom(ArmorTier.None);
            yield return null;

            Assert.IsNull(dropper.TryDropArmor(), "leather comes from the chest now, not from enemies");
        }

        [UnityTest]
        public IEnumerator Enemy_DoesNotDropASecondSet_WhileOneIsOnTheFloor()
        {
            SpawnPlayer();
            var maze = SpawnRoom(ArmorTier.ChainMail);
            var first = SpawnEnemy(new Vector2(5f, 0f));
            var second = SpawnEnemy(new Vector2(7f, 0f));
            first.GetComponent<Enemy>().Room = maze;
            second.GetComponent<Enemy>().Room = maze;
            yield return null;

            Assert.IsNotNull(first.TryDropArmor());
            yield return null;
            Assert.IsNull(second.TryDropArmor(), "only one set of armor should be lying around at a time");
        }

        // ---------- the leather armor chest ----------

        [UnityTest]
        public IEnumerator LeatherChest_PutsLeatherOnThePlayer_AndIsThenEmpty()
        {
            var armor = SpawnPlayer();
            var chest = SpawnChest(new Vector2(6f, 0f), ArmorTier.Leather);
            yield return null;

            Assert.IsTrue(chest.TakeItem(armor.GetComponent<PlayerController>()));

            Assert.AreEqual(ArmorTier.Leather, armor.Tier);
            Assert.IsTrue(chest.IsEmpty);
            Assert.IsFalse(chest.TakeItem(armor.GetComponent<PlayerController>()), "nothing left the second time");
        }

        [UnityTest]
        public IEnumerator LeatherChest_KeepsItsArmor_IfThePlayerAlreadyHasChainMail()
        {
            var armor = SpawnPlayer();
            var chest = SpawnChest(new Vector2(6f, 0f), ArmorTier.Leather);
            yield return null;
            armor.Equip(ArmorTier.ChainMail);

            Assert.IsFalse(chest.TakeArmor(armor.GetComponent<PlayerController>()));
            Assert.AreEqual(ArmorTier.ChainMail, armor.Tier);
            Assert.IsTrue(chest.ContainsArmor);
        }

        // ---------- grabbing with E or a click ----------

        [UnityTest]
        public IEnumerator Grabber_FindsLootInReach_AndGrabsIt()
        {
            var armor = SpawnPlayer();
            var grabber = armor.GetComponent<LootGrabber>();
            yield return new WaitForFixedUpdate();

            // far enough not to be walked over, close enough to reach
            var pickup = LootDropper.SpawnArmor(new Vector2(grabber.Reach - 0.5f, 0f), null);
            yield return new WaitForFixedUpdate();

            var nearest = grabber.NearestLoot();
            Assert.AreEqual(pickup.GetComponent<ArmorPickup>(), nearest);
            Assert.IsTrue(grabber.Grab(nearest));
            Assert.IsTrue(armor.HasArmor);
        }

        [UnityTest]
        public IEnumerator Grabber_IgnoresLootOutOfReach()
        {
            var armor = SpawnPlayer();
            var grabber = armor.GetComponent<LootGrabber>();
            yield return new WaitForFixedUpdate();

            LootDropper.SpawnArmor(new Vector2(grabber.Reach + 2f, 0f), null);
            yield return new WaitForFixedUpdate();

            Assert.IsNull(grabber.NearestLoot());
        }

        // ---------- fixtures ----------

        private PlayerArmor SpawnPlayer()
        {
            var go = new GameObject("TestPlayer");
            go.AddComponent<Rigidbody2D>().gravityScale = 0f;
            go.AddComponent<CircleCollider2D>().radius = 0.4f;
            go.AddComponent<Health>();

            // the controller complains that it has no input asset. movement isn't under test here
            LogAssert.Expect(LogType.Error, new Regex("PlayerController needs the InputSystem_Actions"));
            go.AddComponent<PlayerController>(); // adds the armor component itself

            spawned.Add(go);
            return go.GetComponent<PlayerArmor>();
        }

        private Chest SpawnChest(Vector2 position, ArmorTier armor)
        {
            var go = new GameObject("TestChest");
            go.transform.position = position;
            go.AddComponent<BoxCollider2D>();
            var chest = go.AddComponent<Chest>();
            spawned.Add(go);

            chest.ContainsSword = false; // chests hold the sword by default, this one only holds armor
            chest.Armor = armor;
            return chest;
        }

        private Room SpawnRoom(ArmorTier armorLoot)
        {
            var go = new GameObject("TestRoom");
            spawned.Add(go);
            var room = go.AddComponent<Room>();
            room.ArmorLoot = armorLoot;
            return room;
        }

        private LootDropper SpawnEnemy(Vector2 position)
        {
            var go = new GameObject("TestEnemy");
            go.transform.position = position;
            go.AddComponent<Rigidbody2D>().gravityScale = 0f;
            go.AddComponent<CircleCollider2D>().radius = 0.4f;
            go.AddComponent<Health>();
            go.AddComponent<Enemy>(); // adds the LootDropper itself
            spawned.Add(go);
            return go.GetComponent<LootDropper>();
        }
    }
}

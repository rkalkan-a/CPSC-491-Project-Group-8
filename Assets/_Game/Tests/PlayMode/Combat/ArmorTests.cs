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

namespace DungeonCrawler.Tests
{
    // Play Mode coverage for leather armor: what wearing it does, picking it up, and enemies dropping it.
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
        public IEnumerator Enemy_DropsArmor_WhenThePlayerHasNone()
        {
            SpawnPlayer();
            var dropper = SpawnEnemy(new Vector2(5f, 0f));
            yield return null;

            Assert.IsNotNull(dropper.TryDropArmor());
        }

        [UnityTest]
        public IEnumerator Enemy_DoesNotDropArmor_WhenThePlayerIsWearingIt()
        {
            var armor = SpawnPlayer();
            var dropper = SpawnEnemy(new Vector2(5f, 0f));
            yield return null;
            armor.Equip();

            Assert.IsNull(dropper.TryDropArmor());
        }

        [UnityTest]
        public IEnumerator Enemy_DoesNotDropASecondSet_WhileOneIsOnTheFloor()
        {
            SpawnPlayer();
            var first = SpawnEnemy(new Vector2(5f, 0f));
            var second = SpawnEnemy(new Vector2(7f, 0f));
            yield return null;

            Assert.IsNotNull(first.TryDropArmor());
            yield return null;
            Assert.IsNull(second.TryDropArmor(), "only one set of armor should be lying around at a time");
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

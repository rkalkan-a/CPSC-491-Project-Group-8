using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DungeonCrawler.Loot;
using DungeonCrawler.Player;
using DungeonCrawler.UI;

namespace DungeonCrawler.Tests
{
    // SCRUM-98 (chest and chest screen) and SCRUM-97 (loot pickup through the chest).
    //
    // The chest is a small state machine: closed and full -> open and full -> open and empty. These tests walk
    // that path the way the player does in the demo (walk into the chest, the chest screen opens, take the item),
    // and check the edges: opening twice, taking from an empty chest, and opening with no chest screen in the scene.
    public class ChestTests
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
        }

        // ---------- closed ----------

        [UnityTest]
        public IEnumerator NewChest_StartsClosed_HoldingTheSword_AndGlows()
        {
            var chest = SpawnChest(Vector2.zero);
            yield return null;

            Assert.IsFalse(chest.IsOpen);
            Assert.IsTrue(chest.ContainsSword, "the starting chest holds the sword");
            Assert.IsNotNull(chest.GetComponent<ChestGlow>(), "a chest adds its own glow so the player notices it");
        }

        // ---------- opening ----------

        [UnityTest]
        public IEnumerator WalkingIntoTheChest_OpensIt_AndShowsTheChestScreen()
        {
            var panel = SpawnHud();
            var chest = SpawnChest(new Vector2(1.5f, 0f));
            var player = SpawnPlayer(Vector2.zero);
            yield return null;

            // move the player into the chest like walking into it. there's no input asset in a test, so the
            // controller's own knockback drives the body instead (it sets the velocity the same way walking does)
            player.ApplyKnockback(Vector2.right * 6f, 1f);
            for (int i = 0; i < 50 && !chest.IsOpen; i++)
                yield return new WaitForFixedUpdate();

            Assert.IsTrue(chest.IsOpen, "bumping into the chest should open it");
            Assert.IsTrue(panel.IsShowing, "opening the chest should show the chest screen");
            Assert.IsTrue(panel.transform.Find("ChestScreen").gameObject.activeSelf);
            Assert.IsFalse(player.InputEnabled, "the player shouldn't walk around behind the chest screen");
        }

        [UnityTest]
        public IEnumerator OpeningAnOpenChest_ShowsTheScreenAgain_WithoutChangingWhatsInside()
        {
            var panel = SpawnHud();
            var chest = SpawnChest(Vector2.zero);
            var player = SpawnPlayer(new Vector2(3f, 0f));
            yield return null;

            chest.Open(player);
            panel.Hide();
            chest.Open(player);

            Assert.IsTrue(chest.IsOpen);
            Assert.IsTrue(panel.IsShowing, "walking back into an open chest should show it again");
            Assert.IsTrue(chest.ContainsSword, "opening alone shouldn't take the sword");
        }

        [UnityTest]
        public IEnumerator OpeningWithNoChestScreenInTheScene_StillOpensTheChest()
        {
            var chest = SpawnChest(Vector2.zero);
            var player = SpawnPlayer(new Vector2(3f, 0f));
            yield return null;

            Assert.DoesNotThrow(() => chest.Open(player));
            Assert.IsTrue(chest.IsOpen);
        }

        // ---------- taking the item ----------

        [UnityTest]
        public IEnumerator TakingFromTheChestScreen_GivesTheSword_AndEmptiesTheChest()
        {
            var panel = SpawnHud();
            var chest = SpawnChest(Vector2.zero);
            var player = SpawnPlayer(new Vector2(3f, 0f));
            var sword = player.GetComponent<PlayerSword>();
            yield return null;
            chest.Open(player);

            panel.TakeItem(); // what clicking the sword or pressing E does

            Assert.IsTrue(sword.HasSword, "the player should now hold the sword");
            Assert.IsFalse(chest.ContainsSword);
            Assert.IsTrue(chest.IsEmpty);
            Assert.IsTrue(panel.IsShowing, "the screen stays open until the player closes it");
        }

        [UnityTest]
        public IEnumerator TakingTheSword_ShowsTheUnlockBanner()
        {
            var panel = SpawnHud();
            var chest = SpawnChest(Vector2.zero);
            var player = SpawnPlayer(new Vector2(3f, 0f));
            yield return null;
            chest.Open(player);

            panel.TakeItem();

            Assert.IsNotNull(UnlockBanner.Instance);
            Assert.IsTrue(UnlockBanner.Instance.IsShowing, "picking up the sword should announce it");
        }

        [UnityTest]
        public IEnumerator TakingFromAnEmptyChest_DoesNothing()
        {
            var chest = SpawnChest(Vector2.zero);
            chest.ContainsSword = false;
            var player = SpawnPlayer(new Vector2(3f, 0f));
            yield return null;

            Assert.IsTrue(chest.IsEmpty);
            Assert.IsFalse(chest.TakeItem(player), "there's nothing to take");
            Assert.IsFalse(player.GetComponent<PlayerSword>().HasSword);
        }

        [UnityTest]
        public IEnumerator ArmorChest_PutsTheArmorOn_WhenTaken()
        {
            var panel = SpawnHud();
            var chest = SpawnChest(Vector2.zero);
            chest.ContainsSword = false;
            chest.Armor = ArmorTier.Leather;
            var player = SpawnPlayer(new Vector2(3f, 0f));
            yield return null;
            chest.Open(player);

            panel.TakeItem();

            Assert.IsTrue(player.GetComponent<PlayerArmor>().HasArmor);
            Assert.IsTrue(chest.IsEmpty);
        }

        // ---------- closing ----------

        [UnityTest]
        public IEnumerator ClosingTheChestScreen_GivesTheControlsBack()
        {
            var panel = SpawnHud();
            var chest = SpawnChest(Vector2.zero);
            var player = SpawnPlayer(new Vector2(3f, 0f));
            yield return null;
            chest.Open(player);
            panel.TakeItem();

            panel.Hide(); // what Esc does

            Assert.IsFalse(panel.IsShowing);
            Assert.IsTrue(player.InputEnabled, "the player should be able to move again");
            Assert.IsTrue(player.GetComponent<PlayerSword>().HasSword, "closing the screen keeps the sword");
        }

        // ---------- fixtures ----------

        private ChestPanel SpawnHud()
        {
            var go = new GameObject("TestHud", typeof(RectTransform), typeof(Canvas));
            spawned.Add(go);
            return go.AddComponent<ChestPanel>();
        }

        private Chest SpawnChest(Vector2 position)
        {
            var go = new GameObject("TestChest");
            go.transform.position = position;
            go.AddComponent<BoxCollider2D>().size = Vector2.one;
            spawned.Add(go);
            return go.AddComponent<Chest>();
        }

        private PlayerController SpawnPlayer(Vector2 position)
        {
            var go = new GameObject("TestPlayer");
            go.transform.position = position;
            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            go.AddComponent<CircleCollider2D>().radius = 0.4f;
            go.AddComponent<DungeonCrawler.Combat.Health>();

            // the controller complains that it has no input asset. movement isn't under test here
            LogAssert.Expect(LogType.Error, new Regex("PlayerController needs the InputSystem_Actions"));
            var player = go.AddComponent<PlayerController>(); // adds PlayerSword and PlayerArmor itself
            spawned.Add(go);
            return player;
        }
    }
}

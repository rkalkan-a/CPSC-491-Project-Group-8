using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DungeonCrawler.Player;
using DungeonCrawler.UI;

namespace DungeonCrawler.Tests
{
    // Play Mode coverage for the inventory screen and the chest screen sharing one panel.
    //
    // These are state checks: closed -> open -> closed, that only one screen is ever open,
    // and that the player can't move while a screen is up and can again once it closes.
    public class InventoryScreenTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned)
                if (go != null) Object.Destroy(go);
            spawned.Clear();
        }

        [UnityTest]
        public IEnumerator Screens_StartClosed()
        {
            var panel = SpawnHud();
            yield return null;

            Assert.IsFalse(panel.IsShowing);
        }

        [UnityTest]
        public IEnumerator ShowInventory_OpensTheScreen_AndStopsThePlayerMoving()
        {
            var panel = SpawnHud();
            var player = SpawnPlayer();
            yield return null;

            panel.ShowInventory(player);

            Assert.IsTrue(panel.IsShowing);
            Assert.IsFalse(player.InputEnabled, "the player shouldn't move around behind the inventory");
        }

        [UnityTest]
        public IEnumerator Hide_ClosesTheScreen_AndLetsThePlayerMoveAgain()
        {
            var panel = SpawnHud();
            var player = SpawnPlayer();
            yield return null;
            panel.ShowInventory(player);

            panel.Hide();

            Assert.IsFalse(panel.IsShowing);
            Assert.IsTrue(player.InputEnabled);
        }

        [UnityTest]
        public IEnumerator OpeningInventory_WhileChestIsOpen_LeavesOnlyOneScreenOpen()
        {
            var panel = SpawnHud();
            var player = SpawnPlayer();
            yield return null;

            panel.Show(null, player);
            panel.ShowInventory(player);

            Assert.IsTrue(panel.transform.Find("InventoryScreen").gameObject.activeSelf);
            Assert.IsFalse(panel.transform.Find("ChestScreen").gameObject.activeSelf);
            Assert.IsFalse(player.InputEnabled, "input should stay off while a screen is still open");
        }

        [UnityTest]
        public IEnumerator Hud_ShowsTheControlsHint_WithAnInventoryButton()
        {
            var panel = SpawnHud();
            yield return null;

            var hint = panel.transform.Find("ControlsHint");
            Assert.IsNotNull(hint, "the controls hint should be added to the hud");
            Assert.IsNotNull(hint.Find("InventoryButton"), "the hint should include the inventory button");
        }

        [UnityTest]
        public IEnumerator PuttingOnArmor_ShowsTheUnlockBanner()
        {
            SpawnHud();
            var player = SpawnPlayer();
            yield return null;

            Assert.IsFalse(UnlockBanner.Instance.IsShowing, "the banner shouldn't be up before anything is unlocked");
            player.GetComponent<PlayerArmor>().Equip();

            Assert.IsTrue(UnlockBanner.Instance.IsShowing);
            StringAssert.Contains("LEATHER ARMOR", UnlockBanner.Instance.Title);
        }

        // ---------- fixtures ----------

        private ChestPanel SpawnHud()
        {
            var go = new GameObject("TestHud", typeof(RectTransform), typeof(Canvas));
            spawned.Add(go);
            return go.AddComponent<ChestPanel>();
        }

        private PlayerController SpawnPlayer()
        {
            var go = new GameObject("TestPlayer");
            go.AddComponent<Rigidbody2D>().gravityScale = 0f;

            // the controller complains that it has no input asset. movement isn't under test here
            LogAssert.Expect(LogType.Error, new Regex("PlayerController needs the InputSystem_Actions"));
            var player = go.AddComponent<PlayerController>();
            spawned.Add(go);
            return player;
        }
    }
}

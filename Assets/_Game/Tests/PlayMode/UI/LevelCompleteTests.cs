using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using DungeonCrawler.Rooms;
using DungeonCrawler.UI;

namespace DungeonCrawler.Tests
{
    // Play Mode coverage for the level complete screen at the last level's exit.
    //
    // The exit is a state change (playing -> level complete), so these check it happens when it should, that it
    // doesn't happen from an ordinary unlinked door, and that the game is properly paused while the screen is up.
    // The time format is a small boundary check on the minute rollover.
    public class LevelCompleteTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            RunStats.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            if (LevelCompleteScreen.Instance != null) Object.Destroy(LevelCompleteScreen.Instance.gameObject);
            foreach (var go in spawned)
                if (go != null) Object.Destroy(go);
            spawned.Clear();
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator WalkingIntoTheExit_ShowsLevelComplete_AndPausesTheGame()
        {
            var player = SpawnPlayer(new Vector2(0f, 0f));
            SpawnDoor(new Vector2(0f, 0f), isExit: true);
            // wait on real time, not physics steps: once the screen pauses the game there are no more physics steps
            // to wait for, and plain frames can run faster than one physics step
            float giveUp = Time.realtimeSinceStartup + 1f;
            while (!LevelCompleteScreen.IsShowing && Time.realtimeSinceStartup < giveUp) yield return null;

            Assert.IsTrue(LevelCompleteScreen.IsShowing, "touching the exit should show the level complete screen");
            Assert.IsFalse(player.InputEnabled, "the player shouldn't be able to move behind the screen");
            Assert.AreEqual(0f, Time.timeScale, "enemies should stop while the screen is up");
        }

        [UnityTest]
        public IEnumerator AnUnlinkedDoor_ThatIsntTheExit_DoesNothing()
        {
            var player = SpawnPlayer(new Vector2(0f, 0f));
            SpawnDoor(new Vector2(0f, 0f), isExit: false);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(LevelCompleteScreen.IsShowing);
            Assert.IsTrue(player.InputEnabled);
        }

        [UnityTest]
        public IEnumerator Screen_ShowsEnemiesDefeated_AndHasBothButtons()
        {
            var player = SpawnPlayer(new Vector2(0f, 0f));
            RunStats.EnemyDefeated();
            RunStats.EnemyDefeated();
            RunStats.EnemyDefeated();
            yield return null;

            LevelCompleteScreen.Show(player);

            StringAssert.Contains("3", LevelCompleteScreen.Instance.EnemiesText);
            StringAssert.StartsWith("Time", LevelCompleteScreen.Instance.TimeText);
            Assert.IsNotNull(LevelCompleteScreen.Instance.PlayAgainButton);
            Assert.IsNotNull(LevelCompleteScreen.Instance.MainMenuButton);
        }

        [UnityTest]
        public IEnumerator Close_GivesControlBack_AndUnpauses()
        {
            var player = SpawnPlayer(new Vector2(0f, 0f));
            yield return null;
            LevelCompleteScreen.Show(player);

            LevelCompleteScreen.Instance.Close();

            Assert.IsFalse(LevelCompleteScreen.IsShowing);
            Assert.IsTrue(player.InputEnabled);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void FormatTime_RollsOverIntoMinutes()
        {
            Assert.AreEqual("0:00", RunStats.FormatTime(0f));
            Assert.AreEqual("0:59", RunStats.FormatTime(59.9f));
            Assert.AreEqual("1:00", RunStats.FormatTime(60f));
            Assert.AreEqual("3:42", RunStats.FormatTime(222f));
            Assert.AreEqual("0:00", RunStats.FormatTime(-5f), "a negative time should never show");
        }

        // ---------- fixtures ----------

        private PlayerController SpawnPlayer(Vector2 position)
        {
            var go = new GameObject("TestPlayer");
            go.transform.position = position;
            go.AddComponent<Rigidbody2D>().gravityScale = 0f;
            go.AddComponent<CircleCollider2D>().radius = 0.4f;
            go.AddComponent<Combat.Health>();

            // the controller complains that it has no input asset. movement isn't under test here
            LogAssert.Expect(LogType.Error, new Regex("PlayerController needs the InputSystem_Actions"));
            var player = go.AddComponent<PlayerController>();
            spawned.Add(go);
            return player;
        }

        private Door SpawnDoor(Vector2 position, bool isExit)
        {
            var go = new GameObject(isExit ? "TestExit" : "TestDoor");
            go.transform.position = position;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.5f, 1f);
            var door = go.AddComponent<Door>();
            door.IsLevelExit = isExit;
            spawned.Add(go);
            return door;
        }
    }
}

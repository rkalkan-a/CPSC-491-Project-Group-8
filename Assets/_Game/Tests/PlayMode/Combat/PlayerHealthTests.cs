#if UNITY_EDITOR
using System.Collections;
using DungeonCrawler.Combat;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DungeonCrawler.Tests
{
    // SCRUM-105: exercise the real player prefab, including its configured i-frames.
    // Run-reset behavior belongs to Sprint 4; death checks stop before the respawn delay.
    public class PlayerHealthTests
    {
        private Scene testScene;
        private Health health;
        private PlayerController player;
        private float previousTimeScale;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 1f;

            // Unloading the runner's scene also stops the coroutine executing this setup.
            var runner = GameObject.Find("Code-based tests runner");
            var runnerScene = runner != null ? runner.scene : default;
            testScene = SceneManager.CreateScene("PlayerHealthTests_" + System.Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(testScene);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var other = SceneManager.GetSceneAt(i);
                if (other != testScene && other != runnerScene && other.isLoaded)
                    yield return SceneManager.UnloadSceneAsync(other);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Player.prefab");
            Assert.IsNotNull(prefab, "The gameplay player prefab must exist.");
            var instance = Object.Instantiate(prefab);
            player = instance.GetComponent<PlayerController>();
            health = instance.GetComponent<Health>();
            Assert.IsNotNull(player);
            Assert.IsNotNull(health);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Also remove effects spawned by the prefab and cancel the manager's reset coroutine.
            if (testScene.IsValid() && testScene.isLoaded)
                foreach (var root in testScene.GetRootGameObjects())
                    Object.Destroy(root);
            yield return null;
            Time.timeScale = previousTimeScale;
        }

        [Test]
        public void Player_StartsAtFullHealth_AndAlive()
        {
            Assert.Greater(health.Max, 1, "Boundary tests require at least two health points.");
            Assert.AreEqual(health.Max, health.Current);
            Assert.IsFalse(health.IsDead);
        }

        [TestCase(1)]
        [TestCase(3)]
        public void NormalDamage_ReducesPlayerHealth_WithoutDeath(int damage)
        {
            int before = health.Current;
            Assert.Greater(before, damage);
            int deaths = 0;
            int changes = 0;
            health.Died += () => deaths++;
            health.Changed += (current, max) =>
            {
                changes++;
                Assert.AreEqual(before - damage, current);
                Assert.AreEqual(health.Max, max);
            };

            health.TakeDamage(damage);

            Assert.AreEqual(before - damage, health.Current);
            Assert.IsFalse(health.IsDead);
            Assert.AreEqual(0, deaths);
            Assert.AreEqual(1, changes, "Damage must notify the health HUD once.");
        }

        [Test]
        public void DamageLeavingOneHealth_DoesNotKillPlayer()
        {
            int deaths = 0;
            health.Died += () => deaths++;

            health.TakeDamage(health.Current - 1);

            Assert.AreEqual(1, health.Current);
            Assert.IsFalse(health.IsDead);
            Assert.AreEqual(0, deaths);
        }

        [TestCase(0)]
        [TestCase(5)]
        public void LethalDamage_ClampsAtZero_AndRaisesDeathOnce(int overkill)
        {
            int deaths = 0;
            health.Died += () =>
            {
                deaths++;
                Assert.AreEqual(0, health.Current);
                Assert.IsTrue(health.IsDead, "Listeners must observe the dead state.");
            };

            health.TakeDamage(health.Current + overkill);
            health.TakeDamage(1);

            Assert.AreEqual(0, health.Current);
            Assert.IsTrue(health.IsDead);
            Assert.AreEqual(1, deaths, "Repeated damage must not repeat death notifications.");
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void NonPositiveDamage_DoesNotChangeHealth_OrRaiseEvents(int damage)
        {
            int before = health.Current;
            int changes = 0;
            int deaths = 0;
            health.Changed += (_, __) => changes++;
            health.Died += () => deaths++;

            health.TakeDamage(damage);

            Assert.AreEqual(before, health.Current);
            Assert.IsFalse(health.IsDead);
            Assert.AreEqual(0, changes);
            Assert.AreEqual(0, deaths);
        }

        [Test]
        public void HealingAfterDeath_DoesNotRevivePlayer()
        {
            health.TakeDamage(health.Current);

            health.Heal(health.Max);

            Assert.AreEqual(0, health.Current);
            Assert.IsTrue(health.IsDead);
        }

        [UnityTest]
        public IEnumerator PlayerInvulnerability_BlocksImmediateDamage_ThenAllowsDamage()
        {
            int before = health.Current;
            health.TakeDamage(1);
            Assert.IsTrue(health.IsInvulnerable, "The player prefab should provide hit protection.");

            health.TakeDamage(1);
            Assert.AreEqual(before - 1, health.Current);

            float deadline = Time.realtimeSinceStartup + 2f;
            while (health.IsInvulnerable && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.IsFalse(health.IsInvulnerable, "Hit protection must expire.");

            health.TakeDamage(1);
            Assert.AreEqual(before - 2, health.Current);
            Assert.IsFalse(health.IsDead);
        }

        [UnityTest]
        public IEnumerator ExactLethalDamage_EndsRun_AndDisablesPlayerInput()
        {
            yield return CheckRunDeath(overkill: 0);
        }

        [UnityTest]
        public IEnumerator OverkillDamage_EndsRun_AndDisablesPlayerInput()
        {
            yield return CheckRunDeath(overkill: 5);
        }

        private IEnumerator CheckRunDeath(int overkill)
        {
            var spawn = new GameObject("StartSpawn");
            spawn.AddComponent<PlayerSpawnPoint>();
            var managerObject = new GameObject("TestGameManager");
            var manager = managerObject.AddComponent<GameManager>();
            yield return null; // GameManager.Start wires up the actual player's death listener.

            Assert.IsTrue(manager.RunActive);
            Assert.AreSame(player, manager.Player);
            Assert.AreSame(health, manager.PlayerHealth);
            Assert.IsTrue(player.InputEnabled);

            int playerDeaths = 0;
            System.Action onPlayerDied = () => playerDeaths++;
            GameEvents.PlayerDied += onPlayerDied;
            try
            {
                // A nonzero velocity proves death actively stops the player.
                player.Body.linearVelocity = Vector2.right * 3f;
                health.TakeDamage(health.Current + overkill);
                health.TakeDamage(1);

                Assert.AreEqual(0, health.Current);
                Assert.IsTrue(health.IsDead);
                Assert.IsFalse(manager.RunActive);
                Assert.IsFalse(player.InputEnabled);
                Assert.AreEqual(Vector2.zero, player.Body.linearVelocity);
                Assert.AreEqual(1, playerDeaths, "The run must announce player death only once.");
            }
            finally
            {
                GameEvents.PlayerDied -= onPlayerDied;
            }
        }
    }
}
#endif

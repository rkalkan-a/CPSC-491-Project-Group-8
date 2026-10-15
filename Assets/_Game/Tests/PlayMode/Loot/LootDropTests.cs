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
    // Play Mode coverage for enemies dropping loot and the player picking it up.
    //
    // The drop is random in the game, so the tests pass the roll in directly. That makes the
    // drop chance testable at its boundaries (0, 1, and a roll exactly on the chance).
    public class LootDropTests
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

            foreach (var pickup in Object.FindObjectsByType<HealthPickup>(FindObjectsSortMode.None))
                Object.Destroy(pickup.gameObject);
        }

        // ---------- drop chance (boundary values) ----------

        [UnityTest]
        public IEnumerator TryDrop_RollUnderTheChance_SpawnsAHeartWhereTheEnemyIs()
        {
            var dropper = SpawnEnemy(new Vector2(3f, 2f));
            dropper.DropChance = 0.35f;
            yield return null;

            var heart = dropper.TryDrop(0.2f);

            Assert.IsNotNull(heart, "a roll under the drop chance should drop a heart");
            Assert.IsNotNull(heart.GetComponent<HealthPickup>());
            // the heart bobs up and down a little (PickupBob), so allow for that. it used to fail now and then
            // when the check landed mid-bob
            Assert.AreEqual(3f, heart.transform.position.x, 0.01f);
            Assert.AreEqual(2f, heart.transform.position.y, 0.1f);
        }

        [UnityTest]
        public IEnumerator TryDrop_RollExactlyOnTheChance_DropsNothing()
        {
            var dropper = SpawnEnemy(Vector2.zero);
            dropper.DropChance = 0.35f;
            yield return null;

            Assert.IsNull(dropper.TryDrop(0.35f));
        }

        [UnityTest]
        public IEnumerator TryDrop_ChanceOfZero_NeverDrops()
        {
            var dropper = SpawnEnemy(Vector2.zero);
            dropper.DropChance = 0f;
            yield return null;

            Assert.IsNull(dropper.TryDrop(0f));
        }

        [UnityTest]
        public IEnumerator TryDrop_ChanceOfOne_AlwaysDrops()
        {
            var dropper = SpawnEnemy(Vector2.zero);
            dropper.DropChance = 1f;
            yield return null;

            Assert.IsNotNull(dropper.TryDrop(0.999f));
        }

        // ---------- dropping on death ----------

        [UnityTest]
        public IEnumerator EnemyDying_WithChanceOfOne_LeavesAHeartBehind()
        {
            var dropper = SpawnEnemy(new Vector2(1f, 1f));
            dropper.DropChance = 1f;
            yield return null;

            dropper.GetComponent<Health>().TakeDamage(999);
            yield return null;

            Assert.AreEqual(1, Object.FindObjectsByType<HealthPickup>(FindObjectsSortMode.None).Length);
        }

        [UnityTest]
        public IEnumerator EnemyDying_WithChanceOfZero_LeavesNothing()
        {
            var dropper = SpawnEnemy(new Vector2(1f, 1f));
            dropper.DropChance = 0f;
            yield return null;

            dropper.GetComponent<Health>().TakeDamage(999);
            yield return null;

            Assert.AreEqual(0, Object.FindObjectsByType<HealthPickup>(FindObjectsSortMode.None).Length);
        }

        // ---------- picking the heart up ----------

        [UnityTest]
        public IEnumerator DroppedHeart_HealsAHurtPlayer_AndDisappears()
        {
            var playerHealth = SpawnPlayer(Vector2.zero);
            yield return new WaitForFixedUpdate();
            playerHealth.TakeDamage(3);
            int hurt = playerHealth.Current;

            var heart = LootDropper.SpawnHeart(Vector2.zero, null);
            yield return WaitForPhysics(5);

            Assert.Greater(playerHealth.Current, hurt, "picking up the heart should heal the player");
            Assert.IsTrue(heart == null, "the heart should be gone once picked up");
        }

        [UnityTest]
        public IEnumerator DroppedHeart_IsLeftOnTheFloor_WhenPlayerIsAtFullHealth()
        {
            var playerHealth = SpawnPlayer(Vector2.zero);
            yield return new WaitForFixedUpdate();

            var heart = LootDropper.SpawnHeart(Vector2.zero, null);
            yield return WaitForPhysics(5);

            Assert.AreEqual(playerHealth.Max, playerHealth.Current);
            Assert.IsTrue(heart != null, "a heart shouldn't be used up when the player isn't hurt");
        }

        // ---------- fixtures ----------

        // built the way EnemyBuilder builds real enemies. Enemy adds the LootDropper itself
        private LootDropper SpawnEnemy(Vector2 position)
        {
            var go = new GameObject("TestEnemy");
            go.transform.position = position;
            go.AddComponent<Rigidbody2D>().gravityScale = 0f;
            go.AddComponent<CircleCollider2D>().radius = 0.4f;
            go.AddComponent<Health>();
            go.AddComponent<Enemy>();
            spawned.Add(go);
            return go.GetComponent<LootDropper>();
        }

        private Health SpawnPlayer(Vector2 position)
        {
            var go = new GameObject("TestPlayer");
            go.transform.position = position;
            go.AddComponent<Rigidbody2D>().gravityScale = 0f;
            go.AddComponent<CircleCollider2D>().radius = 0.4f;
            var health = go.AddComponent<Health>();

            // the controller complains that it has no input asset. movement isn't under test here
            LogAssert.Expect(LogType.Error, new Regex("PlayerController needs the InputSystem_Actions"));
            go.AddComponent<PlayerController>();

            spawned.Add(go);
            return health;
        }

        private static IEnumerator WaitForPhysics(int steps)
        {
            for (int i = 0; i < steps; i++) yield return new WaitForFixedUpdate();
        }
    }
}

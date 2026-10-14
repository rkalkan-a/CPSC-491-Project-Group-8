using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using DungeonCrawler.Combat;

namespace DungeonCrawler.Tests
{
    // Play Mode coverage for the ranged projectile system.
    //
    // A shot is supposed to end in exactly one of three ways: it hits something with Health,
    // it hits a solid obstacle, or it runs out of range. These tests cover all three plus the
    // damage amount itself, so a regression in combat shows up in CI instead of in playtesting.
    //
    // The fixtures deliberately mirror how the real game builds things, because the physics
    // behaviour depends on it:
    //   - enemies (EnemyBuilder)      -> Dynamic Rigidbody2D + CircleCollider2D + Health
    //   - walls   (DungeonBootstrapper) -> BoxCollider2D only, no Rigidbody2D (a static collider)
    public class ProjectileTests
    {
        private const float Speed = 10f;
        private const float LongRange = 50f;   // long enough that range never ends these tests
        private const float Timeout = 2f;      // seconds of simulated physics before we give up

        private readonly List<GameObject> spawned = new List<GameObject>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // PauseMenu zeroes the timescale in normal play; make sure physics actually runs here
            Time.timeScale = 1f;

            // tests that run before these (menus, music) leave Level_01 loaded, and its walls sit right
            // where these shots are fired. start every test in an empty scene of its own
            Scene empty = SceneManager.CreateScene("ProjectileTests_" + System.Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene other = SceneManager.GetSceneAt(i);
                if (other != empty && other.isLoaded) yield return SceneManager.UnloadSceneAsync(other);
            }
            yield return new WaitForFixedUpdate(); // let physics drop the unloaded colliders
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned)
                if (go != null) Object.Destroy(go);
            spawned.Clear();
        }

        // ---------- damage ----------

        [UnityTest]
        public IEnumerator Projectile_ReducesEnemyHealth_ByTheDamageAmount()
        {
            var enemy = SpawnEnemy(new Vector2(2f, 0f));
            var health = enemy.GetComponent<Health>();
            int before = health.Current;
            const int damage = 2;

            var shot = SpawnProjectile(Vector2.zero, Vector2.right, damage, LongRange);
            yield return RunUntilDestroyed(shot, Timeout);

            Assert.AreEqual(before - damage, health.Current,
                "enemy health should drop by exactly the projectile's damage");
        }

        [UnityTest]
        public IEnumerator Projectile_WithLethalDamage_KillsEnemy_AndHealthStopsAtZero()
        {
            var enemy = SpawnEnemy(new Vector2(2f, 0f));
            var health = enemy.GetComponent<Health>();

            var shot = SpawnProjectile(Vector2.zero, Vector2.right, health.Current + 5, LongRange);
            yield return RunUntilDestroyed(shot, Timeout);

            Assert.AreEqual(0, health.Current, "health should clamp at zero, never go negative");
            Assert.IsTrue(health.IsDead, "enemy should be marked dead after taking lethal damage");
        }

        [UnityTest]
        public IEnumerator Projectile_DoesNotDamage_TheThingThatFiredIt()
        {
            var shooter = SpawnEnemy(new Vector2(2f, 0f)); // stand-in for the player
            var health = shooter.GetComponent<Health>();
            int before = health.Current;

            var shot = SpawnProjectile(Vector2.zero, Vector2.right, 3, LongRange, owner: shooter);
            yield return RunUntilDestroyed(shot, 0.5f);

            Assert.AreEqual(before, health.Current, "a projectile must never damage its own owner");
            AssertStillAlive(shot, "a projectile should pass straight through its owner, not die on it");
        }

        // ---------- the three ways a shot ends ----------

        [UnityTest]
        public IEnumerator Projectile_IsDestroyed_AfterHittingAnEnemy()
        {
            SpawnEnemy(new Vector2(2f, 0f));

            var shot = SpawnProjectile(Vector2.zero, Vector2.right, 1, LongRange);
            yield return RunUntilDestroyed(shot, Timeout);

            AssertDestroyed(shot, "projectile should be destroyed by the enemy it hits");
        }

        [UnityTest]
        public IEnumerator Projectile_IsDestroyed_AfterHittingAWall()
        {
            SpawnWall(new Vector2(2f, 0f));

            var shot = SpawnProjectile(Vector2.zero, Vector2.right, 1, LongRange);
            yield return RunUntilDestroyed(shot, Timeout);

            AssertDestroyed(shot, "projectile should be stopped by a solid wall, not fly through it");
        }

        [UnityTest]
        public IEnumerator Projectile_IsDestroyed_WhenItReachesMaximumRange()
        {
            var shot = SpawnProjectile(Vector2.zero, Vector2.right, 1, range: 3f);
            yield return RunUntilDestroyed(shot, Timeout);

            AssertDestroyed(shot, "projectile should destroy itself once it runs out of range");
        }

        [UnityTest]
        public IEnumerator Projectile_TravelsRoughlyItsMaxRange_BeforeExpiring()
        {
            const float range = 3f;
            var shot = SpawnProjectile(Vector2.zero, Vector2.right, 1, range);

            // remember the last position we saw it at before it removed itself
            Vector2 lastSeen = shot.transform.position;
            float elapsed = 0f;
            while (shot != null && elapsed < Timeout)
            {
                lastSeen = shot.transform.position;
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }

            AssertDestroyed(shot, "projectile should have expired within the timeout");

            // allow a couple of physics steps of slack either side
            float tolerance = Speed * Time.fixedDeltaTime * 2f + 0.1f;
            Assert.AreEqual(range, lastSeen.magnitude, tolerance,
                $"projectile should stop about {range} units out, stopped at {lastSeen.magnitude}");
        }

        [UnityTest]
        public IEnumerator Projectile_PassesThrough_NonSolidTriggerVolumes()
        {
            SpawnTriggerVolume(new Vector2(2f, 0f)); // e.g. a pickup or a room trigger

            var shot = SpawnProjectile(Vector2.zero, Vector2.right, 1, LongRange);
            yield return RunUntilDestroyed(shot, 0.5f);

            AssertStillAlive(shot, "a non-solid trigger should not stop a projectile");
        }

        // ---------- fixtures ----------

        private GameObject Track(GameObject go)
        {
            spawned.Add(go);
            return go;
        }

        private GameObject SpawnProjectile(Vector2 position, Vector2 direction, int damage,
                                           float range, GameObject owner = null)
        {
            var go = Track(new GameObject("Projectile"));
            go.transform.position = position;

            // same shape PlayerAttack.Fire() builds at runtime
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.25f;
            go.AddComponent<Rigidbody2D>();

            var projectile = go.AddComponent<Projectile>();
            projectile.Launch(direction, Speed, damage, range, 0f, owner);
            return go;
        }

        // matches EnemyBuilder: dynamic body so it can be pushed around, plus Health
        private GameObject SpawnEnemy(Vector2 position)
        {
            var go = Track(new GameObject("Enemy"));
            go.transform.position = position;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;

            go.AddComponent<Health>();
            return go;
        }

        // matches DungeonBootstrapper's solid walls: a collider with no rigidbody at all
        private GameObject SpawnWall(Vector2 position)
        {
            var go = Track(new GameObject("Wall"));
            go.transform.position = position;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 4f);
            return go;
        }

        private GameObject SpawnTriggerVolume(Vector2 position)
        {
            var go = Track(new GameObject("TriggerVolume"));
            go.transform.position = position;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 4f);
            col.isTrigger = true;
            return go;
        }

        // NUnit's Assert.IsNull compares real object references, but a destroyed
        // UnityEngine.Object is not a null reference - Unity only reports it as null through
        // its own overloaded == operator. These helpers go through that operator, so the
        // "was it destroyed?" check is actually meaningful.
        private static void AssertDestroyed(GameObject go, string message)
        {
            Assert.IsTrue(go == null, message);
        }

        private static void AssertStillAlive(GameObject go, string message)
        {
            Assert.IsFalse(go == null, message);
        }

        private static IEnumerator RunUntilDestroyed(GameObject go, float timeout)
        {
            float elapsed = 0f;
            while (go != null && elapsed < timeout)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }
        }
    }
}

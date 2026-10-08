using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DungeonCrawler.Combat;
using DungeonCrawler.Player;

namespace DungeonCrawler.Tests
{
    // Play Mode coverage for the melee swing.
    //
    // The arc check is tested on its own with boundary values (just inside and just outside the
    // range and the arc), then the swing is tested against real colliders: what it should hit,
    // what it should miss, the cooldown, and walls.
    public class MeleeTests
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

        // ---------- arc check (boundary values) ----------

        [Test]
        public void InArc_TargetStraightAhead_InRange_IsHit()
        {
            Assert.IsTrue(PlayerMelee.InArc(Vector2.zero, Vector2.right, new Vector2(1f, 0f), 1.6f, 120f));
        }

        [Test]
        public void InArc_TargetExactlyAtMaxRange_IsHit()
        {
            Assert.IsTrue(PlayerMelee.InArc(Vector2.zero, Vector2.right, new Vector2(1.6f, 0f), 1.6f, 120f));
        }

        [Test]
        public void InArc_TargetJustPastMaxRange_IsMissed()
        {
            Assert.IsFalse(PlayerMelee.InArc(Vector2.zero, Vector2.right, new Vector2(1.65f, 0f), 1.6f, 120f));
        }

        [Test]
        public void InArc_TargetJustInsideArcEdge_IsHit()
        {
            // arc is 120 degrees wide, so 60 either side of the aim direction
            Assert.IsTrue(PlayerMelee.InArc(Vector2.zero, Vector2.right, AtAngle(59f, 1f), 1.6f, 120f));
        }

        [Test]
        public void InArc_TargetJustOutsideArcEdge_IsMissed()
        {
            Assert.IsFalse(PlayerMelee.InArc(Vector2.zero, Vector2.right, AtAngle(61f, 1f), 1.6f, 120f));
        }

        [Test]
        public void InArc_TargetBehindThePlayer_IsMissed()
        {
            Assert.IsFalse(PlayerMelee.InArc(Vector2.zero, Vector2.right, new Vector2(-1f, 0f), 1.6f, 120f));
        }

        // ---------- what a swing hits ----------

        [UnityTest]
        public IEnumerator Swing_DamagesEnemyInFront_ByTheMeleeDamage()
        {
            var melee = SpawnPlayer(Vector2.zero);
            var enemy = SpawnTarget(new Vector2(1.2f, 0f));
            yield return new WaitForFixedUpdate();
            int before = enemy.Current;

            melee.TrySwing(Vector2.right);

            Assert.AreEqual(before - melee.Damage, enemy.Current);
        }

        [UnityTest]
        public IEnumerator Swing_DoesNotHit_EnemyBehindThePlayer()
        {
            var melee = SpawnPlayer(Vector2.zero);
            var enemy = SpawnTarget(new Vector2(-1.2f, 0f));
            yield return new WaitForFixedUpdate();
            int before = enemy.Current;

            melee.TrySwing(Vector2.right);

            Assert.AreEqual(before, enemy.Current);
        }

        [UnityTest]
        public IEnumerator Swing_DoesNotHit_EnemyOutOfRange()
        {
            var melee = SpawnPlayer(Vector2.zero);
            var enemy = SpawnTarget(new Vector2(3f, 0f));
            yield return new WaitForFixedUpdate();
            int before = enemy.Current;

            melee.TrySwing(Vector2.right);

            Assert.AreEqual(before, enemy.Current);
        }

        [UnityTest]
        public IEnumerator Swing_HitsEveryEnemyInsideTheArc()
        {
            var melee = SpawnPlayer(Vector2.zero);
            var upper = SpawnTarget(new Vector2(1.1f, 0.5f));
            var lower = SpawnTarget(new Vector2(1.1f, -0.5f));
            yield return new WaitForFixedUpdate();
            int upperBefore = upper.Current, lowerBefore = lower.Current;

            melee.TrySwing(Vector2.right);

            Assert.AreEqual(upperBefore - melee.Damage, upper.Current, "upper enemy should be hit");
            Assert.AreEqual(lowerBefore - melee.Damage, lower.Current, "lower enemy should be hit");
        }

        [UnityTest]
        public IEnumerator Swing_DoesNotDamage_ThePlayer()
        {
            var melee = SpawnPlayer(Vector2.zero);
            var playerHealth = melee.GetComponent<Health>();
            yield return new WaitForFixedUpdate();
            int before = playerHealth.Current;

            melee.TrySwing(Vector2.right);

            Assert.AreEqual(before, playerHealth.Current);
        }

        [UnityTest]
        public IEnumerator Swing_DoesNotHit_ThroughAWall()
        {
            var melee = SpawnPlayer(Vector2.zero);
            SpawnWall(new Vector2(0.75f, 0f), new Vector2(0.2f, 3f));
            var enemy = SpawnTarget(new Vector2(1.4f, 0f));
            yield return new WaitForFixedUpdate();
            int before = enemy.Current;

            melee.TrySwing(Vector2.right);

            Assert.AreEqual(before, enemy.Current, "a wall between the player and the enemy should stop the hit");
        }

        // ---------- cooldown ----------

        [UnityTest]
        public IEnumerator Swing_IsBlocked_WhileOnCooldown()
        {
            var melee = SpawnPlayer(Vector2.zero);
            var enemy = SpawnTarget(new Vector2(1.2f, 0f));
            yield return new WaitForFixedUpdate();
            int before = enemy.Current;

            Assert.IsTrue(melee.TrySwing(Vector2.right), "first swing should go through");
            Assert.IsFalse(melee.TrySwing(Vector2.right), "second swing right away should be blocked");

            Assert.AreEqual(before - melee.Damage, enemy.Current, "enemy should only have been hit once");
        }

        [UnityTest]
        public IEnumerator Swing_WorksAgain_OnceCooldownEnds()
        {
            var melee = SpawnPlayer(Vector2.zero);
            yield return new WaitForFixedUpdate();

            Assert.IsTrue(melee.TrySwing(Vector2.right));
            yield return new WaitForSeconds(melee.Cooldown + 0.1f);

            Assert.IsFalse(melee.IsOnCooldown);
            Assert.IsTrue(melee.TrySwing(Vector2.right), "swing should be allowed after the cooldown");
        }

        // ---------- fixtures ----------

        private PlayerMelee SpawnPlayer(Vector2 position)
        {
            var go = new GameObject("TestPlayer");
            go.transform.position = position;
            go.AddComponent<Rigidbody2D>().gravityScale = 0f;
            go.AddComponent<CircleCollider2D>().radius = 0.4f;
            go.AddComponent<Health>();

            // the controller complains that it has no input asset. movement isn't under test here
            LogAssert.Expect(LogType.Error, new Regex("PlayerController needs the InputSystem_Actions"));
            go.AddComponent<PlayerController>(); // adds PlayerMelee itself

            spawned.Add(go);
            return go.GetComponent<PlayerMelee>();
        }

        // something with Health and a collider, built like an enemy but with no AI so it stays put
        private Health SpawnTarget(Vector2 position)
        {
            var go = new GameObject("TestTarget");
            go.transform.position = position;
            go.AddComponent<Rigidbody2D>().gravityScale = 0f;
            go.AddComponent<CircleCollider2D>().radius = 0.4f;
            spawned.Add(go);
            return go.AddComponent<Health>();
        }

        private void SpawnWall(Vector2 position, Vector2 size)
        {
            var go = new GameObject("TestWall");
            go.transform.position = position;
            go.AddComponent<BoxCollider2D>().size = size;
            spawned.Add(go);
        }

        private static Vector2 AtAngle(float degrees, float distance)
        {
            float rad = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * distance;
        }
    }
}

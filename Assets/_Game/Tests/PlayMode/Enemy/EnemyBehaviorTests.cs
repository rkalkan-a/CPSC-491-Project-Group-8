
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DungeonCrawler.Combat;
using DungeonCrawler.Enemies;

namespace DungeonCrawler.Tests
{
    // Isolated Play Mode tests: no scene, prefabs, or input asset required.
    public class EnemyBehaviorTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();
        private float oldTimeScale;

        [SetUp]
        public void SetUp()
        {
            oldTimeScale = Time.timeScale;
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned)
                if (go != null) Object.DestroyImmediate(go);

            spawned.Clear();
            Time.timeScale = oldTimeScale;
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            var field = typeof(Enemy).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            Assert.IsNotNull(field, "Enemy.cs no longer has field: " + fieldName);
            field.SetValue(instance, value);
        }

        private Transform MakeTarget(Vector2 position)
        {
            var go = new GameObject("EnemyTestTarget");
            go.transform.position = position;
            spawned.Add(go);
            return go.transform;
        }

        private Enemy MakeEnemy(
            Enemy.Behavior behavior,
            Vector2 position,
            Transform target = null)
        {
            var go = new GameObject("EnemyBehaviorTestEnemy");
            go.transform.position = position;

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            go.AddComponent<CircleCollider2D>();
            go.AddComponent<Health>();

            var enemy = go.AddComponent<Enemy>();

            SetField(enemy, "behavior", behavior);
            SetField(enemy, "moveSpeed", 3f);
            SetField(enemy, "aggroRange", 7f);
            SetField(enemy, "wanderSpeed", 0f);

            if (target != null)
                SetField(enemy, "target", target);

            spawned.Add(go);
            return enemy;
        }

        [UnityTest]
        public IEnumerator Chaser_DetectsPlayerWithinRange()
        {
            var target = MakeTarget(new Vector2(3f, 0f));
            var enemy = MakeEnemy(
                Enemy.Behavior.Chaser,
                Vector2.zero,
                target
            );

            yield return new WaitForFixedUpdate();

            Assert.Greater(
                enemy.GetComponent<Rigidbody2D>().linearVelocity.x,
                2f
            );
        }

        [UnityTest]
        public IEnumerator Chaser_DoesNotChaseOutsideRange()
        {
            var target = MakeTarget(new Vector2(20f, 0f));
            var enemy = MakeEnemy(
                Enemy.Behavior.Chaser,
                Vector2.zero,
                target
            );

            yield return new WaitForFixedUpdate();

            Assert.Less(
                enemy.GetComponent<Rigidbody2D>().linearVelocity.magnitude,
                0.01f
            );
        }

        [UnityTest]
        public IEnumerator Chaser_FollowsMovingPlayer()
        {
            var target = MakeTarget(new Vector2(3f, 0f));
            var enemy = MakeEnemy(
                Enemy.Behavior.Chaser,
                Vector2.zero,
                target
            );

            yield return new WaitForFixedUpdate();

            Assert.Greater(
                enemy.GetComponent<Rigidbody2D>().linearVelocity.x,
                2f
            );

            target.position = new Vector2(-3f, 0f);

            yield return new WaitForFixedUpdate();

            Assert.Less(
                enemy.GetComponent<Rigidbody2D>().linearVelocity.x,
                -2f
            );
        }

        [UnityTest]
        public IEnumerator Wanderer_MovesWhenGivenWanderDirection()
        {
            var enemy = MakeEnemy(
                Enemy.Behavior.Wanderer,
                Vector2.zero
            );

            SetField(enemy, "wanderSpeed", 1.2f);
            SetField(enemy, "wanderDir", Vector2.up);
            SetField(enemy, "wanderTimer", 10f);

            yield return new WaitForFixedUpdate();

            Assert.That(
                enemy.GetComponent<Rigidbody2D>().linearVelocity.y,
                Is.EqualTo(1.2f).Within(0.01f)
            );
        }

        [UnityTest]
        public IEnumerator Wanderer_ChangesDirectionAfterTimerExpires()
        {
            var enemy = MakeEnemy(
                Enemy.Behavior.Wanderer,
                Vector2.zero
            );

            SetField(enemy, "wanderSpeed", 1.2f);
            SetField(enemy, "wanderChangeTime", 0.1f);
            SetField(enemy, "wanderTimer", -1f);

            yield return new WaitForFixedUpdate();

            var timer = (float)typeof(Enemy)
                .GetField(
                    "wanderTimer",
                    BindingFlags.Instance | BindingFlags.NonPublic
                )
                .GetValue(enemy);

            Assert.Greater(
                timer,
                0f,
                "Wander timer should reset after a new direction is chosen"
            );
        }

        [UnityTest]
        public IEnumerator Wanderer_DoesNotChasePlayer()
        {
            var target = MakeTarget(new Vector2(3f, 0f));

            var enemy = MakeEnemy(
                Enemy.Behavior.Wanderer,
                Vector2.zero,
                target
            );

            SetField(enemy, "wanderSpeed", 1.2f);
            SetField(enemy, "wanderDir", Vector2.up);
            SetField(enemy, "wanderTimer", 10f);

            yield return new WaitForFixedUpdate();

            var velocity = enemy.GetComponent<Rigidbody2D>().linearVelocity;

            Assert.That(
                velocity.x,
                Is.EqualTo(0f).Within(0.01f)
            );

            Assert.Greater(velocity.y, 1f);
        }

        [UnityTest]
        public IEnumerator Enemy_AppliesKnockback()
        {
            var enemy = MakeEnemy(
                Enemy.Behavior.Chaser,
                Vector2.zero
            );

            enemy.ApplyKnockback(Vector2.left * 5f, 0.3f);

            yield return new WaitForFixedUpdate();

            Assert.That(
                enemy.GetComponent<Rigidbody2D>().linearVelocity.x,
                Is.EqualTo(-5f).Within(0.01f)
            );
        }

        [UnityTest]
        public IEnumerator Enemy_TakesDamage()
        {
            var enemy = MakeEnemy(
                Enemy.Behavior.Chaser,
                Vector2.zero
            );

            int before = enemy.Health.Current;

            enemy.Health.TakeDamage(2);

            yield return null;

            Assert.AreEqual(
                before - 2,
                enemy.Health.Current
            );

            Assert.IsFalse(enemy.Health.IsDead);
        }

        [UnityTest]
        public IEnumerator Enemy_DiesAtZeroHealth_DisablesColliderAndStopsMovement()
        {
            var target = MakeTarget(new Vector2(3f, 0f));

            var enemy = MakeEnemy(
                Enemy.Behavior.Chaser,
                Vector2.zero,
                target
            );

            var body = enemy.GetComponent<Rigidbody2D>();
            var collider = enemy.GetComponent<Collider2D>();

            bool eventRaised = false;

            enemy.Died += e => eventRaised = (e == enemy);

            yield return new WaitForFixedUpdate();

            Assert.Greater(body.linearVelocity.magnitude, 0f);

            enemy.Health.TakeDamage(enemy.Health.Current);

            Assert.IsTrue(
                eventRaised,
                "Enemy.Died should fire"
            );

            Assert.IsTrue(enemy.Health.IsDead);

            Assert.IsFalse(
                collider.enabled,
                "Dead enemies should not collide"
            );

            Assert.Less(
                body.linearVelocity.magnitude,
                0.01f,
                "Dead enemies should stop moving"
            );

            yield return null;

            Assert.IsTrue(
                enemy == null,
                "Enemy GameObject should be destroyed at the end of the frame"
            );
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DungeonCrawler.Combat;
using DungeonCrawler.Enemies;

namespace DungeonCrawler.Tests
{
    // Isolated Play Mode tests for enemy health and death integration.
    public class EnemyHealthTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();
        private float originalTimeScale;

        [SetUp]
        public void SetUp()
        {
            originalTimeScale = Time.timeScale;
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in spawned)
                if (obj != null) Object.DestroyImmediate(obj);
            spawned.Clear();
            Time.timeScale = originalTimeScale;
        }

        private Enemy CreateEnemy(string name = "HealthTestEnemy")
        {
            var obj = new GameObject(name);
            spawned.Add(obj);
            obj.AddComponent<Rigidbody2D>();
            obj.AddComponent<CircleCollider2D>();
            obj.AddComponent<Health>();
            return obj.AddComponent<Enemy>();
        }

        [Test]
        public void Enemy_StartsAtFullHealth()
        {
            var health = CreateEnemy().Health;
            Assert.AreEqual(health.Max, health.Current);
            Assert.IsFalse(health.IsDead);
        }

        [Test]
        public void Enemy_TakesDamageAndReportsHealthChange()
        {
            var health = CreateEnemy().Health;
            int notifications = 0;
            health.Changed += (current, max) =>
            {
                notifications++;
                Assert.AreEqual(health.Max - 2, current);
                Assert.AreEqual(health.Max, max);
            };
            health.TakeDamage(2);
            Assert.AreEqual(health.Max - 2, health.Current);
            Assert.AreEqual(1, notifications);
        }

        [Test]
        public void Enemy_NonLethalDamageDoesNotCauseDeath()
        {
            var enemy = CreateEnemy();
            int deaths = 0;
            enemy.Died += _ => deaths++;
            enemy.Health.TakeDamage(1);
            Assert.IsFalse(enemy.Health.IsDead);
            Assert.AreEqual(0, deaths);
            Assert.IsTrue(enemy.GetComponent<Collider2D>().enabled);
        }

        [Test]
        public void Enemy_ZeroOrNegativeDamageIsIgnored()
        {
            var health = CreateEnemy().Health;
            int before = health.Current;
            health.TakeDamage(0);
            health.TakeDamage(-5);
            Assert.AreEqual(before, health.Current);
            Assert.IsFalse(health.IsDead);
        }

        [Test]
        public void Enemy_LethalDamageFiresDeathOnce()
        {
            var enemy = CreateEnemy();
            int healthDeaths = 0;
            int enemyDeaths = 0;
            enemy.Health.Died += () => healthDeaths++;
            enemy.Died += deadEnemy =>
            {
                Assert.AreSame(enemy, deadEnemy);
                enemyDeaths++;
            };
            enemy.Health.TakeDamage(enemy.Health.Current);
            enemy.Health.TakeDamage(10);
            Assert.IsTrue(enemy.Health.IsDead);
            Assert.AreEqual(0, enemy.Health.Current);
            Assert.AreEqual(1, healthDeaths);
            Assert.AreEqual(1, enemyDeaths);
        }

        [Test]
        public void Enemy_ExcessDamageDoesNotMakeHealthNegative()
        {
            var health = CreateEnemy().Health;
            health.TakeDamage(health.Current + 100);
            Assert.AreEqual(0, health.Current);
            Assert.IsTrue(health.IsDead);
        }

        [Test]
        public void Enemy_DeadHealthCannotBeHealedOrDamaged()
        {
            var health = CreateEnemy().Health;
            health.TakeDamage(health.Current);
            health.Heal(5);
            health.TakeDamage(1);
            Assert.AreEqual(0, health.Current);
            Assert.IsTrue(health.IsDead);
        }

        [Test]
        public void Enemy_DeathDisablesCollidersAndStopsVelocity()
        {
            var enemy = CreateEnemy();
            var body = enemy.GetComponent<Rigidbody2D>();
            var collider = enemy.GetComponent<Collider2D>();
            body.linearVelocity = Vector2.right * 3f;
            enemy.Health.TakeDamage(enemy.Health.Current);
            Assert.IsFalse(collider.enabled);
            Assert.That(body.linearVelocity.magnitude, Is.LessThan(0.01f));
        }

        [Test]
        public void Enemies_HaveIndependentHealth()
        {
            var first = CreateEnemy("FirstEnemy");
            var second = CreateEnemy("SecondEnemy");
            first.Health.TakeDamage(2);
            Assert.AreEqual(first.Health.Max - 2, first.Health.Current);
            Assert.AreEqual(second.Health.Max, second.Health.Current);
            Assert.IsFalse(second.Health.IsDead);
        }

        [UnityTest]
        public IEnumerator Enemy_IsDestroyedAfterLethalDamage()
        {
            var enemy = CreateEnemy();
            var obj = enemy.gameObject;
            enemy.Health.TakeDamage(enemy.Health.Current);
            yield return null;
            Assert.IsTrue(obj == null, "Dead enemy should be destroyed after the frame.");
        }
    }
}

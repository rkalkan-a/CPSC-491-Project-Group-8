using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DungeonCrawler.Combat;
using DungeonCrawler.Enemies;
using DungeonCrawler.Player;

namespace DungeonCrawler.Tests
{
    // SCRUM-93: integration coverage for contact damage (an enemy touching the player) together with the melee swing.
    //
    // These use real colliders and the physics step, not direct TakeDamage calls, so they check the whole path:
    // collider overlap -> DamageOnContact -> Health -> what the player sees. The groups are:
    //   who gets hurt  - the player yes, other enemies no, dead players no
    //   how much       - one hit is a quarter heart, zero damage does nothing
    //   how often      - i-frames stop a touching enemy from draining every physics step
    //   with melee     - killing an enemy with the sword stops its contact damage
    public class ContactDamageTests
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

        // ---------- who gets hurt ----------

        [UnityTest]
        public IEnumerator EnemyTouchingThePlayer_HurtsThePlayer()
        {
            var player = SpawnPlayer(Vector2.zero, invulnerability: 1f);
            int before = player.Current;

            SpawnToucher(Vector2.zero, damage: 1);
            yield return Physics();

            Assert.Less(player.Current, before, "an enemy overlapping the player should hurt them");
        }

        [UnityTest]
        public IEnumerator EnemyAwayFromThePlayer_DoesNotHurtThem()
        {
            var player = SpawnPlayer(Vector2.zero, invulnerability: 1f);
            int before = player.Current;

            SpawnToucher(new Vector2(5f, 0f), damage: 1);
            yield return Physics();

            Assert.AreEqual(before, player.Current);
        }

        [UnityTest]
        public IEnumerator EnemyTouchingAnotherEnemy_DoesNotHurtIt()
        {
            var other = SpawnTarget(Vector2.zero);
            int before = other.Current;

            SpawnToucher(Vector2.zero, damage: 1);
            yield return Physics();

            Assert.AreEqual(before, other.Current, "contact damage is meant for the player only");
        }

        [UnityTest]
        public IEnumerator DeadPlayer_IsNotHurtAnyFurther_AndOnlyDiesOnce()
        {
            var player = SpawnPlayer(Vector2.zero, invulnerability: 0f);
            int deaths = 0;
            player.Died += () => deaths++;
            player.TakeDamage(player.Max);
            Assert.IsTrue(player.IsDead);

            SpawnToucher(Vector2.zero, damage: 1);
            yield return Physics();

            Assert.AreEqual(0, player.Current);
            Assert.AreEqual(1, deaths, "touching a dead player shouldn't raise the death event again");
        }

        // ---------- how much ----------

        [UnityTest]
        public IEnumerator OneHit_TakesAQuarterHeart()
        {
            var player = SpawnPlayer(Vector2.zero, invulnerability: 1f);
            int before = player.Current;

            SpawnToucher(Vector2.zero, damage: 1);
            yield return Physics();

            Assert.AreEqual(4, Health.HpPerHeart, "a heart is 4 hp, so 1 damage is a quarter heart");
            Assert.AreEqual(before - 1, player.Current, "one hit with i-frames on should take exactly one quarter heart");
        }

        [UnityTest]
        public IEnumerator ZeroDamage_DoesNothing()
        {
            var player = SpawnPlayer(Vector2.zero, invulnerability: 0f);
            int before = player.Current;

            SpawnToucher(Vector2.zero, damage: 0);
            yield return Physics();

            Assert.AreEqual(before, player.Current);
        }

        // ---------- how often ----------

        [UnityTest]
        public IEnumerator StayingInContact_DuringIFrames_OnlyHurtsOnce()
        {
            var player = SpawnPlayer(Vector2.zero, invulnerability: 5f);
            int before = player.Current;

            SpawnToucher(Vector2.zero, damage: 1);
            yield return Physics(steps: 10); // OnTriggerStay fires every step while they overlap

            Assert.AreEqual(before - 1, player.Current, "i-frames should block the repeat hits");
        }

        [UnityTest]
        public IEnumerator StayingInContact_AfterIFramesEnd_HurtsAgain()
        {
            var player = SpawnPlayer(Vector2.zero, invulnerability: 0.2f);
            int before = player.Current;

            var toucher = SpawnToucher(Vector2.zero, damage: 1);
            yield return Physics();
            Assert.AreEqual(before - 1, player.Current);

            float start = Time.time;
            yield return new WaitForSeconds(0.35f);
            yield return Physics();
            float elapsed = Time.time - start;

            // standing in the enemy keeps hurting, but at most once per i-frame window
            int extraHits = (before - 1) - player.Current;
            int maxHits = Mathf.FloorToInt(elapsed / 0.2f) + 1;
            Assert.GreaterOrEqual(extraHits, 1, "another hit should land once the i-frames are over");
            Assert.LessOrEqual(extraHits, maxHits, "hits should be spaced out by the i-frames, not land every physics step");
            Assert.IsNotNull(toucher);
        }

        // ---------- with the melee swing ----------

        [UnityTest]
        public IEnumerator KillingTheEnemyWithTheSword_StopsItsContactDamage()
        {
            var playerHealth = SpawnPlayer(Vector2.zero, invulnerability: 0.1f);
            var melee = playerHealth.GetComponent<PlayerMelee>();
            playerHealth.GetComponent<PlayerSword>().Equip();

            // a real enemy right next to the player, with one hp left so a single swing kills it
            var enemy = SpawnEnemy(new Vector2(0.6f, 0f), damage: 1);
            var enemyHealth = enemy.GetComponent<Health>();
            enemyHealth.TakeDamage(enemyHealth.Max - 1);
            yield return Physics();
            int afterContact = playerHealth.Current;

            Assert.IsTrue(melee.TrySwing(Vector2.right), "the swing should start");
            yield return null;
            Assert.IsTrue(enemy == null || enemyHealth.IsDead, "the swing should kill the enemy");

            yield return new WaitForSeconds(0.2f); // past the player's i-frames
            yield return Physics();

            Assert.AreEqual(afterContact, playerHealth.Current, "a dead enemy shouldn't keep hurting the player");
        }

        [UnityTest]
        public IEnumerator HittingTheEnemy_DoesNotHurtThePlayer()
        {
            var playerHealth = SpawnPlayer(Vector2.zero, invulnerability: 0f);
            var melee = playerHealth.GetComponent<PlayerMelee>();
            playerHealth.GetComponent<PlayerSword>().Equip();
            var target = SpawnTarget(new Vector2(0.8f, 0f));
            int playerBefore = playerHealth.Current, targetBefore = target.Current;
            yield return null;

            Assert.IsTrue(melee.TrySwing(Vector2.right));
            yield return null;

            Assert.Less(target.Current, targetBefore, "the target in front should take the hit");
            Assert.AreEqual(playerBefore, playerHealth.Current, "the player's own swing shouldn't hurt them");
        }

        // ---------- fixtures ----------

        private static IEnumerator Physics(int steps = 3)
        {
            for (int i = 0; i < steps; i++) yield return new WaitForFixedUpdate();
        }

        private Health SpawnPlayer(Vector2 position, float invulnerability)
        {
            var go = new GameObject("TestPlayer");
            go.transform.position = position;
            go.AddComponent<Rigidbody2D>().gravityScale = 0f;
            go.AddComponent<CircleCollider2D>().radius = 0.4f;
            var health = go.AddComponent<Health>();
            SetField(health, "invulnerabilityTime", invulnerability);

            // the controller complains that it has no input asset. movement isn't under test here
            LogAssert.Expect(LogType.Error, new Regex("PlayerController needs the InputSystem_Actions"));
            go.AddComponent<PlayerController>(); // adds PlayerMelee and PlayerSword itself

            spawned.Add(go);
            return health;
        }

        // a still object that hurts on contact, like an enemy body or spikes, with no AI so it stays where it's put
        private DamageOnContact SpawnToucher(Vector2 position, int damage)
        {
            var go = new GameObject("TestToucher");
            go.transform.position = position;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;
            col.isTrigger = true;
            var contact = go.AddComponent<DamageOnContact>();
            contact.Damage = damage;
            SetField(contact, "knockback", 0f); // keep the player in place so the overlap holds
            spawned.Add(go);
            return contact;
        }

        // a real enemy: Enemy + Health + contact damage, the same parts the enemy prefab uses
        private Enemy SpawnEnemy(Vector2 position, int damage)
        {
            var go = new GameObject("TestEnemy");
            go.transform.position = position;
            go.AddComponent<Rigidbody2D>().gravityScale = 0f;
            go.AddComponent<CircleCollider2D>().radius = 0.4f;
            go.AddComponent<Health>();
            var enemy = go.AddComponent<Enemy>();
            var contact = go.AddComponent<DamageOnContact>();
            contact.Damage = damage;
            SetField(contact, "knockback", 0f);
            spawned.Add(go);
            return enemy;
        }

        private Health SpawnTarget(Vector2 position)
        {
            var go = new GameObject("TestTarget");
            go.transform.position = position;
            go.AddComponent<Rigidbody2D>().gravityScale = 0f;
            go.AddComponent<CircleCollider2D>().radius = 0.4f;
            spawned.Add(go);
            return go.AddComponent<Health>();
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"{target.GetType().Name} has no field '{name}', the test needs updating");
            field.SetValue(target, value);
        }
    }
}

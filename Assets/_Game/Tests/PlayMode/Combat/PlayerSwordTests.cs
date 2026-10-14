using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DungeonCrawler.Combat;
using DungeonCrawler.Loot;
using DungeonCrawler.Player;

namespace DungeonCrawler.Tests
{
    // Play Mode coverage for the sword the player carries.
    //
    // The sword is only visual (PlayerMelee decides what gets hit), so these check its states:
    // not owned -> owned (taken from a chest), held -> swinging -> held, and that the blade
    // points the way the attack was aimed.
    public class PlayerSwordTests
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

        // ---------- getting the sword ----------

        [UnityTest]
        public IEnumerator Player_StartsUnarmed_WithTheSwordHidden()
        {
            var sword = SpawnPlayer(equipped: false);
            yield return null;

            Assert.IsNotNull(sword, "PlayerController should add the sword component itself");
            Assert.IsFalse(sword.HasSword);
            Assert.IsFalse(sword.Sword.GetComponent<SpriteRenderer>().enabled, "the sword shouldn't show before it's picked up");
        }

        [UnityTest]
        public IEnumerator Player_CannotAttack_BeforeTakingTheSword()
        {
            var sword = SpawnPlayer(equipped: false);
            yield return null;

            Assert.IsFalse(sword.GetComponent<PlayerMelee>().TrySwing(Vector2.right));
        }

        [UnityTest]
        public IEnumerator Equip_ShowsTheSword_AndAllowsAttacking()
        {
            var sword = SpawnPlayer(equipped: false);
            yield return null;

            sword.Equip();
            yield return null;

            Assert.IsTrue(sword.HasSword);
            Assert.IsTrue(sword.Sword.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(sword.GetComponent<PlayerMelee>().TrySwing(Vector2.right));
        }

        [UnityTest]
        public IEnumerator Chest_GivesTheSword_OnlyOnce()
        {
            var sword = SpawnPlayer(equipped: false);
            var player = sword.GetComponent<PlayerController>();
            var chest = SpawnChest();
            yield return null;

            Assert.IsTrue(chest.ContainsSword, "the chest should start with the sword in it");
            Assert.IsTrue(chest.TakeSword(player));
            Assert.IsTrue(sword.HasSword);
            Assert.IsFalse(chest.ContainsSword);
            Assert.IsFalse(chest.TakeSword(player), "there's nothing left to take the second time");
        }

        // ---------- swinging ----------

        [UnityTest]
        public IEnumerator Sword_IsHeld_UntilAnAttackStarts()
        {
            var sword = SpawnPlayer();
            yield return null;

            Assert.IsFalse(sword.IsSwinging);
        }

        [UnityTest]
        public IEnumerator Attacking_StartsASwing_ThatEndsOnItsOwn()
        {
            var sword = SpawnPlayer();
            yield return null;

            sword.GetComponent<PlayerMelee>().TrySwing(Vector2.right);
            Assert.IsTrue(sword.IsSwinging, "a melee attack should start the sword swinging");

            yield return new WaitForSeconds(0.4f);
            Assert.IsFalse(sword.IsSwinging, "the swing should finish and go back to being held");
        }

        [UnityTest]
        public IEnumerator Swing_KeepsTheBlade_InsideTheAttackArc(
            [ValueSource(nameof(FourDirections))] Vector2 direction)
        {
            var sword = SpawnPlayer();
            var melee = sword.GetComponent<PlayerMelee>();
            yield return null;

            melee.TrySwing(direction);

            // check a few frames of the swing, not just one
            for (int i = 0; i < 4 && sword.IsSwinging; i++)
            {
                yield return null;
                if (!sword.IsSwinging) break;
                Vector2 blade = sword.Sword.up; // the sprite points up, so "up" is the way the blade points
                Assert.LessOrEqual(Vector2.Angle(direction, blade), melee.ArcDegrees * 0.5f + 1f,
                    "the blade should stay within the arc the attack covers");
            }
        }

        private static readonly Vector2[] FourDirections = { Vector2.right, Vector2.left, Vector2.up, Vector2.down };

        [UnityTest]
        public IEnumerator Attack_GoesTheWayThePlayerFaces_NotTowardTheMouse()
        {
            var sword = SpawnPlayer();
            yield return null;

            // a fresh player faces down, and nothing here moves the mouse or the player
            Assert.AreEqual(Vector2.down, sword.GetComponent<PlayerMelee>().FacingDirection());
        }

        [UnityTest]
        public IEnumerator Sword_IsHidden_WhenThePlayerIsDead()
        {
            var sword = SpawnPlayer();
            yield return null;

            sword.GetComponent<Health>().TakeDamage(999);
            yield return null;

            Assert.IsFalse(sword.Sword.GetComponent<SpriteRenderer>().enabled);
        }

        // ---------- fixtures ----------

        private Chest SpawnChest()
        {
            var go = new GameObject("TestChest");
            go.AddComponent<BoxCollider2D>();
            spawned.Add(go);
            return go.AddComponent<Chest>();
        }

        private PlayerSword SpawnPlayer(bool equipped = true)
        {
            var go = new GameObject("TestPlayer");
            go.AddComponent<Rigidbody2D>().gravityScale = 0f;
            go.AddComponent<CircleCollider2D>().radius = 0.4f;
            go.AddComponent<Health>();

            // the controller complains that it has no input asset. movement isn't under test here
            LogAssert.Expect(LogType.Error, new Regex("PlayerController needs the InputSystem_Actions"));
            go.AddComponent<PlayerController>(); // adds PlayerMelee and PlayerSword itself

            spawned.Add(go);
            var sword = go.GetComponent<PlayerSword>();
            if (equipped) sword.Equip();
            return sword;
        }
    }
}

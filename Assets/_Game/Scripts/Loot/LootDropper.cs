using UnityEngine;
using DungeonCrawler.Combat;
using DungeonCrawler.Core;
using DungeonCrawler.Enemies;
using DungeonCrawler.Player;
using DungeonCrawler.UI;

namespace DungeonCrawler.Loot
{
    // drops loot where an enemy dies: a heart some of the time, and the room's armor (leather in the
    // first dungeon, chain mail in the maze) the first time an enemy dies there while the player doesn't have it. every enemy gets this automatically
    [RequireComponent(typeof(Health))]
    public class LootDropper : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float dropChance = 0.35f;

        public float DropChance
        {
            get => dropChance;
            set => dropChance = Mathf.Clamp01(value);
        }

        private static Sprite heartSprite;
        private Health health;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void OnEnable() => health.Died += OnDied;
        private void OnDisable() => health.Died -= OnDied;

        private void OnDied()
        {
            TryDrop(Random.value);
            TryDropArmor();
        }

        // drops this room's armor if it would be an upgrade for the player and there isn't a set already lying around.
        // returns the pickup, or null if nothing dropped
        public GameObject TryDropArmor()
        {
            var enemy = GetComponent<Enemy>();
            ArmorTier tier = enemy != null && enemy.Room != null ? enemy.Room.ArmorLoot : ArmorTier.None;
            if (tier == ArmorTier.None) return null;

            var player = GameManager.Instance != null ? GameManager.Instance.Player : null;
            if (player == null) player = FindFirstObjectByType<PlayerController>();
            var armor = player != null ? player.GetComponent<PlayerArmor>() : null;
            if (armor == null || armor.Tier >= tier) return null;
            if (FindFirstObjectByType<ArmorPickup>() != null) return null;

            // a little to the side so it doesn't land on top of a heart
            return SpawnArmor((Vector2)transform.position + Vector2.right * 0.5f, RoomTransform(), tier);
        }

        // roll is 0..1. drops when the roll is under the drop chance, so a chance of 0 never drops and 1 always does.
        // returns the pickup, or null if nothing dropped
        public GameObject TryDrop(float roll)
        {
            if (roll >= dropChance) return null;

            return SpawnHeart(transform.position, RoomTransform());
        }

        // pickups go under the enemy's room so they hide and show with that room
        private Transform RoomTransform()
        {
            var enemy = GetComponent<Enemy>();
            return enemy != null && enemy.Room != null ? enemy.Room.transform : null;
        }

        public static GameObject SpawnHeart(Vector2 position, Transform parent)
        {
            if (heartSprite == null) heartSprite = PixelUI.Heart(new Color(0.9f, 0.15f, 0.2f), 0.55f, 26f);

            var go = new GameObject("HeartPickup");
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = heartSprite;
            sr.sortingOrder = 4;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.3f;

            go.AddComponent<HealthPickup>();
            go.AddComponent<PickupBob>();
            return go;
        }

        public static GameObject SpawnArmor(Vector2 position, Transform parent, ArmorTier tier = ArmorTier.Leather)
        {
            var go = new GameObject("ArmorPickup");
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PixelUI.ArmorIcon(tier);
            sr.sortingOrder = 4;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.3f;

            go.AddComponent<ArmorPickup>().Tier = tier;
            go.AddComponent<PickupBob>();
            return go;
        }
    }
}

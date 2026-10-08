using UnityEngine;
using DungeonCrawler.Combat;
using DungeonCrawler.Enemies;
using DungeonCrawler.UI;

namespace DungeonCrawler.Loot
{
    // drops a heart where an enemy dies, some of the time. every enemy gets one automatically
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

        private void OnDied() => TryDrop(Random.value);

        // roll is 0..1. drops when the roll is under the drop chance, so a chance of 0 never drops and 1 always does.
        // returns the pickup, or null if nothing dropped
        public GameObject TryDrop(float roll)
        {
            if (roll >= dropChance) return null;

            // keep the pickup under the enemy's room so it hides and shows with that room
            var enemy = GetComponent<Enemy>();
            Transform parent = enemy != null && enemy.Room != null ? enemy.Room.transform : null;
            return SpawnHeart(transform.position, parent);
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
    }
}

using UnityEngine;
using DungeonCrawler.Core;

namespace DungeonCrawler.Combat
{
    // spawns blood splats on the floor when this thing takes damage. amount depends on the gore setting
    [RequireComponent(typeof(Health))]
    public class BleedOnHit : MonoBehaviour
    {
        [SerializeField] private Color bloodColor = new Color(0.45f, 0.02f, 0.05f);
        [SerializeField, Min(0f)] private float spread = 0.7f;
        [SerializeField, Min(1f)] private float lifetime = 25f;

        private static Sprite splatSprite;
        private Health health;
        private int lastHp;

        private void Awake()
        {
            health = GetComponent<Health>();
            if (splatSprite == null)
                splatSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        private void OnEnable()
        {
            lastHp = health.Current;
            health.Changed += OnHealthChanged;
        }

        private void OnDisable()
        {
            health.Changed -= OnHealthChanged;
        }

        private void OnHealthChanged(int current, int max)
        {
            int lost = lastHp - current;
            lastHp = current;
            if (lost <= 0) return;

            int count = GameSettings.Gore switch
            {
                GameSettings.GoreOff => 0,
                GameSettings.GoreLow => 2,
                _ => 5 + lost * 2,
            };
            Splat(transform.position, count);
        }

        public void Splat(Vector2 at, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Blood");
                go.transform.position = at + Random.insideUnitCircle * spread;
                go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                float size = Random.Range(0.15f, 0.45f);
                go.transform.localScale = new Vector3(size, size * Random.Range(0.6f, 1f), 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = splatSprite;
                sr.color = bloodColor * Random.Range(0.8f, 1.2f);
                sr.sortingOrder = -8; // above the floor, under everything else

                Destroy(go, lifetime);
            }
        }
    }
}

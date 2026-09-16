using System.Collections.Generic;
using UnityEngine;

namespace DungeonCrawler.Loot
{
    // soft glow + little rising sparkles so you can tell a chest hasn't been opened yet.
    // turns itself off once the chest opens
    public class ChestGlow : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer glow;
        [SerializeField] private Sprite sparkleSprite;
        [SerializeField] private Color sparkleColor = new Color(1f, 0.93f, 0.7f);
        [SerializeField, Min(0f)] private float pulseSpeed = 2.5f;
        [SerializeField, Range(0f, 1f)] private float minAlpha = 0.25f;
        [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.5f;
        [SerializeField, Min(0f)] private float sparklesPerSecond = 4f;
        [SerializeField, Min(0f)] private float spawnRadius = 0.55f;

        private class Sparkle
        {
            public Transform t;
            public SpriteRenderer sr;
            public float life, maxLife, size;
            public Vector3 vel;
        }

        private Chest chest;
        private readonly List<Sparkle> sparkles = new();
        private float spawnTimer;
        private bool done;

        private void Awake()
        {
            chest = GetComponentInParent<Chest>();
        }

        private void Update()
        {
            if (done) return;

            if (chest != null && chest.IsOpen)
            {
                TurnOff();
                return;
            }

            if (glow != null)
            {
                float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
                var c = glow.color;
                c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
                glow.color = c;
            }

            spawnTimer += Time.deltaTime;
            if (sparklesPerSecond > 0f && spawnTimer >= 1f / sparklesPerSecond)
            {
                spawnTimer = 0f;
                Spawn();
            }

            for (int i = sparkles.Count - 1; i >= 0; i--)
            {
                var s = sparkles[i];
                s.life -= Time.deltaTime;
                if (s.life <= 0f)
                {
                    Destroy(s.t.gameObject);
                    sparkles.RemoveAt(i);
                    continue;
                }
                float k = s.life / s.maxLife;              // 1 -> 0
                s.t.position += s.vel * Time.deltaTime;
                float twinkle = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.time * 12f + i));
                s.t.localScale = Vector3.one * s.size * k * twinkle;
                var col = sparkleColor;
                col.a = k;
                s.sr.color = col;
            }
        }

        private void Spawn()
        {
            if (sparkleSprite == null) return;
            var go = new GameObject("Sparkle");
            go.transform.SetParent(transform, false);
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            go.transform.position = transform.position + new Vector3(offset.x, offset.y * 0.6f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sparkleSprite;
            sr.color = sparkleColor;
            sr.sortingOrder = 6;
            sparkles.Add(new Sparkle
            {
                t = go.transform,
                sr = sr,
                maxLife = Random.Range(0.6f, 1.1f),
                life = Random.Range(0.6f, 1.1f),
                size = Random.Range(0.05f, 0.1f),
                vel = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(0.3f, 0.6f), 0f),
            });
        }

        private void TurnOff()
        {
            done = true;
            if (glow != null) glow.enabled = false;
            foreach (var s in sparkles) if (s.t != null) Destroy(s.t.gameObject);
            sparkles.Clear();
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using DungeonCrawler.UI;

namespace DungeonCrawler.Loot
{
    // makes an unopened chest stand out: a soft warm light behind it, a faint gold rim around its outline,
    // a little light on the ground and a few twinkling stars drifting up. fades out once the chest is opened
    public class ChestGlow : MonoBehaviour
    {
        [SerializeField] private Color glowColor = new Color(1f, 0.78f, 0.35f);
        [SerializeField] private Color sparkleColor = new Color(1f, 0.95f, 0.75f);
        [SerializeField, Min(0f)] private float pulseSpeed = 2.2f;
        [SerializeField, Min(0f)] private float sparklesPerSecond = 2.5f;

        private class Sparkle
        {
            public SpriteRenderer sr;
            public float life, maxLife, size, spin;
            public Vector3 vel;
        }

        private static Sprite glowSprite;
        private static Sprite starSprite;

        private Chest chest;
        private SpriteRenderer chestSprite;
        private SpriteRenderer shadow, halo, pool, rim;
        private Transform fxRoot;
        private readonly List<Sparkle> sparkles = new();
        private float spawnTimer;
        private bool opened;
        private float fadeTime = -1f;
        private const float FadeLength = 0.3f;

        private void Start()
        {
            chest = GetComponentInParent<Chest>();
            chestSprite = chest != null ? chest.Sprite : GetComponentInChildren<SpriteRenderer>();
            if (glowSprite == null) glowSprite = PixelUI.RadialGlow();
            if (starSprite == null) starSprite = PixelUI.Star();
            Build();
        }

        private void Build()
        {
            if (chestSprite == null) return;

            // sit the effects on the drawn chest, not the chest's collider
            fxRoot = new GameObject("ChestFX").transform;
            fxRoot.SetParent(chestSprite.transform, false);
            int order = chestSprite.sortingOrder;
            Vector2 size = chestSprite.sprite.bounds.size; // local size, the fx inherit the chest's scale

            // soft contact shadow so the chest sits on the ground. stays after opening
            shadow = MakeLayer("Shadow", glowSprite, order - 4, new Vector3(0f, -size.y * 0.42f, 0f));
            shadow.transform.localScale = new Vector3(size.x * 0.6f, size.y * 0.14f, 1f);
            shadow.color = new Color(0f, 0f, 0f, 0.45f);

            halo = MakeLayer("Halo", glowSprite, order - 2, new Vector3(0f, size.y * 0.1f, 0f));
            halo.transform.localScale = new Vector3(size.x * 1.25f, size.y * 1.15f, 1f);

            pool = MakeLayer("GroundLight", glowSprite, order - 3, new Vector3(0f, -size.y * 0.42f, 0f));
            pool.transform.localScale = new Vector3(size.x * 1.1f, size.y * 0.35f, 1f);

            // same sprite as the chest, slightly bigger and gold, so it reads as a glowing outline
            rim = MakeLayer("Rim", chestSprite.sprite, order - 1, Vector3.zero);
            rim.transform.localScale = Vector3.one * 1.12f;

            // an already opened chest (coming back to the room) stays dark
            if (chest != null && chest.IsOpen)
            {
                opened = true;
                halo.enabled = pool.enabled = rim.enabled = false;
            }
        }

        private SpriteRenderer MakeLayer(string name, Sprite sprite, int order, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(fxRoot, false);
            go.transform.localPosition = localPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerID = chestSprite.sortingLayerID;
            sr.sortingOrder = order;
            sr.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
            return sr;
        }

        private void Update()
        {
            if (fxRoot == null) return;

            if (!opened && chest != null && chest.IsOpen)
            {
                opened = true;
                fadeTime = 0f;
            }

            if (!opened) UpdateIdle();
            else UpdateFade();

            UpdateSparkles();
        }

        private void UpdateIdle()
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0..1
            SetAlpha(halo, Mathf.Lerp(0.1f, 0.18f, t));
            SetAlpha(pool, Mathf.Lerp(0.06f, 0.1f, t));
            SetAlpha(rim, Mathf.Lerp(0.12f, 0.28f, t));
            if (rim.sprite != chestSprite.sprite) rim.sprite = chestSprite.sprite;

            // no new sparkles while the room is hidden (chest sprite gets switched off in other rooms)
            if (!chestSprite.enabled) return;
            spawnTimer += Time.deltaTime;
            if (sparklesPerSecond > 0f && spawnTimer >= 1f / sparklesPerSecond)
            {
                spawnTimer = 0f;
                SpawnSparkle();
            }
        }

        private void UpdateFade()
        {
            if (fadeTime < 0f) return;
            fadeTime += Time.deltaTime;
            float k = Mathf.Clamp01(fadeTime / FadeLength);
            float fade = 1f - k;
            SetAlpha(halo, 0.18f * fade);
            SetAlpha(pool, 0.1f * fade);
            SetAlpha(rim, 0.28f * fade);
            if (k >= 1f)
            {
                fadeTime = -1f;
                halo.enabled = pool.enabled = rim.enabled = false;
            }
        }

        private void SpawnSparkle()
        {
            var go = new GameObject("Sparkle");
            go.transform.SetParent(fxRoot, false);
            Vector2 size = chestSprite.sprite.bounds.size;
            go.transform.localPosition = new Vector3(Random.Range(-0.5f, 0.5f) * size.x, Random.Range(-0.1f, 0.45f) * size.y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = starSprite;
            sr.sortingLayerID = chestSprite.sortingLayerID;
            sr.sortingOrder = chestSprite.sortingOrder + 1;
            sr.color = Color.clear;

            float life = Random.Range(0.9f, 1.4f);
            sparkles.Add(new Sparkle
            {
                sr = sr,
                life = life,
                maxLife = life,
                size = Random.Range(0.5f, 0.9f),
                spin = Random.Range(0f, 10f),
                vel = new Vector3(Random.Range(-0.08f, 0.08f), Random.Range(0.25f, 0.45f), 0f),
            });
        }

        private void UpdateSparkles()
        {
            for (int i = sparkles.Count - 1; i >= 0; i--)
            {
                var s = sparkles[i];
                s.life -= Time.deltaTime;
                if (s.life <= 0f || s.sr == null)
                {
                    if (s.sr != null) Destroy(s.sr.gameObject);
                    sparkles.RemoveAt(i);
                    continue;
                }
                float k = s.life / s.maxLife; // 1 -> 0
                s.sr.transform.localPosition += s.vel * Time.deltaTime;
                s.vel *= 1f - Time.deltaTime * 1.5f; // drift slows down
                // pop in quickly, twinkle, fade out
                float appear = Mathf.Clamp01((1f - k) * 6f);
                float twinkle = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 9f + s.spin));
                s.sr.transform.localScale = Vector3.one * s.size * twinkle * appear;
                var c = sparkleColor;
                c.a = Mathf.Min(appear, k * 1.5f) * 0.7f;
                s.sr.color = c;
            }
        }

        private static void SetAlpha(SpriteRenderer sr, float a)
        {
            if (sr == null) return;
            var c = sr.color;
            c.a = a;
            sr.color = c;
        }
    }
}

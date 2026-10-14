using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DungeonCrawler.Core;

namespace DungeonCrawler.Combat
{
    // leaves a small blood splat on the floor when this thing takes damage. amount depends on the gore setting.
    // kept light on purpose: one round splat per hit, a few more when an enemy dies, and they fade away
    [RequireComponent(typeof(Health))]
    public class BleedOnHit : MonoBehaviour
    {
        [SerializeField] private Color bloodColor = new Color(0.34f, 0.02f, 0.04f, 0.8f);
        [SerializeField, Min(0f)] private float spread = 0.35f;
        [SerializeField, Min(1f)] private float lifetime = 10f;
        [SerializeField, Min(0.1f)] private float fadeTime = 2.5f;

        private const int SplatPixels = 16;
        private const int Variants = 4;
        private static Sprite[] splatSprites;

        private Health health;
        private int lastHp;

        private void Awake()
        {
            health = GetComponent<Health>();
            if (splatSprites == null) splatSprites = MakeSplatSprites();
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
                GameSettings.GoreLow => Random.value < 0.5f ? 1 : 0,
                _ => lost >= 3 ? 2 : 1,
            };
            Splat(transform.position, count);
        }

        public void Splat(Vector2 at, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Blood");
                go.transform.position = at + Random.insideUnitCircle * spread;
                go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0, 4) * 90f); // quarter turns keep the pixels lined up
                float size = Random.Range(0.35f, 0.6f);
                go.transform.localScale = new Vector3(size, size, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = splatSprites[Random.Range(0, splatSprites.Length)];
                float shade = Random.Range(0.85f, 1.1f);
                sr.color = new Color(bloodColor.r * shade, bloodColor.g * shade, bloodColor.b * shade, bloodColor.a);
                sr.sortingOrder = -8; // above the floor, under everything else

                go.AddComponent<BloodFade>().Begin(lifetime, fadeTime);
            }
        }

        // a few irregular round blobs with a droplet or two, drawn as pixel art. white, tinted by the renderer
        private static Sprite[] MakeSplatSprites()
        {
            var sprites = new Sprite[Variants];
            var rng = new System.Random(491);
            float mid = SplatPixels * 0.5f;
            for (int v = 0; v < Variants; v++)
            {
                // circles as (x, y, radius): a main blob, a few lumps on it, and droplets flung further out
                var circles = new List<Vector3> { new Vector3(mid, mid, 3.6f + (float)rng.NextDouble()) };
                for (int b = 0; b < 3; b++)
                    circles.Add(AtAngle(rng, mid, 1.5f + (float)rng.NextDouble() * 1.5f, 1.8f + (float)rng.NextDouble() * 1.2f));
                int drops = 1 + rng.Next(2);
                for (int d = 0; d < drops; d++)
                    circles.Add(AtAngle(rng, mid, 5.5f + (float)rng.NextDouble() * 1.5f, 0.9f + (float)rng.NextDouble() * 0.4f));

                var tex = new Texture2D(SplatPixels, SplatPixels, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                };
                var px = new Color[SplatPixels * SplatPixels];
                for (int y = 0; y < SplatPixels; y++)
                {
                    for (int x = 0; x < SplatPixels; x++)
                    {
                        float alpha = 0f;
                        foreach (var c in circles)
                        {
                            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c.x, c.y));
                            if (d <= c.z) alpha = Mathf.Max(alpha, d <= c.z - 1f ? 1f : 0.75f); // slightly see-through rim
                        }
                        px[y * SplatPixels + x] = new Color(1f, 1f, 1f, alpha);
                    }
                }
                tex.SetPixels(px);
                tex.Apply();
                sprites[v] = Sprite.Create(tex, new Rect(0, 0, SplatPixels, SplatPixels), new Vector2(0.5f, 0.5f), SplatPixels);
            }
            return sprites;
        }

        private static Vector3 AtAngle(System.Random rng, float mid, float dist, float radius)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            return new Vector3(mid + Mathf.Cos(ang) * dist, mid + Mathf.Sin(ang) * dist, radius);
        }
    }

    // fades a blood splat out at the end of its life, then removes it
    public class BloodFade : MonoBehaviour
    {
        public void Begin(float lifetime, float fadeTime) => StartCoroutine(Run(lifetime, fadeTime));

        private IEnumerator Run(float lifetime, float fadeTime)
        {
            var sr = GetComponent<SpriteRenderer>();
            yield return new WaitForSeconds(Mathf.Max(0f, lifetime - fadeTime));
            float startA = sr.color.a;
            for (float t = 0f; t < fadeTime; t += Time.deltaTime)
            {
                var col = sr.color;
                col.a = startA * (1f - t / fadeTime);
                sr.color = col;
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}

using UnityEngine;

namespace DungeonCrawler.Player
{
    // purely visual: a small reticle sitting at max attack range in whatever direction
    // you're aiming, plus a faint ring around the player showing that range at a glance
    [RequireComponent(typeof(PlayerAttack))]
    public class AimIndicator : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float reticleSize = 0.22f;
        [SerializeField] private Color reticleColor = new Color(1f, 1f, 1f, 0.9f);
        [SerializeField] private Color rangeColor = new Color(1f, 1f, 1f, 0.15f);
        [SerializeField, Min(8)] private int rangeSegments = 40;
        [SerializeField, Min(0.005f)] private float rangeLineWidth = 0.04f;

        private PlayerAttack attack;
        private Transform reticle;
        private LineRenderer rangeRing;
        private static Sprite cachedRingSprite;

        private void Awake()
        {
            attack = GetComponent<PlayerAttack>();
            BuildReticle();
            BuildRangeRing();
        }

        private void LateUpdate()
        {
            Vector3 origin = transform.position;
            Vector2 dir = attack.AimDirection.sqrMagnitude > 0.01f ? attack.AimDirection.normalized : Vector2.down;

            reticle.position = origin + (Vector3)(dir * attack.Range);
            DrawRing(origin, attack.Range);
        }

        private void BuildReticle()
        {
            var go = new GameObject("AimReticle");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetRingSprite();
            sr.color = reticleColor;
            sr.sortingOrder = 12;
            go.transform.localScale = Vector3.one * reticleSize;
            reticle = go.transform;
        }

        private void BuildRangeRing()
        {
            var go = new GameObject("RangeRing");
            go.transform.SetParent(transform, false);
            rangeRing = go.AddComponent<LineRenderer>();
            rangeRing.useWorldSpace = true;
            rangeRing.loop = true;
            rangeRing.positionCount = rangeSegments;
            rangeRing.startWidth = rangeRing.endWidth = rangeLineWidth;
            rangeRing.material = new Material(Shader.Find("Sprites/Default"));
            rangeRing.startColor = rangeRing.endColor = rangeColor;
            rangeRing.sortingOrder = 8;
        }

        private void DrawRing(Vector3 center, float radius)
        {
            for (int i = 0; i < rangeSegments; i++)
            {
                float t = (float)i / rangeSegments * Mathf.PI * 2f;
                rangeRing.SetPosition(i, center + new Vector3(Mathf.Cos(t), Mathf.Sin(t), 0f) * radius);
            }
        }

        // a thin ring texture, shared across every instance (there's only ever one player, but still)
        private static Sprite GetRingSprite()
        {
            if (cachedRingSprite != null) return cachedRingSprite;

            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float outerRadius = size / 2f - 1f;
            float innerRadius = outerRadius - 4f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = 0f;
                    if (dist <= outerRadius && dist >= innerRadius) alpha = 1f;
                    else if (dist > outerRadius) alpha = Mathf.Clamp01(1f - (dist - outerRadius));
                    else if (dist < innerRadius) alpha = Mathf.Clamp01(1f - (innerRadius - dist));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();

            cachedRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return cachedRingSprite;
        }
    }
}

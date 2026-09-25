using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DungeonCrawler.UI;

namespace DungeonCrawler.Rooms
{
    // circle wipe between rooms: the screen closes in on the player, then opens back up in the new room
    public class RoomTransitionFX : MonoBehaviour
    {
        private const int TexSize = 128;
        private const float HoleFraction = 0.25f; // hole radius is a quarter of the texture width
        private const float Big = 100000f;

        [SerializeField, Min(0.05f)] private float closeTime = 0.35f;
        [SerializeField, Min(0.05f)] private float openTime = 0.4f;

        private static RoomTransitionFX instance;

        private RectTransform hole;
        private readonly RectTransform[] fillers = new RectTransform[4];

        public static RoomTransitionFX Get()
        {
            if (instance == null)
            {
                var go = new GameObject("RoomTransitionFX");
                instance = go.AddComponent<RoomTransitionFX>();
                instance.Build();
            }
            return instance;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500; // above the hud and menus

            hole = NewImage("Iris", CircleSprite());
            var black = PixelUI.Solid(Color.black);
            for (int i = 0; i < fillers.Length; i++) fillers[i] = NewImage("Fill" + i, black);
            SetVisible(false);
        }

        private RectTransform NewImage(string name, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = Vector2.zero; // positions are in screen pixels
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return rect;
        }

        // black square with a see-through pixel circle in the middle
        private static Sprite CircleSprite()
        {
            var tex = PixelUI.NewTexture(TexSize, TexSize, true);
            float c = TexSize * 0.5f, r = TexSize * HoleFraction;
            for (int y = 0; y < TexSize; y++)
                for (int x = 0; x < TexSize; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
                    tex.SetPixel(x, y, d < r ? Color.clear : Color.black);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, TexSize, TexSize), new Vector2(0.5f, 0.5f), 100f);
        }

        private void SetVisible(bool on)
        {
            hole.gameObject.SetActive(on);
            foreach (var f in fillers) f.gameObject.SetActive(on);
        }

        private void SetIris(Vector2 center, float radius)
        {
            float size = radius / HoleFraction;
            float half = Mathf.Max(0f, size * 0.5f - 1f); // 1px overlap so no seams show

            Place(hole, new Vector2(0.5f, 0.5f), center, new Vector2(size, size));
            Place(fillers[0], new Vector2(1f, 0.5f), new Vector2(center.x - half, center.y), new Vector2(Big, Big));       // left
            Place(fillers[1], new Vector2(0f, 0.5f), new Vector2(center.x + half, center.y), new Vector2(Big, Big));       // right
            Place(fillers[2], new Vector2(0.5f, 0f), new Vector2(center.x, center.y + half), new Vector2(half * 2f, Big)); // top
            Place(fillers[3], new Vector2(0.5f, 1f), new Vector2(center.x, center.y - half), new Vector2(half * 2f, Big)); // bottom
        }

        private static void Place(RectTransform r, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            r.pivot = pivot;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
        }

        private static float FullRadius(Vector2 center)
        {
            float dx = Mathf.Max(center.x, Screen.width - center.x);
            float dy = Mathf.Max(center.y, Screen.height - center.y);
            return Mathf.Sqrt(dx * dx + dy * dy) * 1.05f;
        }

        private static Vector2 ToScreen(Vector3 world)
        {
            var cam = Camera.main;
            return cam != null ? (Vector2)cam.WorldToScreenPoint(world) : new Vector2(Screen.width, Screen.height) * 0.5f;
        }

        public IEnumerator Close(Transform focus)
        {
            SetVisible(true);
            float start = FullRadius(ToScreen(focus.position));
            for (float t = 0f; t < closeTime; t += Time.unscaledDeltaTime)
            {
                float k = t / closeTime;
                SetIris(ToScreen(focus.position), Mathf.Lerp(start, 0f, k * k)); // speeds up as it closes
                yield return null;
            }
            SetIris(ToScreen(focus.position), 0f);
        }

        public IEnumerator Open(Transform focus)
        {
            for (float t = 0f; t < openTime; t += Time.unscaledDeltaTime)
            {
                float k = t / openTime;
                Vector2 center = ToScreen(focus.position);
                SetIris(center, Mathf.Lerp(0f, FullRadius(center), 1f - (1f - k) * (1f - k))); // slows down as it opens
                yield return null;
            }
            SetVisible(false);
        }
    }
}

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    // the "YOU UNLOCKED ..." banner that drops in near the top of the screen when the player
    // gets something new, so it's obvious what just happened. ChestPanel adds this to the hud
    public class UnlockBanner : MonoBehaviour
    {
        public static UnlockBanner Instance { get; private set; }

        private const float FadeIn = 0.2f, Hold = 2.6f, FadeOut = 0.5f;

        private static readonly Color Fill = new Color(0.13f, 0.1f, 0.1f, 0.96f);
        private static readonly Color Light = new Color(0.86f, 0.66f, 0.28f);
        private static readonly Color Dark = new Color(0.4f, 0.28f, 0.1f);
        private static readonly Color Outline = new Color(0.02f, 0.01f, 0.01f);
        private static readonly Color Gold = new Color(1f, 0.84f, 0.3f);
        private static readonly Color Cream = new Color(0.93f, 0.88f, 0.8f);

        public bool IsShowing => root != null && root.activeSelf;
        public string Title => title != null ? title.text : "";

        private GameObject root;
        private CanvasGroup group;
        private RectTransform panel;
        private Image icon;
        private Image heart;
        private TextMeshProUGUI title;
        private TextMeshProUGUI subtitle;
        private Coroutine routine;

        private void Awake()
        {
            Instance = this;
            var chestPanel = GetComponent<ChestPanel>();
            Build(chestPanel != null ? chestPanel.Font : null);
            root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // heartColor draws a small heart in front of the subtitle, for unlocks that add a heart. leave it null for none
        public void Show(Sprite iconSprite, string titleText, string subtitleText, Color? heartColor = null)
        {
            icon.sprite = iconSprite;
            icon.enabled = iconSprite != null;
            title.text = titleText;
            subtitle.text = subtitleText;

            heart.enabled = heartColor.HasValue;
            if (heartColor.HasValue) heart.sprite = PixelUI.Heart(heartColor.Value, 0.55f);
            // make room for the heart when there is one
            subtitle.rectTransform.offsetMin = new Vector2(heartColor.HasValue ? 140f : 104f, 14f);

            root.transform.SetAsLastSibling(); // over the chest screen too, since the sword is taken from inside it
            root.SetActive(true);
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            for (float t = 0f; t < FadeIn; t += Time.unscaledDeltaTime)
            {
                float k = t / FadeIn;
                group.alpha = k;
                panel.anchoredPosition = new Vector2(0f, Mathf.Lerp(30f, 0f, 1f - (1f - k) * (1f - k))); // drops into place
                yield return null;
            }
            group.alpha = 1f;
            panel.anchoredPosition = Vector2.zero;

            yield return new WaitForSecondsRealtime(Hold);

            for (float t = 0f; t < FadeOut; t += Time.unscaledDeltaTime)
            {
                group.alpha = 1f - t / FadeOut;
                yield return null;
            }
            root.SetActive(false);
            routine = null;
        }

        private void Build(TMP_FontAsset font)
        {
            root = new GameObject("UnlockBanner", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(transform, false);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.8f); // upper middle of the screen
            rootRect.sizeDelta = new Vector2(680f, 112f);
            group = root.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var body = NewImage("Panel", root.transform, PixelUI.Bevel(Fill, Light, Dark, Outline, 2));
            body.type = Image.Type.Sliced;
            body.pixelsPerUnitMultiplier = 0.25f;
            panel = body.rectTransform;
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = panel.offsetMax = Vector2.zero;

            icon = NewImage("Icon", panel, null);
            icon.preserveAspect = true;
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            icon.rectTransform.anchoredPosition = new Vector2(58f, 0f);
            icon.rectTransform.sizeDelta = new Vector2(64f, 64f);

            title = NewText(panel, font, 28f, Gold);
            title.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            title.rectTransform.anchorMax = Vector2.one;
            title.rectTransform.offsetMin = new Vector2(104f, 0f);
            title.rectTransform.offsetMax = new Vector2(-20f, -12f);

            subtitle = NewText(panel, font, 20f, Cream);
            subtitle.rectTransform.anchorMin = Vector2.zero;
            subtitle.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            subtitle.rectTransform.offsetMin = new Vector2(104f, 14f);
            subtitle.rectTransform.offsetMax = new Vector2(-20f, 0f);

            heart = NewImage("Heart", panel, null);
            heart.rectTransform.anchorMin = heart.rectTransform.anchorMax = new Vector2(0f, 0.25f);
            heart.rectTransform.anchoredPosition = new Vector2(118f, 7f);
            heart.rectTransform.sizeDelta = new Vector2(26f, 24f); // the heart art is 13x12
        }

        private static Image NewImage(string name, Transform parent, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI NewText(Transform parent, TMP_FontAsset font, float size, Color color)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Left;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.raycastTarget = false;
            return t;
        }
    }
}

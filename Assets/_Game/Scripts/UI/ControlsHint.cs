using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    // bottom left of the screen: an inventory button and a short list of the controls,
    // so nobody has to guess how to attack or open their inventory. ChestPanel adds this to the hud
    public class ControlsHint : MonoBehaviour
    {
        private const float Margin = 28f;
        private const float RowHeight = 30f;
        private const float RowGap = 6f;
        private const float PixelScale = 0.3f;

        private static readonly Color KeyFill = new Color(0.16f, 0.13f, 0.13f, 0.92f);
        private static readonly Color KeyLight = new Color(0.36f, 0.3f, 0.26f);
        private static readonly Color KeyDark = new Color(0.06f, 0.05f, 0.05f);
        private static readonly Color Outline = new Color(0.02f, 0.01f, 0.01f);
        private static readonly Color KeyText = new Color(1f, 0.82f, 0.45f);
        private static readonly Color LabelText = new Color(0.9f, 0.86f, 0.8f);

        // listed top to bottom
        private static readonly (string key, string action)[] Controls =
        {
            ("WASD", "Move"),
            ("RIGHT CLICK / SPACE", "Attack"),
            ("I / TAB", "Inventory"),
            ("ESC", "Pause"),
        };

        private TMP_FontAsset font;
        private Sprite keySprite;

        private void Awake()
        {
            var panel = GetComponent<ChestPanel>();
            font = panel != null ? panel.Font : null;
            keySprite = PixelUI.Bevel(KeyFill, KeyLight, KeyDark, Outline, 1);
            Build();
        }

        private void Build()
        {
            var root = new GameObject("ControlsHint", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = Vector2.zero; // bottom left
            rootRect.anchoredPosition = new Vector2(Margin, Margin);

            // rows stack upward from the bottom, so build the list in reverse
            float y = 0f;
            for (int i = Controls.Length - 1; i >= 0; i--)
            {
                AddRow(rootRect, Controls[i].key, Controls[i].action, y);
                y += RowHeight + RowGap;
            }

            AddInventoryButton(rootRect, y + 8f);
        }

        private void AddRow(RectTransform parent, string key, string action, float y)
        {
            float keyWidth = KeyWidth(key, 15f);
            var cap = NewImage("Key_" + action, parent, keySprite);
            Place(cap.rectTransform, new Vector2(0f, y), new Vector2(keyWidth, RowHeight));
            var capText = NewText(cap.transform, key, 15f, KeyText, TextAlignmentOptions.Center);
            Stretch(capText.rectTransform);

            var label = NewText(parent, action, 18f, LabelText, TextAlignmentOptions.Left);
            Place(label.rectTransform, new Vector2(keyWidth + 12f, y), new Vector2(220f, RowHeight));
        }

        private void AddInventoryButton(RectTransform parent, float y)
        {
            const float width = 230f, height = 46f;
            var body = NewImage("InventoryButton", parent, PixelUI.Bevel(new Color(0.24f, 0.19f, 0.17f, 0.95f), KeyLight, KeyDark, Outline, 2));
            body.pixelsPerUnitMultiplier = 0.25f;
            body.raycastTarget = true;
            Place(body.rectTransform, new Vector2(0f, y), new Vector2(width, height));

            var button = body.gameObject.AddComponent<Button>();
            button.targetGraphic = body;
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.95f, 0.85f);
            colors.pressedColor = new Color(0.8f, 0.75f, 0.7f);
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None }; // keeps space/enter from pressing it mid-fight
            button.onClick.AddListener(() =>
            {
                // don't leave the button selected, or the next space press (attack) would click it again
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                if (ChestPanel.Instance != null) ChestPanel.Instance.ToggleInventory();
            });

            var text = NewText(body.transform, "INVENTORY", 20f, KeyText, TextAlignmentOptions.Left);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(18f, 0f);

            var cap = NewImage("Key", body.transform, keySprite);
            cap.rectTransform.anchorMin = cap.rectTransform.anchorMax = cap.rectTransform.pivot = new Vector2(1f, 0.5f);
            cap.rectTransform.anchoredPosition = new Vector2(-12f, 0f);
            cap.rectTransform.sizeDelta = new Vector2(34f, 28f);
            var capText = NewText(cap.transform, "I", 15f, KeyText, TextAlignmentOptions.Center);
            Stretch(capText.rectTransform);
        }

        // wide enough for the text plus a little padding either side
        private static float KeyWidth(string key, float fontSize) => Mathf.Max(44f, key.Length * fontSize * 0.74f + 22f);

        private static Image NewImage(string name, Transform parent, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = PixelScale;
            img.raycastTarget = false;
            return img;
        }

        private TextMeshProUGUI NewText(Transform parent, string text, float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.raycastTarget = false;
            return t;
        }

        // position and size measured from the bottom left of the parent
        private static void Place(RectTransform r, Vector2 pos, Vector2 size)
        {
            r.anchorMin = r.anchorMax = r.pivot = Vector2.zero;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
        }

        private static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DungeonCrawler.Player;

namespace DungeonCrawler.UI
{
    // the popup you get when opening a chest, laid out like a minecraft chest screen:
    // chest slots on top, the player's inventory + hotbar underneath. escape closes it.
    // items will go in the slots later. the panel is built in code so the look lives in one place
    public class ChestPanel : MonoBehaviour
    {
        public static ChestPanel Instance { get; private set; }
        // frame the panel was closed on, so the pause menu doesn't also react to that same escape press
        public static int ClosedOnFrame { get; private set; } = -1;

        [SerializeField] private GameObject panelRoot; // old scene panel, replaced at runtime
        [SerializeField] private TMP_FontAsset titleFont;

        private const int Columns = 9;
        private const int ChestRows = 3;
        private const int InventoryRows = 3;
        private const float SlotSize = 56f;
        private const float SlotGap = 4f;
        private const float Pad = 22f;
        private const float PixelScale = 0.25f; // each bevel pixel draws as 4 ui pixels

        // dark stone / wood palette to match the dungeon
        private static readonly Color Backdrop = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color PanelFill = new Color(0.2f, 0.16f, 0.15f);
        private static readonly Color PanelLight = new Color(0.36f, 0.3f, 0.26f);
        private static readonly Color PanelDark = new Color(0.09f, 0.07f, 0.07f);
        private static readonly Color Outline = new Color(0.02f, 0.01f, 0.01f);
        private static readonly Color SlotFill = new Color(0.1f, 0.08f, 0.08f);
        private static readonly Color SlotDark = new Color(0.04f, 0.03f, 0.03f);
        private static readonly Color SlotLight = new Color(0.3f, 0.25f, 0.22f);
        private static readonly Color Label = new Color(0.9f, 0.82f, 0.68f);

        private PlayerController player;
        private GameObject root;

        public bool IsShowing => root != null && root.activeSelf;

        private void Awake()
        {
            Instance = this;
            if (panelRoot != null) Destroy(panelRoot);
            Build();
            root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(Loot.Chest chest, PlayerController opener)
        {
            player = opener;
            if (player != null) player.InputEnabled = false;
            root.SetActive(true);
        }

        public void Hide()
        {
            if (!IsShowing) return;
            root.SetActive(false);
            ClosedOnFrame = Time.frameCount;
            if (player != null) player.InputEnabled = true;
            player = null;
        }

        private void Update()
        {
            if (!IsShowing) return;
            bool esc = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            bool back = Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
            if (esc || back) Hide();
        }

        private void Build()
        {
            root = new GameObject("ChestScreen", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            Stretch((RectTransform)root.transform);

            var backdrop = NewImage("Backdrop", root.transform, PixelUI.Solid(Backdrop));
            Stretch(backdrop.rectTransform);
            backdrop.raycastTarget = true; // blocks clicks to anything behind

            float gridW = Columns * SlotSize + (Columns - 1) * SlotGap;
            float RowsHeight(int rows) => rows * SlotSize + (rows - 1) * SlotGap;
            const float labelH = 34f, hotbarGap = 14f;
            float height = Pad + labelH + RowsHeight(ChestRows) + 12f + labelH + RowsHeight(InventoryRows) + hotbarGap + SlotSize + Pad;
            float width = gridW + Pad * 2f;

            var panel = NewImage("Panel", root.transform, PixelUI.Bevel(PanelFill, PanelLight, PanelDark, Outline, 2));
            panel.type = Image.Type.Sliced;
            panel.pixelsPerUnitMultiplier = PixelScale;
            panel.rectTransform.sizeDelta = new Vector2(width, height);
            var content = panel.rectTransform;

            var slotSprite = PixelUI.Bevel(SlotFill, SlotDark, SlotLight, Color.clear, 2);

            // lay things out top to bottom
            float y = -Pad;
            AddLabel(content, "Chest", y, gridW);
            y -= labelH;
            y = AddGrid(content, slotSprite, ChestRows, y, gridW);

            y -= 12f;
            AddLabel(content, "Inventory", y, gridW);
            y -= labelH;
            y = AddGrid(content, slotSprite, InventoryRows, y, gridW);

            y -= hotbarGap;
            AddGrid(content, slotSprite, 1, y, gridW);
        }

        private void AddLabel(RectTransform parent, string text, float top, float width)
        {
            var go = new GameObject(text + "Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            if (titleFont != null) t.font = titleFont;
            t.text = text;
            t.fontSize = 22f;
            t.color = Label;
            t.alignment = TextAlignmentOptions.BottomLeft;
            t.raycastTarget = false;
            TopLeft(t.rectTransform, new Vector2(Pad + 2f, top), new Vector2(width, 30f));
        }

        // returns the y just below the grid
        private float AddGrid(RectTransform parent, Sprite slotSprite, int rows, float top, float gridW)
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    var slot = NewImage("Slot", parent, slotSprite);
                    slot.type = Image.Type.Sliced;
                    slot.pixelsPerUnitMultiplier = PixelScale;
                    TopLeft(slot.rectTransform,
                        new Vector2(Pad + c * (SlotSize + SlotGap), top - r * (SlotSize + SlotGap)),
                        new Vector2(SlotSize, SlotSize));
                }
            }
            return top - (rows * SlotSize + (rows - 1) * SlotGap);
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

        private static void TopLeft(RectTransform r, Vector2 pos, Vector2 size)
        {
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
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

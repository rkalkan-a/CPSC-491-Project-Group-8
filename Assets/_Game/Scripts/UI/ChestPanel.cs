using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DungeonCrawler.Core;
using DungeonCrawler.Player;

namespace DungeonCrawler.UI
{
    // the slot screens, laid out like minecraft's:
    //   chest screen     - chest slots on top, the player's inventory + hotbar underneath. opens when you walk into a chest
    //   inventory screen - the character and what they're wearing, then the inventory + hotbar. opens with I or Tab, or the inventory button
    // escape closes either one. items will go in the slots later. everything is built in code so the look lives in one place
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
        private GameObject chestScreen;
        private GameObject inventoryScreen;
        private Loot.Chest currentChest;
        private GameObject chestSwordIcon;   // the sword sitting in the chest's first slot
        private GameObject takeHint;
        private readonly List<GameObject> hotbarSwordIcons = new();  // one per screen, shown once the player has the sword
        private Image portrait;              // the character as they look right now, armor included
        private GameObject armorIcon;
        private TextMeshProUGUI armorText;

        public TMP_FontAsset Font => titleFont;
        public bool IsShowing => (chestScreen != null && chestScreen.activeSelf) || (inventoryScreen != null && inventoryScreen.activeSelf);

        private void Awake()
        {
            Instance = this;
            if (panelRoot != null) Destroy(panelRoot);

            // added before the screens are built so the screens draw over it
            if (GetComponent<ControlsHint>() == null) gameObject.AddComponent<ControlsHint>();
            if (GetComponent<UnlockBanner>() == null) gameObject.AddComponent<UnlockBanner>();

            chestScreen = BuildScreen("ChestScreen", true);
            inventoryScreen = BuildScreen("InventoryScreen", false);
            chestScreen.SetActive(false);
            inventoryScreen.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(Loot.Chest chest, PlayerController opener) => Open(chestScreen, opener, chest);

        public void ShowInventory(PlayerController opener) => Open(inventoryScreen, opener, null);

        // moves the sword from the open chest to the player. clicking the sword or pressing E does this
        public void TakeSword()
        {
            if (currentChest == null || !currentChest.TakeSword(player)) return;
            Refresh();
        }

        // what the inventory key and the on-screen inventory button both do
        public void ToggleInventory()
        {
            if (IsShowing)
            {
                Hide();
                return;
            }
            if (PauseMenu.IsPaused) return;
            var current = GameManager.Instance != null ? GameManager.Instance.Player : null;
            if (current == null || !current.InputEnabled) return; // mid room transition, dead, etc
            ShowInventory(current);
        }

        private void Open(GameObject screen, PlayerController opener, Loot.Chest chest)
        {
            Hide();
            player = opener;
            currentChest = chest;
            if (player != null) player.InputEnabled = false;
            screen.SetActive(true);
            Refresh();
        }

        // shows the sword wherever it currently is: still in the chest, or in the player's hotbar
        private void Refresh()
        {
            bool inChest = currentChest != null && currentChest.ContainsSword;
            chestSwordIcon.SetActive(inChest);
            takeHint.SetActive(inChest);

            var sword = player != null ? player.GetComponent<PlayerSword>() : null;
            bool carried = sword != null && sword.HasSword;
            foreach (var icon in hotbarSwordIcons) icon.SetActive(carried);

            var armor = player != null ? player.GetComponent<PlayerArmor>() : null;
            bool armored = armor != null && armor.HasArmor;
            armorIcon.SetActive(armored);
            if (armored)
            {
                int hearts = armor.ExtraHearts;
                armorText.text = $"{PlayerArmor.NameOf(armor.Tier)}\n+{hearts} heart{(hearts == 1 ? "" : "s")}";
                armorIcon.GetComponent<Image>().sprite = PixelUI.ArmorIcon(armor.Tier);
            }
            else armorText.text = PlayerArmor.NameOf(ArmorTier.None);

            var look = player != null ? player.GetComponent<PlayerSpriteDirection>() : null;
            portrait.sprite = look != null ? look.FrontSprite : null;
            portrait.enabled = portrait.sprite != null;
        }

        public void Hide()
        {
            if (!IsShowing) return;
            chestScreen.SetActive(false);
            inventoryScreen.SetActive(false);
            ClosedOnFrame = Time.frameCount;
            if (player != null) player.InputEnabled = true;
            player = null;
            currentChest = null;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            bool inventoryKey = keyboard != null && (keyboard.iKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame);
            if (inventoryKey)
            {
                ToggleInventory();
                return;
            }

            if (!IsShowing) return;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame) TakeSword();
            bool esc = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
            bool back = Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
            if (esc || back) Hide();
        }

        private GameObject BuildScreen(string name, bool withChest)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(transform, false);
            Stretch((RectTransform)root.transform);

            var backdrop = NewImage("Backdrop", root.transform, PixelUI.Solid(Backdrop));
            Stretch(backdrop.rectTransform);
            backdrop.raycastTarget = true; // blocks clicks to anything behind

            float gridW = Columns * SlotSize + (Columns - 1) * SlotGap;
            float RowsHeight(int rows) => rows * SlotSize + (rows - 1) * SlotGap;
            const float labelH = 34f, hotbarGap = 14f, sectionGap = 12f;
            float portraitSize = RowsHeight(2);
            float height = Pad + labelH + RowsHeight(InventoryRows) + hotbarGap + SlotSize + Pad;
            height += withChest ? labelH + RowsHeight(ChestRows) + sectionGap : labelH + portraitSize + sectionGap;
            float width = gridW + Pad * 2f;

            var panel = NewImage("Panel", root.transform, PixelUI.Bevel(PanelFill, PanelLight, PanelDark, Outline, 2));
            panel.type = Image.Type.Sliced;
            panel.pixelsPerUnitMultiplier = PixelScale;
            panel.rectTransform.sizeDelta = new Vector2(width, height);
            var content = panel.rectTransform;

            var slotSprite = PixelUI.Bevel(SlotFill, SlotDark, SlotLight, Color.clear, 2);

            // lay things out top to bottom
            float y = -Pad;
            if (withChest)
            {
                AddLabel(content, "Chest", y, gridW);
                takeHint = AddHint(content, "Click the sword or press E to take it", y, gridW);
                y -= labelH;
                y = AddGrid(content, slotSprite, ChestRows, y, gridW, out List<Image> chestSlots);
                Image chestSlot = chestSlots[0];
                y -= sectionGap;

                // the sword starts in the chest's first slot, and the slot is a button that takes it
                chestSwordIcon = AddSwordIcon(chestSlot.transform);
                chestSlot.raycastTarget = true;
                var take = chestSlot.gameObject.AddComponent<Button>();
                take.targetGraphic = chestSlot;
                take.navigation = new Navigation { mode = Navigation.Mode.None };
                take.onClick.AddListener(TakeSword);
            }
            else
            {
                // the character as they look now, and the armor they have on
                AddLabel(content, "Character", y, gridW);
                y -= labelH;

                var frame = NewImage("Portrait", content, slotSprite);
                frame.type = Image.Type.Sliced;
                frame.pixelsPerUnitMultiplier = PixelScale;
                TopLeft(frame.rectTransform, new Vector2(Pad, y), new Vector2(portraitSize, portraitSize));
                portrait = NewImage("Character", frame.transform, null);
                portrait.preserveAspect = true;
                Stretch(portrait.rectTransform);
                portrait.rectTransform.offsetMin = new Vector2(8f, 8f);
                portrait.rectTransform.offsetMax = new Vector2(-8f, -8f);

                float armorX = Pad + portraitSize + 16f;
                var armorSlot = NewImage("ArmorSlot", content, slotSprite);
                armorSlot.type = Image.Type.Sliced;
                armorSlot.pixelsPerUnitMultiplier = PixelScale;
                TopLeft(armorSlot.rectTransform, new Vector2(armorX, y), new Vector2(SlotSize, SlotSize));
                var icon = NewImage("ArmorIcon", armorSlot.transform, PixelUI.ArmorIcon());
                icon.rectTransform.sizeDelta = new Vector2(40f, 40f);
                armorIcon = icon.gameObject;

                var text = new GameObject("ArmorText", typeof(RectTransform), typeof(TextMeshProUGUI));
                text.transform.SetParent(content, false);
                armorText = text.GetComponent<TextMeshProUGUI>();
                if (titleFont != null) armorText.font = titleFont;
                armorText.fontSize = 16f;
                armorText.color = Label;
                armorText.alignment = TextAlignmentOptions.Left;
                armorText.raycastTarget = false;
                TopLeft(armorText.rectTransform, new Vector2(armorX + SlotSize + 12f, y), new Vector2(240f, SlotSize));

                y -= portraitSize + sectionGap;
            }

            AddLabel(content, "Inventory", y, gridW);
            y -= labelH;
            y = AddGrid(content, slotSprite, InventoryRows, y, gridW, out _);

            y -= hotbarGap;
            AddGrid(content, slotSprite, 1, y, gridW, out List<Image> hotbar);
            hotbarSwordIcons.Add(AddSwordIcon(hotbar[0].transform));
            return root;
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

        // small right aligned note on the same line as a section label
        private GameObject AddHint(RectTransform parent, string text, float top, float width)
        {
            var go = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            if (titleFont != null) t.font = titleFont;
            t.text = text;
            t.fontSize = 14f;
            t.color = new Color(Label.r, Label.g, Label.b, 0.7f);
            t.alignment = TextAlignmentOptions.BottomRight;
            t.raycastTarget = false;
            TopLeft(t.rectTransform, new Vector2(Pad, top), new Vector2(width - 2f, 30f));
            return go;
        }

        private static GameObject AddSwordIcon(Transform slot)
        {
            var icon = NewImage("SwordIcon", slot, PixelUI.Sword());
            icon.rectTransform.sizeDelta = new Vector2(18f, 44f); // the sword art is 9x22
            icon.rectTransform.localEulerAngles = new Vector3(0f, 0f, -45f);
            return icon.gameObject;
        }

        // returns the y just below the grid. slots are listed left to right, top row first
        private float AddGrid(RectTransform parent, Sprite slotSprite, int rows, float top, float gridW, out List<Image> slots)
        {
            slots = new List<Image>();
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    var slot = NewImage("Slot", parent, slotSprite);
                    slots.Add(slot);
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

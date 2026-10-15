using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DungeonCrawler.Core;
using DungeonCrawler.Player;

namespace DungeonCrawler.UI
{
    // shown when the player walks out of the last level's exit. pauses the game, shows the time and how many
    // enemies were defeated, and offers play again or back to the main menu. builds itself the first time it's needed
    public class LevelCompleteScreen : MonoBehaviour
    {
        public const string MainMenuScene = "Main Menu";

        private static readonly Color Backdrop = new Color(0f, 0f, 0f, 0.72f);
        private static readonly Color Fill = new Color(0.13f, 0.1f, 0.1f, 0.97f);
        private static readonly Color Light = new Color(0.86f, 0.66f, 0.28f);
        private static readonly Color Dark = new Color(0.4f, 0.28f, 0.1f);
        private static readonly Color Outline = new Color(0.02f, 0.01f, 0.01f);
        private static readonly Color Gold = new Color(1f, 0.84f, 0.3f);
        private static readonly Color Cream = new Color(0.93f, 0.88f, 0.8f);
        private static readonly Color ButtonFill = new Color(0.24f, 0.19f, 0.17f, 0.97f);
        private static readonly Color ButtonLight = new Color(0.36f, 0.3f, 0.26f);
        private static readonly Color ButtonDark = new Color(0.06f, 0.05f, 0.05f);

        public static LevelCompleteScreen Instance { get; private set; }
        public static bool IsShowing => Instance != null && Instance.root != null && Instance.root.activeSelf;

        public string TimeText => timeText != null ? timeText.text : "";
        public string EnemiesText => enemiesText != null ? enemiesText.text : "";
        public Button PlayAgainButton { get; private set; }
        public Button MainMenuButton { get; private set; }

        private GameObject root;
        private TextMeshProUGUI timeText;
        private TextMeshProUGUI enemiesText;
        private PlayerController player;
        private float timeScaleBefore = 1f;

        public static void Show(PlayerController player)
        {
            if (Instance == null)
            {
                var go = new GameObject("LevelCompleteScreen");
                Instance = go.AddComponent<LevelCompleteScreen>();
                Instance.Build();
            }
            Instance.Open(player);
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            if (root != null && root.activeSelf) Time.timeScale = timeScaleBefore; // never leave the game frozen
            Instance = null;
        }

        private void Open(PlayerController opener)
        {
            if (root.activeSelf) return;
            player = opener;
            if (player != null) player.InputEnabled = false;

            timeText.text = "Time  " + RunStats.FormatTime(RunStats.Elapsed);
            enemiesText.text = "Enemies defeated  " + RunStats.EnemiesDefeated;

            if (ChestPanel.Instance != null) ChestPanel.Instance.Hide();
            timeScaleBefore = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f; // enemies stop while the screen is up
            root.SetActive(true);
            EnsureEventSystem();
        }

        public void Close()
        {
            if (!root.activeSelf) return;
            root.SetActive(false);
            Time.timeScale = timeScaleBefore;
            if (player != null) player.InputEnabled = true;
            player = null;
        }

        public void PlayAgain()
        {
            Close();
            if (GameManager.Instance != null) GameManager.Instance.ResetRun();
            else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void GoToMainMenu()
        {
            Close();
            SceneManager.LoadScene(MainMenuScene);
        }

        // buttons need an event system, and a scene started on its own in a test might not have one
        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        }

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 450; // over the hud and the chest screen, under the room transition
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            TMP_FontAsset font = ChestPanel.Instance != null ? ChestPanel.Instance.Font : null;

            root = new GameObject("Root", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            Stretch((RectTransform)root.transform);

            var backdrop = NewImage("Backdrop", root.transform, PixelUI.Solid(Backdrop));
            Stretch(backdrop.rectTransform);
            backdrop.raycastTarget = true; // nothing behind it can be clicked

            var panel = NewImage("Panel", root.transform, PixelUI.Bevel(Fill, Light, Dark, Outline, 2));
            panel.type = Image.Type.Sliced;
            panel.pixelsPerUnitMultiplier = 0.25f;
            panel.rectTransform.sizeDelta = new Vector2(620f, 400f);

            var title = NewText(panel.transform, font, "LEVEL COMPLETE", 46f, Gold);
            Place(title.rectTransform, new Vector2(0f, 128f), new Vector2(560f, 64f));

            timeText = NewText(panel.transform, font, "", 26f, Cream);
            Place(timeText.rectTransform, new Vector2(0f, 46f), new Vector2(560f, 40f));
            enemiesText = NewText(panel.transform, font, "", 26f, Cream);
            Place(enemiesText.rectTransform, new Vector2(0f, 4f), new Vector2(560f, 40f));

            PlayAgainButton = NewButton(panel.transform, font, "PLAY AGAIN", new Vector2(-138f, -112f), PlayAgain);
            MainMenuButton = NewButton(panel.transform, font, "MAIN MENU", new Vector2(138f, -112f), GoToMainMenu);

            root.SetActive(false);
        }

        private Button NewButton(Transform parent, TMP_FontAsset font, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            var body = NewImage(label, parent, PixelUI.Bevel(ButtonFill, ButtonLight, ButtonDark, Outline, 2));
            body.type = Image.Type.Sliced;
            body.pixelsPerUnitMultiplier = 0.25f;
            body.raycastTarget = true;
            Place(body.rectTransform, pos, new Vector2(240f, 64f));

            var button = body.gameObject.AddComponent<Button>();
            button.targetGraphic = body;
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.95f, 0.85f);
            colors.pressedColor = new Color(0.8f, 0.75f, 0.7f);
            button.colors = colors;
            button.onClick.AddListener(onClick);

            var text = NewText(body.transform, font, label, 24f, Gold);
            Stretch(text.rectTransform);
            return button;
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

        private static TextMeshProUGUI NewText(Transform parent, TMP_FontAsset font, string text, float size, Color color)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.raycastTarget = false;
            return t;
        }

        private static void Place(RectTransform r, Vector2 pos, Vector2 size)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
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

using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DungeonCrawler.UI;

namespace DungeonCrawler.EditorTools
{
    // Tools > Dungeon > Build Main Menu. makes the title screen with start/options/exit and the options sliders
    public static class MainMenuBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/MainMenu.unity";
        private const string LevelScenePath = "Assets/_Game/Scenes/Level_01.unity";

        private static readonly Color BackgroundColor = new Color(0.05f, 0.035f, 0.06f);
        private static readonly Color ButtonColor = new Color(0.22f, 0.12f, 0.15f);
        private static readonly Color ButtonHover = new Color(0.4f, 0.16f, 0.2f);
        private static readonly Color AccentColor = new Color(0.75f, 0.12f, 0.18f);
        private static readonly Color TextColor = new Color(0.9f, 0.86f, 0.86f);
        private static readonly Color SliderTrack = new Color(0.16f, 0.12f, 0.14f);

        [MenuItem("Tools/Dungeon/Build Main Menu", priority = 2)]
        public static void BuildMainMenu()
        {
            if (File.Exists(ScenePath))
            {
                if (!EditorUtility.DisplayDialog("MainMenu already exists", "Overwrite MainMenu.unity?", "Overwrite", "Cancel"))
                    return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = BackgroundColor;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<AudioListener>();

            // needed for buttons to get clicks. uses the new input system module since that's what the project is on
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGo = new GameObject("MenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var bg = MakeImage("Background", canvasGo.transform, BackgroundColor);
            Stretch((RectTransform)bg.transform);

            var menu = canvasGo.AddComponent<MainMenu>();

            // ---- main panel ----
            var mainPanel = MakePanel("MainPanel", canvasGo.transform);
            MakeText("Title", mainPanel.transform, "DUNGEON", 110, new Vector2(0f, 260f), new Vector2(1000f, 140f), TextAnchor.MiddleCenter);
            MakeText("Subtitle", mainPanel.transform, "cpsc 491 group 8", 28, new Vector2(0f, 170f), new Vector2(600f, 40f), TextAnchor.MiddleCenter, 0.6f);

            var startBtn = MakeButton("StartButton", mainPanel.transform, "Start Game", new Vector2(0f, 20f));
            var optionsBtn = MakeButton("OptionsButton", mainPanel.transform, "Options", new Vector2(0f, -80f));
            var exitBtn = MakeButton("ExitButton", mainPanel.transform, "Exit Game", new Vector2(0f, -180f));

            UnityEventTools.AddPersistentListener(startBtn.onClick, menu.StartGame);
            UnityEventTools.AddPersistentListener(optionsBtn.onClick, menu.ShowOptions);
            UnityEventTools.AddPersistentListener(exitBtn.onClick, menu.QuitGame);

            // ---- options panel ----
            var optionsPanel = MakePanel("OptionsPanel", canvasGo.transform);
            MakeText("OptionsTitle", optionsPanel.transform, "Options", 72, new Vector2(0f, 330f), new Vector2(800f, 100f), TextAnchor.MiddleCenter);

            var options = optionsPanel.AddComponent<OptionsMenu>();
            float y = 190f;
            const float rowGap = 85f;

            var volume = MakeSliderRow(optionsPanel.transform, "Volume", 0f, 1f, false, y, out var volumeValue); y -= rowGap;
            var gore = MakeSliderRow(optionsPanel.transform, "Gore", 0f, 2f, true, y, out var goreValue); y -= rowGap;
            var textSize = MakeSliderRow(optionsPanel.transform, "Text Size", 0.8f, 1.6f, false, y, out var textValue); y -= rowGap;
            var brightness = MakeSliderRow(optionsPanel.transform, "Brightness", 0.5f, 1.5f, false, y, out var brightValue); y -= rowGap;
            var sensitivity = MakeSliderRow(optionsPanel.transform, "Sensitivity", 0.5f, 2f, false, y, out var sensValue); y -= rowGap;

            DungeonBootstrapper.SetRef(options, "volumeSlider", volume);
            DungeonBootstrapper.SetRef(options, "goreSlider", gore);
            DungeonBootstrapper.SetRef(options, "textSizeSlider", textSize);
            DungeonBootstrapper.SetRef(options, "brightnessSlider", brightness);
            DungeonBootstrapper.SetRef(options, "sensitivitySlider", sensitivity);
            DungeonBootstrapper.SetRef(options, "volumeValue", volumeValue);
            DungeonBootstrapper.SetRef(options, "goreValue", goreValue);
            DungeonBootstrapper.SetRef(options, "textSizeValue", textValue);
            DungeonBootstrapper.SetRef(options, "brightnessValue", brightValue);
            DungeonBootstrapper.SetRef(options, "sensitivityValue", sensValue);

            var backBtn = MakeButton("BackButton", optionsPanel.transform, "Back", new Vector2(0f, y - 30f));
            UnityEventTools.AddPersistentListener(backBtn.onClick, menu.ShowMain);

            DungeonBootstrapper.SetRef(menu, "mainPanel", mainPanel);
            DungeonBootstrapper.SetRef(menu, "optionsPanel", optionsPanel);
            optionsPanel.SetActive(false);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            DungeonBootstrapper.AddSceneToBuildSettings(ScenePath, first: true);
            if (File.Exists(LevelScenePath)) DungeonBootstrapper.AddSceneToBuildSettings(LevelScenePath);
            AssetDatabase.SaveAssets();

            Selection.activeObject = canvasGo;
            Debug.Log("Built MainMenu. Press play here to test the menu, Start Game loads Level_01.");
        }

        private static GameObject MakePanel(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);
            return go;
        }

        private static Image MakeImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        private static Text MakeText(string name, Transform parent, string content, int size, Vector2 pos, Vector2 sizeDelta, TextAnchor anchor, float alpha = 1f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(ScaledText));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = sizeDelta;
            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = DungeonBootstrapper.DefaultFont();
            text.fontSize = size;
            text.alignment = anchor;
            text.color = new Color(TextColor.r, TextColor.g, TextColor.b, alpha);
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button MakeButton(string name, Transform parent, string label, Vector2 pos)
        {
            var img = MakeImage(name, parent, ButtonColor);
            var rect = (RectTransform)img.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(420f, 80f);

            var button = img.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.6f, 1.3f, 1.3f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            colors.selectedColor = new Color(1.4f, 1.2f, 1.2f);
            button.colors = colors;

            var text = MakeText("Label", img.transform, label, 40, Vector2.zero, new Vector2(400f, 70f), TextAnchor.MiddleCenter);
            return button;
        }

        private static Slider MakeSliderRow(Transform parent, string label, float min, float max, bool whole, float y, out Text valueText)
        {
            var row = new GameObject(label.Replace(" ", "") + "Row", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rowRect = (RectTransform)row.transform;
            rowRect.anchorMin = rowRect.anchorMax = new Vector2(0.5f, 0.5f);
            rowRect.anchoredPosition = new Vector2(0f, y);
            rowRect.sizeDelta = new Vector2(900f, 60f);

            MakeText("Label", row.transform, label, 34, new Vector2(-300f, 0f), new Vector2(260f, 50f), TextAnchor.MiddleLeft);
            valueText = MakeText("Value", row.transform, "", 30, new Vector2(370f, 0f), new Vector2(140f, 50f), TextAnchor.MiddleRight, 0.8f);

            // slider built by hand: background, fill, handle
            var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(row.transform, false);
            var sliderRect = (RectTransform)sliderGo.transform;
            sliderRect.anchorMin = sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRect.anchoredPosition = new Vector2(50f, 0f);
            sliderRect.sizeDelta = new Vector2(440f, 30f);

            var track = MakeImage("Background", sliderGo.transform, SliderTrack);
            var trackRect = (RectTransform)track.transform;
            trackRect.anchorMin = new Vector2(0f, 0.3f);
            trackRect.anchorMax = new Vector2(1f, 0.7f);
            trackRect.offsetMin = trackRect.offsetMax = Vector2.zero;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fillAreaRect = (RectTransform)fillArea.transform;
            fillAreaRect.anchorMin = new Vector2(0f, 0.3f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.7f);
            fillAreaRect.offsetMin = new Vector2(8f, 0f);
            fillAreaRect.offsetMax = new Vector2(-8f, 0f);

            var fill = MakeImage("Fill", fillArea.transform, AccentColor);
            var fillRect = (RectTransform)fill.transform;
            fillRect.sizeDelta = new Vector2(16f, 0f);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderGo.transform, false);
            var handleAreaRect = (RectTransform)handleArea.transform;
            Stretch(handleAreaRect);
            handleAreaRect.offsetMin = new Vector2(12f, 0f);
            handleAreaRect.offsetMax = new Vector2(-12f, 0f);

            var handle = MakeImage("Handle", handleArea.transform, TextColor);
            var handleRect = (RectTransform)handle.transform;
            handleRect.sizeDelta = new Vector2(26f, 0f);

            var slider = sliderGo.GetComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = whole;
            slider.value = whole ? Mathf.Round((min + max) * 0.5f) : (min + max) * 0.5f;

            return slider;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}

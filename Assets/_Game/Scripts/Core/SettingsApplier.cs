using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.Core
{
    // lives across scenes and applies the settings that are global (volume, brightness).
    // creates itself on startup so no scene needs to have it
    public class SettingsApplier : MonoBehaviour
    {
        private static SettingsApplier instance;
        private Image overlay;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (instance != null) return;
            var go = new GameObject("SettingsApplier");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SettingsApplier>();
        }

        private void Awake()
        {
            BuildOverlay();
            Apply();
            GameSettings.Changed += Apply;
        }

        private void OnDestroy()
        {
            GameSettings.Changed -= Apply;
            if (instance == this) instance = null;
        }

        // full screen image we tint to fake brightness. dark = black with alpha, bright = white with alpha
        private void BuildOverlay()
        {
            var canvasGo = new GameObject("BrightnessOverlay", typeof(RectTransform), typeof(Canvas));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var imgGo = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
            imgGo.transform.SetParent(canvasGo.transform, false);
            var rect = (RectTransform)imgGo.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            overlay = imgGo.GetComponent<Image>();
            overlay.raycastTarget = false;
        }

        private void Apply()
        {
            AudioListener.volume = GameSettings.Volume;

            float b = GameSettings.Brightness;
            if (b < 1f) overlay.color = new Color(0f, 0f, 0f, 1f - b);
            else overlay.color = new Color(1f, 1f, 1f, (b - 1f) * 0.4f);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using DungeonCrawler.Core;

namespace DungeonCrawler.UI
{
    // hooks the option sliders up to GameSettings
    public class OptionsMenu : MonoBehaviour
    {
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Slider goreSlider;
        [SerializeField] private Slider textSizeSlider;
        [SerializeField] private Slider brightnessSlider;
        [SerializeField] private Slider sensitivitySlider;

        [SerializeField] private Text volumeValue;
        [SerializeField] private Text goreValue;
        [SerializeField] private Text textSizeValue;
        [SerializeField] private Text brightnessValue;
        [SerializeField] private Text sensitivityValue;

        private void Awake()
        {
            if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(v => { GameSettings.Volume = v; RefreshLabels(); });
            if (goreSlider != null) goreSlider.onValueChanged.AddListener(v => { GameSettings.Gore = Mathf.RoundToInt(v); RefreshLabels(); });
            if (textSizeSlider != null) textSizeSlider.onValueChanged.AddListener(v => { GameSettings.TextScale = v; RefreshLabels(); });
            if (brightnessSlider != null) brightnessSlider.onValueChanged.AddListener(v => { GameSettings.Brightness = v; RefreshLabels(); });
            if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(v => { GameSettings.Sensitivity = v; RefreshLabels(); });
        }

        private void OnEnable()
        {
            // load saved values without firing the listeners
            if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(GameSettings.Volume);
            if (goreSlider != null) goreSlider.SetValueWithoutNotify(GameSettings.Gore);
            if (textSizeSlider != null) textSizeSlider.SetValueWithoutNotify(GameSettings.TextScale);
            if (brightnessSlider != null) brightnessSlider.SetValueWithoutNotify(GameSettings.Brightness);
            if (sensitivitySlider != null) sensitivitySlider.SetValueWithoutNotify(GameSettings.Sensitivity);
            RefreshLabels();
        }

        private void OnDisable()
        {
            GameSettings.Save();
        }

        private void RefreshLabels()
        {
            if (volumeValue != null) volumeValue.text = Mathf.RoundToInt(GameSettings.Volume * 100f) + "%";
            if (goreValue != null) goreValue.text = GameSettings.GoreName(GameSettings.Gore);
            if (textSizeValue != null) textSizeValue.text = Mathf.RoundToInt(GameSettings.TextScale * 100f) + "%";
            if (brightnessValue != null) brightnessValue.text = Mathf.RoundToInt(GameSettings.Brightness * 100f) + "%";
            if (sensitivityValue != null) sensitivityValue.text = GameSettings.Sensitivity.ToString("0.0") + "x";
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using DungeonCrawler.Core;

namespace DungeonCrawler.UI
{
    // put on any Text so the text size option scales it
    [RequireComponent(typeof(Text))]
    public class ScaledText : MonoBehaviour
    {
        private Text text;
        private int baseSize;

        private void Awake()
        {
            text = GetComponent<Text>();
            baseSize = text.fontSize;
        }

        private void OnEnable()
        {
            GameSettings.Changed += Apply;
            Apply();
        }

        private void OnDisable()
        {
            GameSettings.Changed -= Apply;
        }

        private void Apply()
        {
            if (text != null) text.fontSize = Mathf.RoundToInt(baseSize * GameSettings.TextScale);
        }
    }
}

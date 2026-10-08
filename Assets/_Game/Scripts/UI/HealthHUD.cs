using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DungeonCrawler.Combat;
using DungeonCrawler.Core;

namespace DungeonCrawler.UI
{
    // hearts in the top left. 1 heart = 2 hp so you can have half hearts like isaac
    public class HealthHUD : MonoBehaviour
    {
        [SerializeField] private RectTransform heartsRoot;
        [SerializeField] private Text deathText;
        [SerializeField] private Color fullColor = new Color(0.9f, 0.15f, 0.2f);
        [SerializeField] private Color emptyColor = new Color(0.25f, 0.08f, 0.1f);
        [SerializeField, Min(1)] private int heartPixelScale = 4; // each art pixel becomes this many ui pixels
        [SerializeField, Min(0f)] private float heartGap = 6f;
        [SerializeField] private Vector2 screenMargin = new Vector2(32f, 28f);

        private readonly List<Image> heartFills = new();
        private readonly List<GameObject> heartObjects = new();
        private Health tracked;
        private Sprite fullHeart;
        private Sprite emptyHeart;

        private void Awake()
        {
            fullHeart = PixelUI.Heart(fullColor, 0.55f);
            emptyHeart = PixelUI.Heart(emptyColor, 0.12f);

            if (heartsRoot != null)
            {
                heartsRoot.anchorMin = heartsRoot.anchorMax = new Vector2(0f, 1f);
                heartsRoot.pivot = new Vector2(0f, 1f);
                heartsRoot.anchoredPosition = new Vector2(screenMargin.x, -screenMargin.y);
            }
        }

        private void OnEnable()
        {
            GameEvents.RunStarted += OnRunStarted;
            GameEvents.PlayerDied += OnPlayerDied;
            if (deathText != null) deathText.enabled = false;

            // in case the run already started before we enabled
            if (GameManager.Instance != null && GameManager.Instance.PlayerHealth != null)
                Track(GameManager.Instance.PlayerHealth);
        }

        private void OnDisable()
        {
            GameEvents.RunStarted -= OnRunStarted;
            GameEvents.PlayerDied -= OnPlayerDied;
            Untrack();
        }

        private void OnRunStarted()
        {
            Track(GameManager.Instance != null ? GameManager.Instance.PlayerHealth : null);
        }

        private void OnPlayerDied()
        {
            if (deathText != null) deathText.enabled = true;
        }

        private void Track(Health health)
        {
            Untrack();
            tracked = health;
            if (tracked == null) return;
            tracked.Changed += OnHealthChanged;
            Rebuild(tracked.Current, tracked.Max);
        }

        private void Untrack()
        {
            if (tracked != null) tracked.Changed -= OnHealthChanged;
            tracked = null;
        }

        private void OnHealthChanged(int current, int max) => Rebuild(current, max);

        private void Rebuild(int current, int max)
        {
            if (heartsRoot == null) return;

            int heartCount = Mathf.CeilToInt(max / 2f);
            while (heartObjects.Count < heartCount) MakeHeart(heartObjects.Count);

            for (int i = 0; i < heartObjects.Count; i++)
            {
                bool show = i < heartCount;
                heartObjects[i].SetActive(show);
                if (!show) continue;

                int hpInThisHeart = Mathf.Clamp(current - i * 2, 0, 2);
                heartFills[i].fillAmount = hpInThisHeart * 0.5f;
            }
        }

        private void MakeHeart(int index)
        {
            var size = (Vector2)PixelUI.HeartSize * heartPixelScale;

            var bg = new GameObject("Heart" + index, typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(heartsRoot, false);
            var bgRect = (RectTransform)bg.transform;
            bgRect.anchorMin = bgRect.anchorMax = new Vector2(0f, 1f);
            bgRect.pivot = new Vector2(0f, 1f);
            bgRect.sizeDelta = size;
            bgRect.anchoredPosition = new Vector2(index * (size.x + heartGap), 0f);
            var bgImage = bg.GetComponent<Image>();
            bgImage.sprite = emptyHeart;
            bgImage.raycastTarget = false;

            // full heart drawn on top, filled left to right so 1 hp shows as half a heart
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(bg.transform, false);
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            var fillImage = fill.GetComponent<Image>();
            fillImage.sprite = fullHeart;
            fillImage.raycastTarget = false;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 1f;

            heartObjects.Add(bg);
            heartFills.Add(fillImage);
        }
    }
}

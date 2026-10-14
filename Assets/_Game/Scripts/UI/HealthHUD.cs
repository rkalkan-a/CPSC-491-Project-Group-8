using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DungeonCrawler.Combat;
using DungeonCrawler.Core;
using DungeonCrawler.Player;

namespace DungeonCrawler.UI
{
    // hearts in the top left. 1 heart = Health.HpPerHeart hp, so a heart can be a quarter, half or three quarters full
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
        private Sprite armorFullHeart;  // hearts added by armor are drawn yellow so they stand out
        private Sprite armorEmptyHeart;
        private int shownHearts;

        private void Awake()
        {
            fullHeart = PixelUI.Heart(fullColor, 0.55f);
            emptyHeart = PixelUI.Heart(emptyColor, 0.12f);
            armorFullHeart = PixelUI.Heart(PlayerArmor.HeartColor, 0.55f);
            armorEmptyHeart = PixelUI.Heart(new Color(0.34f, 0.26f, 0.06f), 0.12f);

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

            int heartCount = Mathf.CeilToInt(max / (float)Health.HpPerHeart);
            while (heartObjects.Count < heartCount) MakeHeart(heartObjects.Count);

            // hearts that come from armor sit at the end of the row and are drawn yellow
            var armor = tracked != null ? tracked.GetComponent<PlayerArmor>() : null;
            int armorHearts = armor != null ? armor.ExtraHearts : 0;

            for (int i = 0; i < heartObjects.Count; i++)
            {
                bool show = i < heartCount;
                heartObjects[i].SetActive(show);
                if (!show) continue;

                bool fromArmor = i >= heartCount - armorHearts;
                heartObjects[i].GetComponent<Image>().sprite = fromArmor ? armorEmptyHeart : emptyHeart;
                heartFills[i].sprite = fromArmor ? armorFullHeart : fullHeart;

                int hpInThisHeart = Mathf.Clamp(current - i * Health.HpPerHeart, 0, Health.HpPerHeart);
                heartFills[i].fillAmount = hpInThisHeart / (float)Health.HpPerHeart;

                // a heart that wasn't there a moment ago pops in so the player notices it
                if (shownHearts > 0 && i >= shownHearts && isActiveAndEnabled) StartCoroutine(Pop(heartObjects[i].transform));
            }
            shownHearts = heartCount;
        }

        private static IEnumerator Pop(Transform heart)
        {
            const float duration = 0.45f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                if (heart == null) yield break;
                float k = t / duration;
                heart.localScale = Vector3.one * (1f + 0.7f * Mathf.Sin(k * Mathf.PI) * (1f - k)); // swells, then settles
                yield return null;
            }
            if (heart != null) heart.localScale = Vector3.one;
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

            // full heart drawn on top, filled left to right so a partly lost heart shows partly filled
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

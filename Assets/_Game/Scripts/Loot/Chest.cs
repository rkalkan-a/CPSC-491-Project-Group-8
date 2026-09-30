using UnityEngine;
using DungeonCrawler.Player;
using DungeonCrawler.UI;
using DungeonCrawler.Audio;

namespace DungeonCrawler.Loot
{
    // walk into it and it pops open and shows the chest panel. nothing inside yet, that's the loot phase
    [RequireComponent(typeof(Collider2D))]
    public class Chest : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite openSprite;
        [SerializeField] private AudioClip openSound;

        public bool IsOpen { get; private set; }
        public SpriteRenderer Sprite => sprite;

        private void Awake()
        {
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null && closedSprite != null) sprite.sprite = closedSprite;
            if (GetComponent<ChestGlow>() == null) gameObject.AddComponent<ChestGlow>();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var player = collision.collider.GetComponentInParent<PlayerController>();
            if (player == null) return;
            Open(player);
        }

        public void Open(PlayerController player)
        {
            if (!IsOpen)
            {
                IsOpen = true;

                if (sprite != null && openSprite != null)
                    sprite.sprite = openSprite;

                SFXManager.Instance.PlaySFX(openSound);
            }

            if (ChestPanel.Instance != null)
                ChestPanel.Instance.Show(this, player);
        }
    }
}

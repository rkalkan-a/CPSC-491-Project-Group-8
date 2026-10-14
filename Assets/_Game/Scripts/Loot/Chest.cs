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
        [SerializeField] private bool containsSword = true; // the first chest is where the player gets their sword

        public bool IsOpen { get; private set; }
        public SpriteRenderer Sprite => sprite;
        public bool ContainsSword => containsSword;

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

                // no sound manager in the scene shouldn't stop the chest from opening
                if (SFXManager.Instance != null && openSound != null) SFXManager.Instance.PlaySFX(openSound);
            }

            if (ChestPanel.Instance != null)
                ChestPanel.Instance.Show(this, player);
        }

        // gives the sword to the player if it's still in here. returns false if there was nothing to take
        public bool TakeSword(PlayerController player)
        {
            if (!containsSword || player == null) return false;
            var sword = player.GetComponent<PlayerSword>();
            if (sword == null) return false;

            containsSword = false;
            sword.Equip();
            return true;
        }
    }
}

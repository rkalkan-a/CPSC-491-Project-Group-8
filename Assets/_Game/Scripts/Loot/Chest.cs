using UnityEngine;
using DungeonCrawler.Player;
using DungeonCrawler.UI;
using DungeonCrawler.Audio;

namespace DungeonCrawler.Loot
{
    // walk into it, click it, or press E next to it and it pops open and shows the chest panel.
    // holds the sword, a piece of armor, or both
    [RequireComponent(typeof(Collider2D))]
    public class Chest : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite openSprite;
        [SerializeField] private AudioClip openSound;
        [SerializeField] private bool containsSword = true; // the first chest is where the player gets their sword
        [SerializeField] private ArmorTier armor = ArmorTier.None; // the leather armor chest in the first dungeon room

        public bool IsOpen { get; private set; }
        public SpriteRenderer Sprite => sprite;
        public bool ContainsSword { get => containsSword; set => containsSword = value; }
        public ArmorTier Armor { get => armor; set => armor = value; }
        public bool ContainsArmor => armor != ArmorTier.None;
        public bool IsEmpty => !containsSword && !ContainsArmor;

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

        // puts the armor in here on the player. returns false if there's none or it's not better than what they have on
        public bool TakeArmor(PlayerController player)
        {
            if (!ContainsArmor || player == null) return false;
            var worn = player.GetComponent<PlayerArmor>();
            if (worn == null || !worn.Equip(armor)) return false;

            armor = ArmorTier.None;
            return true;
        }

        // takes whatever is in the chest, sword first
        public bool TakeItem(PlayerController player) => TakeSword(player) || TakeArmor(player);
    }
}

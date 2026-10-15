using UnityEngine;
using DungeonCrawler.Combat;
using DungeonCrawler.UI;
using DungeonCrawler.Audio;

namespace DungeonCrawler.Player
{
    // armor goes up in tiers. a better tier replaces the one being worn
    public enum ArmorTier { None, Leather, ChainMail }

    // armor the player picks up from enemy drops. each tier adds extra hearts and switches the
    // character over to that tier's sprites. leather drops in the first dungeon, chain mail in the maze
    [RequireComponent(typeof(PlayerController))]
    public class PlayerArmor : MonoBehaviour
    {
        [SerializeField, Min(0)] private int leatherHearts = 1;
        [SerializeField, Min(0)] private int chainMailHearts = 2;

        // the colour of the hearts armor adds, on the hud and in the unlock banner
        public static readonly Color HeartColor = new Color(1f, 0.82f, 0.2f);

        public ArmorTier Tier { get; private set; } = ArmorTier.None;
        public bool HasArmor => Tier != ArmorTier.None;
        public int ExtraHearts => HeartsFor(Tier);

        private Health health;
        private PlayerSpriteDirection sprites;

        private void Awake()
        {
            health = GetComponent<Health>();
            sprites = GetComponent<PlayerSpriteDirection>();
        }

        public int HeartsFor(ArmorTier tier) => tier switch
        {
            ArmorTier.Leather => leatherHearts,
            ArmorTier.ChainMail => chainMailHearts,
            _ => 0,
        };

        public static string NameOf(ArmorTier tier) => tier switch
        {
            ArmorTier.Leather => "Leather Armor",
            ArmorTier.ChainMail => "Chain Mail Armor",
            _ => "No armor",
        };

        // the first armor the player finds
        public void Equip() => Equip(ArmorTier.Leather);

        // puts on this tier if it's better than what's being worn. returns false if it wasn't an upgrade
        public bool Equip(ArmorTier tier)
        {
            if (tier <= Tier) return false;
            GetComponent<PlayerAudio>()?.PlayArmorEquip();

            int addedHearts = HeartsFor(tier) - HeartsFor(Tier);
            Tier = tier; // set before the health changes, so the hud already knows the new hearts are armor hearts
            if (health != null) health.SetMax(health.Max + addedHearts * Health.HpPerHeart); // also heals the new hearts
            if (sprites != null) sprites.Tier = tier;

            if (UnlockBanner.Instance != null)
            {
                string hearts = addedHearts == 1 ? "+1 HEART" : $"+{addedHearts} HEARTS";
                UnlockBanner.Instance.Show(PixelUI.ArmorIcon(tier), $"YOU UNLOCKED {NameOf(tier).ToUpperInvariant()}", hearts, HeartColor);
            }
            return true;
        }
    }
}

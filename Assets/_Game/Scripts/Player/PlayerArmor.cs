using UnityEngine;
using DungeonCrawler.Combat;
using DungeonCrawler.UI;

namespace DungeonCrawler.Player
{
    // leather armor the player picks up from an enemy drop. wearing it adds one extra heart
    // and switches the character over to the armored sprites
    [RequireComponent(typeof(PlayerController))]
    public class PlayerArmor : MonoBehaviour
    {
        [SerializeField, Min(0)] private int extraHearts = 1;

        // the colour of the heart armor adds, on the hud and in the unlock banner
        public static readonly Color HeartColor = new Color(1f, 0.82f, 0.2f);

        public bool HasArmor { get; private set; }
        public int ExtraHearts => HasArmor ? extraHearts : 0;
        public int ExtraHealth => extraHearts * Health.HpPerHeart;

        private Health health;
        private PlayerSpriteDirection sprites;

        private void Awake()
        {
            health = GetComponent<Health>();
            sprites = GetComponent<PlayerSpriteDirection>();
        }

        // called when the player picks the armor up. does nothing the second time
        public void Equip()
        {
            if (HasArmor) return;
            HasArmor = true;
            if (health != null) health.SetMax(health.Max + ExtraHealth); // also heals the new heart
            if (sprites != null) sprites.Armored = true;

            if (UnlockBanner.Instance != null)
                UnlockBanner.Instance.Show(PixelUI.ArmorIcon(), "YOU UNLOCKED LEATHER ARMOR", "+1 HEART", HeartColor);
        }
    }
}

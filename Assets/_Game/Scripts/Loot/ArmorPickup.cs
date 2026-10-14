using UnityEngine;
using DungeonCrawler.Player;

namespace DungeonCrawler.Loot
{
    // armor on the floor. the player puts it on by walking over it, clicking it or pressing E, if it's better than what they're wearing
    [RequireComponent(typeof(Collider2D))]
    public class ArmorPickup : MonoBehaviour
    {
        [SerializeField] private ArmorTier tier = ArmorTier.Leather;

        public ArmorTier Tier { get => tier; set => tier = value; }

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null) Grab(player);
        }

        // walking over it, clicking it, or pressing E next to it all end up here
        public bool Grab(PlayerController player)
        {
            var armor = player != null ? player.GetComponent<PlayerArmor>() : null;
            if (armor == null || !armor.Equip(tier)) return false;

            Destroy(gameObject);
            return true;
        }
    }
}

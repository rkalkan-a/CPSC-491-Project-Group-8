using UnityEngine;
using DungeonCrawler.Player;

namespace DungeonCrawler.Loot
{
    // leather armor on the floor. the player puts it on by walking over it
    [RequireComponent(typeof(Collider2D))]
    public class ArmorPickup : MonoBehaviour
    {
        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;

            var armor = player.GetComponent<PlayerArmor>();
            if (armor == null || armor.HasArmor) return;

            armor.Equip();
            Destroy(gameObject);
        }
    }
}

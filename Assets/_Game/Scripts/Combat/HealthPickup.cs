using UnityEngine;
using DungeonCrawler.Player;

namespace DungeonCrawler.Combat
{
    // heart on the floor, heals the player when they walk over it
    [RequireComponent(typeof(Collider2D))]
    public class HealthPickup : MonoBehaviour
    {
        [SerializeField, Min(1)] private int healAmount = Health.HpPerHeart; // one full heart
        [SerializeField] private bool onlyIfHurt = true;

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
            var health = player != null ? player.GetComponent<Health>() : null;
            if (health == null || health.IsDead) return false;
            if (onlyIfHurt && health.Current >= health.Max) return false;

            health.Heal(healAmount);
            Destroy(gameObject);
            return true;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}

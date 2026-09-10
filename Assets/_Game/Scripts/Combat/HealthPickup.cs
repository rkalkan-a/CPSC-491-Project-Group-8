using UnityEngine;
using DungeonCrawler.Player;

namespace DungeonCrawler.Combat
{
    // heart on the floor, heals the player when they walk over it
    [RequireComponent(typeof(Collider2D))]
    public class HealthPickup : MonoBehaviour
    {
        [SerializeField, Min(1)] private int healAmount = 2;
        [SerializeField] private bool onlyIfHurt = true;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;

            var health = player.GetComponent<Health>();
            if (health == null || health.IsDead) return;
            if (onlyIfHurt && health.Current >= health.Max) return;

            health.Heal(healAmount);
            Destroy(gameObject);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}

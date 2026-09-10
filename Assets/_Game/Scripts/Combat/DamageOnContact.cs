using UnityEngine;
using DungeonCrawler.Player;

namespace DungeonCrawler.Combat
{
    // put this on spikes, enemies, projectiles etc. anything with Health that touches it gets hurt
    [RequireComponent(typeof(Collider2D))]
    public class DamageOnContact : MonoBehaviour
    {
        [SerializeField, Min(0)] private int damage = 1;
        [SerializeField] private bool onlyHurtsPlayer = true;
        [SerializeField, Min(0f)] private float knockback = 6f;

        public int Damage { get => damage; set => damage = Mathf.Max(0, value); }

        private void OnTriggerEnter2D(Collider2D other) => TryHit(other);
        private void OnTriggerStay2D(Collider2D other) => TryHit(other); // keeps hurting if you stand in it
        private void OnCollisionEnter2D(Collision2D collision) => TryHit(collision.collider);
        private void OnCollisionStay2D(Collision2D collision) => TryHit(collision.collider);

        private void TryHit(Collider2D other)
        {
            if (damage <= 0) return;

            var health = other.GetComponentInParent<Health>();
            if (health == null || health.gameObject == gameObject) return;
            if (health.IsDead || health.IsInvulnerable) return;

            var player = other.GetComponentInParent<PlayerController>();
            if (onlyHurtsPlayer && player == null) return;

            health.TakeDamage(damage);

            if (knockback > 0f)
            {
                Vector2 dir = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;
                if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;

                if (player != null)
                    player.ApplyKnockback(dir * knockback, 0.15f);
                else if (other.attachedRigidbody != null)
                    other.attachedRigidbody.AddForce(dir * knockback, ForceMode2D.Impulse);
            }
        }
    }
}

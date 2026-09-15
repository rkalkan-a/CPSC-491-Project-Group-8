using UnityEngine;

namespace DungeonCrawler.Combat
{
    // simple straight-line shot. damages the first Health it touches (that isn't its owner)
    // and destroys itself on that hit, on hitting a solid obstacle, or after it runs out of range
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        private Rigidbody2D rb;
        private Vector2 direction;
        private float speed;
        private int damage;
        private float knockback;
        private float remainingRange;
        private GameObject owner;

        public void Launch(Vector2 dir, float speed, int damage, float range, float knockback, GameObject owner)
        {
            direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
            this.speed = speed;
            this.damage = damage;
            this.knockback = knockback;
            this.owner = owner;
            remainingRange = range;

            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = direction * speed;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void FixedUpdate()
        {
            remainingRange -= speed * Time.fixedDeltaTime;
            if (remainingRange <= 0f) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (owner != null && other.gameObject == owner) return;

            var health = other.GetComponentInParent<Health>();
            if (health != null)
            {
                if (!health.IsDead)
                {
                    health.TakeDamage(damage);
                    if (knockback > 0f && other.attachedRigidbody != null)
                        other.attachedRigidbody.AddForce(direction * knockback, ForceMode2D.Impulse);
                }
                Destroy(gameObject);
                return;
            }

            // not something with health, so only stop if it's actually solid (a wall, not another trigger volume)
            if (!other.isTrigger) Destroy(gameObject);
        }
    }
}

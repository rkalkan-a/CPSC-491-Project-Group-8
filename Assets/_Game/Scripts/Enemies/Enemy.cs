using System;
using UnityEngine;
using DungeonCrawler.Combat;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using DungeonCrawler.Rooms;

namespace DungeonCrawler.Enemies
{
    // basic enemy. chasers run at the player when close, wanderers just shuffle around.
    // contact damage comes from the DamageOnContact component on the same object
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class Enemy : MonoBehaviour
    {
        public enum Behavior { Chaser, Wanderer }

        [SerializeField] private Behavior behavior = Behavior.Chaser;
        [SerializeField, Min(0f)] private float moveSpeed = 3f;
        [SerializeField, Min(0f)] private float aggroRange = 7f;
        [SerializeField, Min(0f)] private float wanderSpeed = 1.2f;
        [SerializeField, Min(0.1f)] private float wanderChangeTime = 1.5f;

        public Room Room { get; set; }
        public Health Health => health;
        public event Action<Enemy> Died;

        private Rigidbody2D rb;
        private Health health;
        private Transform target;
        private Vector2 wanderDir;
        private float wanderTimer;
        private Vector2 knockbackVelocity;
        private float knockbackTimer;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        private void OnEnable()
        {
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            health.Died -= OnDied;
        }

        private void Start()
        {
            var player = GameManager.Instance != null ? GameManager.Instance.Player : FindFirstObjectByType<PlayerController>();
            if (player != null) target = player.transform;
        }

        private void FixedUpdate()
        {
            if (health.IsDead) return;

            if (knockbackTimer > 0f)
            {
                knockbackTimer -= Time.fixedDeltaTime;
                rb.linearVelocity = knockbackVelocity;
                return;
            }

            switch (behavior)
            {
                case Behavior.Chaser:
                    if (target != null && Vector2.Distance(target.position, transform.position) <= aggroRange)
                    {
                        Vector2 dir = ((Vector2)target.position - rb.position).normalized;
                        rb.linearVelocity = dir * moveSpeed;
                    }
                    else Wander();
                    break;

                case Behavior.Wanderer:
                    Wander();
                    break;
            }
        }

        private void Wander()
        {
            wanderTimer -= Time.fixedDeltaTime;
            if (wanderTimer <= 0f)
            {
                // sometimes just stand still for a bit
                wanderDir = UnityEngine.Random.value < 0.3f ? Vector2.zero : UnityEngine.Random.insideUnitCircle.normalized;
                wanderTimer = wanderChangeTime * UnityEngine.Random.Range(0.6f, 1.4f);
            }
            rb.linearVelocity = wanderDir * wanderSpeed;
        }

        public void ApplyKnockback(Vector2 velocity, float duration)
        {
            knockbackVelocity = velocity;
            knockbackTimer = duration;
        }

        private void OnDied()
        {
            rb.linearVelocity = Vector2.zero;
            foreach (var col in GetComponentsInChildren<Collider2D>()) col.enabled = false;

            var bleed = GetComponent<BleedOnHit>();
            if (bleed != null && GameSettings.Gore != GameSettings.GoreOff)
                bleed.Splat(transform.position, GameSettings.Gore == GameSettings.GoreFull ? 10 : 3);

            Died?.Invoke(this);
            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            if (behavior != Behavior.Chaser) return;
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, aggroRange);
        }
    }
}

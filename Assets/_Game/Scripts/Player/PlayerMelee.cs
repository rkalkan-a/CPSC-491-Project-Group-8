using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DungeonCrawler.Combat;
using DungeonCrawler.Enemies;

namespace DungeonCrawler.Player
{
    // close range swing. hits everything with Health inside an arc in front of the player.
    // right mouse button, space, or gamepad north. aims at the mouse, or the way you're facing
    [RequireComponent(typeof(PlayerController))]
    public class PlayerMelee : MonoBehaviour
    {
        [SerializeField, Min(0)] private int damage = 2;
        [SerializeField, Min(0.2f)] private float range = 1.6f;
        [SerializeField, Range(10f, 170f)] private float arcDegrees = 120f;
        [SerializeField, Min(0f)] private float knockback = 7f;
        [SerializeField, Min(0.05f)] private float cooldown = 0.4f;
        [SerializeField] private Color slashColor = new Color(1f, 0.95f, 0.8f);

        public int Damage => damage;
        public float Range => range;
        public float ArcDegrees => arcDegrees;
        public float Cooldown => cooldown;
        public bool IsOnCooldown => Time.time < cooldownUntil;
        public event Action<Vector2> Swung; // direction of the swing

        private const float SlashTime = 0.12f;

        private PlayerController player;
        private PlayerAttack rangedAttack;
        private Health ownHealth;
        private InputAction swingAction;
        private float cooldownUntil;
        private SpriteRenderer slash;
        private Coroutine slashRoutine;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            rangedAttack = GetComponent<PlayerAttack>();
            ownHealth = GetComponent<Health>();

            // bound in code so the shared input actions asset doesn't need editing
            swingAction = new InputAction("Melee", InputActionType.Button);
            swingAction.AddBinding("<Mouse>/rightButton");
            swingAction.AddBinding("<Keyboard>/space");
            swingAction.AddBinding("<Gamepad>/buttonNorth");
        }

        private void OnEnable() => swingAction.Enable();
        private void OnDisable() => swingAction.Disable();
        private void OnDestroy() => swingAction.Dispose();

        private void Update()
        {
            if (!player.InputEnabled || PauseMenu.IsPaused) return;
            if (ownHealth != null && ownHealth.IsDead) return;
            if (swingAction.WasPressedThisFrame()) TrySwing(AimDirection());
        }

        private Vector2 AimDirection()
        {
            if (rangedAttack != null && rangedAttack.isActiveAndEnabled) return rangedAttack.AimDirection;

            if (Mouse.current != null && Camera.main != null)
            {
                Vector2 mouseScreen = Mouse.current.position.ReadValue();
                float depth = -Camera.main.transform.position.z;
                Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, depth));
                Vector2 toMouse = (Vector2)mouseWorld - (Vector2)transform.position;
                if (toMouse.sqrMagnitude > 0.01f) return toMouse.normalized;
            }
            return player.Facing;
        }

        // swings if the cooldown is over. returns false if it was still cooling down
        public bool TrySwing(Vector2 direction)
        {
            if (IsOnCooldown) return false;
            if (direction.sqrMagnitude < 0.0001f) direction = player.Facing;
            direction.Normalize();

            cooldownUntil = Time.time + cooldown;
            HitTargets(direction);
            ShowSlash(direction);
            Swung?.Invoke(direction);
            return true;
        }

        private void HitTargets(Vector2 direction)
        {
            Vector2 origin = transform.position;
            var alreadyHit = new HashSet<Health>();

            foreach (var col in Physics2D.OverlapCircleAll(origin, range))
            {
                var health = col.GetComponentInParent<Health>();
                if (health == null || health == ownHealth || health.IsDead) continue;
                if (alreadyHit.Contains(health)) continue; // enemies with several colliders only get hit once

                Vector2 point = col.ClosestPoint(origin);
                if (!InArc(origin, direction, point, range, arcDegrees)) continue;
                if (BlockedByWall(origin, point)) continue;

                alreadyHit.Add(health);
                health.TakeDamage(damage);
                if (knockback <= 0f) continue;

                var enemy = health.GetComponent<Enemy>();
                if (enemy != null) enemy.ApplyKnockback(direction * knockback, 0.15f);
                else if (col.attachedRigidbody != null) col.attachedRigidbody.AddForce(direction * knockback, ForceMode2D.Impulse);
            }
        }

        // true if the point is within range and inside the swing arc centred on the aim direction
        public static bool InArc(Vector2 origin, Vector2 aim, Vector2 point, float range, float arcDegrees)
        {
            Vector2 toPoint = point - origin;
            float distance = toPoint.magnitude;
            if (distance > range) return false;
            if (distance < 0.0001f) return true; // standing right on top of it
            return Vector2.Angle(aim, toPoint) <= arcDegrees * 0.5f;
        }

        // a solid collider between the player and the target stops the hit, so you can't swing through walls
        private bool BlockedByWall(Vector2 origin, Vector2 point)
        {
            foreach (var hit in Physics2D.LinecastAll(origin, point))
            {
                var col = hit.collider;
                if (col.isTrigger) continue;
                if (col.transform.IsChildOf(transform)) continue;          // the player
                if (col.GetComponentInParent<Health>() != null) continue;  // other things that can be hit
                return true;
            }
            return false;
        }

        // ---------- visual ----------

        private void ShowSlash(Vector2 direction)
        {
            if (slash == null)
            {
                var go = new GameObject("Slash");
                go.transform.SetParent(transform, false);
                slash = go.AddComponent<SpriteRenderer>();
                slash.sprite = MakeSlashSprite(arcDegrees, range);
                slash.sortingOrder = 12;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            slash.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

            if (slashRoutine != null) StopCoroutine(slashRoutine);
            slashRoutine = StartCoroutine(SlashRoutine());
        }

        private IEnumerator SlashRoutine()
        {
            slash.enabled = true;
            for (float t = 0f; t < SlashTime; t += Time.deltaTime)
            {
                float k = t / SlashTime;
                slash.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, k);
                var c = slashColor;
                c.a = 1f - k * k;
                slash.color = c;
                yield return null;
            }
            slash.enabled = false;
            slashRoutine = null;
        }

        // pixel crescent covering the swing arc. the sprite's pivot sits on the player and it points along +x
        private static Sprite MakeSlashSprite(float arcDegrees, float range)
        {
            const int w = 32, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            float halfArc = arcDegrees * 0.5f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var v = new Vector2(x + 0.5f, y + 0.5f - h * 0.5f);
                    float d = v.magnitude / w;                    // 0 at the player, 1 at full range
                    float angle = Vector2.Angle(Vector2.right, v);
                    float a = 0f;
                    if (d > 0.6f && d <= 1f && angle <= halfArc)
                    {
                        float edge = (d - 0.6f) / 0.4f;           // brighter toward the outer edge
                        float taper = 1f - angle / halfArc;       // fades out toward the ends of the arc
                        a = edge * Mathf.Sqrt(taper);
                    }
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 0.5f), w / range);
        }
    }
}

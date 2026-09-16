using UnityEngine;
using UnityEngine.InputSystem;
using DungeonCrawler.Combat;

namespace DungeonCrawler.Player
{
    // fires a projectile toward the mouse (or right stick / facing direction as a fallback)
    // when the shared "Attack" input action fires - that's the left mouse button, gamepad
    // West button, touchscreen tap, or joystick trigger, per InputSystem_Actions
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAttack : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;

        [Header("Shot")]
        [SerializeField, Min(0)] private int damage = 1;
        [SerializeField, Min(0f)] private float projectileSpeed = 14f;
        [SerializeField, Min(0.5f)] private float range = 6f;
        [SerializeField, Min(0f)] private float knockback = 4f;
        [SerializeField, Min(0.05f)] private float attacksPerSecond = 4f;
        [SerializeField, Min(0f)] private float spawnOffset = 0.5f;
        [SerializeField, Min(0.05f)] private float projectileSize = 0.25f;
        [SerializeField] private Color projectileColor = new Color(1f, 0.85f, 0.3f);

        public Vector2 AimDirection { get; private set; } = Vector2.down;
        public float Range => range;

        private PlayerController player;
        private InputActionMap playerMap;
        private InputAction attackAction;
        private InputAction lookAction;
        private float cooldownUntil;
        private static Sprite cachedProjectileSprite;

        private void Awake()
        {
            player = GetComponent<PlayerController>();

            if (inputActions == null)
            {
                Debug.LogError("PlayerAttack needs the InputSystem_Actions asset assigned", this);
                return;
            }

            playerMap = inputActions.FindActionMap("Player", throwIfNotFound: true);
            attackAction = playerMap.FindAction("Attack", throwIfNotFound: true);
            lookAction = playerMap.FindAction("Look", throwIfNotFound: false);
        }

        private void OnEnable() => playerMap?.Enable();
        private void OnDisable() => playerMap?.Disable();

        private void Update()
        {
            if (player == null || !player.InputEnabled) return;

            UpdateAimDirection();

            if (attackAction != null && attackAction.IsPressed() && Time.time >= cooldownUntil)
            {
                Fire();
                cooldownUntil = Time.time + 1f / attacksPerSecond;
            }
        }

        private void UpdateAimDirection()
        {
            // mouse aim wins whenever there's a mouse and it's actually pointing somewhere
            if (Mouse.current != null && Camera.main != null)
            {
                Vector2 mouseScreen = Mouse.current.position.ReadValue();
                float depth = -Camera.main.transform.position.z;
                Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, depth));
                Vector2 toMouse = (Vector2)mouseWorld - (Vector2)player.transform.position;
                if (toMouse.sqrMagnitude > 0.01f)
                {
                    AimDirection = toMouse.normalized;
                    return;
                }
            }

            // gamepad right stick as a fallback aim direction
            if (lookAction != null)
            {
                Vector2 stick = lookAction.ReadValue<Vector2>();
                if (stick.sqrMagnitude > 0.2f)
                {
                    AimDirection = stick.normalized;
                    return;
                }
            }

            // no aim input available, just shoot the way you're moving/facing
            if (player.Facing.sqrMagnitude > 0.01f) AimDirection = player.Facing.normalized;
        }

        private void Fire()
        {
            var go = new GameObject("Projectile");
            go.transform.position = (Vector2)transform.position + AimDirection * spawnOffset;
            go.transform.localScale = Vector3.one * projectileSize;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetProjectileSprite();
            sr.color = projectileColor;
            sr.sortingOrder = 11;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;

            go.AddComponent<Rigidbody2D>();

            var projectile = go.AddComponent<Projectile>();
            projectile.Launch(AimDirection, projectileSpeed, damage, range, knockback, gameObject);
        }

        // a soft white dot, generated once and reused for every shot so we're not allocating textures mid-combat
        private static Sprite GetProjectileSprite()
        {
            if (cachedProjectileSprite != null) return cachedProjectileSprite;

            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = Mathf.Clamp01(1f - (dist - radius));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();

            cachedProjectileSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return cachedProjectileSprite;
        }
    }
}

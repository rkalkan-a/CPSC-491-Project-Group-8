using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonCrawler.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField, Min(0f)] private float moveSpeed = 6f;
        [SerializeField] private bool fourDirectionOnly = false; // false = 8 directions like isaac

        public Vector2 Facing { get; private set; } = Vector2.down; // last direction moved, for attacks later
        public Vector2 MoveInput { get; private set; }
        public bool InputEnabled { get; set; } = true;
        public Rigidbody2D Body => rb;

        private Rigidbody2D rb;
        private InputActionMap playerMap;
        private InputAction moveAction;
        private Vector2 knockbackVelocity;
        private float knockbackTimer;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            if (inputActions == null)
            {
                Debug.LogError("PlayerController needs the InputSystem_Actions asset assigned", this);
                return;
            }

            playerMap = inputActions.FindActionMap("Player", throwIfNotFound: true);
            moveAction = playerMap.FindAction("Move", throwIfNotFound: true);
        }

        private void OnEnable()
        {
            playerMap?.Enable();
        }

        private void OnDisable()
        {
            playerMap?.Disable();
        }

        private void Update()
        {
            if (moveAction == null || !InputEnabled)
            {
                MoveInput = Vector2.zero;
                return;
            }

            Vector2 input = moveAction.ReadValue<Vector2>();

            if (fourDirectionOnly && input.sqrMagnitude > 0.01f)
            {
                input = Mathf.Abs(input.x) >= Mathf.Abs(input.y)
                    ? new Vector2(Mathf.Sign(input.x), 0f)
                    : new Vector2(0f, Mathf.Sign(input.y));
            }

            // don't let diagonals be faster
            MoveInput = input.sqrMagnitude > 1f ? input.normalized : input;

            if (MoveInput.sqrMagnitude > 0.01f)
                Facing = MoveInput.normalized;
        }

        private void FixedUpdate()
        {
            // getting hit overrides normal movement for a moment
            if (knockbackTimer > 0f)
            {
                knockbackTimer -= Time.fixedDeltaTime;
                rb.linearVelocity = knockbackVelocity;
                return;
            }

            rb.linearVelocity = MoveInput * moveSpeed;
        }

        public void ApplyKnockback(Vector2 velocity, float duration)
        {
            knockbackVelocity = velocity;
            knockbackTimer = duration;
        }

        public void TeleportTo(Vector2 worldPosition)
        {
            knockbackTimer = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.position = worldPosition;
            transform.position = worldPosition;
        }
    }
}

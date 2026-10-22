using UnityEngine;
using DungeonCrawler.Player;
using DungeonCrawler.UI;

namespace DungeonCrawler.Rooms
{
    public enum DoorSide { North, South, East, West }

    // trigger on a wall that sends the player to the linked door in another room
    [RequireComponent(typeof(BoxCollider2D))]
    public class Door : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer doorRenderer;
        [SerializeField] private DoorSide side = DoorSide.North;
        [SerializeField] private Door targetDoor;
        [SerializeField, Min(0.5f)] private float arrivalDistance = 1.5f; // how far into the room you land
        [SerializeField] private bool isLocked = false;
        private bool combatLocked = false;
        // off = travel as soon as you touch the door. on = you have to walk this far past the door's center first (like into a tunnel)
        [SerializeField] private bool requireWalkIn = false;
        [SerializeField, Min(0f)] private float walkInDepth = 0.2f;
        // the way out of the last level. walking in shows the level complete screen instead of changing rooms
        [SerializeField] private bool isLevelExit = false;

        public DoorSide Side => side;
        public Door TargetDoor => targetDoor;
        public bool IsConnected => targetDoor != null;
        public bool IsLevelExit { get => isLevelExit; set => isLevelExit = value; }
        public bool IsLocked
        {
            get => isLocked || combatLocked;
            set => isLocked = value;
        }
        public void SetCombatLocked(bool locked)
        {
            combatLocked = locked;
        }
        public Room Room => GetComponentInParent<Room>();

        private SpriteRenderer sprite;
        private Color openColor;

        // points from the door into its own room
        public Vector2 InwardDirection => side switch
        {
            DoorSide.North => Vector2.down,
            DoorSide.South => Vector2.up,
            DoorSide.East => Vector2.left,
            DoorSide.West => Vector2.right,
            _ => Vector2.zero,
        };

        public Vector2 ArrivalPosition => (Vector2)transform.position + InwardDirection * arrivalDistance;

        private void Start()
        {
            RefreshVisualState();
        }

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void UpdateDoorColor()
        {
            if (doorRenderer == null)
                doorRenderer = GetComponent<SpriteRenderer>();

            if (doorRenderer == null)
                return;

            doorRenderer.color = GetExpectedColor();
        }

        private void Awake()
        {
            sprite = GetComponent<SpriteRenderer>();
            if (sprite != null) openColor = sprite.color;
            SetLocked(isLocked);
        }

        public void SetLocked(bool locked)
        {
            isLocked = locked;
            if (sprite != null) sprite.color = locked ? new Color(0.2f, 0.18f, 0.2f) : openColor;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!requireWalkIn) TryTravel(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (requireWalkIn) TryTravel(other);
        }

        private void TryTravel(Collider2D other)
        {
            if (isLocked) return;
            if (isLevelExit)
            {
                var exiting = other.GetComponentInParent<PlayerController>();
                if (exiting != null) LevelCompleteScreen.Show(exiting);
                return;
            }
            if (targetDoor == null) return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;

            if (requireWalkIn)
            {
                Vector2 outward = -InwardDirection;
                float depth = Vector2.Dot((Vector2)player.transform.position - (Vector2)transform.position, outward);
                if (depth < walkInDepth) return;
            }

            Debug.Log(
                $"Source Door: {name}, " +
                $"Source Room: {(Room != null ? Room.RoomId : "NULL")}, " +
                $"Target Door: {(targetDoor != null ? targetDoor.name : "NULL")}, " +
                $"Target Room: {(targetDoor?.Room != null ? targetDoor.Room.RoomId : "NULL")}"
            );

            if (RoomManager.Instance == null)
            {
                Debug.LogError("RoomManager.Instance is NULL!");
                return;
            }
            Debug.Log("Calling RoomManager.TravelThrough...");
            
            RoomManager.Instance?.TravelThrough(this, player);
        }
        private void RefreshVisualState()
        {
            if (doorRenderer == null)
                doorRenderer = GetComponent<SpriteRenderer>();

            // the level exit isn't linked to another door on purpose (it ends the run), but it still has to be usable
            bool connected = targetDoor != null || isLevelExit;

            if (doorRenderer != null)
                doorRenderer.enabled = connected;

            var col = GetComponent<Collider2D>();
            if (col != null)
                col.enabled = connected;
        }
        public void Link(Door other)
        {
            targetDoor = other;

            if (other != null)
            {
                other.targetDoor = this;
                other.RefreshVisualState();
            }

            RefreshVisualState();
        }

        private void OnEnable()
        {
            UpdateDoorColor();
        }

        private void OnDrawGizmos()
        {
            if (isLocked)
            {
                Gizmos.color = Color.red;
            }
            else if (targetDoor != null)
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = Color.yellow;
            }

            Gizmos.DrawWireCube(
                transform.position,
                new Vector3(0.9f, 0.9f, 0f)
            );

            Gizmos.DrawLine(
                transform.position,
                ArrivalPosition
            );

            if (targetDoor != null)
            {
                Gizmos.color = Color.green;

                Gizmos.DrawLine(
                    transform.position,
                    targetDoor.transform.position
                );
            }
        }
        private Color GetExpectedColor()
        {
            if (isLocked)
                return Color.red;

            if (targetDoor != null)
                return Color.green;

            return Color.yellow;
        }

        private void LateUpdate()
        {
            if (doorRenderer == null)
                return;

            Color expected = GetExpectedColor();

            if (doorRenderer.color != expected)
                doorRenderer.color = expected;
        }
    }
}

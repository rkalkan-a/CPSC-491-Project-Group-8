using UnityEngine;
using DungeonCrawler.Player;

namespace DungeonCrawler.Rooms
{
    public enum DoorSide { North, South, East, West }

    // trigger on a wall that sends the player to the linked door in another room
    [RequireComponent(typeof(BoxCollider2D))]
    public class Door : MonoBehaviour
    {
        [SerializeField] private DoorSide side = DoorSide.North;
        [SerializeField] private Door targetDoor;
        [SerializeField, Min(0.5f)] private float arrivalDistance = 1.5f; // how far into the room you land
        [SerializeField] private bool isLocked = false;

        public DoorSide Side => side;
        public Door TargetDoor => targetDoor;
        public bool IsConnected => targetDoor != null;
        public bool IsLocked { get => isLocked; set => isLocked = value; }
        public bool IsLocked => isLocked;
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

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

            private void OnTriggerEnter2D(Collider2D other)
            {
                if (isLocked || targetDoor == null) return;
                var player = other.GetComponentInParent<PlayerController>();
                if (player == null) return;
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
            if (isLocked || targetDoor == null) return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;

                Debug.Log(
                    $"Travel: {Room?.RoomId} -> {targetDoor.Room?.RoomId}"
                );

                RoomManager.Instance?.TravelThrough(this, player);
            }

        public void Link(Door other)
        {
            targetDoor = other;
            if (other != null) other.targetDoor = this;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = isLocked ? Color.red : Color.yellow;
            Gizmos.DrawWireCube(transform.position, new Vector3(0.9f, 0.9f, 0f));
            Gizmos.DrawLine(transform.position, ArrivalPosition);
            if (targetDoor != null)
            {
                Gizmos.color = new Color(1f, 0.6f, 0f, 0.6f);
                Gizmos.DrawLine(transform.position, targetDoor.transform.position);
            }
        }
    }
}

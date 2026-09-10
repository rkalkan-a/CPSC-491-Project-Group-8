using UnityEngine;
using DungeonCrawler.Player;

namespace DungeonCrawler.Rooms
{
    public enum DoorSide { North, South, East, West }

    // trigger on a wall that sends the player to the linked door in another room
    [RequireComponent(typeof(Collider2D))]
    public class Door : MonoBehaviour
    {
        [SerializeField] private DoorSide side = DoorSide.North;
        [SerializeField] private Door targetDoor;
        [SerializeField, Min(0.5f)] private float arrivalDistance = 1.5f; // how far into the room you land
        [SerializeField] private bool isLocked = false;

        public DoorSide Side => side;
        public Door TargetDoor => targetDoor;
        public bool IsLocked { get => isLocked; set => isLocked = value; }
        public Room Room => GetComponentInParent<Room>();

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

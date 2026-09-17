using UnityEngine;
using DungeonCrawler.Core;

namespace DungeonCrawler.Rooms
{
    // camera locks to the current room like isaac and slides over when you change rooms.
    // big rooms (the forest map) are too large to show at once so it follows the player instead, clamped to the room edges
    [RequireComponent(typeof(Camera))]
    public class RoomCamera : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float smoothTime = 0.12f; // 0 = snap
        [SerializeField, Min(0f)] private float padding = 0f;
        [SerializeField, Min(1f)] private float maxLockedViewHeight = 17f; // rooms taller than this switch to follow mode (dungeon rooms are 16, the forest is 21)
        [SerializeField, Min(1f)] private float followViewHeight = 11f;

        private Camera cam;
        private Room currentRoom;
        private bool followMode;
        private Vector3 targetPosition;
        private float targetSize;
        private Vector3 velocity;
        private float sizeVelocity;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            targetPosition = transform.position;
            targetSize = cam.orthographicSize;
        }

        private void OnEnable() => GameEvents.RoomEntered += OnRoomEntered;
        private void OnDisable() => GameEvents.RoomEntered -= OnRoomEntered;

        private void OnRoomEntered(Room room) => FocusRoom(room, smoothTime <= 0f);

        public void FocusRoom(Room room, bool instant = false)
        {
            if (room == null) return;
            currentRoom = room;

            float aspect = Mathf.Max(0.01f, cam.aspect);
            float neededHeight = Mathf.Max(room.Size.y, room.Size.x / aspect) + padding * 2f;
            followMode = neededHeight > maxLockedViewHeight;

            if (followMode)
            {
                targetSize = followViewHeight * 0.5f;
                targetPosition = ClampToRoom(PlayerPosition());
            }
            else
            {
                targetSize = neededHeight * 0.5f;
                targetPosition = new Vector3(room.Center.x, room.Center.y, transform.position.z);
            }

            if (instant)
            {
                transform.position = targetPosition;
                cam.orthographicSize = targetSize;
                velocity = Vector3.zero;
                sizeVelocity = 0f;
            }
        }

        private Vector3 PlayerPosition()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.Player != null) return gm.Player.transform.position;
            return transform.position;
        }

        // keeps the view inside the room so you never see past the map edge
        private Vector3 ClampToRoom(Vector3 pos)
        {
            float halfH = targetSize;
            float halfW = targetSize * Mathf.Max(0.01f, cam.aspect);
            Vector2 half = currentRoom.Size * 0.5f;
            float minX = currentRoom.Center.x - half.x + halfW, maxX = currentRoom.Center.x + half.x - halfW;
            float minY = currentRoom.Center.y - half.y + halfH, maxY = currentRoom.Center.y + half.y - halfH;
            float x = minX <= maxX ? Mathf.Clamp(pos.x, minX, maxX) : currentRoom.Center.x;
            float y = minY <= maxY ? Mathf.Clamp(pos.y, minY, maxY) : currentRoom.Center.y;
            return new Vector3(x, y, transform.position.z);
        }

        private void LateUpdate()
        {
            if (followMode && currentRoom != null)
                targetPosition = ClampToRoom(PlayerPosition());

            if (smoothTime <= 0f)
            {
                transform.position = targetPosition;
                cam.orthographicSize = targetSize;
                return;
            }
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetSize, ref sizeVelocity, smoothTime);
        }
    }
}

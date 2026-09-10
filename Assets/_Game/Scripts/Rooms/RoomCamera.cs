using UnityEngine;
using DungeonCrawler.Core;

namespace DungeonCrawler.Rooms
{
    // camera locks to the current room like isaac, slides over when you change rooms
    [RequireComponent(typeof(Camera))]
    public class RoomCamera : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float smoothTime = 0.12f; // 0 = snap
        [SerializeField, Min(0f)] private float padding = 0f;

        private Camera cam;
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

            targetPosition = new Vector3(room.Center.x, room.Center.y, transform.position.z);

            // fit height, but also width in case the game view is narrow
            float halfHeight = room.Size.y * 0.5f + padding;
            float halfWidth = (room.Size.x * 0.5f + padding) / Mathf.Max(0.01f, cam.aspect);
            targetSize = Mathf.Max(halfHeight, halfWidth);

            if (instant)
            {
                transform.position = targetPosition;
                cam.orthographicSize = targetSize;
                velocity = Vector3.zero;
                sizeVelocity = 0f;
            }
        }

        private void LateUpdate()
        {
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

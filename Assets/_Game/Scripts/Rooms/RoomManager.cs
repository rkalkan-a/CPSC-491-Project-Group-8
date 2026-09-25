using System.Collections;
using UnityEngine;
using DungeonCrawler.Core;
using DungeonCrawler.Player;

namespace DungeonCrawler.Rooms
{
    // keeps track of the current room and handles going through doors
    public class RoomManager : MonoBehaviour
    {
        public static RoomManager Instance { get; private set; }

        [SerializeField] private Room startingRoom;
        [SerializeField, Min(0f)] private float transitionLockTime = 0.2f; // stops you bouncing straight back through the door

        public Room CurrentRoom { get; private set; }
        public Room StartingRoom => startingRoom;
        public bool IsTransitioning { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void EnterRoom(Room room)
        {
            if (room == null) return;
            CurrentRoom = room;
            foreach (var r in FindObjectsByType<Room>(FindObjectsSortMode.None))
                r.SetVisible(r == room);
            GameEvents.RaiseRoomEntered(room);
            room.OnPlayerEntered();
        }

        public Room FindRoomAt(Vector2 worldPosition)
        {
            foreach (var room in FindObjectsByType<Room>(FindObjectsSortMode.None))
                if (room.Contains(worldPosition)) return room;
            return null;
        }

        public void TravelThrough(Door door, PlayerController player)
        {
            if (IsTransitioning || door == null || door.TargetDoor == null || player == null) return;
            StartCoroutine(TransitionRoutine(door, player));
        }

        private IEnumerator TransitionRoutine(Door fromDoor, PlayerController player)
        {
            IsTransitioning = true;
            //player.InputEnabled = false;

            Door toDoor = fromDoor.TargetDoor;
            Room toRoom = toDoor.Room;

            player.TeleportTo(toDoor.ArrivalPosition);
            EnterRoom(toRoom);

            yield return new WaitForSeconds(transitionLockTime);

            //player.InputEnabled = true;
            IsTransitioning = false;
        }
    }
}

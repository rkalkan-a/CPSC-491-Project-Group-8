using System.Collections.Generic;
using UnityEngine;

namespace DungeonCrawler.Rooms
{
    public class RoomGenerator : MonoBehaviour
    {
        [SerializeField] private Room startRoomPrefab;
        [SerializeField] private List<Room> roomPrefabs = new();

        [SerializeField] private Vector2 firstRoomPosition = Vector2.zero;
        [SerializeField] private Vector2 secondRoomOffset = new Vector2(20f, 0f);

        private Room startRoom;
        private Room secondRoom;

        private void Start()
        {
            Generate();
        }

        public void Generate()
        {
            startRoom = Instantiate(
                startRoomPrefab,
                firstRoomPosition,
                Quaternion.identity,
                transform
            );

            if (roomPrefabs.Count == 0)
            {
                Debug.LogError("No room prefabs assigned.");
                return;
            }

            Room chosenPrefab = roomPrefabs[
                Random.Range(0, roomPrefabs.Count)
            ];

            secondRoom = Instantiate(
                chosenPrefab,
                firstRoomPosition + secondRoomOffset,
                Quaternion.identity,
                transform
            );

            LinkRooms();
        }

        private void LinkRooms()
        {
            Door startEast = startRoom.GetDoor(DoorSide.East);
            Door secondWest = secondRoom.GetDoor(DoorSide.West);

            if (startEast == null || secondWest == null)
            {
                Debug.LogError(
                    "Could not link rooms. Need East door on start room " +
                    "and West door on second room."
                );

                return;
            }

            startEast.Link(secondWest);

            Debug.Log(
                $"Linked {startRoom.RoomId}/East -> " +
                $"{secondRoom.RoomId}/West"
            );
        }
    }
}
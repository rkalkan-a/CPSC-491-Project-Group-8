using System.Collections.Generic;
using UnityEngine;

namespace DungeonCrawler.Rooms
{
    public class RoomGenerator : MonoBehaviour
    {
        [SerializeField] private Room startRoomPrefab;
        [SerializeField] private List<Room> roomPrefabs = new();

        [SerializeField, Min(2)]
        private int roomCount = 20;

        [SerializeField]
        private Vector2 roomSpacing = new Vector2(20f, 13f);

        private readonly List<Room> generatedRooms = new();
        private readonly Dictionary<Vector2Int, Room> roomsByGridPosition = new();
        private readonly Dictionary<Room, Vector2Int> gridPositionByRoom = new();
        private void Start()
        {
            Generate();
        }
        private Vector2Int GetGridOffset(DoorSide side)
        {
            return side switch
            {
                DoorSide.North => Vector2Int.up,
                DoorSide.South => Vector2Int.down,
                DoorSide.East => Vector2Int.right,
                DoorSide.West => Vector2Int.left,
                _ => Vector2Int.zero
            };
        }

        private Vector2 GridToWorld(Vector2Int gridPosition)
        {
            return new Vector2(
                gridPosition.x * roomSpacing.x,
                gridPosition.y * roomSpacing.y
            );
        }

        public void Generate()
        {
            generatedRooms.Clear();
            roomsByGridPosition.Clear();
            gridPositionByRoom.Clear();

            if (startRoomPrefab == null)
            {
                Debug.LogError("No start room prefab assigned.");
                return;
            }

            if (roomPrefabs.Count == 0)
            {
                Debug.LogError("No room prefabs assigned.");
                return;
            }

            Vector2Int startGridPosition = Vector2Int.zero;

            Room firstRoom = Instantiate(
                startRoomPrefab,
                GridToWorld(startGridPosition),
                Quaternion.identity,
                transform
            );

            firstRoom.SetRoomId("Room_01");
            firstRoom.gameObject.name = "Room_01";

            generatedRooms.Add(firstRoom);
            roomsByGridPosition.Add(startGridPosition, firstRoom);
            gridPositionByRoom.Add(firstRoom, startGridPosition);

            for (int i = 1; i < roomCount; i++)
            {
                List<Room> expandableRooms = GetExpandableRooms();

                if (expandableRooms.Count == 0)
                {
                    Debug.LogWarning(
                        $"No expandable rooms remain. Generated {generatedRooms.Count}/{roomCount} rooms."
                    );

                    break;
                }

                Room currentRoom =
                    expandableRooms[Random.Range(0, expandableRooms.Count)];

                Vector2Int currentGridPosition =
                    gridPositionByRoom[currentRoom];

                List<DoorSide> availableDirections =
                    GetAvailableDirections(currentRoom, currentGridPosition);

                DoorSide exitSide =
                    availableDirections[
                        Random.Range(0, availableDirections.Count)
                    ];

                Vector2Int nextGridPosition =
                    currentGridPosition + GetGridOffset(exitSide);

                DoorSide entranceSide = exitSide.Opposite();

                Room chosenPrefab =
                    FindRandomPrefabWithDoor(entranceSide);

                if (chosenPrefab == null)
                {
                    Debug.LogWarning(
                        $"Could not find a room prefab with a {entranceSide} door."
                    );

                    break;
                }

                Room newRoom = Instantiate(
                    chosenPrefab,
                    GridToWorld(nextGridPosition),
                    Quaternion.identity,
                    transform
                );

                string roomId = $"Room_{i + 1:00}";

                newRoom.SetRoomId(roomId);
                newRoom.gameObject.name = roomId;

                Door exitDoor =
                    currentRoom.GetDoor(exitSide);

                Door entranceDoor =
                    newRoom.GetDoor(entranceSide);

                if (exitDoor == null || entranceDoor == null)
                {
                    Debug.LogError(
                        $"Could not link {currentRoom.RoomId} to {roomId}."
                    );

                    Destroy(newRoom.gameObject);
                    break;
                }

                exitDoor.Link(entranceDoor);

                generatedRooms.Add(newRoom);
                roomsByGridPosition.Add(nextGridPosition, newRoom);
                gridPositionByRoom.Add(newRoom, nextGridPosition);

                Debug.Log(
                    $"{currentRoom.RoomId} generated {roomId} to the {exitSide}."
                );
            }

            Debug.Log(
                $"Generated {generatedRooms.Count} rooms."
            );
        }

        private List<DoorSide> GetAvailableDirections(
            Room room,
            Vector2Int gridPosition)
        {
            List<DoorSide> available = new();

            DoorSide[] directions =
            {
                DoorSide.North,
                DoorSide.South,
                DoorSide.East,
                DoorSide.West
            };

            foreach (DoorSide side in directions)
            {
                Door door = room.GetDoor(side);

                if (door == null || door.IsConnected)
                    continue;

                Vector2Int neighborPosition =
                    gridPosition + GetGridOffset(side);

                if (roomsByGridPosition.ContainsKey(neighborPosition))
                    continue;

                if (!HasPrefabWithDoor(side.Opposite()))
                    continue;

                available.Add(side);
            }

            return available;
        }
        private List<Room> GetExpandableRooms()
        {
            List<Room> expandableRooms = new();

            foreach (Room room in generatedRooms)
            {
                Vector2Int gridPosition = gridPositionByRoom[room];

                if (GetAvailableDirections(room, gridPosition).Count > 0)
                {
                    expandableRooms.Add(room);
                }
            }

            return expandableRooms;
        }
        private bool HasPrefabWithDoor(DoorSide requiredSide)
        {
            foreach (Room prefab in roomPrefabs)
            {
                if (prefab != null &&
                    prefab.GetComponentInChildren<Door>(true) != null &&
                    PrefabHasDoor(prefab, requiredSide))
                {
                    return true;
                }
            }

            return false;
        }
        private bool PrefabHasDoor(Room prefab, DoorSide requiredSide)
        {
            Door[] doors = prefab.GetComponentsInChildren<Door>(true);

            foreach (Door door in doors)
            {
                if (door.Side == requiredSide)
                    return true;
            }

            return false;
        }
        private Room FindRandomPrefabWithDoor(DoorSide requiredSide)
        {
            List<Room> candidates = new();

            foreach (Room prefab in roomPrefabs)
            {
                if (prefab != null &&
                    PrefabHasDoor(prefab, requiredSide))
                {
                    candidates.Add(prefab);
                }
            }

            if (candidates.Count == 0)
                return null;

            return candidates[
                Random.Range(0, candidates.Count)
            ];
        }
    }
}
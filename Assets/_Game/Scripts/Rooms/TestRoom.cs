using UnityEngine;
using DungeonCrawler.Rooms;

public class TestRoom : MonoBehaviour
{
    [SerializeField] private Room room;

    private void Start()
    {
        Debug.Log("Door count: " + room.Doors.Count);

        Door eastDoor = room.GetDoor(DoorSide.East);

        if (eastDoor != null)
            Debug.Log("East door found: " + eastDoor.name);
        else
            Debug.LogError("East door was not found.");

        Debug.Log("Available doors: " + room.GetAvailableDoors().Count);

        Debug.Log("Opposite of East: " + DoorSide.East.Opposite());
    }
}
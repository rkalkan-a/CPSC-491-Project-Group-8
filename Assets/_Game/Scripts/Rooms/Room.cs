using System.Collections.Generic;
using UnityEngine;
using DungeonCrawler.Core;
using DungeonCrawler.Player;

namespace DungeonCrawler.Rooms
{
    // parent object for a room. floor, walls, doors and spawn points go under it
    public class Room : MonoBehaviour
    {
        [SerializeField] private string roomId = "Room";
        [SerializeField] private Vector2 size = new Vector2(17f, 10f); // outer size including walls
        [SerializeField] private bool clearedOnEnter = true; // TODO turn off once enemies exist

        public string RoomId => roomId;
        public Vector2 Size => size;
        public Vector2 Center => transform.position;
        public Bounds WorldBounds => new Bounds(transform.position, size);
        public bool IsCleared { get; private set; }

        private readonly List<Door> doors = new();
        private readonly List<PlayerSpawnPoint> playerSpawns = new();
        private readonly List<EnemySpawnPoint> enemySpawns = new();

        public IReadOnlyList<Door> Doors => doors;
        public IReadOnlyList<PlayerSpawnPoint> PlayerSpawns => playerSpawns;
        public IReadOnlyList<EnemySpawnPoint> EnemySpawns => enemySpawns;

        private void Awake()
        {
            RefreshChildren();
        }

        public void RefreshChildren()
        {
            doors.Clear();
            doors.AddRange(GetComponentsInChildren<Door>(true));
            playerSpawns.Clear();
            playerSpawns.AddRange(GetComponentsInChildren<PlayerSpawnPoint>(true));
            enemySpawns.Clear();
            enemySpawns.AddRange(GetComponentsInChildren<EnemySpawnPoint>(true));
        }

        public PlayerSpawnPoint GetPlayerSpawn(string id = null)
        {
            if (!string.IsNullOrEmpty(id))
            {
                foreach (var s in playerSpawns)
                    if (s.Id == id) return s;
            }
            return playerSpawns.Count > 0 ? playerSpawns[0] : null;
        }

        public bool Contains(Vector2 worldPoint) => WorldBounds.Contains(worldPoint);

        public void OnPlayerEntered()
        {
            // sprint 3: spawn enemies from enemySpawns here and only clear when they're dead
            if (!IsCleared && (clearedOnEnter || enemySpawns.Count == 0))
                MarkCleared();
        }

        public void MarkCleared()
        {
            if (IsCleared) return;
            IsCleared = true;
            GameEvents.RaiseRoomCleared(this);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
            Gizmos.DrawWireCube(transform.position, size);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + new Vector3(-size.x * 0.5f + 0.3f, size.y * 0.5f - 0.3f, 0f), roomId);
#endif
        }
    }
}

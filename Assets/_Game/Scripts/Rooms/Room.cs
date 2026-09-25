using System.Collections.Generic;
using UnityEngine;
using DungeonCrawler.Core;
using DungeonCrawler.Enemies;
using DungeonCrawler.Player;

namespace DungeonCrawler.Rooms
{
    // parent object for a room. floor, walls, doors and spawn points go under it
    public class Room : MonoBehaviour
    {
        [SerializeField] private string roomId = "Room";
        [SerializeField] private Vector2 size = new Vector2(17f, 10f); // outer size including walls
        [SerializeField] private bool clearedOnEnter = false; // for rooms with no fights (start room, shops etc)
        [SerializeField] private bool lockDoorsUntilCleared = false; // turn on once the player can actually attack

        public string RoomId => roomId;
        public Vector2 Size => size;
        public Vector2 Center => transform.position;
        public Bounds WorldBounds => new Bounds(transform.position, size);
        public bool IsCleared { get; private set; }

        private readonly List<Door> doors = new();
        private readonly List<PlayerSpawnPoint> playerSpawns = new();
        private readonly List<EnemySpawnPoint> enemySpawns = new();
        private readonly List<Enemy> aliveEnemies = new();
        private bool spawnedEnemies;
        private readonly List<Renderer> hiddenRenderers = new();

        public IReadOnlyList<Door> Doors => doors;
        public IReadOnlyList<PlayerSpawnPoint> PlayerSpawns => playerSpawns;
        public IReadOnlyList<EnemySpawnPoint> EnemySpawns => enemySpawns;
        public int AliveEnemyCount => aliveEnemies.Count;

        private void Awake()
        {
            RefreshChildren();
        }

        // only the room you're in gets drawn, everything else is left as black void
        public void SetVisible(bool visible)
        {
            if (visible)
            {
                foreach (var r in hiddenRenderers)
                    if (r != null) r.enabled = true;
                hiddenRenderers.Clear();
                return;
            }

            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (!r.enabled) continue; // leave anything that was already off (like an opened chest's glow) alone
                r.enabled = false;
                hiddenRenderers.Add(r);
            }
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

        public Door GetDoor(DoorSide side)
        {
            foreach(var door in doors)
            {
                if(door.Side == side)
                    return door;
            }
            return null;
        }

        public List<Door> GetAvailableDoors()
        {
            var availableDoors = new List<Door>();

            foreach(var door in doors)
            {
                if(!door.IsConnected)
                    availableDoors.Add(door);
            }

            return availableDoors;

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
            if (IsCleared) return;

            if (clearedOnEnter || enemySpawns.Count == 0)
            {
                MarkCleared();
                return;
            }

            if (!spawnedEnemies) SpawnEnemies();
        }

        private void SpawnEnemies()
        {
            spawnedEnemies = true;
            foreach (var spawn in enemySpawns)
            {
                foreach (var go in spawn.Spawn(transform))
                {
                    var enemy = go.GetComponent<Enemy>();
                    if (enemy == null) continue;
                    enemy.Room = this;
                    enemy.Died += OnEnemyDied;
                    aliveEnemies.Add(enemy);
                }
            }

            if (aliveEnemies.Count == 0) MarkCleared();
            else if (lockDoorsUntilCleared) SetDoorsLocked(true);
        }

        private void OnEnemyDied(Enemy enemy)
        {
            enemy.Died -= OnEnemyDied;
            aliveEnemies.Remove(enemy);
            if (aliveEnemies.Count == 0) MarkCleared();
        }

        public void MarkCleared()
        {
            if (IsCleared) return;
            IsCleared = true;
            SetDoorsLocked(false);
            GameEvents.RaiseRoomCleared(this);
        }

        private void SetDoorsLocked(bool locked)
        {
            foreach (var door in doors) door.SetLocked(locked);
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

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using DungeonCrawler.Combat;
using DungeonCrawler.Player;
using DungeonCrawler.Rooms;

namespace DungeonCrawler.Core
{
    // handles the run: spawning the player, counting cleared rooms, and reset on death
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private PlayerController playerPrefab;
        [SerializeField] private PlayerSpawnPoint startSpawn; // optional, finds the "Start" spawn if empty
        [SerializeField, Min(1)] private int currentLevel = 1;
        [SerializeField, Min(0f)] private float respawnDelay = 1.5f;

        public PlayerController Player { get; private set; }
        public Health PlayerHealth { get; private set; }
        public int CurrentLevel => currentLevel;
        public int RoomsCleared { get; private set; }
        public bool RunActive { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.RoomCleared += OnRoomCleared;
        }

        private void OnDisable()
        {
            GameEvents.RoomCleared -= OnRoomCleared;
            if (PlayerHealth != null) PlayerHealth.Died -= OnPlayerDied;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            StartRun();
        }

        public void StartRun()
        {
            RoomsCleared = 0;

            PlayerSpawnPoint spawn = ResolveStartSpawn();
            if (spawn == null)
            {
                Debug.LogError("No PlayerSpawnPoint in the scene", this);
                return;
            }

            // use the player already in the scene if there is one, otherwise spawn the prefab
            Player = FindFirstObjectByType<PlayerController>();
            if (Player == null)
            {
                if (playerPrefab == null)
                {
                    Debug.LogError("No player prefab assigned and no player in scene", this);
                    return;
                }
                Player = Instantiate(playerPrefab, spawn.Position, Quaternion.identity);
                Player.name = "Player";
            }
            else
            {
                Player.TeleportTo(spawn.Position);
            }

            PlayerHealth = Player.GetComponent<Health>();
            if (PlayerHealth != null)
            {
                PlayerHealth.Died -= OnPlayerDied;
                PlayerHealth.Died += OnPlayerDied;
            }

            RunActive = true;
            GameEvents.RaiseRunStarted();

            Room startRoom = spawn.Room != null ? spawn.Room : RoomManager.Instance?.FindRoomAt(spawn.Position);
            RoomManager.Instance?.EnterRoom(startRoom);
        }

        private PlayerSpawnPoint ResolveStartSpawn()
        {
            if (startSpawn != null) return startSpawn;

            var spawns = FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None);
            foreach (var s in spawns)
                if (s.Id == "Start") return s;
            return spawns.Length > 0 ? spawns[0] : null;
        }

        private void OnRoomCleared(Room room)
        {
            RoomsCleared++;
        }

        private void OnPlayerDied()
        {
            if (!RunActive) return;
            RunActive = false;
            if (Player != null) Player.InputEnabled = false;

            GameEvents.RaisePlayerDied();
            StartCoroutine(ResetRunAfterDelay());
        }

        private IEnumerator ResetRunAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);
            ResetRun();
        }

        // dying loses your loot and sends you back to the start. reloading the scene is the easiest way to do that
        public void ResetRun()
        {
            GameEvents.RaiseRunReset();
            Scene active = SceneManager.GetActiveScene();
            if (active.buildIndex < 0)
            {
                Debug.LogError("Scene " + active.name + " isn't in build settings so it can't be reloaded", this);
                return;
            }
            SceneManager.LoadScene(active.buildIndex);
        }
    }
}

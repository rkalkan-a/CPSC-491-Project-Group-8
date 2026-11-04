using UnityEngine;
using UnityEngine.SceneManagement;
using DungeonCrawler.Combat;
using DungeonCrawler.Rooms;

namespace DungeonCrawler.Core
{
    // Hooks once per play session; does not require a scene object or Inspector setup.
    public static class DiagnosticLogMonitor
    {
        private static bool subscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            Unsubscribe();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            Subscribe();
        }

        private static void Subscribe()
        {
            if (subscribed) return;
            GameEvents.RunStarted += OnRunStarted;
            GameEvents.RunReset += OnRunReset;
            GameEvents.PlayerDied += OnPlayerDied;
            GameEvents.RoomCleared += OnRoomCleared;
            SceneManager.sceneLoaded += OnSceneLoaded;
            subscribed = true;
        }

        private static void Unsubscribe()
        {
            GameEvents.RunStarted -= OnRunStarted;
            GameEvents.RunReset -= OnRunReset;
            GameEvents.PlayerDied -= OnPlayerDied;
            GameEvents.RoomCleared -= OnRoomCleared;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            subscribed = false;
        }

        private static void OnRunStarted() => DiagnosticLogger.Info("Game", "Run started");
        private static void OnRunReset() => DiagnosticLogger.Info("Game", "Run reset requested");
        private static void OnPlayerDied() => DiagnosticLogger.Warning("Player", "Player died");
        private static void OnRoomCleared(Room room) => DiagnosticLogger.Info("Rooms", "Room cleared");
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) =>
            DiagnosticLogger.Info("Scenes", $"Loaded scene '{scene.name}' ({mode})");
    }
}

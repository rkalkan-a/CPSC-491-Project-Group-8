using System;
using DungeonCrawler.Combat;
using DungeonCrawler.Rooms;

namespace DungeonCrawler.Core
{
    // global events so systems don't need references to each other
    // subscribe in OnEnable, unsubscribe in OnDisable
    public static class GameEvents
    {
        public static event Action RunStarted;
        public static event Action RunReset;
        public static event Action PlayerDied;

        public static event Action<Room> RoomEntered;
        public static event Action<Room> RoomCleared;

        public static event Action<Health, int> DamageTaken;
        public static event Action<Health> EntityDied;

        public static void RaiseRunStarted() => RunStarted?.Invoke();
        public static void RaiseRunReset() => RunReset?.Invoke();
        public static void RaisePlayerDied() => PlayerDied?.Invoke();

        public static void RaiseRoomEntered(Room room) => RoomEntered?.Invoke(room);
        public static void RaiseRoomCleared(Room room) => RoomCleared?.Invoke(room);

        public static void RaiseDamageTaken(Health target, int amount) => DamageTaken?.Invoke(target, amount);
        public static void RaiseEntityDied(Health target) => EntityDied?.Invoke(target);
    }
}

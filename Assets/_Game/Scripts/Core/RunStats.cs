using UnityEngine;

namespace DungeonCrawler.Core
{
    // how long the current run has gone on and how many enemies were defeated, for the level complete screen.
    // starts over whenever a run starts (GameManager raises RunStarted when the level loads)
    public static class RunStats
    {
        public static float StartTime { get; private set; }
        public static int EnemiesDefeated { get; private set; }
        public static float Elapsed => Time.time - StartTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            // domain reload can be off in the editor, so don't subscribe twice
            GameEvents.RunStarted -= Reset;
            GameEvents.RunStarted += Reset;
            Reset();
        }

        public static void Reset()
        {
            StartTime = Time.time;
            EnemiesDefeated = 0;
        }

        public static void EnemyDefeated() => EnemiesDefeated++;

        // 3:42 style, minutes can go past 59
        public static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{total / 60}:{total % 60:00}";
        }
    }
}

using System;
using UnityEngine;

namespace DungeonCrawler.Loot
{
    // how many coins you have picked up this run. this lives on its own instead of inside
    // GameManager so the coin feature doesn't touch any shared file - nothing else needs it
    public static class CoinWallet
    {
        public static int Total { get; private set; }

        // fires whenever the total changes. CoinHUD is what listens to it
        public static event Action<int> Changed;

        public static void Add(int amount)
        {
            if (amount <= 0) return;
            Total += amount;
            Changed?.Invoke(Total);
        }

        public static void ResetTotal()
        {
            Total = 0;
            Changed?.Invoke(Total);
        }

        // statics stick around between play sessions when domain reload is disabled,
        // so clear them before the scene loads or last run's coins carry over
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearOnEnterPlayMode()
        {
            Total = 0;
            Changed = null;
        }
    }
}

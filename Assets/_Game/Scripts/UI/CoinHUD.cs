using UnityEngine;
using UnityEngine.UI;
using DungeonCrawler.Core;
using DungeonCrawler.Loot;

namespace DungeonCrawler.UI
{
    // "Coins: 0" in the top right corner. resets back to zero when a new run starts.
    public class CoinHUD : MonoBehaviour
    {
        [SerializeField] private Text coinText;
        [SerializeField] private string format = "Coins: {0}";

        private void OnEnable()
        {
            CoinWallet.Changed += Refresh;
            GameEvents.RunStarted += OnRunStarted;
            Refresh(CoinWallet.Total);
        }

        private void OnDisable()
        {
            CoinWallet.Changed -= Refresh;
            GameEvents.RunStarted -= OnRunStarted;
        }

        private void OnRunStarted() => CoinWallet.ResetTotal();

        private void Refresh(int total)
        {
            if (coinText != null) coinText.text = string.Format(format, total);
        }
    }
}

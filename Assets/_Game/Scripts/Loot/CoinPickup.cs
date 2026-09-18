using UnityEngine;
using DungeonCrawler.Player;

namespace DungeonCrawler.Loot
{
    // a coin sitting on the floor. walk over it, the counter goes up, the coin disappears.
    [RequireComponent(typeof(Collider2D))]
    public class CoinPickup : MonoBehaviour
    {
        [SerializeField, Min(1)] private int value = 1;

        private bool collected; // guards against two triggers firing in the same frame

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;

            collected = true;
            CoinWallet.Add(value);
            Destroy(gameObject);
        }
    }
}

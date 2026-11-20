using UnityEngine;
using UnityEngine.SceneManagement;
using DungeonCrawler.Player;

namespace DungeonCrawler.Rooms
{
    [RequireComponent(typeof(Collider2D))]
    public class InfiniteFloorExit : MonoBehaviour
    {
        private bool used;

        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();

            if (col != null)
                col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (used)
                return;

            PlayerController player =
                other.GetComponentInParent<PlayerController>();

            if (player == null)
                return;

            used = true;

            // Prevent movement while the old floor closes.
            player.InputEnabled = false;

            RoomTransitionFX.Get().LoadSceneWithTransition(
                SceneManager.GetActiveScene().name,
                player.transform,
                0.35f
            );
        }
    }
}
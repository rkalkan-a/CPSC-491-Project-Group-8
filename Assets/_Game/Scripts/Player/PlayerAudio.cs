using UnityEngine;
using DungeonCrawler.Combat;
using DungeonCrawler.Audio;

namespace DungeonCrawler.Player
{
    [RequireComponent(typeof(Health))]
    public class PlayerAudio : MonoBehaviour
    {
        [Header("Sound Effects")]
        [SerializeField] private AudioClip hurtSound;
        [SerializeField] private AudioClip swordSwingSound;

        private Health health;
        private PlayerMelee playerMelee;
        private int lastHealth;

        private void Awake()
        {
            health = GetComponent<Health>();
            playerMelee = GetComponent<PlayerMelee>();
        }

        private void OnEnable()
        {
            lastHealth = health.Current;
            health.Changed += OnHealthChanged;

            if (playerMelee != null)
            {
                playerMelee.Swung += OnSwordSwing;
            }
        }

        private void OnDisable()
        {
            health.Changed -= OnHealthChanged;

            if (playerMelee != null)
            {
                playerMelee.Swung -= OnSwordSwing;
            }
        }

        private void OnHealthChanged(int current, int max)
        {
            if (current < lastHealth)
            {
                SFXManager.Instance?.PlaySFX(hurtSound);
            }

            lastHealth = current;
        }

        private void OnSwordSwing(Vector2 direction)
        {
            SFXManager.Instance?.PlaySFX(swordSwingSound);
        }
    }
}
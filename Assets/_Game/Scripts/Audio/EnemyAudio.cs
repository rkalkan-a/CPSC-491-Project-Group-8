using UnityEngine;
using DungeonCrawler.Combat;
using DungeonCrawler.Audio;

namespace DungeonCrawler.Enemies
{
    [RequireComponent(typeof(Health))]
    public class EnemyAudio : MonoBehaviour
    {
        [Header("Enemy Sound Effects")]
        [SerializeField] private AudioClip hurtSound;
        [SerializeField] private AudioClip deathSound;

        private Health health;
        private int lastHealth;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            lastHealth = health.Current;
            health.Changed += OnHealthChanged;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            health.Changed -= OnHealthChanged;
            health.Died -= OnDied;
        }

        private void OnHealthChanged(int current, int max)
        {
            // Only play hurt sound if the enemy survives
            if (current < lastHealth && current > 0)
            {
                SFXManager.Instance?.PlaySFX(hurtSound);
            }

            lastHealth = current;
        }

        private void OnDied()
        {
            SFXManager.Instance?.PlaySFX(deathSound);
        }
    }
}
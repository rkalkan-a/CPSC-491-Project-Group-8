
using UnityEngine;
using DungeonCrawler.Combat;
using DungeonCrawler.Audio;

namespace DungeonCrawler.Enemies
{
    [RequireComponent(typeof(Health))]
    public class EnemyAudio : MonoBehaviour
    {
        [Header("Enemy Sound Effects")]
        [SerializeField] private AudioClip[] hurtSounds;
        [SerializeField] private AudioClip[] deathSounds;

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

        // Randomly selects and plays a sound from an array.
        private void PlayRandomSound(AudioClip[] sounds)
        {
            if (sounds == null || sounds.Length == 0)
                return;

            int index = Random.Range(0, sounds.Length);

            SFXManager.Instance?.PlaySFX(sounds[index]);
        }

        private void OnHealthChanged(int current, int max)
        {
            // Only play hurt sound if enemy survives.
            if (current < lastHealth && current > 0)
            {
                PlayRandomSound(hurtSounds);
            }

            lastHealth = current;
        }

        private void OnDied()
        {
            PlayRandomSound(deathSounds);
        }
    }
}

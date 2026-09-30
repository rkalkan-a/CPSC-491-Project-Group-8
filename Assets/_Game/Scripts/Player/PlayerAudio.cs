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
        }

        private void OnDisable()
        {
            health.Changed -= OnHealthChanged;
        }

        private void OnHealthChanged(int current, int max)
        {
            if (current < lastHealth)
            {
                SFXManager.Instance.PlaySFX(hurtSound);
            }

            lastHealth = current;
        }
    }
}

using UnityEngine;
using DungeonCrawler.Combat;
using DungeonCrawler.Audio;

namespace DungeonCrawler.Player
{
    [RequireComponent(typeof(Health))]
    public class PlayerAudio : MonoBehaviour
    {
        [Header("Player Sound Effects")]
        [SerializeField] private AudioClip[] hurtSounds;
        [SerializeField] private AudioClip[] swordSwingSounds;
        [SerializeField] private AudioClip[] swordEquipSounds;
        [SerializeField] private AudioClip[] armorEquipSounds;
        [SerializeField] private AudioClip[] healSounds;

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
            if (current < lastHealth)
            {
                PlayRandomSound(hurtSounds);
            }

            lastHealth = current;
        }

        private void OnSwordSwing(Vector2 direction)
        {
            PlayRandomSound(swordSwingSounds);
        }

        public void PlaySwordEquip()
        {
            PlayRandomSound(swordEquipSounds);
        }

        public void PlayArmorEquip()
        {
            PlayRandomSound(armorEquipSounds);
        }

        public void PlayHeal()
        {
            PlayRandomSound(healSounds);
        }
    }
}

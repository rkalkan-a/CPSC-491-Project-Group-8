using UnityEngine;

namespace DungeonCrawler.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class SFXManager : MonoBehaviour
    {
        public static SFXManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Automatically use the AudioSource on this GameObject
            // if one was not assigned in the Inspector.
            if (sfxSource == null)
            {
                sfxSource = GetComponent<AudioSource>();
            }
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        public void SetSFXVolume(float volume)
        {
            if (sfxSource != null)
            {
                sfxSource.volume = volume;
            }
        }
    }
}
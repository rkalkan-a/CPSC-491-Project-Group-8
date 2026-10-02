using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DungeonCrawler.UI;
using DungeonCrawler.Core;

public class PauseMenu : MonoBehaviour
{
    public GameObject Container;

    [Header("Settings")]
    public Slider volumeSlider;

    public static bool IsPaused { get; private set; }

    void Awake()
    {
        // Ensure paused state and time scale are reset when the scene loads
        IsPaused = false;
        Time.timeScale = 1f;

        // Load the saved volume into the pause menu slider
        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(GameSettings.Volume);
        }

        // Make sure the actual game volume also matches the saved value
        AudioListener.volume = GameSettings.Volume;

        if (Container != null)
            Container.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // Escape is closing the chest popup, don't pause too
            if (ChestPanel.Instance != null &&
                (ChestPanel.Instance.IsShowing ||
                 ChestPanel.ClosedOnFrame == Time.frameCount))
            {
                return;
            }

            if (Container.activeSelf)
            {
                Time.timeScale = 1f;
                Container.SetActive(false);
                IsPaused = false;
            }
            else
            {
                // Refresh slider every time pause menu opens
                if (volumeSlider != null)
                {
                    volumeSlider.SetValueWithoutNotify(GameSettings.Volume);
                }

                Time.timeScale = 0f;
                Container.SetActive(true);
                IsPaused = true;
            }
        }
    }

    public void Resume()
    {
        Time.timeScale = 1f;
        Container.SetActive(false);
        IsPaused = false;
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main Menu");
    }

    public void SetVolume(float volume)
    {
        GameSettings.Volume = volume;
        AudioListener.volume = volume;
        GameSettings.Save();
    }
}
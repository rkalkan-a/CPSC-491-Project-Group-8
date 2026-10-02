using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DungeonCrawler.Core;

public class MainMenuController : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainMenuPanel;
    public GameObject optionsPanel;

    [Header("Option Settings")]
    public Slider volumeSlider;
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;
    public Slider brightnessSlider;

    private Resolution[] resolutions;

    void Start()
    {
        volumeSlider.value = GameSettings.Volume;
        AudioListener.volume = GameSettings.Volume;

        brightnessSlider.value = GameSettings.Brightness;

        // Set fullscreen toggle to current fullscreen state
        fullscreenToggle.isOn = Screen.fullScreen;

        // Set up resolution dropdown
        resolutions = Screen.resolutions
            .OrderByDescending(r => r.width * r.height)
            .ToArray();

        resolutionDropdown.ClearOptions();

        List<string> resolutionOptions = new List<string>();

        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            resolutionOptions.Add(option);

            if (resolutions[i].width == Screen.width &&
                resolutions[i].height == Screen.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(resolutionOptions);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("Level_01");
    }

    public void OpenOptions()
    {
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        optionsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game");
    }

    // -------------------------
    // VOLUME
    // -------------------------

    public void SetVolume(float volume)
    {
        GameSettings.Volume = volume;
        AudioListener.volume = volume;
        GameSettings.Save();
    }

    // -------------------------
    // BRIGHTNESS
    // -------------------------

    public void SetBrightness(float brightness)
    {
        GameSettings.Brightness = brightness;
        GameSettings.Save();
    }

    // -------------------------
    // FULLSCREEN
    // -------------------------

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    // -------------------------
    // RESOLUTION
    // -------------------------

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];

        Screen.SetResolution(
            resolution.width,
            resolution.height,
            Screen.fullScreen
        );
    }
}
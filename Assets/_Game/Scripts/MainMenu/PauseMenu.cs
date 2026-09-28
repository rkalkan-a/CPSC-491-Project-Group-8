using System.ComponentModel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using DungeonCrawler.UI;

public class PauseMenu : MonoBehaviour
{
    public GameObject Container;
    public static bool IsPaused { get; private set; }

    void Awake()
    {
        // Ensure paused state and time scale are reset when the scene loads
        IsPaused = false;
        Time.timeScale = 1f;

        if (Container != null)
            Container.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame) {

            // escape is closing the chest popup, don't pause too
            if (ChestPanel.Instance != null && (ChestPanel.Instance.IsShowing || ChestPanel.ClosedOnFrame == Time.frameCount))
                return;

            if (Container.activeSelf) {
                Time.timeScale = 1f; 
                Container.SetActive(false); 
                IsPaused = false;
            } else {
                Time.timeScale = 0f;          
                Container.SetActive(true); 
                IsPaused = true;
            }
        }
    }

    public void Resume() {
        Time.timeScale = 1f;
        Container.SetActive(false); 
        IsPaused = false;
    }

    public void MainMenu() {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main Menu");
    }
}

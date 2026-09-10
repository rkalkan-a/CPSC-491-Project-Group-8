using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.UI
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private string firstLevel = "Level_01";
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject optionsPanel;

        private void Start()
        {
            ShowMain();
        }

        public void StartGame()
        {
            SceneManager.LoadScene(firstLevel);
        }

        public void ShowOptions()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(true);
        }

        public void ShowMain()
        {
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (mainPanel != null) mainPanel.SetActive(true);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

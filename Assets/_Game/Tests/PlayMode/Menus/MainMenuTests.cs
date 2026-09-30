using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DungeonCrawler.Tests
{
    public class MainMenuTests
    {
        private float originalVolume;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            originalVolume = AudioListener.volume;

            Time.timeScale = 1f;

            SceneManager.LoadScene("Main Menu");

            // Wait one frame so the scene and MainMenuController can initialize.
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            AudioListener.volume = originalVolume;

            PlayerPrefs.DeleteKey("Volume");
            PlayerPrefs.Save();

            Time.timeScale = 1f;

            // Give Unity one frame to finish cleanup before the next test starts.
            yield return null;
        }

        [UnityTest]
        public IEnumerator MainMenu_LoadsSuccessfully()
        {
            Assert.AreEqual(
                "Main Menu",
                SceneManager.GetActiveScene().name,
                "Main Menu scene should be loaded"
            );

            yield return null;
        }

        [UnityTest]
        public IEnumerator MainMenuController_Exists()
        {
            var controller = Object.FindAnyObjectByType<MainMenuController>();

            Assert.IsNotNull(
                controller,
                "MainMenuController should exist in the Main Menu scene"
            );

            yield return null;
        }

        [UnityTest]
        public IEnumerator OpenOptions_ShowsOptionsAndHidesMainMenu()
        {
            var controller = Object.FindAnyObjectByType<MainMenuController>();

            Assert.IsNotNull(controller);

            controller.OpenOptions();

            yield return null;

            Assert.IsFalse(
                controller.mainMenuPanel.activeSelf,
                "Main menu panel should be hidden after opening options"
            );

            Assert.IsTrue(
                controller.optionsPanel.activeSelf,
                "Options panel should be visible after opening options"
            );
        }

        [UnityTest]
        public IEnumerator CloseOptions_ShowsMainMenuAndHidesOptions()
        {
            var controller = Object.FindAnyObjectByType<MainMenuController>();

            Assert.IsNotNull(controller);

            controller.OpenOptions();
            yield return null;

            controller.CloseOptions();
            yield return null;

            Assert.IsTrue(
                controller.mainMenuPanel.activeSelf,
                "Main menu panel should be visible after closing options"
            );

            Assert.IsFalse(
                controller.optionsPanel.activeSelf,
                "Options panel should be hidden after closing options"
            );
        }

        [UnityTest]
        public IEnumerator SetVolume_ChangesAudioVolumeAndSavesPreference()
        {
            var controller = Object.FindAnyObjectByType<MainMenuController>();

            Assert.IsNotNull(controller);

            const float testVolume = 0.5f;

            controller.SetVolume(testVolume);

            yield return null;

            Assert.AreEqual(
                testVolume,
                AudioListener.volume,
                0.01f,
                "AudioListener volume should match the selected volume"
            );

            Assert.AreEqual(
                testVolume,
                PlayerPrefs.GetFloat("Volume"),
                0.01f,
                "Volume preference should be saved"
            );
        }
    }
}
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using DungeonCrawler.Core;

namespace DungeonCrawler.Tests
{
    public class PauseMenuTests
    {
        private float originalVolume;
        private float originalSavedVolume;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            originalVolume = AudioListener.volume;
            originalSavedVolume = GameSettings.Volume;

            Time.timeScale = 1f;

            SceneManager.LoadScene("Level_01");

            // Wait one frame so the scene and PauseMenu can initialize.
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            AudioListener.volume = originalVolume;

            // Put the saved volume back so the tests don't change the real setting.
            GameSettings.Volume = originalSavedVolume;
            GameSettings.Save();

            Time.timeScale = 1f;

            // Give Unity one frame to finish cleanup before the next test starts.
            yield return null;
        }

        [UnityTest]
        public IEnumerator Level01_LoadsSuccessfully()
        {
            Assert.AreEqual(
                "Level_01",
                SceneManager.GetActiveScene().name,
                "Level_01 scene should be loaded"
            );

            yield return null;
        }

        [UnityTest]
        public IEnumerator PauseMenu_Exists()
        {
            var pauseMenu = Object.FindAnyObjectByType<PauseMenu>();

            Assert.IsNotNull(
                pauseMenu,
                "PauseMenu should exist in the Level_01 scene"
            );

            yield return null;
        }

        [UnityTest]
        public IEnumerator PauseMenu_StartsHiddenAndGameIsUnpaused()
        {
            var pauseMenu = Object.FindAnyObjectByType<PauseMenu>();

            Assert.IsNotNull(pauseMenu);
            Assert.IsNotNull(
                pauseMenu.Container,
                "PauseMenu Container should be assigned"
            );

            Assert.IsFalse(
                pauseMenu.Container.activeSelf,
                "Pause menu should be hidden when the level starts"
            );

            Assert.IsFalse(
                PauseMenu.IsPaused,
                "PauseMenu.IsPaused should be false when the level starts"
            );

            Assert.AreEqual(
                1f,
                Time.timeScale,
                0.01f,
                "Time.timeScale should be 1 when the level starts"
            );

            yield return null;
        }

        [UnityTest]
        public IEnumerator Resume_HidesPauseMenuAndRestoresTimeScale()
        {
            var pauseMenu = Object.FindAnyObjectByType<PauseMenu>();

            Assert.IsNotNull(pauseMenu);
            Assert.IsNotNull(pauseMenu.Container);

            // Put the game into the same visible/frozen state as a paused game.
            pauseMenu.Container.SetActive(true);
            Time.timeScale = 0f;

            pauseMenu.Resume();

            yield return null;

            Assert.IsFalse(
                pauseMenu.Container.activeSelf,
                "Pause menu should be hidden after Resume()"
            );

            Assert.IsFalse(
                PauseMenu.IsPaused,
                "PauseMenu.IsPaused should be false after Resume()"
            );

            Assert.AreEqual(
                1f,
                Time.timeScale,
                0.01f,
                "Time.timeScale should return to 1 after Resume()"
            );
        }

        [UnityTest]
        public IEnumerator SetVolume_ChangesAudioVolumeAndSavesPreference()
        {
            var pauseMenu = Object.FindAnyObjectByType<PauseMenu>();

            Assert.IsNotNull(pauseMenu);

            const float testVolume = 0.5f;

            pauseMenu.SetVolume(testVolume);

            yield return null;

            Assert.AreEqual(
                testVolume,
                AudioListener.volume,
                0.01f,
                "AudioListener volume should match the selected volume"
            );

            Assert.AreEqual(
                testVolume,
                GameSettings.Volume,
                0.01f,
                "Volume preference should be saved"
            );
        }

        [UnityTest]
        public IEnumerator MainMenu_LoadsMainMenuSceneAndRestoresTimeScale()
        {
            var pauseMenu = Object.FindAnyObjectByType<PauseMenu>();

            Assert.IsNotNull(pauseMenu);

            Time.timeScale = 0f;

            pauseMenu.MainMenu();

            // Wait one frame for the scene change to complete.
            yield return null;

            Assert.AreEqual(
                "Main Menu",
                SceneManager.GetActiveScene().name,
                "MainMenu() should load the Main Menu scene"
            );

            Assert.AreEqual(
                1f,
                Time.timeScale,
                0.01f,
                "MainMenu() should restore Time.timeScale before changing scenes"
            );
        }
    }
}

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using DungeonCrawler.Core;

namespace DungeonCrawler.Tests
{
    public class BrightnessSliderTests
    {
        private float originalBrightness;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            originalBrightness = GameSettings.Brightness;
            Time.timeScale = 1f;
            SceneManager.LoadScene("Main Menu");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameSettings.Brightness = originalBrightness;
            GameSettings.Save();
            Time.timeScale = 1f;
            yield return null;
        }

        private MainMenuController GetController()
        {
            var controller = Object.FindAnyObjectByType<MainMenuController>();
            Assert.IsNotNull(controller, "MainMenuController should exist in Main Menu.");
            return controller;
        }

        private Image GetOverlay()
        {
            var applier = Object.FindAnyObjectByType<SettingsApplier>();
            Assert.IsNotNull(applier, "SettingsApplier should be initialized.");
            var canvas = applier.transform.Find("BrightnessOverlay");
            Assert.IsNotNull(canvas, "BrightnessOverlay canvas should exist.");
            var overlay = canvas.Find("Overlay");
            Assert.IsNotNull(overlay, "Overlay image should exist.");
            return overlay.GetComponent<Image>();
        }

        [UnityTest]
        public IEnumerator MinimumBrightness_AppliesDarkOverlay()
        {
            GetController().SetBrightness(0.5f);
            yield return null;
            Assert.AreEqual(0.5f, GameSettings.Brightness, 0.001f);
            var color = GetOverlay().color;
            Assert.AreEqual(0f, color.r, 0.001f);
            Assert.AreEqual(0f, color.g, 0.001f);
            Assert.AreEqual(0f, color.b, 0.001f);
            Assert.AreEqual(0.5f, color.a, 0.001f);
        }

        [UnityTest]
        public IEnumerator MidpointBrightness_HasTransparentOverlay()
        {
            GetController().SetBrightness(1f);
            yield return null;
            Assert.AreEqual(1f, GameSettings.Brightness, 0.001f);
            Assert.AreEqual(0f, GetOverlay().color.a, 0.001f);
        }

        [UnityTest]
        public IEnumerator MaximumBrightness_AppliesLightOverlay()
        {
            GetController().SetBrightness(1.5f);
            yield return null;
            Assert.AreEqual(1.5f, GameSettings.Brightness, 0.001f);
            var color = GetOverlay().color;
            Assert.AreEqual(1f, color.r, 0.001f);
            Assert.AreEqual(1f, color.g, 0.001f);
            Assert.AreEqual(1f, color.b, 0.001f);
            Assert.AreEqual(0.2f, color.a, 0.001f);
        }

        [UnityTest]
        public IEnumerator BrightnessOutsideRange_IsClamped()
        {
            GetController().SetBrightness(0f);
            yield return null;
            Assert.AreEqual(0.5f, GameSettings.Brightness, 0.001f);
            GetController().SetBrightness(2f);
            yield return null;
            Assert.AreEqual(1.5f, GameSettings.Brightness, 0.001f);
        }

        [UnityTest]
        public IEnumerator BrightnessSetting_PersistsWhenOptionsReopened()
        {
            var controller = GetController();
            controller.OpenOptions();
            controller.SetBrightness(1.3f);
            controller.CloseOptions();
            GameSettings.Save();
            yield return null;
            controller.OpenOptions();
            yield return null;
            Assert.IsTrue(controller.optionsPanel.activeSelf);
            Assert.AreEqual(1.3f, GameSettings.Brightness, 0.001f);
            Assert.AreEqual(1.3f, PlayerPrefs.GetFloat("brightness", -1f), 0.001f);
        }
    }
}

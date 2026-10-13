using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DungeonCrawler.Audio;
using DungeonCrawler.Core;

namespace DungeonCrawler.Tests
{
    public class AudioSettingsTests
    {
        private float originalSavedVolume;

        [SetUp]
        public void SetUp()
        {
            // Save the player's current volume setting
            // so the tests do not permanently change it.
            originalSavedVolume = GameSettings.Volume;
        }

        [TearDown]
        public void TearDown()
        {
            // Restore the original volume setting.
            GameSettings.Volume = originalSavedVolume;
            GameSettings.Save();
        }

        [UnityTest]
        public IEnumerator SFXManager_CreatesSingletonInstance()
        {
            GameObject obj = new GameObject("SFXManager Test");

            obj.AddComponent<AudioSource>();
            SFXManager manager = obj.AddComponent<SFXManager>();

            yield return null;

            Assert.IsNotNull(
                SFXManager.Instance,
                "SFXManager.Instance should not be null."
            );

            Assert.AreEqual(
                manager,
                SFXManager.Instance,
                "The created SFXManager should become the singleton instance."
            );

            Object.Destroy(obj);

            yield return null;
        }

        [UnityTest]
        public IEnumerator SetSFXVolume_ChangesAudioSourceVolume()
        {
            GameObject obj = new GameObject("SFXManager Test");

            AudioSource source = obj.AddComponent<AudioSource>();
            SFXManager manager = obj.AddComponent<SFXManager>();

            yield return null;

            const float testVolume = 0.4f;

            manager.SetSFXVolume(testVolume);

            yield return null;

            Assert.AreEqual(
                testVolume,
                source.volume,
                0.01f,
                "AudioSource volume should match the volume given to SetSFXVolume."
            );

            Object.Destroy(obj);

            yield return null;
        }

        [UnityTest]
        public IEnumerator PlaySFX_WithValidClip_PlaysAudio()
        {
            GameObject obj = new GameObject("SFXManager Test");

            AudioSource source = obj.AddComponent<AudioSource>();
            SFXManager manager = obj.AddComponent<SFXManager>();

            yield return null;

            AudioClip testClip = AudioClip.Create(
                "Test Audio Clip",
                44100,
                1,
                44100,
                false
            );

            manager.PlaySFX(testClip);

            yield return null;

            Assert.IsTrue(
                source.isPlaying,
                "AudioSource should be playing after PlaySFX is called."
            );

            Object.Destroy(testClip);
            Object.Destroy(obj);

            yield return null;
        }

        [UnityTest]
        public IEnumerator PlaySFX_WithNullClip_DoesNotThrowException()
        {
            GameObject obj = new GameObject("SFXManager Test");

            obj.AddComponent<AudioSource>();
            SFXManager manager = obj.AddComponent<SFXManager>();

            yield return null;

            Assert.DoesNotThrow(
                () => manager.PlaySFX(null),
                "PlaySFX should safely handle a null AudioClip."
            );

            Object.Destroy(obj);

            yield return null;
        }

        [Test]
        public void GameSettings_VolumeClampsAboveOne()
        {
            GameSettings.Volume = 2f;

            Assert.AreEqual(
                1f,
                GameSettings.Volume,
                0.001f,
                "Volume values above 1 should be clamped to 1."
            );
        }

        [Test]
        public void GameSettings_VolumeClampsBelowZero()
        {
            GameSettings.Volume = -1f;

            Assert.AreEqual(
                0f,
                GameSettings.Volume,
                0.001f,
                "Volume values below 0 should be clamped to 0."
            );
        }

        [Test]
        public void GameSettings_VolumeAcceptsValidValue()
        {
            const float testVolume = 0.65f;

            GameSettings.Volume = testVolume;

            Assert.AreEqual(
                testVolume,
                GameSettings.Volume,
                0.001f,
                "A valid volume between 0 and 1 should be stored correctly."
            );
        }
    }
}
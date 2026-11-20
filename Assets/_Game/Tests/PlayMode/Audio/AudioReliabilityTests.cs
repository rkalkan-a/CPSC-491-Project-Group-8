using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DungeonCrawler.Audio;

namespace DungeonCrawler.Tests
{
    // Sprint 5 regression tests. Compatible with the ORIGINAL SFXManager.cs.
    // No AudioClipSelector or other Sprint 5 production changes are required.
    public class AudioReliabilityTests
    {
        private GameObject managerObject;
        private SFXManager manager;
        private AudioSource source;
        private AudioClip clip;
        private bool ownsManager;
        private float oldVolume;
        private bool oldMute;
        private bool oldEnabled;
        private bool oldPlayOnAwake;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            manager = SFXManager.Instance;
            if (manager == null)
            {
                managerObject = new GameObject("Sprint5 Audio Reliability Manager");
                managerObject.AddComponent<AudioSource>();
                manager = managerObject.AddComponent<SFXManager>();
                ownsManager = true;
            }

            FieldInfo field = typeof(SFXManager).GetField("sfxSource",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "Original SFXManager.sfxSource field was not found.");
            source = (AudioSource)field.GetValue(manager);
            Assert.IsNotNull(source, "SFXManager requires an assigned AudioSource.");

            oldVolume = source.volume;
            oldMute = source.mute;
            oldEnabled = source.enabled;
            oldPlayOnAwake = source.playOnAwake;
            source.Stop();
            source.enabled = true;
            source.playOnAwake = false;
            source.mute = false;
            source.volume = 1f;

            clip = AudioClip.Create("Sprint5 test tone", 44100, 1, 44100, false);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (source != null)
            {
                source.Stop();
                if (!ownsManager)
                {
                    source.volume = oldVolume;
                    source.mute = oldMute;
                    source.enabled = oldEnabled;
                    source.playOnAwake = oldPlayOnAwake;
                }
            }
            if (clip != null) Object.Destroy(clip);
            if (ownsManager && managerObject != null) Object.Destroy(managerObject);
            yield return null;
        }

        [Test]
        public void Manager_HasConfiguredAudioSource()
        {
            Assert.IsNotNull(manager);
            Assert.IsNotNull(source);
            Assert.IsTrue(source.enabled);
        }

        [Test]
        public void NullClip_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => manager.PlaySFX(null));
        }

        [Test]
        public void Volume_ValidValueIsApplied()
        {
            manager.SetSFXVolume(0.35f);
            Assert.AreEqual(0.35f, source.volume, 0.001f);
        }

        [Test]
        public void Volume_AboveMaximumIsClamped()
        {
            manager.SetSFXVolume(2f);
            Assert.AreEqual(1f, source.volume, 0.001f);
        }

        [Test]
        public void Volume_BelowMinimumIsClamped()
        {
            manager.SetSFXVolume(-1f);
            Assert.AreEqual(0f, source.volume, 0.001f);
        }

        [Test]
        public void Mute_DoesNotOverwriteConfiguredVolume()
        {
            manager.SetSFXVolume(0.6f);
            source.mute = true;
            Assert.IsTrue(source.mute);
            Assert.AreEqual(0.6f, source.volume, 0.001f);
            source.mute = false;
            Assert.AreEqual(0.6f, source.volume, 0.001f);
        }

        [UnityTest]
        public IEnumerator ValidClip_StartsPlayback()
        {
            manager.PlaySFX(clip);
            yield return null;
            Assert.IsTrue(source.isPlaying,
                "PlayOneShot should start playback in a Play Mode test with audio enabled.");
        }

        [UnityTest]
        public IEnumerator RepeatedPlayback_DoesNotThrowOrStopSource()
        {
            Assert.DoesNotThrow(() =>
            {
                manager.PlaySFX(clip);
                manager.PlaySFX(clip);
                manager.PlaySFX(clip);
            });
            yield return null;
            Assert.IsTrue(source.isPlaying,
                "Rapid PlayOneShot calls should leave the source playing.");
        }

        [UnityTest]
        public IEnumerator Stop_ClearsPlaybackState()
        {
            manager.PlaySFX(clip);
            yield return null;
            Assert.IsTrue(source.isPlaying);
            source.Stop();
            yield return null;
            Assert.IsFalse(source.isPlaying);
        }

        [UnityTest]
        public IEnumerator Playback_CanRestartAfterStop()
        {
            manager.PlaySFX(clip);
            yield return null;
            source.Stop();
            yield return null;
            manager.PlaySFX(clip);
            yield return null;
            Assert.IsTrue(source.isPlaying);
        }
    }
}

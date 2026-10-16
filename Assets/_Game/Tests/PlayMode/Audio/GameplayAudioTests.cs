using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DungeonCrawler.Audio;
using DungeonCrawler.Combat;
using DungeonCrawler.Loot;
using DungeonCrawler.Player;

namespace DungeonCrawler.Tests
{
    // Playmode tests for audio playback
    public class GameplayAudioTests
    {
        private GameObject managerObject;
        private GameObject actorObject;
        private AudioSource source;
        private AudioClip clip;
        private SFXManager manager;
        private bool createdManager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Reuse an existing manager if a scene or another test has one.
            manager = SFXManager.Instance;
            if (manager == null)
            {
                managerObject = new GameObject("Sprint3 SFX Manager");
                source = managerObject.AddComponent<AudioSource>();
                manager = managerObject.AddComponent<SFXManager>();
                createdManager = true;
            }
            else
            {
                source = manager.GetComponent<AudioSource>();
                if (source == null)
                {
                    // The manager may use a serialized AudioSource on another object.
                    source = (AudioSource)typeof(SFXManager)
                        .GetField("sfxSource", BindingFlags.Instance | BindingFlags.NonPublic)
                        .GetValue(manager);
                }
            }

            Assert.IsNotNull(source, "The SFXManager must have a working SFX AudioSource.");
            source.Stop();
            source.playOnAwake = false;
            source.mute = false;
            source.volume = 1f;
            clip = AudioClip.Create("Sprint3 runtime sound", 44100, 1, 44100, false);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (source != null) source.Stop();
            if (actorObject != null) Object.Destroy(actorObject);
            if (clip != null) Object.Destroy(clip);
            if (createdManager && managerObject != null) Object.Destroy(managerObject);
            yield return null;
        }

        private static void AssignAudioField<T>(Component target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Expected audio field was not found: " + fieldName);
            field.SetValue(target, value);
        }

        private Chest CreateChest()
        {
            actorObject = new GameObject("Sprint3 Chest");
            actorObject.SetActive(false);
            actorObject.AddComponent<BoxCollider2D>();
            Chest chest = actorObject.AddComponent<Chest>();
            AssignAudioField(chest, "openSound", clip);
            actorObject.SetActive(true);
            return chest;
        }

        [UnityTest]
        public IEnumerator ChestOpen_PlaysSoundEffect()
        {
            Chest chest = CreateChest();
            yield return null;
            Assert.IsFalse(chest.IsOpen);
            chest.Open(null); 
            yield return null;
            Assert.IsTrue(chest.IsOpen, "Chest should be open after the interaction.");
            Assert.IsTrue(source.isPlaying, "Opening a chest should trigger its SFX.");
        }

        [UnityTest]
        public IEnumerator PlayerTakesDamage_PlaysHurtSound()
        {
            actorObject = new GameObject("Sprint3 Player Audio");
            actorObject.SetActive(false);
            Health health = actorObject.AddComponent<Health>();
            PlayerAudio playerAudio = actorObject.AddComponent<PlayerAudio>();
            // One clip keeps the randomized playback selection deterministic.
            AssignAudioField(playerAudio, "hurtSounds", new[] { clip });
            actorObject.SetActive(true);
            yield return null;

            int before = health.Current;
            Assert.Greater(before, 1, "Test player must have enough health to survive a hit.");
            health.TakeDamage(1);
            yield return null;

            Assert.AreEqual(before - 1, health.Current, "Damage must reach the Health component.");
            Assert.IsTrue(source.isPlaying, "Health loss should trigger the player's hurt SFX.");
        }

        [UnityTest]
        public IEnumerator RepeatedChestInteraction_DoesNotReplayOpeningSound()
        {
            Chest chest = CreateChest();
            yield return null;
            chest.Open(null);
            yield return null;
            Assert.IsTrue(source.isPlaying, "First opening should trigger audio.");

            // Stop the first playback, then interact again to detect a new request.
            source.Stop();
            chest.Open(null);
            yield return null;

            Assert.IsTrue(chest.IsOpen);
            Assert.IsFalse(source.isPlaying,
                "An already-open chest should not replay its opening SFX.");
        }
    }
}

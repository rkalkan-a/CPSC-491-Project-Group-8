using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DungeonCrawler.Audio;
using DungeonCrawler.Combat;
using DungeonCrawler.Core;
using DungeonCrawler.Enemies;

namespace DungeonCrawler.Tests
{
    // Sprint 4: abnormal configuration and runtime recovery checks.
    public class SystemAnomalyTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private float originalTimeScale;
        private float originalVolume;
        private float originalBrightness;
        private float originalTextScale;

        [SetUp]
        public void SetUp()
        {
            originalTimeScale = Time.timeScale;
            Time.timeScale = 1f;
            originalVolume = GameSettings.Volume;
            originalBrightness = GameSettings.Brightness;
            originalTextScale = GameSettings.TextScale;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in objects)
                if (obj != null) Object.DestroyImmediate(obj);
            objects.Clear();
            GameSettings.Volume = originalVolume;
            GameSettings.Brightness = originalBrightness;
            GameSettings.TextScale = originalTextScale;
            Time.timeScale = originalTimeScale;
        }

        private GameObject Track(string name)
        {
            var obj = new GameObject(name);
            objects.Add(obj);
            return obj;
        }

        [Test]
        public void Anomaly_InvalidBrightness_IsClampedToSupportedRange()
        {
            GameSettings.Brightness = -100f;
            Assert.That(GameSettings.Brightness, Is.EqualTo(0.5f).Within(0.001f));
            GameSettings.Brightness = 100f;
            Assert.That(GameSettings.Brightness, Is.EqualTo(1.5f).Within(0.001f));
        }

        [Test]
        public void Anomaly_InvalidTextScale_IsClampedToSupportedRange()
        {
            GameSettings.TextScale = -10f;
            Assert.That(GameSettings.TextScale, Is.EqualTo(0.8f).Within(0.001f));
            GameSettings.TextScale = 10f;
            Assert.That(GameSettings.TextScale, Is.EqualTo(1.6f).Within(0.001f));
        }

        [Test]
        public void Anomaly_InvalidMaxHealth_DoesNotProduceZeroOrNegativeCapacity()
        {
            var health = Track("InvalidHealthConfiguration").AddComponent<Health>();
            health.SetMax(-20);
            Assert.GreaterOrEqual(health.Max, 1);
            Assert.That(health.Current, Is.InRange(0, health.Max));
        }

        [Test]
        public void Anomaly_ExcessiveHealing_DoesNotExceedMaximum()
        {
            var health = Track("OverhealCheck").AddComponent<Health>();
            health.TakeDamage(2);
            health.Heal(1000000);
            Assert.AreEqual(health.Max, health.Current);
        }

        [Test]
        public void Anomaly_MissingSfxSource_IsRecoveredFromRequiredComponent()
        {
            // A pre-existing SFXManager in a loaded scene would own the singleton;
            // this test only verifies the local fallback reference.
            var obj = Track("SfxFallbackCheck");
            var source = obj.AddComponent<AudioSource>();
            var manager = obj.AddComponent<SFXManager>();
            var field = typeof(SFXManager).GetField("sfxSource", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            Assert.AreSame(source, field.GetValue(manager));
        }

        [Test]
        public void Anomaly_MissingAudioClip_IsHandledWithoutException()
        {
            var obj = Track("MissingClipCheck");
            obj.AddComponent<AudioSource>();
            var manager = obj.AddComponent<SFXManager>();
            Assert.DoesNotThrow(() => manager.PlaySFX(null));
        }

        [UnityTest]
        public IEnumerator Anomaly_EnemyWithoutPlayerTarget_RemainsOperational()
        {
            var obj = Track("NoTargetEnemy");
            obj.AddComponent<Rigidbody2D>();
            obj.AddComponent<CircleCollider2D>();
            var enemy = obj.AddComponent<Enemy>();
            var targetField = typeof(Enemy).GetField("target", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(targetField);
            targetField.SetValue(enemy, null);
            yield return new WaitForFixedUpdate();
            Assert.IsNotNull(enemy);
            Assert.IsFalse(float.IsNaN(obj.GetComponent<Rigidbody2D>().linearVelocity.x));
            Assert.IsFalse(float.IsNaN(obj.GetComponent<Rigidbody2D>().linearVelocity.y));
        }
    }
}

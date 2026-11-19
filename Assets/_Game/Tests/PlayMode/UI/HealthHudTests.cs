using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using DungeonCrawler.Combat;
using DungeonCrawler.UI;

namespace DungeonCrawler.Tests
{
    // SCRUM-99: coverage for the hearts in the top left.
    //
    // A heart is Health.HpPerHeart (4) hp, so the HUD has to turn hp into whole hearts plus a partly filled
    // last heart. The tests use equivalence classes and boundaries on that conversion:
    //   heart count - exact multiples of 4, and a remainder that has to round up to one more heart
    //   fill amount - full, three quarters, a quarter, empty
    //   changes     - damage, healing and a bigger max (armor) all redraw the row
    public class HealthHudTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned)
                if (go != null) Object.Destroy(go);
            spawned.Clear();
        }

        // ---------- heart count ----------

        [UnityTest]
        public IEnumerator Shows_OneHeartPerFourHp([Values(4, 8, 12)] int max)
        {
            var (hud, health) = Spawn(max);
            yield return null;

            Assert.AreEqual(max / Health.HpPerHeart, ActiveHearts(hud).Count);
        }

        [UnityTest]
        public IEnumerator PartialHeartOfHp_RoundsUp_ToAnExtraHeart()
        {
            var (hud, health) = Spawn(6); // 1.5 hearts of hp
            yield return null;

            Assert.AreEqual(2, ActiveHearts(hud).Count, "6 hp should show as 2 hearts, the second one half full");
            Assert.AreEqual(0.5f, Fill(ActiveHearts(hud)[1]), 0.001f);
        }

        [UnityTest]
        public IEnumerator FullHealth_ShowsEveryHeartFull()
        {
            var (hud, health) = Spawn(12);
            yield return null;

            foreach (var heart in ActiveHearts(hud))
                Assert.AreEqual(1f, Fill(heart), 0.001f);
        }

        // ---------- fill amount after damage ----------

        [UnityTest]
        public IEnumerator OneDamage_EmptiesAQuarter_OfTheLastHeart()
        {
            var (hud, health) = Spawn(12);
            yield return null;

            health.TakeDamage(1);

            var hearts = ActiveHearts(hud);
            Assert.AreEqual(1f, Fill(hearts[0]), 0.001f);
            Assert.AreEqual(1f, Fill(hearts[1]), 0.001f);
            Assert.AreEqual(0.75f, Fill(hearts[2]), 0.001f, "the last heart should be three quarters full");
        }

        [UnityTest]
        public IEnumerator LosingAWholeHeart_EmptiesExactlyOneHeart()
        {
            var (hud, health) = Spawn(12);
            yield return null;

            health.TakeDamage(Health.HpPerHeart);

            var hearts = ActiveHearts(hud);
            Assert.AreEqual(1f, Fill(hearts[1]), 0.001f);
            Assert.AreEqual(0f, Fill(hearts[2]), 0.001f);
            Assert.AreEqual(3, hearts.Count, "an empty heart stays in the row, it just shows empty");
        }

        [UnityTest]
        public IEnumerator Death_EmptiesEveryHeart()
        {
            var (hud, health) = Spawn(8);
            yield return null;

            health.TakeDamage(100);

            foreach (var heart in ActiveHearts(hud))
                Assert.AreEqual(0f, Fill(heart), 0.001f);
        }

        // ---------- changes ----------

        [UnityTest]
        public IEnumerator Healing_RefillsTheHeart()
        {
            var (hud, health) = Spawn(8);
            yield return null;
            health.TakeDamage(3);
            Assert.AreEqual(0.25f, Fill(ActiveHearts(hud)[1]), 0.001f);

            health.Heal(3);

            Assert.AreEqual(1f, Fill(ActiveHearts(hud)[1]), 0.001f);
        }

        [UnityTest]
        public IEnumerator RaisingMaxHealth_AddsAHeart_ThatStartsFull()
        {
            var (hud, health) = Spawn(8);
            yield return null;

            health.SetMax(health.Max + Health.HpPerHeart); // what putting on armor does

            var hearts = ActiveHearts(hud);
            Assert.AreEqual(3, hearts.Count);
            Assert.AreEqual(1f, Fill(hearts[2]), 0.001f);
        }

        [UnityTest]
        public IEnumerator Hud_StopsUpdating_WhenDisabled()
        {
            var (hud, health) = Spawn(8);
            yield return null;
            var lastHeart = ActiveHearts(hud)[1];

            hud.enabled = false;
            health.TakeDamage(2);

            Assert.AreEqual(1f, Fill(lastHeart), 0.001f, "a disabled HUD shouldn't keep listening to health changes");
        }

        // ---------- fixtures ----------

        private (HealthHUD, Health) Spawn(int maxHp)
        {
            var target = new GameObject("TestPlayerHealth");
            spawned.Add(target);
            var health = target.AddComponent<Health>();
            health.SetMax(maxHp);

            var canvas = new GameObject("TestHudCanvas", typeof(RectTransform), typeof(Canvas));
            spawned.Add(canvas);
            var root = new GameObject("Hearts", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);

            var hud = canvas.AddComponent<HealthHUD>();
            SetField(hud, "heartsRoot", (RectTransform)root.transform);

            // in the game the HUD finds the player through GameManager when the run starts. here we hand it the health directly
            var track = typeof(HealthHUD).GetMethod("Track", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(track, "HealthHUD has no Track method, the test needs updating");
            track.Invoke(hud, new object[] { health });

            return (hud, health);
        }

        private static List<GameObject> ActiveHearts(HealthHUD hud)
        {
            var root = hud.transform.Find("Hearts");
            return root.Cast<Transform>()
                .Where(t => t.gameObject.activeSelf && t.name.StartsWith("Heart"))
                .Select(t => t.gameObject)
                .ToList();
        }

        private static float Fill(GameObject heart) => heart.transform.Find("Fill").GetComponent<Image>().fillAmount;

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"{target.GetType().Name} has no field '{name}', the test needs updating");
            field.SetValue(target, value);
        }
    }
}

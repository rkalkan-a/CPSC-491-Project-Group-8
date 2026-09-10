using System;
using UnityEngine;

namespace DungeonCrawler.Core
{
    // options menu values. saved in PlayerPrefs so they stick between sessions
    public static class GameSettings
    {
        public static event Action Changed;

        // 0 = off, 1 = low, 2 = full
        public const int GoreOff = 0, GoreLow = 1, GoreFull = 2;

        public static float Volume
        {
            get => PlayerPrefs.GetFloat("volume", 0.8f);
            set { PlayerPrefs.SetFloat("volume", Mathf.Clamp01(value)); Changed?.Invoke(); }
        }

        public static int Gore
        {
            get => PlayerPrefs.GetInt("gore", GoreFull);
            set { PlayerPrefs.SetInt("gore", Mathf.Clamp(value, GoreOff, GoreFull)); Changed?.Invoke(); }
        }

        public static float TextScale
        {
            get => PlayerPrefs.GetFloat("textScale", 1f);
            set { PlayerPrefs.SetFloat("textScale", Mathf.Clamp(value, 0.8f, 1.6f)); Changed?.Invoke(); }
        }

        public static float Brightness
        {
            get => PlayerPrefs.GetFloat("brightness", 1f);
            set { PlayerPrefs.SetFloat("brightness", Mathf.Clamp(value, 0.5f, 1.5f)); Changed?.Invoke(); }
        }

        // used for aiming once ranged attacks are in, movement speed doesn't change
        public static float Sensitivity
        {
            get => PlayerPrefs.GetFloat("sensitivity", 1f);
            set { PlayerPrefs.SetFloat("sensitivity", Mathf.Clamp(value, 0.5f, 2f)); Changed?.Invoke(); }
        }

        public static string GoreName(int gore) => gore switch
        {
            GoreOff => "Off",
            GoreLow => "Low",
            _ => "Full",
        };

        public static void Save() => PlayerPrefs.Save();
    }
}

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DungeonCrawler.Tests
{
    // loads the real scenes and checks the real music AudioSources.
    public class SceneMusicPlayModeTests
    {
        private const string MenuScene = "Main Menu";
        private const string LevelScene = "Level_01";
        private const string MenuClip = "Title Theme";
        private const string LevelClip = "Level 1 - Forest Theme";

        private static AudioSource GetMusicSource()
        {
            GameObject musicObject = GameObject.Find("MusicManager");
            Assert.IsNotNull(musicObject, "The loaded scene must contain an active MusicManager object.");
            AudioSource source = musicObject.GetComponent<AudioSource>();
            Assert.IsNotNull(source, "MusicManager must have an AudioSource.");
            return source;
        }

        private static IEnumerator LoadSceneAndWaitForMusic(string scene)
        {
            // Scene must be included in Build Settings for LoadSceneAsync to work.
            AsyncOperation load = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
            Assert.IsNotNull(load, "Could not start loading scene: " + scene);
            while (!load.isDone) yield return null;
            // Allow play-on-awake AudioSources to start.
            yield return null;
            yield return new WaitForSecondsRealtime(0.2f);
        }

        private static void AssertMusicPlaying(string expectedClip)
        {
            AudioSource source = GetMusicSource();
            Assert.IsNotNull(source.clip, "MusicManager has no assigned music clip.");
            Assert.AreEqual(expectedClip, source.clip.name, "The scene has the wrong music clip.");
            Assert.IsTrue(source.isPlaying, "The scene music should be playing during Play Mode.");
        }

        [UnityTest]
        public IEnumerator MainMenu_PlaysTitleTheme()
        {
            yield return LoadSceneAndWaitForMusic(MenuScene);
            AssertMusicPlaying(MenuClip);
        }

        [UnityTest]
        public IEnumerator Level1_PlaysForestTheme()
        {
            yield return LoadSceneAndWaitForMusic(LevelScene);
            AssertMusicPlaying(LevelClip);
        }

        [UnityTest]
        public IEnumerator MainMenu_MusicLoopsDuringPlay()
        {
            yield return LoadSceneAndWaitForMusic(MenuScene);
            AudioSource source = GetMusicSource();
            AssertMusicPlaying(MenuClip);
            Assert.IsTrue(source.loop, "Main menu music should loop while the menu is open.");
        }

        [UnityTest]
        public IEnumerator Level1_MusicLoopsDuringPlay()
        {
            yield return LoadSceneAndWaitForMusic(LevelScene);
            AudioSource source = GetMusicSource();
            AssertMusicPlaying(LevelClip);
            Assert.IsTrue(source.loop, "Level 1 music should loop during gameplay.");
        }
    }
}

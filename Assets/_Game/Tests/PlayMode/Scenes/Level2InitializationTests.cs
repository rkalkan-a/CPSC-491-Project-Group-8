
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DungeonCrawler.Tests
{
    public class Level2InitializationTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;

            SceneManager.LoadScene("Level_02");
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Level2_LoadsSuccessfully()
        {
            Assert.AreEqual(
                "Level_02",
                SceneManager.GetActiveScene().name,
                "Level 2 should be the active scene."
            );

            yield return null;
        }

        [UnityTest]
        public IEnumerator Level2_GameManagerExists()
        {
            GameObject manager = GameObject.Find("GameManager");

            Assert.IsNotNull(
                manager,
                "GameManager should exist in Level 2."
            );

            Assert.IsTrue(manager.activeInHierarchy);

            yield return null;
        }

        [UnityTest]
        public IEnumerator Level2_PlayerSpawns()
        {
            GameObject player = null;

            for (int i = 0; i < 30; i++)
            {
                player = GameObject.Find("Player");

                if (player != null)
                    break;

                yield return null;
            }

            Assert.IsNotNull(
                player,
                "Player should spawn when Level 2 initializes."
            );

            Assert.IsTrue(
                player.activeInHierarchy,
                "Player should be active after spawning."
            );
        }

        [UnityTest]
        public IEnumerator Level2_RequiredObjectsExist()
        {
            Assert.IsNotNull(
                GameObject.Find("Main Camera"),
                "Main Camera should exist."
            );

            Assert.IsNotNull(
                GameObject.Find("Rooms"),
                "Rooms container should exist."
            );

            Assert.IsNotNull(
                GameObject.Find("TestRoom"),
                "TestRoom should exist."
            );

            Assert.IsNotNull(
                GameObject.Find("PlayerSpawn_Start"),
                "Player spawn point should exist."
            );

            yield return null;
        }

        [UnityTest]
        public IEnumerator Level2_RoomManagerExists()
        {
            GameObject manager = GameObject.Find("GameManager");

            Assert.IsNotNull(manager);

            var roomManager = manager.GetComponent(
                "RoomManager"
            );

            Assert.IsNotNull(
                roomManager,
                "RoomManager component should be initialized."
            );

            yield return null;
        }
    }
}

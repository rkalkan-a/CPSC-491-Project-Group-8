using UnityEditor;
using UnityEditor.SceneManagement;

namespace DungeonCrawler.EditorTools
{
    // one click to play the whole game from the title screen. doesn't change anything in the project
    public static class PlayGameMenu
    {
        private const string MainMenuScene = "Assets/_Game/Scenes/Main Menu.unity";
        private const string FirstLevel = "Assets/_Game/Scenes/Level_01.unity";

        [MenuItem("Tools/Dungeon/▶ Play Game (from the main menu)", priority = -100)]
        public static void PlayFromMainMenu() => Play(MainMenuScene);

        [MenuItem("Tools/Dungeon/▶ Play Level_01 (skip the menu)", priority = -99)]
        public static void PlayFirstLevel() => Play(FirstLevel);

        private static void Play(string scenePath)
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(scenePath);
            EditorApplication.EnterPlaymode();
        }
    }
}

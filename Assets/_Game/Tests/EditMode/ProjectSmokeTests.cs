using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ProjectSmokeTests
{
    [Test]
    public void BuildSettings_IncludeMainMenuAndLevel01()
    {
        var enabled = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
        Assert.Contains("Assets/_Game/Scenes/Main Menu.unity", enabled);
        Assert.Contains("Assets/_Game/Scenes/Level_01.unity", enabled);
    }

    [Test]
    public void BuildSettings_AllEnabledScenesExist()
    {
        foreach (var scene in EditorBuildSettings.scenes.Where(s => s.enabled))
            Assert.IsTrue(File.Exists(scene.path), $"missing scene: {scene.path}");
    }

    [Test]
    public void Level01_HasPlayerCameraAndPauseMenu()
    {
        var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/Level_01.unity", OpenSceneMode.Additive);
        try
        {
            var names = scene.GetRootGameObjects().Select(g => g.name).ToList();
            Assert.Contains("Player", names);
            Assert.Contains("PauseMenu", names);
            Assert.IsTrue(scene.GetRootGameObjects().Any(g => g.GetComponentInChildren<Camera>() != null), "no camera in scene");
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void PauseMenuPrefab_HasPauseMenuScript()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/PauseMenu.prefab");
        Assert.IsNotNull(prefab, "PauseMenu prefab missing");
        Assert.IsNotNull(prefab.GetComponent("PauseMenu"), "PauseMenu script missing on prefab");
    }
}

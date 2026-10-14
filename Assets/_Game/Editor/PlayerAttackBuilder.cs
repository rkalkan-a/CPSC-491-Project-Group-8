using UnityEditor;
using UnityEngine;
using DungeonCrawler.Player;

namespace DungeonCrawler.EditorTools
{
    // adds PlayerAttack + AimIndicator to the Player prefab and wires up the same
    // InputSystem_Actions asset the PlayerController already uses. Run this once from
    // the menu below, then press play - left click (or gamepad West / touch) to fire.
    public static class PlayerAttackBuilder
    {
        private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Player.prefab";

        [MenuItem("Tools/Dungeon/Advanced (changes the level)/Setup Player Attack", priority = 5)]
        public static void SetupPlayerAttack()
        {
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError("Couldn't find " + PlayerPrefabPath);
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                var controller = root.GetComponent<PlayerController>();
                if (controller == null)
                {
                    Debug.LogError("Player prefab has no PlayerController", root);
                    return;
                }

                var controllerSo = new SerializedObject(controller);
                var inputActionsRef = controllerSo.FindProperty("inputActions").objectReferenceValue;
                if (inputActionsRef == null)
                    Debug.LogWarning("PlayerController's Input Actions field is empty - assign it after this runs.", root);

                var attack = root.GetComponent<PlayerAttack>();
                if (attack == null) attack = root.AddComponent<PlayerAttack>();

                var attackSo = new SerializedObject(attack);
                attackSo.FindProperty("inputActions").objectReferenceValue = inputActionsRef;
                attackSo.ApplyModifiedPropertiesWithoutUndo();

                if (root.GetComponent<AimIndicator>() == null)
                    root.AddComponent<AimIndicator>();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                Debug.Log("Player prefab now has Attack (projectile + fire button) and an aim reticle/range ring. Press play and left-click to fire.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}

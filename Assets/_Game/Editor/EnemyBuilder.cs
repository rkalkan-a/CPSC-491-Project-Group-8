using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DungeonCrawler.Combat;
using DungeonCrawler.Enemies;
using DungeonCrawler.Rooms;

namespace DungeonCrawler.EditorTools
{
    // makes the placeholder enemy prefabs and drops them onto any empty enemy spawn points in the open scene
    public static class EnemyBuilder
    {
        private const string PrefabDir = "Assets/_Game/Prefabs/Enemies";
        private const string SkeletonSpritePath = "Assets/_Game/Art/Enemies/Skeleton_Idle.png";
        private const string SlimeSpritePath = "Assets/_Game/Art/Enemies/Slime_Idle.png";

        [MenuItem("Tools/Dungeon/Advanced (changes the level)/Setup Enemies In Scene", priority = 4)]
        public static void SetupEnemiesInScene()
        {
            // Ghoul = skeleton art (chaser), Crawler = slime art (wanderer) - real sprites from the
            // free Cute Fantasy RPG pack (Kenmi, non-commercial license) instead of placeholder squares
            var ghoul = EnsureEnemyPrefab("Ghoul", SkeletonSpritePath, Color.white, 0.8f, 3, Enemy.Behavior.Chaser, 3f);
            var crawler = EnsureEnemyPrefab("Crawler", SlimeSpritePath, Color.white, 0.45f, 2, Enemy.Behavior.Wanderer, 1.2f);

            int assigned = 0;
            bool alternate = false;
            foreach (var spawn in Object.FindObjectsByType<EnemySpawnPoint>(FindObjectsSortMode.None))
            {
                var so = new SerializedObject(spawn);
                var prop = so.FindProperty("enemyPrefab");
                if (prop.objectReferenceValue != null) continue;
                prop.objectReferenceValue = alternate ? crawler : ghoul;
                alternate = !alternate;
                so.ApplyModifiedProperties();
                assigned++;
            }

            // rooms with spawn points should actually fight now
            foreach (var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None))
            {
                bool hasSpawns = room.GetComponentInChildren<EnemySpawnPoint>(true) != null;
                var so = new SerializedObject(room);
                so.FindProperty("clearedOnEnter").boolValue = !hasSpawns;
                so.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Enemy prefabs ready, assigned " + assigned + " spawn point(s). Save the scene (Cmd+S) then press play.");
        }

        private static GameObject EnsureEnemyPrefab(string name, string spritePath, Color color, float size, int hp, Enemy.Behavior behavior, float speed)
        {
            string path = PrefabDir + "/" + name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            Directory.CreateDirectory(PrefabDir);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
                Debug.LogWarning("No sprite found at " + spritePath + " - " + name + " will be invisible until art is imported.");

            var go = new GameObject(name);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(size, size, 1f);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = 9;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.mass = 3f;
            rb.linearDamping = 2f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = size * 0.5f;

            var health = go.AddComponent<Health>();
            var hso = new SerializedObject(health);
            hso.FindProperty("maxHealth").intValue = hp;
            hso.FindProperty("invulnerabilityTime").floatValue = 0.1f;
            hso.ApplyModifiedPropertiesWithoutUndo();

            go.AddComponent<HurtFlash>();
            go.AddComponent<BleedOnHit>();

            var contact = go.AddComponent<DamageOnContact>();
            var cso = new SerializedObject(contact);
            cso.FindProperty("damage").intValue = 1;
            cso.FindProperty("knockback").floatValue = 7f;
            cso.FindProperty("onlyHurtsPlayer").boolValue = true;
            cso.ApplyModifiedPropertiesWithoutUndo();

            var enemy = go.AddComponent<Enemy>();
            var eso = new SerializedObject(enemy);
            eso.FindProperty("behavior").enumValueIndex = (int)behavior;
            eso.FindProperty("moveSpeed").floatValue = speed;
            eso.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }
    }
}

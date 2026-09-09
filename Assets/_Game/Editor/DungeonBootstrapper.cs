using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using DungeonCrawler.Combat;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using DungeonCrawler.Rooms;

namespace DungeonCrawler.EditorTools
{
    // editor menu (Tools > Dungeon) that builds the test level so we don't have to place everything by hand.
    // everything it makes is a normal scene object, move/resize/delete whatever
    public static class DungeonBootstrapper
    {
        private const string PlaceholderDir = "Assets/_Game/Art/Placeholders";
        private const string SquareSpritePath = PlaceholderDir + "/Square.png";
        private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Player.prefab";
        private const string ScenePath = "Assets/_Game/Scenes/Level_01.unity";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        // room layout in world units, camera fits the room on its own
        private static readonly Vector2 RoomSize = new Vector2(17f, 10f);
        private const float WallThickness = 1f;
        private const float DoorWidth = 2f;
        private const float RoomGap = 3f; // space between rooms

        private static readonly Color FloorColor = new Color(0.16f, 0.14f, 0.17f);
        private static readonly Color WallColor = new Color(0.38f, 0.33f, 0.36f);
        private static readonly Color DoorColor = new Color(0.55f, 0.25f, 0.25f);
        private static readonly Color PlayerColor = new Color(0.85f, 0.92f, 0.95f);

        [MenuItem("Tools/Dungeon/Build Starter Level", priority = 0)]
        public static void BuildStarterLevel()
        {
            if (File.Exists(ScenePath))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Level_01 already exists",
                    "Level_01.unity already exists. Overwrite it? (scripts and prefabs aren't touched)",
                    "Overwrite", "Cancel");
                if (!overwrite) return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Sprite square = EnsureSquareSprite();
            GameObject playerPrefab = EnsurePlayerPrefab(square);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // camera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = RoomSize.y * 0.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.03f, 0.05f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.GetUniversalAdditionalCameraData(); // urp needs this
            camGo.AddComponent<RoomCamera>();

            // rooms
            var roomsRoot = new GameObject("Rooms");
            Vector2 room1Center = Vector2.zero;
            Vector2 room2Center = new Vector2(RoomSize.x + RoomGap, 0f);

            Room room1 = BuildRoom(roomsRoot.transform, "Room_01", room1Center, RoomSize, square,
                new[] { DoorSide.East });
            Room room2 = BuildRoom(roomsRoot.transform, "Room_02", room2Center, RoomSize, square,
                new[] { DoorSide.West });

            // link room 1 east <-> room 2 west
            Door r1East = FindDoor(room1, DoorSide.East);
            Door r2West = FindDoor(room2, DoorSide.West);
            r1East.Link(r2West);
            EditorUtility.SetDirty(r1East);
            EditorUtility.SetDirty(r2West);

            // player start
            PlayerSpawnPoint startSpawn = AddPlayerSpawn(room1, "Start", room1Center + new Vector2(-4f, 0f));

            // enemy spawn markers in room 2, no prefab yet
            AddEnemySpawn(room2, room2Center + new Vector2(2f, 2f));
            AddEnemySpawn(room2, room2Center + new Vector2(3.5f, -2f));

            // put a player in the scene so it's easy to see/edit, GameManager reuses it
            var playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            playerInstance.name = "Player";
            playerInstance.transform.position = startSpawn.Position;

            // managers
            var managers = new GameObject("GameManager");
            var roomManager = managers.AddComponent<RoomManager>();
            SetRef(roomManager, "startingRoom", room1);
            var gameManager = managers.AddComponent<GameManager>();
            SetRef(gameManager, "playerPrefab", playerPrefab.GetComponent<PlayerController>());
            SetRef(gameManager, "startSpawn", startSpawn);

            // save and add to build settings
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.in2DMode = true;
                SceneView.lastActiveSceneView.Frame(new Bounds(room1Center, RoomSize * 1.2f), false);
            }

            Selection.activeObject = room1.gameObject;
            Debug.Log("Built Level_01. Hit play, WASD to move, red doorway switches rooms.");
        }

        [MenuItem("Tools/Dungeon/Add Room To Scene", priority = 1)]
        public static void AddRoomToScene()
        {
            Sprite square = EnsureSquareSprite();

            var existing = Object.FindObjectsByType<Room>(FindObjectsSortMode.None);
            Transform parent = GameObject.Find("Rooms")?.transform;
            if (parent == null) parent = new GameObject("Rooms").transform;

            // put it to the right of the furthest room
            Vector2 center = Vector2.zero;
            float maxRight = float.NegativeInfinity;
            foreach (var r in existing)
            {
                float right = r.Center.x + r.Size.x * 0.5f;
                if (right > maxRight) { maxRight = right; center = new Vector2(right + RoomGap + RoomSize.x * 0.5f, r.Center.y); }
            }

            string name = $"Room_{existing.Length + 1:00}";
            Room room = BuildRoom(parent, name, center, RoomSize, square, new DoorSide[0]);
            Undo.RegisterCreatedObjectUndo(room.gameObject, "Add Room");
            Selection.activeObject = room.gameObject;
            EditorSceneManager.MarkSceneDirty(room.gameObject.scene);
            Debug.Log("Added " + name + ". Use Add Door To Selected Room, then drag doors into each other's Target Door field to link them.");
        }

        [MenuItem("Tools/Dungeon/Add Door To Selected Room/North", priority = 10)] public static void AddDoorNorth() => AddDoorToSelected(DoorSide.North);
        [MenuItem("Tools/Dungeon/Add Door To Selected Room/South", priority = 11)] public static void AddDoorSouth() => AddDoorToSelected(DoorSide.South);
        [MenuItem("Tools/Dungeon/Add Door To Selected Room/East", priority = 12)] public static void AddDoorEast() => AddDoorToSelected(DoorSide.East);
        [MenuItem("Tools/Dungeon/Add Door To Selected Room/West", priority = 13)] public static void AddDoorWest() => AddDoorToSelected(DoorSide.West);

        private static void AddDoorToSelected(DoorSide side)
        {
            Room room = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<Room>() : null;
            if (room == null)
            {
                EditorUtility.DisplayDialog("No room selected", "Select a Room object in the Hierarchy first.", "OK");
                return;
            }
            if (FindDoor(room, side) != null)
            {
                EditorUtility.DisplayDialog("Door exists", $"{room.RoomId} already has a {side} door.", "OK");
                return;
            }

            Sprite square = EnsureSquareSprite();
            // rebuild that wall with a gap then add the door
            Transform wallsRoot = room.transform.Find("Walls");
            if (wallsRoot == null)
            {
                wallsRoot = new GameObject("Walls").transform;
                wallsRoot.SetParent(room.transform, false);
                wallsRoot.localPosition = Vector3.zero;
            }
            for (int i = wallsRoot.childCount - 1; i >= 0; i--)
            {
                var child = wallsRoot.GetChild(i);
                if (child.name.StartsWith(side.ToString())) Undo.DestroyObjectImmediate(child.gameObject);
            }
            BuildWallSide(wallsRoot, room.Center, room.Size, side, true, square);
            Door door = BuildDoor(room.transform, room.Center, room.Size, side, square);
            Undo.RegisterCreatedObjectUndo(door.gameObject, "Add Door");
            room.RefreshChildren();
            EditorSceneManager.MarkSceneDirty(room.gameObject.scene);
            Selection.activeObject = door.gameObject;
        }

        private static Room BuildRoom(Transform parent, string name, Vector2 center, Vector2 size, Sprite square, DoorSide[] doorSides)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;

            var room = go.AddComponent<Room>();
            var so = new SerializedObject(room);
            so.FindProperty("roomId").stringValue = name;
            so.FindProperty("size").vector2Value = size;
            so.ApplyModifiedPropertiesWithoutUndo();

            // floor
            var floor = MakeSpriteObject("Floor", go.transform, center, new Vector2(size.x, size.y), square, FloorColor, -10, false);

            // walls
            var walls = new GameObject("Walls");
            walls.transform.SetParent(go.transform, false);
            walls.transform.localPosition = Vector3.zero;
            var doorSet = new HashSet<DoorSide>(doorSides);
            foreach (DoorSide side in System.Enum.GetValues(typeof(DoorSide)))
                BuildWallSide(walls.transform, center, size, side, doorSet.Contains(side), square);

            // doors
            foreach (var side in doorSides)
                BuildDoor(go.transform, center, size, side, square);

            room.RefreshChildren();
            return room;
        }

        // one wall, split in two with a gap if there's a door
        private static void BuildWallSide(Transform parent, Vector2 center, Vector2 size, DoorSide side, bool hasDoor, Sprite square)
        {
            float t = WallThickness;
            float halfW = size.x * 0.5f, halfH = size.y * 0.5f;
            bool horizontal = side == DoorSide.North || side == DoorSide.South;

            float fixedCoord = side switch
            {
                DoorSide.North => center.y + halfH - t * 0.5f,
                DoorSide.South => center.y - halfH + t * 0.5f,
                DoorSide.East => center.x + halfW - t * 0.5f,
                _ => center.x - halfW + t * 0.5f,
            };
            // top/bottom walls go full width, side walls fit between them
            float from = horizontal ? center.x - halfW : center.y - halfH + t;
            float to = horizontal ? center.x + halfW : center.y + halfH - t;
            float doorCenter = horizontal ? center.x : center.y;

            var segments = new List<(float a, float b)>();
            if (hasDoor)
            {
                segments.Add((from, doorCenter - DoorWidth * 0.5f));
                segments.Add((doorCenter + DoorWidth * 0.5f, to));
            }
            else segments.Add((from, to));

            int i = 0;
            foreach (var (a, b) in segments)
            {
                float len = b - a;
                if (len <= 0.01f) continue;
                float mid = (a + b) * 0.5f;
                Vector2 pos = horizontal ? new Vector2(mid, fixedCoord) : new Vector2(fixedCoord, mid);
                Vector2 scale = horizontal ? new Vector2(len, t) : new Vector2(t, len);
                string name = segments.Count > 1 ? $"{side}Wall_{i++}" : $"{side}Wall";
                MakeSpriteObject(name, parent, pos, scale, square, WallColor, 0, true);
            }
        }

        private static Door BuildDoor(Transform parent, Vector2 center, Vector2 size, DoorSide side, Sprite square)
        {
            float t = WallThickness;
            float halfW = size.x * 0.5f, halfH = size.y * 0.5f;
            Vector2 pos = side switch
            {
                DoorSide.North => new Vector2(center.x, center.y + halfH - t * 0.5f),
                DoorSide.South => new Vector2(center.x, center.y - halfH + t * 0.5f),
                DoorSide.East => new Vector2(center.x + halfW - t * 0.5f, center.y),
                _ => new Vector2(center.x - halfW + t * 0.5f, center.y),
            };
            bool horizontal = side == DoorSide.North || side == DoorSide.South;
            Vector2 scale = horizontal ? new Vector2(DoorWidth, t) : new Vector2(t, DoorWidth);

            var go = MakeSpriteObject($"Door_{side}", parent, pos, scale, square, DoorColor, -5, false);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Vector2.one;

            var door = go.AddComponent<Door>();
            var so = new SerializedObject(door);
            so.FindProperty("side").enumValueIndex = (int)side;
            so.ApplyModifiedPropertiesWithoutUndo();
            return door;
        }

        private static PlayerSpawnPoint AddPlayerSpawn(Room room, string id, Vector2 position)
        {
            var go = new GameObject($"PlayerSpawn_{id}");
            go.transform.SetParent(room.transform, false);
            go.transform.position = position;
            var spawn = go.AddComponent<PlayerSpawnPoint>();
            var so = new SerializedObject(spawn);
            so.FindProperty("id").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
            room.RefreshChildren();
            return spawn;
        }

        private static EnemySpawnPoint AddEnemySpawn(Room room, Vector2 position)
        {
            var go = new GameObject("EnemySpawn");
            go.transform.SetParent(room.transform, false);
            go.transform.position = position;
            var spawn = go.AddComponent<EnemySpawnPoint>();
            room.RefreshChildren();
            return spawn;
        }

        private static GameObject MakeSpriteObject(string name, Transform parent, Vector2 position, Vector2 scale, Sprite sprite, Color color, int sortingOrder, bool solid)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;

            if (solid)
            {
                var col = go.AddComponent<BoxCollider2D>();
                col.size = Vector2.one;
            }
            return go;
        }

        private static Door FindDoor(Room room, DoorSide side)
        {
            foreach (var d in room.GetComponentsInChildren<Door>(true))
                if (d.Side == side) return d;
            return null;
        }

        private static Sprite EnsureSquareSprite()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
            if (existing != null) return existing;

            Directory.CreateDirectory(PlaceholderDir);
            const int px = 32;
            var tex = new Texture2D(px, px, TextureFormat.RGBA32, false);
            var pixels = new Color32[px * px];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(SquareSpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(SquareSpritePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SquareSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = px; // 1 unit square
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
        }

        private static GameObject EnsurePlayerPrefab(Sprite square)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (existing != null) return existing;

            Directory.CreateDirectory(Path.GetDirectoryName(PlayerPrefabPath));

            var go = new GameObject("Player");
            go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = PlayerColor;
            sr.sortingOrder = 10;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;

            var health = go.AddComponent<Health>();
            var hso = new SerializedObject(health);
            hso.FindProperty("maxHealth").intValue = 6;
            hso.FindProperty("invulnerabilityTime").floatValue = 0.5f;
            hso.ApplyModifiedPropertiesWithoutUndo();

            var controller = go.AddComponent<PlayerController>();
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (actions == null)
                Debug.LogWarning("Couldn't find " + InputActionsPath + ", assign it on the player prefab manually");
            SetRef(controller, "inputActions", actions);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PlayerPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning("no field " + field + " on " + target.GetType().Name);
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in scenes)
                if (s.path == path) { s.enabled = true; EditorBuildSettings.scenes = scenes.ToArray(); return; }
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}

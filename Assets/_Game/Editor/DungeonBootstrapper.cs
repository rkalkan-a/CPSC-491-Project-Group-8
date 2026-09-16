using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DungeonCrawler.Combat;
using DungeonCrawler.Core;
using DungeonCrawler.Loot;
using DungeonCrawler.Player;
using DungeonCrawler.Rooms;
using DungeonCrawler.UI;

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
        private const string PlayerArtDir = "Assets/_Game/Art/Player";
        private const string ForestMapPath = "Assets/_Game/Art/Maps/forest_camp.png";
        private const string ForestCollidersPath = "Assets/_Game/Art/Maps/forest_camp_colliders.json";
        private const float ForestPixelsPerUnit = 48f; // 1536px map -> 32 units wide
        private const float PlayerArtPixelsPerUnit = 800f; // the sprites are big, this makes the character about 1.4 units tall

        // room layout in world units, camera fits the room on its own
        private static readonly Vector2 RoomSize = new Vector2(17f, 10f);
        private const float WallThickness = 1f;
        private const float DoorWidth = 2f;
        private const float RoomGap = 3f; // space between rooms

        private static readonly Color FloorColor = new Color(0.16f, 0.14f, 0.17f);
        private static readonly Color WallColor = new Color(0.38f, 0.33f, 0.36f);
        private static readonly Color DoorColor = new Color(0.55f, 0.25f, 0.25f);
        private static readonly Color PlayerColor = new Color(0.85f, 0.92f, 0.95f);
        private static readonly Color SpikeColor = new Color(0.62f, 0.62f, 0.68f);
        private static readonly Color HeartColor = new Color(0.9f, 0.15f, 0.2f);

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
            EnsurePlayerPrefabComponents();

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
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<RoomCamera>();

            // rooms
            var roomsRoot = new GameObject("Rooms");
            Vector2 room1Center = Vector2.zero;
            Vector2 room2Center = new Vector2(-(RoomSize.x + RoomGap), 0f); // progress goes right to left

            bool hasForest = File.Exists(ForestMapPath);
            Room room1 = BuildRoom(roomsRoot.transform, "Room_01", room1Center, RoomSize, square,
                hasForest ? new[] { DoorSide.West, DoorSide.East } : new[] { DoorSide.West });
            Room room2 = BuildRoom(roomsRoot.transform, "Room_02", room2Center, RoomSize, square,
                new[] { DoorSide.East });

            // link room 1 west <-> room 2 east
            Door r1West = FindDoor(room1, DoorSide.West);
            Door r2East = FindDoor(room2, DoorSide.East);
            r1West.Link(r2East);
            EditorUtility.SetDirty(r1West);
            EditorUtility.SetDirty(r2East);

            // forest camp area with the dungeon gate, sits to the right of room 1 since that's where the run starts
            Room forest = hasForest ? BuildForestArea(roomsRoot.transform, room1Center.x + RoomSize.x * 0.5f + RoomGap) : null;
            if (forest != null)
            {
                Door gate = FindDoor(forest, DoorSide.North);
                Door r1East = FindDoor(room1, DoorSide.East);
                gate.Link(r1East);
                EditorUtility.SetDirty(gate);
                EditorUtility.SetDirty(r1East);
            }

            // player start. by the campfire if the forest exists, otherwise in room 1
            PlayerSpawnPoint startSpawn;
            if (forest != null)
            {
                startSpawn = AddPlayerSpawn(forest, "Start", ForestPixelToWorld(forest, 1250f, 500f));
                MakeChest(forest.transform, ForestPixelToWorld(forest, 1195f, 452f), square);
                AddPlayerSpawn(room1, "DungeonEntrance", room1Center + new Vector2(4f, 0f));
                cam.transform.position = new Vector3(startSpawn.Position.x, startSpawn.Position.y, -10f);
            }
            else
            {
                startSpawn = AddPlayerSpawn(room1, "Start", room1Center + new Vector2(4f, 0f));
            }

            // enemy spawn markers in room 2, no prefab yet
            AddEnemySpawn(room2, room2Center + new Vector2(-2f, 2f));
            AddEnemySpawn(room2, room2Center + new Vector2(-3.5f, -2f));

            // spikes to test taking damage, heart to test healing
            MakeSpikes(room2.transform, room2Center + new Vector2(3f, 0f), new Vector2(1.5f, 3f), square);
            MakeHeartPickup(room1.transform, room1Center + new Vector2(-2f, 2.5f), square);

            // put a player in the scene so it's easy to see/edit, GameManager reuses it
            var playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            playerInstance.name = "Player";
            playerInstance.transform.position = startSpawn.Position;

            // managers
            var managers = new GameObject("GameManager");
            var roomManager = managers.AddComponent<RoomManager>();
            SetRef(roomManager, "startingRoom", forest != null ? forest : room1);
            var gameManager = managers.AddComponent<GameManager>();
            SetRef(gameManager, "playerPrefab", playerPrefab.GetComponent<PlayerController>());
            SetRef(gameManager, "startSpawn", startSpawn);

            BuildHud(square);

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
            Debug.Log("Built Level_01. Hit play, WASD to move, red doorway on the left switches rooms.");
        }

        // refreshes the player prefab (art, components) without rebuilding the whole level
        [MenuItem("Tools/Dungeon/Update Player Prefab", priority = 3)]
        public static void UpdatePlayerPrefab()
        {
            Sprite square = EnsureSquareSprite();
            EnsurePlayerPrefab(square);
            EnsurePlayerPrefabComponents();
            AssetDatabase.SaveAssets();
            Debug.Log("Player prefab updated");
        }

        [MenuItem("Tools/Dungeon/Add Room To Scene", priority = 1)]
        public static void AddRoomToScene()
        {
            Sprite square = EnsureSquareSprite();

            var existing = Object.FindObjectsByType<Room>(FindObjectsSortMode.None);
            Transform parent = GameObject.Find("Rooms")?.transform;
            if (parent == null) parent = new GameObject("Rooms").transform;

            // put it to the left of the furthest room since the level runs right to left
            Vector2 center = Vector2.zero;
            float minLeft = float.PositiveInfinity;
            foreach (var r in existing)
            {
                float left = r.Center.x - r.Size.x * 0.5f;
                if (left < minLeft) { minLeft = left; center = new Vector2(left - RoomGap - RoomSize.x * 0.5f, r.Center.y); }
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

        // colliders json is a list of polygon outlines in image pixels. outer shapes block, shapes inside them are holes (the walkable bits)
        // the painted forest map as one big room. colliders come from the json next to the png
        private static Room BuildForestArea(Transform parent, float leftEdgeX)
        {
            ImportAsPixelSprite(ForestMapPath, ForestPixelsPerUnit);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ForestMapPath);
            if (sprite == null) return null;

            float w = sprite.texture.width / ForestPixelsPerUnit;
            float h = sprite.texture.height / ForestPixelsPerUnit;
            Vector2 center = new Vector2(leftEdgeX + w * 0.5f, 0f);

            var go = new GameObject("Forest_Camp");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var room = go.AddComponent<Room>();
            var so = new SerializedObject(room);
            so.FindProperty("roomId").stringValue = "Forest_Camp";
            so.FindProperty("size").vector2Value = new Vector2(w, h);
            so.FindProperty("clearedOnEnter").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            var mapGo = new GameObject("Map");
            mapGo.transform.SetParent(go.transform, false);
            var sr = mapGo.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -10;

            if (File.Exists(ForestCollidersPath))
            {
                var paths = ReadColliderPaths(File.ReadAllText(ForestCollidersPath));
                var colGo = new GameObject("Colliders");
                colGo.transform.SetParent(go.transform, false);
                colGo.transform.localPosition = Vector3.zero;
                var poly = colGo.AddComponent<PolygonCollider2D>();
                poly.pathCount = paths.Count;
                for (int i = 0; i < paths.Count; i++)
                {
                    var pts = new Vector2[paths[i].Count];
                    for (int j = 0; j < pts.Length; j++)
                        pts[j] = ForestPixelToWorld(room, paths[i][j].x, paths[i][j].y) - room.Center; // local to the room
                    poly.SetPath(i, pts);
                }
            }

            // dungeon gate at the top left is the way into the first level
            var gateGo = new GameObject("Door_Gate");
            gateGo.transform.SetParent(go.transform, false);
            gateGo.transform.position = ForestPixelToWorld(room, 320f, 340f);
            var gateCol = gateGo.AddComponent<BoxCollider2D>();
            gateCol.isTrigger = true;
            gateCol.size = new Vector2(2.6f, 0.8f);
            var gate = gateGo.AddComponent<Door>();
            var dso = new SerializedObject(gate);
            dso.FindProperty("side").enumValueIndex = (int)DoorSide.North;
            dso.FindProperty("arrivalDistance").floatValue = 1.2f;
            dso.ApplyModifiedPropertiesWithoutUndo();

            room.RefreshChildren();
            return room;
        }

        // JsonUtility can't do nested arrays so this just parses the "paths" bit by hand
        private static List<List<Vector2>> ReadColliderPaths(string json)
        {
            var result = new List<List<Vector2>>();
            int start = json.IndexOf("\"paths\"");
            if (start < 0) return result;
            int i = json.IndexOf('[', start) + 1; // inside the outer list
            int depth = 1;
            List<Vector2> current = null;
            string num = "";
            float? pendingX = null;
            for (; i < json.Length && depth > 0; i++)
            {
                char c = json[i];
                if (c == '[')
                {
                    depth++;
                    if (depth == 2) current = new List<Vector2>();
                }
                else if (c == ']' || c == ',')
                {
                    if (num.Length > 0)
                    {
                        float v = float.Parse(num, System.Globalization.CultureInfo.InvariantCulture);
                        if (pendingX == null) pendingX = v;
                        else { current.Add(new Vector2(pendingX.Value, v)); pendingX = null; }
                        num = "";
                    }
                    if (c == ']')
                    {
                        depth--;
                        if (depth == 1 && current != null) { result.Add(current); current = null; }
                    }
                }
                else if (char.IsDigit(c) || c == '.' || c == '-') num += c;
            }
            return result;
        }

        // image pixel coords (top left origin) -> world position inside the forest room
        private static Vector2 ForestPixelToWorld(Room forest, float px, float py)
        {
            float w = forest.Size.x * ForestPixelsPerUnit;
            float h = forest.Size.y * ForestPixelsPerUnit;
            return forest.Center + new Vector2((px - w * 0.5f) / ForestPixelsPerUnit, -(py - h * 0.5f) / ForestPixelsPerUnit);
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

        private static void MakeChest(Transform parent, Vector2 position, Sprite square)
        {
            const string closedPath = "Assets/_Game/Art/Chests/chest_closed.png";
            const string openPath = "Assets/_Game/Art/Chests/chest_open.png";
            if (!File.Exists(closedPath)) { Debug.LogWarning("no chest art at " + closedPath); return; }
            ImportAsPixelSprite(closedPath, 48f);
            ImportAsPixelSprite(openPath, 48f);
            var closed = AssetDatabase.LoadAssetAtPath<Sprite>(closedPath);
            var open = AssetDatabase.LoadAssetAtPath<Sprite>(openPath);
            if (closed == null) { Debug.LogWarning("chest sprite didn't import, click into unity and run the build again"); return; }

            var go = new GameObject("Chest");
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.15f, 0f); // sprite sits a bit above the hitbox so it reads as standing on the floor
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = closed;
            sr.sortingOrder = 5;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.85f, 0.4f);

            var chest = go.AddComponent<Chest>();
            SetRef(chest, "sprite", sr);
            SetRef(chest, "closedSprite", closed);
            SetRef(chest, "openSprite", open);

            // glow under it until it's opened
            const string glowPath = "Assets/_Game/Art/Effects/glow.png";
            ImportAsPixelSprite(glowPath, 48f);
            var glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(glowPath);
            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(go.transform, false);
            glowGo.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            glowGo.transform.localScale = new Vector3(1.7f, 1.3f, 1f);
            var glowSr = glowGo.AddComponent<SpriteRenderer>();
            glowSr.sprite = glowSprite;
            glowSr.color = new Color(1f, 0.85f, 0.5f, 0.4f);
            glowSr.sortingOrder = 4;

            var fx = go.AddComponent<ChestGlow>();
            SetRef(fx, "glow", glowSr);
            SetRef(fx, "sparkleSprite", square);
        }

        private static void BuildChestPanel(Transform canvas)
        {
            var root = new GameObject("ChestPanel", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvas, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(640f, 380f);
            root.GetComponent<Image>().color = new Color(0.06f, 0.05f, 0.07f, 0.95f);

            var title = new GameObject("Title", typeof(RectTransform), typeof(Text), typeof(ScaledText));
            title.transform.SetParent(root.transform, false);
            var titleRect = (RectTransform)title.transform;
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -40f);
            titleRect.sizeDelta = new Vector2(600f, 60f);
            var titleText = title.GetComponent<Text>();
            titleText.text = "Chest";
            titleText.font = DefaultFont();
            titleText.fontSize = 44;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.9f, 0.86f, 0.86f);

            // 6 empty slots
            for (int i = 0; i < 6; i++)
            {
                var slot = new GameObject("Slot" + i, typeof(RectTransform), typeof(Image));
                slot.transform.SetParent(root.transform, false);
                var slotRect = (RectTransform)slot.transform;
                slotRect.anchorMin = slotRect.anchorMax = new Vector2(0.5f, 0.5f);
                slotRect.sizeDelta = new Vector2(90f, 90f);
                slotRect.anchoredPosition = new Vector2(-250f + i * 100f, 10f);
                slot.GetComponent<Image>().color = new Color(0.16f, 0.13f, 0.15f);
            }

            var hint = new GameObject("Hint", typeof(RectTransform), typeof(Text), typeof(ScaledText));
            hint.transform.SetParent(root.transform, false);
            var hintRect = (RectTransform)hint.transform;
            hintRect.anchorMin = hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 45f);
            hintRect.sizeDelta = new Vector2(600f, 50f);
            var hintText = hint.GetComponent<Text>();
            hintText.text = "nothing inside yet. press esc to close";
            hintText.font = DefaultFont();
            hintText.fontSize = 26;
            hintText.alignment = TextAnchor.MiddleCenter;
            hintText.color = new Color(0.7f, 0.65f, 0.65f);

            var panel = canvas.gameObject.AddComponent<ChestPanel>();
            SetRef(panel, "panelRoot", root);
            root.SetActive(false);
        }

        private static void MakeSpikes(Transform parent, Vector2 position, Vector2 size, Sprite square)
        {
            var go = MakeSpriteObject("Spikes", parent, position, size, square, SpikeColor, -3, false);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Vector2.one;
            go.AddComponent<DamageOnContact>();
        }

        private static void MakeHeartPickup(Transform parent, Vector2 position, Sprite square)
        {
            var go = MakeSpriteObject("HeartPickup", parent, position, new Vector2(0.5f, 0.5f), square, HeartColor, 1, false);
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;
            go.AddComponent<HealthPickup>();
        }

        private static void BuildHud(Sprite square)
        {
            var canvasGo = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var hearts = new GameObject("Hearts", typeof(RectTransform));
            hearts.transform.SetParent(canvasGo.transform, false);
            var heartsRect = (RectTransform)hearts.transform;
            heartsRect.anchorMin = heartsRect.anchorMax = new Vector2(0f, 1f);
            heartsRect.pivot = new Vector2(0f, 1f);
            heartsRect.anchoredPosition = new Vector2(24f, -24f);
            heartsRect.sizeDelta = new Vector2(400f, 40f);

            var textGo = new GameObject("DeathText", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(canvasGo.transform, false);
            var textRect = (RectTransform)textGo.transform;
            textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(800f, 120f);
            var text = textGo.GetComponent<Text>();
            text.text = "you died";
            text.font = DefaultFont();
            text.fontSize = 72;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.85f, 0.1f, 0.15f);
            text.raycastTarget = false;
            text.enabled = false;
            textGo.AddComponent<ScaledText>();

            var hud = canvasGo.AddComponent<HealthHUD>();
            SetRef(hud, "heartsRoot", heartsRect);
            SetRef(hud, "heartSprite", square);
            SetRef(hud, "deathText", text);

            BuildChestPanel(canvasGo.transform);
        }

        // adds any newer components to the player prefab so re-running the menu keeps it up to date
        private static void EnsurePlayerPrefabComponents()
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            bool changed = false;
            if (root.GetComponent<HurtFlash>() == null) { root.AddComponent<HurtFlash>(); changed = true; }
            if (root.GetComponent<BleedOnHit>() == null) { root.AddComponent<BleedOnHit>(); changed = true; }

            // move the sprite onto a child so the walk bob doesn't move the collider
            var rootSprite = root.GetComponent<SpriteRenderer>();
            if (rootSprite != null)
            {
                var visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                var sr = visual.AddComponent<SpriteRenderer>();
                sr.sortingOrder = rootSprite.sortingOrder;
                Object.DestroyImmediate(rootSprite);
                root.transform.localScale = Vector3.one;

                var col = root.GetComponent<CircleCollider2D>();
                if (col != null) { col.radius = 0.4f; col.offset = new Vector2(0f, -0.2f); }
                changed = true;
            }

            // slippery material so you slide along walls instead of sticking on corners
            var playerCol = root.GetComponent<Collider2D>();
            if (playerCol != null && playerCol.sharedMaterial == null)
            {
                playerCol.sharedMaterial = EnsureNoFrictionMaterial();
                changed = true;
            }

            // real character art if it's in the project
            var sprites = LoadPlayerArt();
            if (sprites != null)
            {
                var visualSr = root.GetComponentInChildren<SpriteRenderer>();
                visualSr.sprite = sprites[0];
                visualSr.color = Color.white;

                var dirComp = root.GetComponent<PlayerSpriteDirection>();
                if (dirComp == null) dirComp = root.AddComponent<PlayerSpriteDirection>();
                SetRef(dirComp, "target", visualSr);
                SetRef(dirComp, "front", sprites[0]);
                SetRef(dirComp, "back", sprites[1]);
                SetRef(dirComp, "left", sprites[2]);
                SetRef(dirComp, "right", sprites[3]);
                changed = true;
            }

            if (changed) PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        // front, back, left, right. returns null if the pngs aren't there
        private static Sprite[] LoadPlayerArt()
        {
            string[] names = { "player_front", "player_back", "player_left", "player_right" };
            var result = new Sprite[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                string path = PlayerArtDir + "/" + names[i] + ".png";
                if (!File.Exists(path)) return null;
                ImportAsPixelSprite(path, PlayerArtPixelsPerUnit);
                result[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (result[i] == null) return null;
            }
            return result;
        }

        private static PhysicsMaterial2D EnsureNoFrictionMaterial()
        {
            const string path = "Assets/_Game/Prefabs/NoFriction.physicsMaterial2D";
            var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (mat != null) return mat;
            mat = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        internal static void ImportAsPixelSprite(string path, float pixelsPerUnit)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) return;
            }
            bool dirty = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || !Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit)
                || importer.filterMode != FilterMode.Point
                || importer.mipmapEnabled
                || importer.textureCompression != TextureImporterCompression.Uncompressed
                || !importer.alphaIsTransparency
                || importer.maxTextureSize < 2048;
            if (!dirty) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
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

            go.AddComponent<HurtFlash>();
            go.AddComponent<BleedOnHit>();

            var controller = go.AddComponent<PlayerController>();
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (actions == null)
                Debug.LogWarning("Couldn't find " + InputActionsPath + ", assign it on the player prefab manually");
            SetRef(controller, "inputActions", actions);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PlayerPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        internal static void SetRef(Object target, string field, Object value)
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

        // menu goes first so it's what a build opens on, levels after
        internal static void AddSceneToBuildSettings(string path, bool first = false)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            var entry = new EditorBuildSettingsScene(path, true);
            if (first) scenes.Insert(0, entry);
            else scenes.Add(entry);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        internal static Font DefaultFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}

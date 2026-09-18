using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using DungeonCrawler.Loot;
using DungeonCrawler.Rooms;
using DungeonCrawler.UI;

namespace DungeonCrawler.EditorTools
{
    // drops a handful of coins into one room and puts a "Coins: N" counter in the HUD.
    // safe to run more than once - it clears the old coins out before placing new ones.
    public static class CoinPickupBuilder
    {
        private const string TargetRoomId = "Room_01"; // the starting room, doors on both sides
        private const string CoinSpritePath = "Assets/_Game/Art/Items/Coin.png";
        private const int CoinCount = 6;

        [MenuItem("Tools/Dungeon/Setup Coin Pickups", priority = 6)]
        public static void SetupCoinPickups()
        {
            var room = FindTargetRoom();
            if (room == null)
            {
                Debug.LogError("Couldn't find a room called '" + TargetRoomId + "' in the open scene. " +
                               "Open Assets/_Game/Scenes/Level_01.unity first, then run this again.");
                return;
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CoinSpritePath);
            if (sprite == null)
            {
                Debug.LogError("Couldn't load the coin sprite at " + CoinSpritePath +
                               ". Make sure the file is there, then use Assets > Refresh.");
                return;
            }

            int placed = ScatterCoins(room, sprite);
            SetupHud();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("Placed " + placed + " coins in " + TargetRoomId +
                      " and added the coin counter. Save the scene (Cmd+S), then press play.");
        }

        private static Room FindTargetRoom()
        {
            foreach (var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None))
            {
                if (room.RoomId == TargetRoomId) return room;
            }
            return null;
        }

        private static int ScatterCoins(Room room, Sprite sprite)
        {
            // clear out anything from a previous run so re-running doesn't stack coins up
            foreach (var old in room.GetComponentsInChildren<CoinPickup>(true))
            {
                Undo.DestroyObjectImmediate(old.gameObject);
            }

            // a fixed 3 x 2 grid, kept well inside the walls and clear of the doorways
            // (the doors sit at the middle of the left and right walls, so nothing goes at y = 0)
            float halfW = room.Size.x * 0.5f;
            float halfH = room.Size.y * 0.5f;
            float[] xs = { -halfW * 0.52f, 0f, halfW * 0.52f };
            float[] ys = { halfH * 0.45f, -halfH * 0.45f };

            int placed = 0;
            foreach (float y in ys)
            {
                foreach (float x in xs)
                {
                    if (placed >= CoinCount) break;
                    MakeCoin(room, sprite, room.Center + new Vector2(x, y));
                    placed++;
                }
            }
            return placed;
        }

        private static void MakeCoin(Room room, Sprite sprite, Vector2 position)
        {
            var go = new GameObject("Coin");
            Undo.RegisterCreatedObjectUndo(go, "Setup Coin Pickups");
            go.transform.SetParent(room.transform, false);
            go.transform.position = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 7; // above the floor, below the player

            // the coin art is only 0.2 units across, so give it a roomier grab radius
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.35f;

            go.AddComponent<CoinPickup>();
        }

        private static void SetupHud()
        {
            var hud = GameObject.Find("HUD");
            if (hud == null)
            {
                Debug.LogWarning("No 'HUD' object in the open scene, so the coin counter was skipped.");
                return;
            }

            if (hud.GetComponentInChildren<CoinHUD>(true) != null) return; // already there

            var go = new GameObject("CoinCounter", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Setup Coin Pickups");
            go.transform.SetParent(hud.transform, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f); // top right, hearts are top left
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(220f, 44f);
            rect.anchoredPosition = new Vector2(-16f, -16f);

            var text = go.AddComponent<Text>();
            text.font = HudFont(hud);
            text.fontSize = 28;
            text.alignment = TextAnchor.UpperRight;
            text.color = new Color(1f, 0.85f, 0.3f); // gold, matches the coins
            text.text = "Coins: 0";
            text.raycastTarget = false;

            var coinHud = go.AddComponent<CoinHUD>();
            var so = new SerializedObject(coinHud);
            so.FindProperty("coinText").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // reuse whatever font the HUD already uses so the counter matches the rest of the UI
        private static Font HudFont(GameObject hud)
        {
            var existing = hud.GetComponentInChildren<Text>(true);
            if (existing != null && existing.font != null) return existing.font;
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}

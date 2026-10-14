using UnityEngine;
using UnityEngine.InputSystem;
using DungeonCrawler.Combat;
using DungeonCrawler.Player;
using DungeonCrawler.UI;

namespace DungeonCrawler.Loot
{
    // lets the player grab loot without walking into it: click a chest or pickup that's close by,
    // or press E to grab the nearest one. PlayerController adds this to the player
    [RequireComponent(typeof(PlayerController))]
    public class LootGrabber : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float reach = 2.5f;

        public float Reach => reach;

        private PlayerController player;
        private Health ownHealth;
        private int clickedLootFrame = -1;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            ownHealth = GetComponent<Health>();
        }

        private void Update()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.isPressed) clickedLootFrame = -1;
            if (!player.InputEnabled || PauseMenu.IsPaused) return;
            if (ChestPanel.Instance != null && (ChestPanel.Instance.IsShowing || ChestPanel.ClosedOnFrame == Time.frameCount)) return;
            if (ownHealth != null && ownHealth.IsDead) return;

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                var clicked = LootUnderMouse();
                if (clicked != null)
                {
                    clickedLootFrame = Time.frameCount;
                    Grab(clicked);
                    return;
                }
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                var nearest = NearestLoot();
                if (nearest != null) Grab(nearest);
            }
        }

        // true while the left click that grabbed something is still held, so it doesn't also fire a shot
        public bool HoldingLootClick => clickedLootFrame >= 0 && Mouse.current != null && Mouse.current.leftButton.isPressed;

        // also true the moment the mouse is pressed over loot, in case this runs after the shooting does
        public bool PointerOnLoot => LootUnderMouse() != null;

        public bool Grab(Component loot)
        {
            switch (loot)
            {
                case Chest chest: chest.Open(player); return true;
                case ArmorPickup armor: return armor.Grab(player);
                case HealthPickup heart: return heart.Grab(player);
                default: return false;
            }
        }

        // the chest or pickup under the mouse, if it's close enough to reach
        public Component LootUnderMouse()
        {
            var cam = Camera.main;
            if (cam == null || Mouse.current == null) return null;
            Vector2 screen = Mouse.current.position.ReadValue();
            Vector2 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));

            foreach (var col in Physics2D.OverlapCircleAll(world, 0.3f))
            {
                var loot = LootOn(col);
                if (loot != null && InReach(col)) return loot;
            }
            return null;
        }

        // the closest chest or pickup within reach
        public Component NearestLoot()
        {
            Vector2 origin = transform.position;
            Component best = null;
            float bestDistance = float.MaxValue;
            foreach (var col in Physics2D.OverlapCircleAll(origin, reach))
            {
                var loot = LootOn(col);
                if (loot == null) continue;
                if (loot is Chest chest && chest.IsOpen && chest.IsEmpty) continue; // nothing left in it
                float distance = Vector2.Distance(origin, col.ClosestPoint(origin));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = loot;
                }
            }
            return best;
        }

        private bool InReach(Collider2D col)
        {
            Vector2 origin = transform.position;
            return Vector2.Distance(origin, col.ClosestPoint(origin)) <= reach;
        }

        private static Component LootOn(Collider2D col)
        {
            if (col.TryGetComponent(out Chest chest)) return chest;
            if (col.TryGetComponent(out ArmorPickup armor)) return armor;
            if (col.TryGetComponent(out HealthPickup heart)) return heart;
            return null;
        }
    }
}

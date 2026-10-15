using UnityEngine;
using DungeonCrawler.Combat;
using DungeonCrawler.UI;
using DungeonCrawler.Audio;

namespace DungeonCrawler.Player
{
    // the sword the player carries once they've taken it from a chest. it's a separate sprite, not part of the character art:
    // it sits in the player's hand, switches sides with the way they face, and sweeps through
    // the melee arc whenever PlayerMelee swings
    [RequireComponent(typeof(PlayerController), typeof(PlayerMelee))]
    public class PlayerSword : MonoBehaviour
    {
        [SerializeField] private bool startsWithSword = false; // normally it comes out of the first chest
        [SerializeField, Min(0.05f)] private float swingTime = 0.16f;
        [SerializeField, Min(0f)] private float swingReach = 0.26f; // how far from the body the hand travels during a swing
        [SerializeField] private Vector2 bodyCenter = new Vector2(0f, -0.36f); // the character's small body sits low under the head, in sprite space

        public bool HasSword { get; private set; }
        public bool IsSwinging => swingTimer > 0f;
        public Transform Sword => sword != null ? sword.transform : null;

        private PlayerController player;
        private PlayerMelee melee;
        private Health health;
        private SpriteRenderer body;
        private SpriteRenderer sword;
        private Vector2 swingDirection;
        private float swingTimer;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            melee = GetComponent<PlayerMelee>();
            health = GetComponent<Health>();
            body = GetComponentInChildren<SpriteRenderer>(); // the character art, found before the sword exists

            var go = new GameObject("Sword");
            // under the character art so it bobs with the walk cycle
            go.transform.SetParent(body != null ? body.transform : transform, false);
            sword = go.AddComponent<SpriteRenderer>();
            sword.sprite = PixelUI.Sword();
            if (body != null) sword.sortingLayerID = body.sortingLayerID;

            HasSword = startsWithSword;
            melee.Armed = HasSword;
        }

        // called when the player takes the sword. from then on they can attack
        public void Equip()
        {
            bool isNew = !HasSword;
            if (isNew)
                GetComponent<PlayerAudio>()?.PlaySwordEquip();
            HasSword = true;
            melee.Armed = true;

            if (isNew && UnlockBanner.Instance != null)
                UnlockBanner.Instance.Show(PixelUI.Sword(), "YOU FOUND A SWORD", "Right click or Space to attack");
        }

        private void OnEnable() => melee.Swung += OnSwung;
        private void OnDisable() => melee.Swung -= OnSwung;

        private void OnSwung(Vector2 direction)
        {
            swingDirection = direction;
            swingTimer = swingTime;
        }

        private void LateUpdate()
        {
            sword.enabled = HasSword && (health == null || !health.IsDead);

            if (swingTimer > 0f)
            {
                swingTimer -= Time.deltaTime;
                PoseSwing(1f - Mathf.Clamp01(swingTimer / swingTime));
            }
            else PoseHeld();
        }

        // resting in the hand, on whichever side matches the way the player is facing
        private void PoseHeld()
        {
            Vector2 dir = player.MoveInput.sqrMagnitude > 0.01f ? player.MoveInput : player.Facing;
            int order = body != null ? body.sortingOrder : 10;

            Vector2 hand;
            float tilt;
            bool behind = false;
            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            {
                bool right = dir.x > 0f;
                hand = new Vector2(right ? 0.17f : -0.17f, -0.4f);
                tilt = right ? -40f : 40f;
            }
            else if (dir.y > 0f)
            {
                // facing away from the camera, so the sword is on the far side of the body
                hand = new Vector2(-0.2f, -0.38f);
                tilt = 20f;
                behind = true;
            }
            else
            {
                hand = new Vector2(0.2f, -0.4f);
                tilt = -20f;
            }

            sword.transform.localPosition = hand;
            sword.transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
            sword.sortingOrder = behind ? order - 1 : order + 1;
        }

        // sweeps the blade across the same arc the melee attack hits. progress goes 0 -> 1 over the swing
        private void PoseSwing(float progress)
        {
            float eased = 1f - Mathf.Pow(1f - progress, 3f); // fast at the start, settles at the end
            float aim = Mathf.Atan2(swingDirection.y, swingDirection.x) * Mathf.Rad2Deg;
            float half = melee.ArcDegrees * 0.5f;

            // always chop downward: clockwise when aiming right, counter-clockwise when aiming left
            float from = swingDirection.x >= 0f ? aim + half : aim - half;
            float to = swingDirection.x >= 0f ? aim - half : aim + half;
            float angle = Mathf.Lerp(from, to, eased);

            float rad = angle * Mathf.Deg2Rad;
            var outward = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            sword.transform.localPosition = bodyCenter + outward * swingReach;
            sword.transform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f); // the sprite points up, so turn it to point outward
            sword.sortingOrder = (body != null ? body.sortingOrder : 10) + 1;
        }
    }
}

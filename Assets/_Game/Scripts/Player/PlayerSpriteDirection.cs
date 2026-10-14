using UnityEngine;

namespace DungeonCrawler.Player
{
    // swaps the player sprite based on which way they're moving/facing and adds a little walk bob
    [RequireComponent(typeof(PlayerController))]
    public class PlayerSpriteDirection : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private Sprite front;
        [SerializeField] private Sprite back;
        [SerializeField] private Sprite left;
        [SerializeField] private Sprite right;
        [Tooltip("walk cycle drawn facing left. walking right uses the same frames flipped")]
        [SerializeField] private Sprite[] walkSide;

        [Header("Leather armor")]
        [SerializeField] private Sprite armoredFront;
        [SerializeField] private Sprite armoredBack;
        [SerializeField] private Sprite armoredLeft;
        [SerializeField] private Sprite armoredRight;
        [SerializeField] private Sprite[] armoredWalkSide;

        // switches every sprite over to the armored set. PlayerArmor turns this on
        public bool Armored { get; set; }

        // what the character looks like from the front right now, for the inventory portrait
        public Sprite FrontSprite => Pick(front, armoredFront);

        // the armored version when armor is on and that sprite exists, otherwise the normal one
        private Sprite Pick(Sprite normal, Sprite armored) => Armored && armored != null ? armored : normal;
        private Sprite[] WalkFrames => Armored && armoredWalkSide != null && armoredWalkSide.Length > 0 ? armoredWalkSide : walkSide;

        [SerializeField, Min(0f)] private float bobAmount = 0.05f;
        [SerializeField, Min(0f)] private float bobSpeed = 12f;

        private PlayerController player;
        private Vector3 restPosition;
        private float bobTime;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            if (target == null) target = GetComponentInChildren<SpriteRenderer>();
            if (target != null) restPosition = target.transform.localPosition;
        }

        private void Update()
        {
            if (target == null) return;

            bool moving = player.MoveInput.sqrMagnitude > 0.01f;
            Vector2 dir = moving ? player.MoveInput : player.Facing;

            // side sprites win on diagonals since that's what reads best
            Sprite pick;
            bool flip = false;
            bool side = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y);
            var walk = WalkFrames;
            if (side && moving && walk != null && walk.Length > 0)
            {
                // one frame per step, synced with the bob so the feet land when the body dips
                int step = Mathf.FloorToInt(bobTime / Mathf.PI);
                pick = walk[step % walk.Length];
                flip = dir.x > 0f;
            }
            else if (side)
                pick = dir.x < 0f ? Pick(left, armoredLeft) : Pick(right, armoredRight);
            else
                pick = dir.y > 0f ? Pick(back, armoredBack) : Pick(front, armoredFront);

            if (pick != null && target.sprite != pick) target.sprite = pick;
            target.flipX = flip;

            if (moving)
            {
                bobTime += Time.deltaTime * bobSpeed;
                float offset = Mathf.Abs(Mathf.Sin(bobTime)) * bobAmount;
                target.transform.localPosition = restPosition + Vector3.up * offset;
            }
            else
            {
                bobTime = 0f;
                target.transform.localPosition = restPosition;
            }
        }
    }
}

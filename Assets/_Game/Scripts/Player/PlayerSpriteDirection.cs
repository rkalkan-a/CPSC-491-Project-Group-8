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
            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
                pick = dir.x < 0f ? left : right;
            else
                pick = dir.y > 0f ? back : front;

            if (pick != null && target.sprite != pick) target.sprite = pick;

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

using UnityEngine;

namespace DungeonCrawler.Loot
{
    // pops a pickup in when it spawns, then floats it gently so it reads as something to grab
    public class PickupBob : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float popTime = 0.18f;
        [SerializeField, Min(0f)] private float bobHeight = 0.06f;
        [SerializeField, Min(0f)] private float bobSpeed = 3.5f;

        private Vector3 restPosition;
        private Vector3 restScale;
        private float age;

        private void Start()
        {
            restPosition = transform.localPosition;
            restScale = transform.localScale;
            transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            age += Time.deltaTime;

            float k = Mathf.Clamp01(age / popTime);
            float pop = 1f + 0.25f * Mathf.Sin(k * Mathf.PI); // overshoots a little, then settles
            transform.localScale = restScale * (k * pop);

            transform.localPosition = restPosition + Vector3.up * (Mathf.Sin(age * bobSpeed) * bobHeight);
        }
    }
}

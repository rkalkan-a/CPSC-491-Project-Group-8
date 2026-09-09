using UnityEngine;

namespace DungeonCrawler.Rooms
{
    // marker for where enemies spawn. prefab stays empty until we have enemies
    public class EnemySpawnPoint : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField, Min(1)] private int count = 1;
        [SerializeField, Min(0f)] private float scatterRadius = 0.5f;

        public GameObject EnemyPrefab => enemyPrefab;
        public int Count => count;

        public GameObject[] Spawn(Transform parent = null)
        {
            if (enemyPrefab == null) return System.Array.Empty<GameObject>();

            var spawned = new GameObject[count];
            for (int i = 0; i < count; i++)
            {
                Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle * scatterRadius;
                spawned[i] = Instantiate(enemyPrefab, pos, Quaternion.identity, parent);
            }
            return spawned;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = enemyPrefab != null ? Color.red : new Color(1f, 0.3f, 0.3f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            if (scatterRadius > 0f)
            {
                Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, scatterRadius);
            }
#if UNITY_EDITOR
            string label = enemyPrefab != null ? "Enemy x" + count + ": " + enemyPrefab.name : "Enemy x" + count + " (no prefab)";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.6f, label);
#endif
        }
    }
}

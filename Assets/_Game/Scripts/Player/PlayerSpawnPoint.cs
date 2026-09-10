using UnityEngine;
using DungeonCrawler.Rooms;

namespace DungeonCrawler.Player
{
    // where the player starts in a room. the first room needs one with id "Start"
    public class PlayerSpawnPoint : MonoBehaviour
    {
        [SerializeField] private string id = "Start";

        public string Id => id;
        public Vector2 Position => transform.position;
        public Room Room => GetComponentInParent<Room>();

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            Gizmos.DrawLine(transform.position + Vector3.left * 0.3f, transform.position + Vector3.right * 0.3f);
            Gizmos.DrawLine(transform.position + Vector3.down * 0.3f, transform.position + Vector3.up * 0.3f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.6f, "Spawn: " + id);
#endif
        }
    }
}

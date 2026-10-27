using UnityEngine;

namespace Project_D.Dungeon
{
    /// <summary>
    /// Component gắn vào các điểm đánh dấu trên Tilemap (PlayerSpawn, EnemySpawn).
    /// Giúp hiển thị trực quan trong Scene View / Tile Palette, nhưng tự ẩn đi khi vào Play Mode.
    /// </summary>
    public class EditorMarker : MonoBehaviour
    {
        private void Awake()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = false;
            }
        }
    }
}

using UnityEngine;
using Project_D.Dungeon.Room;
using Project_D.Dungeon.Interactions;

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
            // Tuyệt đối không ẩn Cổng dịch chuyển hoặc Điểm tương tác gameplay thật!
            if (GetComponent<RoomPortal>() != null || GetComponent<InteractionPoint>() != null)
            {
                return;
            }

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = false;
            }
        }
    }
}

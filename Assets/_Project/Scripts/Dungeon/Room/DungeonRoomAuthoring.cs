using UnityEngine;
using Project_D.Dungeon.Run;

namespace Project_D.Dungeon.Room
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Grid))]
    public class DungeonRoomAuthoring : MonoBehaviour
    {
        [Header("Room Identity")]
        public string roomName = "Room_Dungeon_01";
        public RunNodeType roomType = RunNodeType.NormalBattle;
        [Range(1, 50)] public int weight = 10;
        public string description = "Phòng vẽ bằng Tile Palette";

        public Transform GetPlayerSpawn()
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t != transform && t.name.IndexOf("PlayerSpawn", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return t;
            }
            return null;
        }

        public int GetEnemySpawnCount()
        {
            var p = GetPlayerSpawn();
            int count = 0;
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t != transform && t != p && t.name.IndexOf("EnemySpawn", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    count++;
            }
            return count;
        }

        public int GetPortalCount()
        {
            var portals = GetComponentsInChildren<RoomPortal>(true);
            return portals != null ? portals.Length : 0;
        }

        public int GetInteractionPointCount()
        {
            var points = GetComponentsInChildren<Project_D.Dungeon.Interactions.InteractionPoint>(true);
            return points != null ? points.Length : 0;
        }
    }
}

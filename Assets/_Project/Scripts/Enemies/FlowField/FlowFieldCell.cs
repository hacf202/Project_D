using UnityEngine;

namespace Project_D.Enemies.FlowField
{
    public class FlowFieldCell
    {
        public Vector2Int GridIndex;      // Tọa độ ô trong ma trận (X, Y)
        public Vector3 WorldPos;          // Tọa độ thực tế trong thế giới game
        public byte Cost;                 // Chi phí: 1 = Đất trống, 255 = Tường/Vật cản
        public ushort BestCost;           // Khoảng cách sóng loang từ Player đến ô này
        public Vector2 BestDirection;     // Mũi tên vector chỉ hướng dòng chảy

        public FlowFieldCell(Vector2Int gridIndex, Vector3 worldPos)
        {
            GridIndex = gridIndex;
            WorldPos = worldPos;
            Cost = 1;                     // Mặc định là đất trống
            BestCost = ushort.MaxValue;   // Mặc định khởi tạo bằng vô cực
            BestDirection = Vector2.zero;
        }

        public void ResetIntegration()
        {
            BestCost = ushort.MaxValue;
            BestDirection = Vector2.zero;
        }
    }
}

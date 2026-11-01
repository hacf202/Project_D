using System.Collections.Generic;
using UnityEngine;

namespace Project_D.Enemies.FlowField
{
    public class FlowFieldGrid
    {
        public FlowFieldCell[,] Cells { get; private set; }
        public Vector2Int GridSize { get; private set; }
        public float CellSize { get; private set; }
        public Vector3 OriginPosition { get; private set; }

        // Mảng 8 hướng cố định tĩnh để triệt tiêu 100% rác GC khi cập nhật liên tục
        private static readonly Vector2Int[] Directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),   // Bắc
            new Vector2Int(0, -1),  // Nam
            new Vector2Int(1, 0),   // Đông
            new Vector2Int(-1, 0),  // Tây
            new Vector2Int(1, 1),   // Đông Bắc (chéo)
            new Vector2Int(-1, 1),  // Tây Bắc (chéo)
            new Vector2Int(1, -1),  // Đông Nam (chéo)
            new Vector2Int(-1, -1)  // Tây Nam (chéo)
        };

        // Hàng đợi tái sử dụng Zero-GC cho thuật toán sóng loang Dijkstra
        private readonly Queue<FlowFieldCell> _openQueue = new Queue<FlowFieldCell>(512);

        public FlowFieldGrid(Vector2Int gridSize, float cellSize, Vector3 originPosition)
        {
            GridSize = gridSize;
            CellSize = cellSize;
            OriginPosition = originPosition;
            InitializeGrid();
        }

        private void InitializeGrid()
        {
            Cells = new FlowFieldCell[GridSize.x, GridSize.y];

            for (int x = 0; x < GridSize.x; x++)
            {
                for (int y = 0; y < GridSize.y; y++)
                {
                    // Tính tọa độ tâm của từng ô trong không gian thế giới
                    Vector3 worldPos = OriginPosition + new Vector3(x * CellSize + CellSize * 0.5f, y * CellSize + CellSize * 0.5f, 0f);
                    Cells[x, y] = new FlowFieldCell(new Vector2Int(x, y), worldPos);
                }
            }
        }

        // 1. Quét tìm vật cản (Tường hầm ngục)
        public void UpdateCostField(LayerMask obstacleLayer)
        {
            // Kích thước kiểm tra ô vật cản:
            // Với ô kích thước 0.5m, tâm của ô đất trống liền kề cách mép tường 0.25m.
            // Đặt checkSize = CellSize * 0.40f (0.2m x 0.2m, bán kính quét 0.1m):
            // - Mép hộp quét của ô đất trống cách mép tường 0.15m -> KHÔNG BAO GIỜ chạm vào collider tường (Cost = 1).
            // - Tâm và hộp quét của ô tường thực sự nằm sâu bên trong collider tường -> Nhận diện chính xác 100% (Cost = 255).
            // - LOẠI BỎ TRIỆT ĐỂ hiện tượng ô đỏ tràn ra ngoài hành lang 4 hướng làm bít lối đi và sai lệch dòng chảy!
            Vector2 checkSize = new Vector2(CellSize * 0.40f, CellSize * 0.40f);

            for (int x = 0; x < GridSize.x; x++)
            {
                for (int y = 0; y < GridSize.y; y++)
                {
                    FlowFieldCell cell = Cells[x, y];
                    // Quét xem tâm hoặc diện tích lõi ô này có nằm trong collider vật cản không
                    Collider2D hit = Physics2D.OverlapBox(cell.WorldPos, checkSize, 0f, obstacleLayer);

                    if (hit != null || Physics2D.OverlapPoint(cell.WorldPos, obstacleLayer) != null)
                    {
                        cell.Cost = 255; // Tường đá: Không thể đi qua
                    }
                    else
                    {
                        cell.Cost = 1;   // Đất trống: Chi phí chuẩn
                    }
                }
            }
        }

        // Sao chép bản đồ vật cản từ một Grid khác mà không cần quét lại Physics2D (Zero CPU Cost)
        public void CopyCostFrom(FlowFieldGrid sourceGrid)
        {
            if (sourceGrid == null || sourceGrid.GridSize != GridSize) return;

            for (int x = 0; x < GridSize.x; x++)
            {
                for (int y = 0; y < GridSize.y; y++)
                {
                    Cells[x, y].Cost = sourceGrid.Cells[x, y].Cost;
                }
            }
        }

        // 2. Thuật toán Sóng Loang Dijkstra (Integration Field)
        public void CreateIntegrationField(FlowFieldCell destinationCell)
        {
            if (destinationCell == null) return;
            CreateIntegrationField(new[] { destinationCell });
        }

        // Hỗ trợ Đa Điểm Đến đồng mức (Multi-source Dijkstra): Tất cả điểm đến có BestCost = 0
        public void CreateIntegrationField(IEnumerable<FlowFieldCell> destinationCells)
        {
            if (destinationCells == null) return;

            // Reset toàn bộ khoảng cách cũ
            for (int x = 0; x < GridSize.x; x++)
            {
                for (int y = 0; y < GridSize.y; y++)
                {
                    Cells[x, y].ResetIntegration();
                }
            }

            _openQueue.Clear();
            foreach (var cell in destinationCells)
            {
                if (cell != null && cell.Cost != 255)
                {
                    cell.BestCost = 0;
                    _openQueue.Enqueue(cell);
                }
            }

            RunDijkstraWavefront();
        }

        // Hỗ trợ Đa Điểm Đến Có Trọng Số (Weighted Multi-source Dijkstra):
        // Điểm đến có initialCost thấp hơn (Lính = 0) sẽ tạo hố trũng sâu hơn, hút quái mạnh hơn Player (initialCost = 25)
        public void CreateIntegrationField(IEnumerable<(FlowFieldCell cell, ushort initialCost)> weightedDestinations)
        {
            if (weightedDestinations == null) return;

            for (int x = 0; x < GridSize.x; x++)
            {
                for (int y = 0; y < GridSize.y; y++)
                {
                    Cells[x, y].ResetIntegration();
                }
            }

            _openQueue.Clear();
            foreach (var item in weightedDestinations)
            {
                FlowFieldCell cell = item.cell;
                ushort initCost = item.initialCost;

                if (cell != null && cell.Cost != 255)
                {
                    if (initCost < cell.BestCost)
                    {
                        cell.BestCost = initCost;
                        _openQueue.Enqueue(cell);
                    }
                }
            }

            RunDijkstraWavefront();
        }

        private void RunDijkstraWavefront()
        {
            if (_openQueue.Count == 0) return;

            while (_openQueue.Count > 0)
            {
                FlowFieldCell currentCell = _openQueue.Dequeue();

                foreach (var dir in Directions)
                {
                    Vector2Int neighborPos = currentCell.GridIndex + dir;

                    // Kiểm tra xem hàng xóm có nằm ngoài viền bản đồ không
                    if (neighborPos.x < 0 || neighborPos.x >= GridSize.x || neighborPos.y < 0 || neighborPos.y >= GridSize.y)
                        continue;

                    FlowFieldCell neighbor = Cells[neighborPos.x, neighborPos.y];

                    // Nếu là tường đá thì bỏ qua
                    if (neighbor.Cost == 255) continue;

                    // QUY TẮC "CHỐNG CẮT GÓC TƯỜNG" (NO CORNER CUTTING):
                    if (dir.x != 0 && dir.y != 0) // Đây là hướng đi chéo
                    {
                        // Kiểm tra 2 ô kề bên: nếu một trong 2 ô là TƯỜNG thì TUYỆT ĐỐI KHÔNG ĐƯỢC ĐI CHÉO!
                        FlowFieldCell side1 = Cells[currentCell.GridIndex.x + dir.x, currentCell.GridIndex.y];
                        FlowFieldCell side2 = Cells[currentCell.GridIndex.x, currentCell.GridIndex.y + dir.y];

                        if (side1.Cost == 255 || side2.Cost == 255)
                        {
                            continue; // Chặn ngay, không cho chui qua khe tường hở!
                        }
                    }

                    // Đi thẳng tốn 10, đi chéo tốn 14 (~ 10 * 1.414 Pytago)
                    ushort moveCost = (dir.x != 0 && dir.y != 0) ? (ushort)14 : (ushort)10;
                    ushort newCost = (ushort)(currentCell.BestCost + moveCost);

                    if (newCost < neighbor.BestCost)
                    {
                        neighbor.BestCost = newCost;
                        _openQueue.Enqueue(neighbor);
                    }
                }
            }
        }

        // 3. Tạo Mũi Tên Dòng Chảy (Flow Field)
        public void CreateFlowField()
        {
            for (int x = 0; x < GridSize.x; x++)
            {
                for (int y = 0; y < GridSize.y; y++)
                {
                    FlowFieldCell cell = Cells[x, y];

                    // Ô tường thì không có mũi tên
                    if (cell.Cost == 255 || cell.BestCost == ushort.MaxValue)
                    {
                        cell.BestDirection = Vector2.zero;
                        continue;
                    }

                    ushort lowestCost = cell.BestCost;
                    FlowFieldCell bestNeighbor = null;

                    foreach (var dir in Directions)
                    {
                        Vector2Int neighborPos = cell.GridIndex + dir;

                        if (neighborPos.x < 0 || neighborPos.x >= GridSize.x || neighborPos.y < 0 || neighborPos.y >= GridSize.y)
                            continue;

                        FlowFieldCell neighbor = Cells[neighborPos.x, neighborPos.y];
                        if (neighbor.Cost == 255) continue;

                        // Kiểm tra chống cắt góc
                        if (dir.x != 0 && dir.y != 0)
                        {
                            FlowFieldCell side1 = Cells[cell.GridIndex.x + dir.x, cell.GridIndex.y];
                            FlowFieldCell side2 = Cells[cell.GridIndex.x, cell.GridIndex.y + dir.y];
                            if (side1.Cost == 255 || side2.Cost == 255) continue;
                        }

                        if (neighbor.BestCost < lowestCost)
                        {
                            lowestCost = neighbor.BestCost;
                            bestNeighbor = neighbor;
                        }
                    }

                    // Mũi tên chỉ về phía ô hàng xóm có khoảng cách ngắn nhất
                    if (bestNeighbor != null)
                    {
                        cell.BestDirection = ((Vector2)(bestNeighbor.WorldPos - cell.WorldPos)).normalized;
                    }
                }
            }
        }

        // Lấy ô lưới từ vị trí thực tế trong thế giới game (O(1) cực nhanh)
        public FlowFieldCell GetCellFromWorldPos(Vector3 worldPos)
        {
            float percentX = (worldPos.x - OriginPosition.x) / (GridSize.x * CellSize);
            float percentY = (worldPos.y - OriginPosition.y) / (GridSize.y * CellSize);

            percentX = Mathf.Clamp01(percentX);
            percentY = Mathf.Clamp01(percentY);

            int x = Mathf.Clamp(Mathf.FloorToInt(percentX * GridSize.x), 0, GridSize.x - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(percentY * GridSize.y), 0, GridSize.y - 1);

            return Cells[x, y];
        }

        // Nội suy song tuyến tính (Bilinear Interpolation) để tạo dòng chảy uốn lượn mượt mà quanh góc cua
        public Vector2 GetInterpolatedFlowDirection(Vector3 worldPos)
        {
            FlowFieldCell currentCell = GetCellFromWorldPos(worldPos);

            // NẾU QUÁI LỠ BỊ KẸT VÀO Ô TƯỜNG (Cost = 255):
            // Lập tức kích hoạt Vector Tự Đẩy (Unstuck Vector) hất văng quái ra ô đất trống gần nhất!
            if (currentCell != null && currentCell.Cost == 255)
            {
                Vector2 unstuckDir = GetUnstuckDirection(currentCell);
                if (unstuckDir != Vector2.zero)
                {
                    return unstuckDir;
                }
            }

            // Tính tọa độ vị trí theo đơn vị ô lưới (tính từ tâm ô)
            float gridPosX = (worldPos.x - OriginPosition.x) / CellSize - 0.5f;
            float gridPosY = (worldPos.y - OriginPosition.y) / CellSize - 0.5f;

            int x0 = Mathf.FloorToInt(gridPosX);
            int y0 = Mathf.FloorToInt(gridPosY);
            int x1 = x0 + 1;
            int y1 = y0 + 1;

            // Tỉ lệ khoảng cách (từ 0.0 đến 1.0) giữa 4 ô kề cận
            float tx = gridPosX - x0;
            float ty = gridPosY - y0;

            // Giới hạn chỉ số trong viền bản đồ
            x0 = Mathf.Clamp(x0, 0, GridSize.x - 1);
            x1 = Mathf.Clamp(x1, 0, GridSize.x - 1);
            y0 = Mathf.Clamp(y0, 0, GridSize.y - 1);
            y1 = Mathf.Clamp(y1, 0, GridSize.y - 1);

            Vector2 d00 = Cells[x0, y0].BestDirection;
            Vector2 d10 = Cells[x1, y0].BestDirection;
            Vector2 d01 = Cells[x0, y1].BestDirection;
            Vector2 d11 = Cells[x1, y1].BestDirection;

            // Công thức nội suy song tuyến tính 4 góc
            Vector2 top = Vector2.Lerp(d01, d11, tx);
            Vector2 bottom = Vector2.Lerp(d00, d10, tx);
            Vector2 blendedDirection = Vector2.Lerp(bottom, top, ty);

            // Nếu vector sau khi nội suy có độ dài hợp lệ, chuẩn hóa về độ dài 1
            if (blendedDirection.sqrMagnitude > 0.001f)
            {
                return blendedDirection.normalized;
            }

            // Dự phòng: Trả về hướng của ô rời rạc hiện tại
            return currentCell != null ? currentCell.BestDirection : Vector2.zero;
        }

        // Tìm hướng đẩy văng quái ra khỏi tường về phía ô đất trống tối ưu nhất
        private Vector2 GetUnstuckDirection(FlowFieldCell wallCell)
        {
            FlowFieldCell bestNeighbor = null;
            ushort lowestCost = ushort.MaxValue;

            foreach (var dir in Directions)
            {
                int nx = wallCell.GridIndex.x + dir.x;
                int ny = wallCell.GridIndex.y + dir.y;

                if (nx >= 0 && nx < GridSize.x && ny >= 0 && ny < GridSize.y)
                {
                    FlowFieldCell neighbor = Cells[nx, ny];
                    // Chỉ tìm ô đất trống (Cost != 255) có con đường ngắn nhất về Player
                    if (neighbor.Cost != 255 && neighbor.BestCost < lowestCost)
                    {
                        lowestCost = neighbor.BestCost;
                        bestNeighbor = neighbor;
                    }
                }
            }

            if (bestNeighbor != null)
            {
                return ((Vector2)(bestNeighbor.WorldPos - wallCell.WorldPos)).normalized;
            }

            return Vector2.zero;
        }
    }
}

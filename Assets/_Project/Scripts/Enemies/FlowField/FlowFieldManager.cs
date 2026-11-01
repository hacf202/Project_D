using System.Collections.Generic;
using UnityEngine;
using Project_D.Player;
using Project_D.Minions;
using Project_D.Dungeon.Room;

namespace Project_D.Enemies.FlowField
{
    public class FlowFieldManager : MonoBehaviour
    {
        public static FlowFieldManager Instance { get; private set; }

        [Header("Grid Settings")]
        [SerializeField] private Vector2Int _defaultGridSize = new Vector2Int(72, 72);
        [SerializeField] private float _cellSize = 0.5f; // Độ phân giải 0.5m giúp ô vật cản ôm sát mép tường, không bị phình to
        [SerializeField] private LayerMask _obstacleLayer;

        [Header("Update Interval")]
        [SerializeField] private float _updateInterval = 0.2f; // Cập nhật hướng dòng chảy mỗi 0.2s

        [Header("Debug")]
        [SerializeField] private bool _drawGizmos = false;

        // KIẾN TRÚC LƯỚI BẤT ĐỐI XỨNG (ASYMMETRIC DUAL-GRID):
        // 1. _minionGrid: Điểm đến DUY NHẤT là Player -> Lính dùng để tìm đường về Player, 100% không bao giờ kẹt hố tự thân
        // 2. _enemyGrid:  Điểm đến gồm LÍNH (ưu tiên cao, initialCost = 0) + PLAYER (initialCost = 25) -> Quái dùng để dồn vào Lính tối đa
        private FlowFieldGrid _minionGrid;
        private FlowFieldGrid _enemyGrid;

        private Transform _playerTransform;
        private float _timeSinceLastUpdate;
        private readonly List<FlowFieldCell> _targetCellsBuffer = new List<FlowFieldCell>(16);
        private readonly List<(FlowFieldCell cell, ushort initialCost)> _weightedTargetsBuffer = new List<(FlowFieldCell cell, ushort initialCost)>(32);

        public FlowFieldGrid Grid => _enemyGrid;
        public FlowFieldGrid MinionGrid => _minionGrid;
        public FlowFieldGrid EnemyGrid => _enemyGrid;
        public float CellSize => _cellSize;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Đảm bảo độ phân giải ô lưới mịn (0.5m) giúp ô vật cản ôm khít theo đúng mép collider
            if (_cellSize > 0.5f)
            {
                _cellSize = 0.5f;
            }

            if (_obstacleLayer.value == 0)
            {
                _obstacleLayer = LayerMask.GetMask("Obstacle", "Obstacle ");
                if (_obstacleLayer.value == 0) _obstacleLayer = 1 << 7;
            }
        }

        private void Start()
        {
            FindPlayer();

            // Nếu trong Scene đã có RoomLayout hiện hữu (sàn phòng), gắn lưới trực tiếp lên sàn phòng
            var roomLayout = Object.FindAnyObjectByType<RoomLayout>();
            if (roomLayout != null)
            {
                InitializeForRoom(roomLayout);
            }
            else
            {
                // Khởi tạo lưới mặc định bao quanh tâm (0,0) hoặc tâm phòng
                InitializeGrid(Vector3.zero, _defaultGridSize);
            }
        }

        private void Update()
        {
            _timeSinceLastUpdate += Time.deltaTime;

            if (_timeSinceLastUpdate >= _updateInterval)
            {
                _timeSinceLastUpdate = 0f;
                UpdateFlowField();
            }
        }

        private void FindPlayer()
        {
            if (_playerTransform == null)
            {
                var player = Object.FindAnyObjectByType<PlayerController>();
                if (player != null) _playerTransform = player.transform;
            }
        }

        // =========================================================================
        // GẮN LƯỚI FLOW FIELD CỐ ĐỊNH LÊN SÀN CỦA PHÒNG
        // =========================================================================

        public void InitializeForRoom(RoomLayout room)
        {
            if (room == null) return;

            Vector3 center = room.transform.position;
            Vector2 roomSize = room.RoomSize;
            if (roomSize.x <= 0f || roomSize.y <= 0f) roomSize = new Vector2(32f, 32f);

            // Thêm vùng biên an toàn 4m để bao trọn mọi cửa ngõ và vị trí spawn
            int gridX = Mathf.CeilToInt((roomSize.x + 4f) / _cellSize);
            int gridY = Mathf.CeilToInt((roomSize.y + 4f) / _cellSize);
            Vector2Int gridSize = new Vector2Int(gridX, gridY);

            InitializeGrid(center, gridSize);
        }

        public void InitializeGrid(Vector3 center, Vector2Int gridSize)
        {
            // Căn chỉnh tọa độ gốc Origin của FlowField theo đúng bội số của CellSize
            // để từng ô FlowField trùng khớp hoàn hảo với hệ tọa độ Tilemap, không bị lệch nửa ô
            float originX = Mathf.Floor((center.x - gridSize.x * _cellSize * 0.5f) / _cellSize) * _cellSize;
            float originY = Mathf.Floor((center.y - gridSize.y * _cellSize * 0.5f) / _cellSize) * _cellSize;
            Vector3 origin = new Vector3(originX, originY, 0f);

            // Khởi tạo 2 lưới bất đối xứng:
            _minionGrid = new FlowFieldGrid(gridSize, _cellSize, origin);
            _enemyGrid = new FlowFieldGrid(gridSize, _cellSize, origin);

            // Nướng bản đồ vật cản một lần duy nhất trên _minionGrid, rồi sao chép sang _enemyGrid (0 Raycast, 0 lag CPU)
            _minionGrid.UpdateCostField(_obstacleLayer);
            _enemyGrid.CopyCostFrom(_minionGrid);

            UpdateFlowField();
        }

        // =========================================================================
        // CẬP NHẬT DÒNG CHẢY 2 LƯỚI BẤT ĐỐI XỨNG
        // =========================================================================

        public void UpdateFlowField()
        {
            if (_minionGrid == null || _enemyGrid == null) return;

            FindPlayer();
            if (_playerTransform == null) return;

            // 1. CẬP NHẬT LƯỚI LÍNH (_minionGrid): Đích đến DUY NHẤT là Player
            // Lính 100% không bao giờ tự nhắm vào chính mình, luôn tìm đường thoát mọi góc tường về phía Player!
            _targetCellsBuffer.Clear();
            TryAddTargetCell(_minionGrid, _playerTransform.position, _targetCellsBuffer);
            if (_targetCellsBuffer.Count > 0)
            {
                _minionGrid.CreateIntegrationField(_targetCellsBuffer);
                _minionGrid.CreateFlowField();
            }

            // 2. CẬP NHẬT LƯỚI QUÁI (_enemyGrid): Đích đến là LÍNH (ưu tiên cao, initialCost = 0) + PLAYER (initialCost = 25)
            // Lính thu hút quái tối đa: quái ở cự ly ngang nhau giữa Player và Lính sẽ ƯU TIÊN đổ dồn vào Lính!
            _weightedTargetsBuffer.Clear();

            if (MinionManager.Instance != null)
            {
                var minions = MinionManager.Instance.ActiveMinions;
                for (int i = 0; i < minions.Count; i++)
                {
                    var m = minions[i];
                    if (m != null && m.IsAlive && m.CanAcceptMoreAttackers)
                    {
                        FlowFieldCell cell = _enemyGrid.GetCellFromWorldPos(m.transform.position);
                        if (cell != null)
                        {
                            if (cell.Cost == 255) cell = GetNearestWalkableCell(_enemyGrid, cell);
                            if (cell != null)
                            {
                                // Lính có initialCost = 0: Hố trũng sâu nhất, hút quái tối đa
                                _weightedTargetsBuffer.Add((cell, 0));
                            }
                        }
                    }
                }
            }

            // Player có initialCost = 25: Trọng lực nền tảng, thấp hơn lính để quái ưu tiên đổ dồn vào lính
            FlowFieldCell playerCell = _enemyGrid.GetCellFromWorldPos(_playerTransform.position);
            if (playerCell != null)
            {
                if (playerCell.Cost == 255) playerCell = GetNearestWalkableCell(_enemyGrid, playerCell);
                if (playerCell != null)
                {
                    _weightedTargetsBuffer.Add((playerCell, 25));
                }
            }

            if (_weightedTargetsBuffer.Count > 0)
            {
                _enemyGrid.CreateIntegrationField(_weightedTargetsBuffer);
                _enemyGrid.CreateFlowField();
            }
        }

        private void TryAddTargetCell(FlowFieldGrid grid, Vector3 worldPos, List<FlowFieldCell> buffer)
        {
            if (grid == null) return;
            FlowFieldCell cell = grid.GetCellFromWorldPos(worldPos);
            if (cell == null) return;

            if (cell.Cost != 255)
            {
                if (!buffer.Contains(cell))
                {
                    buffer.Add(cell);
                }
            }
            else
            {
                FlowFieldCell nearestWalkable = GetNearestWalkableCell(grid, cell);
                if (nearestWalkable != null && !buffer.Contains(nearestWalkable))
                {
                    buffer.Add(nearestWalkable);
                }
            }
        }

        private FlowFieldCell GetNearestWalkableCell(FlowFieldGrid grid, FlowFieldCell centerCell)
        {
            if (grid == null || centerCell == null) return null;

            Vector2Int center = centerCell.GridIndex;
            for (int r = 1; r <= 3; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                        int nx = center.x + dx;
                        int ny = center.y + dy;
                        if (nx >= 0 && nx < grid.GridSize.x && ny >= 0 && ny < grid.GridSize.y)
                        {
                            FlowFieldCell c = grid.Cells[nx, ny];
                            if (c != null && c.Cost != 255)
                            {
                                return c;
                            }
                        }
                    }
                }
            }
            return null;
        }

        // Lấy hướng dòng chảy cho QUÁI (hướng dồn về Lính & Player)
        public Vector2 GetFlowDirection(Vector3 worldPos)
        {
            if (_enemyGrid == null) return Vector2.zero;
            return _enemyGrid.GetInterpolatedFlowDirection(worldPos);
        }

        // Lấy hướng dòng chảy RIÊNG CHO LÍNH (hướng 100% về Player, không bị hố tự thân)
        public Vector2 GetMinionFlowDirection(Vector3 worldPos)
        {
            if (_minionGrid == null) return Vector2.zero;
            return _minionGrid.GetInterpolatedFlowDirection(worldPos);
        }

        public bool IsWalkable(Vector3 worldPos)
        {
            if (_enemyGrid == null) return true;
            var cell = _enemyGrid.GetCellFromWorldPos(worldPos);
            return cell != null && cell.Cost != 255;
        }

        private void OnDrawGizmos()
        {
            if (!_drawGizmos || _enemyGrid == null) return;

            for (int x = 0; x < _enemyGrid.GridSize.x; x++)
            {
                for (int y = 0; y < _enemyGrid.GridSize.y; y++)
                {
                    FlowFieldCell cell = _enemyGrid.Cells[x, y];

                    if (cell.Cost == 255)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireCube(cell.WorldPos, Vector3.one * (_enemyGrid.CellSize * 0.9f));
                    }
                    else if (cell.BestDirection != Vector2.zero)
                    {
                        Gizmos.color = Color.yellow;
                        Vector3 start = cell.WorldPos;
                        Vector3 end = start + (Vector3)(cell.BestDirection * (_enemyGrid.CellSize * 0.4f));
                        Gizmos.DrawLine(start, end);
                    }
                }
            }
        }
    }
}

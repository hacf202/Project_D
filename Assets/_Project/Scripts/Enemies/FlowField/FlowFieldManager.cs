using UnityEngine;
using Project_D.Player;

namespace Project_D.Enemies.FlowField
{
    public class FlowFieldManager : MonoBehaviour
    {
        public static FlowFieldManager Instance { get; private set; }

        [Header("Grid Settings")]
        [SerializeField] private Vector2Int _gridSize = new Vector2Int(30, 30);
        [SerializeField] private float _cellSize = 1f;
        [SerializeField] private LayerMask _obstacleLayer;

        [Header("Update Interval")]
        [SerializeField] private float _updateInterval = 0.2f; // Cập nhật mỗi 0.2s để tiết kiệm CPU

        [Header("Debug")]
        [SerializeField] private bool _drawGizmos = true;

        private FlowFieldGrid _grid;
        private Transform _playerTransform;
        private float _timeSinceLastUpdate;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                _playerTransform = player.transform;
            }

            // Tạo lưới bao quanh tâm ban đầu
            Vector3 origin = transform.position - new Vector3(_gridSize.x * _cellSize * 0.5f, _gridSize.y * _cellSize * 0.5f, 0f);
            _grid = new FlowFieldGrid(_gridSize, _cellSize, origin);

            UpdateFlowField();
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

        public void UpdateFlowField()
        {
            if (_playerTransform == null || _grid == null) return;

            // Khóa cứng tâm bàn cờ theo CellSize (Grid Snapping) để lưới không bao giờ bị trôi dạt lẻ
            float snappedX = Mathf.Round(_playerTransform.position.x / _cellSize) * _cellSize;
            float snappedY = Mathf.Round(_playerTransform.position.y / _cellSize) * _cellSize;
            Vector3 snappedCenter = new Vector3(snappedX, snappedY, 0f);

            Vector3 newOrigin = snappedCenter - new Vector3(_gridSize.x * _cellSize * 0.5f, _gridSize.y * _cellSize * 0.5f, 0f);
            
            // Tái khởi tạo lưới nếu cần hoặc cập nhật tại chỗ
            _grid = new FlowFieldGrid(_gridSize, _cellSize, newOrigin);
            _grid.UpdateCostField(_obstacleLayer);

            FlowFieldCell playerCell = _grid.GetCellFromWorldPos(_playerTransform.position);
            _grid.CreateIntegrationField(playerCell);
            _grid.CreateFlowField();
        }

        // Hàm để bất kỳ con quái nào cũng có thể xin hướng đi trong O(1) với hướng uốn cong mượt mà (Bilinear)
        public Vector2 GetFlowDirection(Vector3 worldPos)
        {
            if (_grid == null) return Vector2.zero;

            return _grid.GetInterpolatedFlowDirection(worldPos);
        }

        // Vẽ các mũi tên dòng chảy trong cửa sổ Scene
        private void OnDrawGizmos()
        {
            if (!_drawGizmos || _grid == null) return;

            for (int x = 0; x < _grid.GridSize.x; x++)
            {
                for (int y = 0; y < _grid.GridSize.y; y++)
                {
                    FlowFieldCell cell = _grid.Cells[x, y];

                    if (cell.Cost == 255)
                    {
                        Gizmos.color = Color.red; // Ô tường: Màu đỏ
                        Gizmos.DrawWireCube(cell.WorldPos, Vector3.one * (_grid.CellSize * 0.9f));
                    }
                    else if (cell.BestDirection != Vector2.zero)
                    {
                        Gizmos.color = Color.yellow; // Mũi tên dòng chảy: Màu vàng
                        Vector3 start = cell.WorldPos;
                        Vector3 end = start + (Vector3)(cell.BestDirection * (_grid.CellSize * 0.4f));
                        Gizmos.DrawLine(start, end);
                    }
                }
            }
        }
    }
}

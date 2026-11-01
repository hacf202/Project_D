using System;
using UnityEngine;
using Project_D.Core;

namespace Project_D.Combat
{
    [RequireComponent(typeof(Collider2D))]
    public class ExpGem : MonoBehaviour
    {
        [Header("Experience Settings")]
        [SerializeField] private int _expAmount = 10;

        [Header("Movement Settings")]
        [SerializeField] private float _initialSpeed = 2f;
        [SerializeField] private float _acceleration = 25f;
        [SerializeField] private float _maxSpeed = 22f;
        [SerializeField] private float _collectDistance = 0.5f;

        // Observer Pattern: Hệ thống UI, PlayerStats, Audio chỉ cần lắng nghe sự kiện này
        public static event Action<int> OnExpCollected;

        private static int _globalOrderCounter = 0;

        private GameObject _prefabOrigin;
        private Transform _targetTransform;
        private SpriteRenderer _spriteRenderer;
        private Animator _animator;
        private bool _isAttracted;
        private bool _isCollected;
        private float _currentSpeed;
        private float _collectDistanceSqr;

        private void Awake()
        {
            _collectDistanceSqr = _collectDistance * _collectDistance;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();
            
            // Đảm bảo Collider là Trigger để không cản trở di chuyển
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        public void Initialize(int expAmount, GameObject prefabOrigin)
        {
            _expAmount = expAmount;
            _prefabOrigin = prefabOrigin;
            _isCollected = false;
        }

        private void OnEnable()
        {
            _isAttracted = false;
            _isCollected = false;
            _targetTransform = null;
            _currentSpeed = _initialSpeed;

            // Xử lý triệt để lỗi chớp nháy (Z-fighting / Sorting Order Tie-break):
            // 1. Phân tầng SortingOrder duy nhất (2 -> 51) nằm trên sàn nhà (Order 0) và dưới nhân vật (Order 5)
            _globalOrderCounter = (_globalOrderCounter + 1) % 500;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sortingOrder = 2 + (_globalOrderCounter % 50);
            }

            // 2. Micro-Z depth phân lớp cố định: loại bỏ xung đột Depth Buffer của Camera
            Vector3 pos = transform.position;
            pos.z = -0.0001f * (_globalOrderCounter + 1);
            transform.position = pos;

            // 3. Phân tán pha chuyển động (Animation Cycle Offset) giúp các viên ngọc lấp lánh tự nhiên
            if (_animator != null && _animator.isActiveAndEnabled)
            {
                _animator.Play(0, -1, UnityEngine.Random.value);
            }
        }

        // Được kích hoạt bởi PlayerMagnet khi Player đi vào phạm vi hút
        public void StartAttract(Transform target)
        {
            if (_isAttracted || _isCollected) return;

            _targetTransform = target;
            _isAttracted = true;
            _currentSpeed = _initialSpeed;
        }

        private void Update()
        {
            if (!_isAttracted || _isCollected || _targetTransform == null) return;

            // 1. Gia tốc tăng dần theo thời gian (Zero Allocation)
            _currentSpeed = Mathf.Min(_currentSpeed + _acceleration * Time.deltaTime, _maxSpeed);

            // 2. Di chuyển mượt mà về phía Player
            transform.position = Vector3.MoveTowards(
                transform.position, 
                _targetTransform.position, 
                _currentSpeed * Time.deltaTime
            );

            // 3. Kiểm tra cự ly bình phương (chống xuyên thấu khi tốc độ quá cao)
            float sqrDistance = (transform.position - _targetTransform.position).sqrMagnitude;
            if (sqrDistance <= _collectDistanceSqr)
            {
                Collect();
            }
        }

        private void Collect()
        {
            if (_isCollected) return;
            _isCollected = true;

            // Bắn tín hiệu nạp Exp
            OnExpCollected?.Invoke(_expAmount);

            // Tái sử dụng ngọc thông qua PoolManager (Zero GC)
            if (PoolManager.Instance != null && _prefabOrigin != null)
            {
                PoolManager.Instance.ReturnToPool(_prefabOrigin, gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}

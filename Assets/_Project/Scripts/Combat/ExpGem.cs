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

        private GameObject _prefabOrigin;
        private Transform _targetTransform;
        private bool _isAttracted;
        private bool _isCollected;
        private float _currentSpeed;
        private float _collectDistanceSqr;

        private void Awake()
        {
            _collectDistanceSqr = _collectDistance * _collectDistance;
            
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

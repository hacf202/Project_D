using UnityEngine;

namespace Project_D.Core
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private Transform _target;        // Mục tiêu cần bám theo (Player)
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0f, -10f); // Khoảng cách lệch (khóa Z = -10)

        [Header("Smooth Settings")]
        [SerializeField] private float _smoothTime = 0.2f; // Thời gian làm mượt (càng nhỏ càng bám sát)

        private Vector3 _currentVelocity = Vector3.zero;   // Biến vận tốc nội bộ dùng cho SmoothDamp

        // Dùng LateUpdate để triệt tiêu hoàn toàn rung giật màn hình
        private void LateUpdate()
        {
            if (_target == null) return;

            // 1. Tọa độ đích mà Camera cần đến = Tọa độ Player + khoảng lệch Offset
            Vector3 targetPosition = _target.position + _offset;

            // 2. Lướt êm ái từ vị trí hiện tại tới tọa độ đích bằng SmoothDamp
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _currentVelocity, _smoothTime);
        }

        // Hàm hỗ trợ tự động tìm Player nếu chưa kéo thả vào Inspector
        public void SetTarget(Transform target)
        {
            _target = target;
        }
    }
}

using UnityEngine;
using Project_D.Combat;

namespace Project_D.Player
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class PlayerMagnet : MonoBehaviour
    {
        [Header("Magnet Configuration")]
        [SerializeField] private float _pickupRadius = 3f;
        [SerializeField] private Transform _targetCenter;

        private CircleCollider2D _triggerCollider;

        public float PickupRadius => _pickupRadius;

        private void Awake()
        {
            if (_targetCenter == null)
            {
                _targetCenter = transform.parent != null ? transform.parent : transform;
            }

            _triggerCollider = GetComponent<CircleCollider2D>();
            _triggerCollider.isTrigger = true;
            UpdateColliderRadius();
        }

        private void UpdateColliderRadius()
        {
            if (_triggerCollider != null)
            {
                _triggerCollider.radius = _pickupRadius;
            }
        }

        // Hỗ trợ nâng cấp bán kính hút từ thẻ Level Up
        public void SetPickupRadius(float newRadius)
        {
            _pickupRadius = Mathf.Max(0.5f, newRadius);
            UpdateColliderRadius();
        }

        public void IncreasePickupRadius(float amount)
        {
            SetPickupRadius(_pickupRadius + amount);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Kiểm tra và kích hoạt bay ngọc
            if (other.TryGetComponent<ExpGem>(out var gem))
            {
                gem.StartAttract(_targetCenter != null ? _targetCenter : transform);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _pickupRadius);
        }
    }
}

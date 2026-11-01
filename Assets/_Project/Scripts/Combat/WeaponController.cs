using UnityEngine;
using Project_D.Core;

namespace Project_D.Combat
{
    public class WeaponController : MonoBehaviour
    {
        [Header("Weapon Configuration")]
        [SerializeField] private WeaponDataSO _weaponData;         // Cấu hình dữ liệu vũ khí (ScriptableObject)
        [SerializeField] private Transform _firePoint;             // Điểm xuất phát của viên đạn (nòng súng)
        [SerializeField] private LayerMask _enemyLayerMask;        // Bộ lọc: Chỉ quét Layer Enemy
        [Tooltip("Bước góc tách cho mỗi luồng đạn phụ bổ sung xòe sang 2 bên (độ)")]
        [SerializeField] private float _subProjectileSpreadStep = 10f;

        // Bộ đệm tĩnh chứa kết quả quét va chạm (Tránh sinh rác GC trên Mobile)
        private readonly Collider2D[] _targetBuffer = new Collider2D[20];
        private ContactFilter2D _contactFilter;
        private float _fireCooldown;

        public WeaponDataSO WeaponData => _weaponData;

        private void Awake()
        {
            // Thiết lập bộ lọc va chạm chuẩn Unity 6
            _contactFilter = new ContactFilter2D();
            _contactFilter.SetLayerMask(_enemyLayerMask);
            _contactFilter.useLayerMask = true;
            _contactFilter.useTriggers = true;
        }

        // Cho phép gán hoặc thay đổi vũ khí trong lúc chơi (dành cho Level Up / nhặt súng mới)
        public void SetWeaponData(WeaponDataSO data)
        {
            _weaponData = data;
            if (_weaponData != null && _weaponData.ProjectilePrefab != null && PoolManager.Instance != null)
            {
                PoolManager.Instance.Prewarm(_weaponData.ProjectilePrefab, 15);
            }
        }

        private void Start()
        {
            if (_firePoint == null)
            {
                _firePoint = transform;
            }

            if (_weaponData != null)
            {
                SetWeaponData(_weaponData);
            }
        }

        private void Update()
        {
            if (_weaponData == null) return;

            _fireCooldown -= Time.deltaTime;

            if (_fireCooldown <= 0f)
            {
                Transform nearestEnemy = FindNearestEnemy();

                if (nearestEnemy != null)
                {
                    Shoot(nearestEnemy.position);
                    float effectiveRate = _weaponData.FireRate * (Player.PlayerStats.Instance != null ? Player.PlayerStats.Instance.FireRateModifier : 1f);
                    _fireCooldown = 1f / Mathf.Max(0.1f, effectiveRate);
                }
            }
        }

        // 1. Quét tìm quái vật gần nhất trong bán kính của vũ khí
        private Transform FindNearestEnemy()
        {
            // Quét các collider bằng ContactFilter2D chuẩn Unity 6 (Zero Alloc)
            int hitCount = Physics2D.OverlapCircle(transform.position, _weaponData.Range, _contactFilter, _targetBuffer);

            if (hitCount == 0) return null;

            Transform closestTarget = null;
            float closestDistanceSqr = Mathf.Infinity;

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D col = _targetBuffer[i];
                if (col == null) continue;

                Vector2 directionToTarget = col.transform.position - transform.position;
                float distanceSqr = directionToTarget.sqrMagnitude;

                if (distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    closestTarget = col.transform;
                }
            }

            return closestTarget;
        }

        // 2. Tính góc xoay và bắn đạn (hỗ trợ cả bắn 1 viên lẫn bắn chùm Shotgun)
        private void Shoot(Vector3 targetPosition)
        {
            if (_weaponData.ProjectilePrefab == null || PoolManager.Instance == null) return;

            Vector2 direction = targetPosition - _firePoint.position;
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            int extraProjectiles = Player.PlayerStats.Instance != null ? Player.PlayerStats.Instance.ProjectilesBonus : 0;
            int count = Mathf.Max(1, _weaponData.ProjectilesPerShot + extraProjectiles);

            // Xác định góc tách cho các luồng đạn phụ bổ sung xòe sang 2 bên
            float step = _weaponData.SpreadAngle > 0f 
                ? (_weaponData.SpreadAngle / Mathf.Max(2, count - 1)) 
                : _subProjectileSpreadStep;

            int baseDamage = _weaponData != null ? _weaponData.Damage : 10;
            int bonusDamage = Player.PlayerStats.Instance != null ? Player.PlayerStats.Instance.DamageBonus : 0;
            int totalDamage = Mathf.Max(1, baseDamage + bonusDamage);
            float knockback = _weaponData != null ? _weaponData.KnockbackForce : 0f;

            for (int i = 0; i < count; i++)
            {
                // TIA CHÍNH (i = 0): LUÔN LUÔN bắn thẳng tâm vào mục tiêu (độ lệch 0 độ)
                // CÁC TIA PHỤ (i > 0): Là các luồng đạn phụ bổ sung xòe đều đối xứng sang 2 bên (+-10, +-20...)
                float angleOffset = 0f;
                if (i > 0)
                {
                    int pair = (i + 1) / 2;
                    float sign = (i % 2 == 1) ? 1f : -1f;
                    angleOffset = sign * pair * step;
                }

                float currentAngle = baseAngle + angleOffset;
                Quaternion bulletRotation = Quaternion.Euler(0f, 0f, currentAngle);

                // Lấy đạn từ Pool ra
                GameObject bulletObj = PoolManager.Instance.Get(_weaponData.ProjectilePrefab, _firePoint.position, bulletRotation);

                if (bulletObj.TryGetComponent<Projectile>(out var projectile))
                {
                    projectile.Initialize(_weaponData.ProjectilePrefab, totalDamage, knockback);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_weaponData == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _weaponData.Range);
        }
    }
}

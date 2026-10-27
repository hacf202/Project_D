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
            float spread = _weaponData.SpreadAngle;

            // Nếu bình thường bắn 1 viên nhưng được nâng cấp thêm đạn, tự động tạo góc xòe nhẹ (ví dụ 15 độ)
            if (spread <= 0f && count > 1)
            {
                spread = 15f * (count - 1);
            }

            // Tính góc bắt đầu và bước nhảy góc nếu bắn nhiều viên đạn cùng lúc
            float startAngle = count > 1 ? baseAngle - (spread / 2f) : baseAngle;
            float angleStep = count > 1 ? spread / (count - 1) : 0f;

            int baseDamage = _weaponData != null ? _weaponData.Damage : 10;
            int bonusDamage = Player.PlayerStats.Instance != null ? Player.PlayerStats.Instance.DamageBonus : 0;
            int totalDamage = Mathf.Max(1, baseDamage + bonusDamage);
            float knockback = _weaponData != null ? _weaponData.KnockbackForce : 0f;

            for (int i = 0; i < count; i++)
            {
                float currentAngle = count > 1 ? startAngle + (i * angleStep) : baseAngle;
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

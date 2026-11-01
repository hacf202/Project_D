using UnityEngine;

namespace Project_D.Enemies
{
    public enum EnemyClassification
    {
        Normal = 0,
        Elite = 1,
        MiniBoss = 2,
        Boss = 3
    }

    public enum EnemyAttackMethod
    {
        Melee = 0,
        Ranged = 1,
        Explode = 2,
        Summoner = 3
    }

    [CreateAssetMenu(fileName = "New_Enemy_Data", menuName = "Project_D/Enemies/Enemy Data")]
    public class EnemyDataSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _enemyName = "Basic Zombie";
        [SerializeField] private EnemyClassification _classification = EnemyClassification.Normal;
        [SerializeField] private EnemyAttackMethod _attackMethod = EnemyAttackMethod.Melee;
        [SerializeField] private GameObject _prefab;

        [Header("Stats")]
        [SerializeField] private int _maxHealth = 30;
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private int _contactDamage = 5;
        [Tooltip("Khoảng thời gian tối thiểu giữa 2 lần gây sát thương hoặc tấn công (giây).")]
        [SerializeField] private float _attackInterval = 1.0f;
        [Tooltip("Cự ly kích hoạt tấn công tầm xa hoặc cự ly kích nổ (m).")]
        [SerializeField] private float _attackRange = 0f;
        [Range(0f, 1f)]
        [Tooltip("Khả năng kháng đẩy lùi (0 = bị đẩy bình thường, 1 = miễn nhiễm hoàn toàn).")]
        [SerializeField] private float _knockbackResistance = 0f;
        [SerializeField] private int _expReward = 10;
        [SerializeField] private int _soulReward = 2; // Số Souls cộng thẳng cho người chơi khi diệt quái này

        [Header("Combat & Projectile")]
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private float _projectileSpeed = 7f;
        [SerializeField] private int _projectileDamage = 0; // 0 = dùng contactDamage
        [SerializeField] private float _explosionRadius = 2.5f;

        [Header("Loot Settings")]
        [SerializeField] private GameObject _expGemPrefab;

        // Public Properties (Read-Only)
        public string EnemyName => _enemyName;
        public EnemyClassification Classification => _classification;
        public EnemyAttackMethod AttackMethod => _attackMethod;
        public GameObject Prefab => _prefab;
        public int MaxHealth => _maxHealth;
        public float MoveSpeed => _moveSpeed;
        public int ContactDamage => _contactDamage;
        public float AttackInterval => _attackInterval > 0f ? _attackInterval : 1.0f;
        public float AttackRange => _attackRange;
        public float KnockbackResistance => Mathf.Clamp01(_knockbackResistance);
        public int ExpReward => _expReward;
        public int SoulReward => _soulReward;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public float ProjectileSpeed => _projectileSpeed > 0f ? _projectileSpeed : 7f;
        public int ProjectileDamage => _projectileDamage > 0 ? _projectileDamage : _contactDamage;
        public float ExplosionRadius => _explosionRadius > 0f ? _explosionRadius : 2.5f;
        public GameObject ExpGemPrefab => _expGemPrefab;

        public bool IsBoss => _classification == EnemyClassification.Boss;
        public bool IsMiniBoss => _classification == EnemyClassification.MiniBoss;
        public bool IsBossOrMiniBoss => _classification == EnemyClassification.Boss || _classification == EnemyClassification.MiniBoss;
    }
}

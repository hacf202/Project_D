using UnityEngine;

namespace Project_D.Minions
{
    [CreateAssetMenu(fileName = "NewMinionData", menuName = "Project_D/Minions/Minion Data")]
    public class MinionDataSO : ScriptableObject
    {
        [Header("General Identity")]
        [SerializeField] private string _id = "minion_warrior_t1";
        [SerializeField] private string _minionName = "Chiến Binh Kiếm Gỗ";
        [TextArea(2, 4)]
        [SerializeField] private string _description = "Lính cận chiến cơ bản bảo vệ chủ nhân.";
        [SerializeField] private MinionClass _class = MinionClass.Warrior;
        [SerializeField] private MinionTier _tier = MinionTier.Tier1;
        [SerializeField] private Sprite _icon;

        [Header("Combat Stats")]
        [SerializeField] private int _maxHealth = 100;
        [SerializeField] private int _attackDamage = 15;
        [SerializeField] private float _attackRange = 1.2f;      // Tầm đánh tối đa
        [SerializeField] private float _attackSpeed = 1.2f;      // Tần suất đánh (đòn / giây)
        [SerializeField] private float _moveSpeed = 4.5f;        // Tốc độ di chuyển
        [SerializeField] private float _aggroRange = 7.0f;       // Tầm quét phát hiện quái
        [SerializeField] private float _knockbackForce = 1.5f;   // Lực đẩy lùi quái khi trúng đòn

        [Header("Attack Configuration")]
        [SerializeField] private AttackMethod _attackMethod = AttackMethod.Melee;
        [SerializeField] private GameObject _projectilePrefab;   // Prefab đạn nếu dùng RangedProjectile
        [SerializeField] private float _aoeRadius = 2.0f;        // Bán kính sát thương nếu dùng AoE

        [Header("Respawn & Lifecycle")]
        [SerializeField] private float _respawnDuration = 10f;   // Thời gian đếm ngược hồi sinh (giây)
        [SerializeField] private GameObject _prefab;             // Prefab đại diện của lính

        // Properties công khai
        public string Id => _id;
        public string MinionName => _minionName;
        public string Description => _description;
        public MinionClass Class => _class;
        public MinionTier Tier => _tier;
        public Sprite Icon => _icon;

        public int MaxHealth => _maxHealth;
        public int AttackDamage => _attackDamage;
        public float AttackRange => _attackRange;
        public float AttackSpeed => _attackSpeed;
        public float MoveSpeed => _moveSpeed;
        public float AggroRange => _aggroRange;
        public float KnockbackForce => _knockbackForce;

        public AttackMethod AttackMethod => _attackMethod;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public float AoERadius => _aoeRadius;

        public float RespawnDuration => _respawnDuration;
        public GameObject Prefab => _prefab;

        public void SetPrefab(GameObject prefab)
        {
            _prefab = prefab;
        }
    }
}

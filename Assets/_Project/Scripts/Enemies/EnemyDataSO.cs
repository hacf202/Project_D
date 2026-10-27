using UnityEngine;

namespace Project_D.Enemies
{
    [CreateAssetMenu(fileName = "New_Enemy_Data", menuName = "Project_D/Enemies/Enemy Data")]
    public class EnemyDataSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _enemyName = "Basic Zombie";
        [SerializeField] private GameObject _prefab;

        [Header("Stats")]
        [SerializeField] private int _maxHealth = 30;
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private int _contactDamage = 5;
        [SerializeField] private int _expReward = 10;
        [SerializeField] private int _soulReward = 2; // Số Souls cộng thẳng cho người chơi khi diệt quái này

        [Header("Loot Settings")]
        [SerializeField] private GameObject _expGemPrefab;

        // Public Properties (Read-Only)
        public string EnemyName => _enemyName;
        public GameObject Prefab => _prefab;
        public int MaxHealth => _maxHealth;
        public float MoveSpeed => _moveSpeed;
        public int ContactDamage => _contactDamage;
        public int ExpReward => _expReward;
        public int SoulReward => _soulReward;
        public GameObject ExpGemPrefab => _expGemPrefab;
    }
}

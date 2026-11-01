using System;
using UnityEngine;
using Project_D.Combat;
using Project_D.Player.Data;

namespace Project_D.Player
{
    public class PlayerStats : MonoBehaviour
    {
        // Singleton để các hệ thống Combat, Movement, Health và UI dễ dàng truy vấn chỉ số
        public static PlayerStats Instance { get; private set; }

        [Header("Character Data Asset (ScriptableObject)")]
        [Tooltip("Dữ liệu chỉ số gốc của nhân vật")]
        [SerializeField] private PlayerDataSO _playerData;

        [Header("Runtime Level & Experience")]
        [SerializeField] private int _currentLevel = 1;
        [SerializeField] private int _currentExp = 0;
        private int _expToNextLevel;

        [Header("Combat Modifiers (Chỉ Số Cộng Dồn Từ Thẻ Bài / Shop)")]
        [SerializeField] private int _maxHealthBonus = 0;             // Máu tối đa cộng thêm
        [SerializeField] private float _moveSpeedModifier = 1f;       // Hệ số tốc độ chạy (1.0 = 100%)
        [SerializeField] private float _fireRateModifier = 1f;        // Hệ số tốc độ bắn (1.0 = 100%)
        [SerializeField] private int _damageBonus = 0;                // Sát thương cộng thêm
        [SerializeField] private int _projectilesBonus = 0;           // Số viên đạn bắn thêm mỗi loạt
        [SerializeField] private float _pickupRadiusBonus = 0f;       // Tăng cự ly hút nam châm
        [SerializeField] private float _healthRegenBonus = 0f;        // Hồi máu theo thời gian cộng thêm (HP/s)

        private PlayerHealth _playerHealth;

        // Observer Pattern: UI Thanh Exp và Bảng Level Up lắng nghe
        public static event Action<int, int, int> OnExpChanged; // currentExp, expToNextLevel, currentLevel
        public static event Action<int> OnLevelUp;               // newLevel

        public PlayerDataSO PlayerData => _playerData;

        // --- Base Stats (Lấy từ PlayerDataSO nếu có, hoặc Fallback an toàn) ---
        public int BaseMaxHealth => _playerData != null ? _playerData.BaseMaxHealth : 1000;
        public float BaseMoveSpeed => _playerData != null ? _playerData.BaseMoveSpeed : 5f;
        public float BasePickupRadius => _playerData != null ? _playerData.BasePickupRadius : 3.5f;
        public float BaseHealthRegen => _playerData != null ? _playerData.BaseHealthRegen : 0f;
        public int BaseDamageBonus => _playerData != null ? _playerData.BaseDamageBonus : 0;
        public float BaseFireRateModifier => _playerData != null ? _playerData.BaseFireRateModifier : 1f;
        public int BaseProjectilesBonus => _playerData != null ? _playerData.BaseProjectilesBonus : 0;
        public int BaseExpToNextLevel => _playerData != null ? _playerData.BaseExpToNextLevel : 20;
        public int ExpIncreasePerLevel => _playerData != null ? _playerData.ExpIncreasePerLevel : 15;

        // --- Runtime State Properties ---
        public int CurrentLevel => _currentLevel;
        public int CurrentExp => _currentExp;
        public int ExpToNextLevel => _expToNextLevel;

        // --- Effective Stats (Base + Modifiers) ---
        public int MaxHealth => BaseMaxHealth + _maxHealthBonus;
        public float MoveSpeed => BaseMoveSpeed * _moveSpeedModifier;
        public float PickupRadius => BasePickupRadius + _pickupRadiusBonus;
        public float FireRateModifier => BaseFireRateModifier * _fireRateModifier;
        public int DamageBonus => BaseDamageBonus + _damageBonus;
        public int ProjectilesBonus => BaseProjectilesBonus + _projectilesBonus;
        public float HealthRegen => BaseHealthRegen + _healthRegenBonus;

        private void Reset()
        {
            AutoWireComponents();
        }

        private void OnValidate()
        {
            AutoWireComponents();
        }

        private void AutoWireComponents()
        {
            if (_playerHealth == null)
            {
                _playerHealth = GetComponent<PlayerHealth>();
            }

            CalculateExpToNextLevel();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            AutoWireComponents();
        }

        private void Start()
        {
            // Cập nhật bán kính hút ban đầu cho PlayerMagnet nếu có
            var magnet = GetComponentInChildren<PlayerMagnet>();
            if (magnet != null)
            {
                magnet.SetPickupRadius(PickupRadius);
            }

            // Bắn tín hiệu ban đầu để cập nhật thanh Exp UI lúc mới vào trận
            OnExpChanged?.Invoke(_currentExp, _expToNextLevel, _currentLevel);
        }

        private void OnEnable()
        {
            // Lắng nghe sự kiện nhặt ngọc kinh nghiệm từ ExpGem
            ExpGem.OnExpCollected += HandleExpCollected;
        }

        private void OnDisable()
        {
            // BẮT BUỘC hủy đăng ký để tránh rò rỉ bộ nhớ (Memory Leak)
            ExpGem.OnExpCollected -= HandleExpCollected;
        }

        private void HandleExpCollected(int amount)
        {
            AddExp(amount);
        }

        // Tích lũy Exp và xử lý tràn kinh nghiệm (Exp Overflow)
        public void AddExp(int amount)
        {
            if (amount <= 0) return;

            _currentExp += amount;
            Debug.Log($"<color=cyan>[EXP +{amount}]</color> Hiện tại: {_currentExp}/{_expToNextLevel} (Cấp {_currentLevel})");

            // Vòng lặp while giúp xử lý trường hợp nhận Exp cực lớn tăng nhiều cấp liên tục
            while (_currentExp >= _expToNextLevel)
            {
                _currentExp -= _expToNextLevel;
                _currentLevel++;
                CalculateExpToNextLevel();

                Debug.Log($"<color=yellow><b>[LEVEL UP!]</b></color> Chúc mừng! Thăng cấp lên Level {_currentLevel}!");
                OnLevelUp?.Invoke(_currentLevel);
            }

            OnExpChanged?.Invoke(_currentExp, _expToNextLevel, _currentLevel);
        }

        private void CalculateExpToNextLevel()
        {
            // Công thức: Level 1 -> 20 exp, Level 2 -> 35 exp, Level 3 -> 50 exp...
            _expToNextLevel = BaseExpToNextLevel + (_currentLevel - 1) * ExpIncreasePerLevel;
        }

        public void SetPlayerData(PlayerDataSO data)
        {
            _playerData = data;
            CalculateExpToNextLevel();
            var magnet = GetComponentInChildren<PlayerMagnet>();
            if (magnet != null)
            {
                magnet.SetPickupRadius(PickupRadius);
            }
        }

        // --- Các hàm kích hoạt nâng cấp từ thẻ (Upgrade Cards API) ---
        public void AddMoveSpeedBonus(float percent)
        {
            _moveSpeedModifier += percent;
            Debug.Log($"<color=green>[UPGRADE]</color> Tốc độ chạy tăng +{percent * 100}%! Hiện tại: {MoveSpeed:F1} m/s");
        }

        public void AddFireRateBonus(float percent)
        {
            _fireRateModifier += percent;
            Debug.Log($"<color=green>[UPGRADE]</color> Tốc độ bắn tăng +{percent * 100}%! Hệ số: {FireRateModifier:F2}x");
        }

        public void AddDamageBonus(int amount)
        {
            _damageBonus += amount;
            Debug.Log($"<color=green>[UPGRADE]</color> Sát thương đạn tăng +{amount}! Tổng bonus: +{_damageBonus}");
        }

        public void AddProjectilesBonus(int count)
        {
            _projectilesBonus += count;
            Debug.Log($"<color=green>[UPGRADE]</color> Bắn thêm +{count} viên đạn mỗi loạt! Tổng đạn thêm: +{_projectilesBonus}");
        }

        public void AddPickupRadiusBonus(float radius)
        {
            _pickupRadiusBonus += radius;
            var magnet = GetComponentInChildren<PlayerMagnet>();
            if (magnet != null)
            {
                magnet.IncreasePickupRadius(radius);
            }
            Debug.Log($"<color=green>[UPGRADE]</color> Bán kính hút nam châm tăng +{radius}m! Hiện tại: {PickupRadius:F1}m");
        }

        public void AddMaxHealthBonus(int amount)
        {
            _maxHealthBonus += amount;
            if (_playerHealth != null)
            {
                _playerHealth.OnMaxHealthBonusAdded(amount, true);
            }
            Debug.Log($"<color=green>[UPGRADE]</color> Máu tối đa tăng +{amount}! Máu tối đa mới: {MaxHealth}");
        }

        public void AddHealthRegenBonus(float amount)
        {
            _healthRegenBonus += amount;
            Debug.Log($"<color=green>[UPGRADE]</color> Hồi phục máu tăng +{amount:F1} HP/s! Tổng hồi phục: {HealthRegen:F1} HP/s");
        }
    }
}

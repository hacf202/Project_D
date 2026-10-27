using System;
using UnityEngine;
using Project_D.Combat;

namespace Project_D.Player
{
    public class PlayerStats : MonoBehaviour
    {
        // Singleton để các hệ thống Combat, Movement, Health và UI dễ dàng truy vấn chỉ số
        public static PlayerStats Instance { get; private set; }

        [Header("Base Stats (Chỉ Số Gốc Nhân Vật)")]
        [SerializeField] private int _baseMaxHealth = 100;
        [SerializeField] private float _baseMoveSpeed = 5f;
        [SerializeField] private float _basePickupRadius = 3.5f;

        [Header("Level & Experience")]
        [SerializeField] private int _currentLevel = 1;
        [SerializeField] private int _currentExp = 0;
        [SerializeField] private int _baseExpToNextLevel = 20;
        [SerializeField] private int _expIncreasePerLevel = 15;
        private int _expToNextLevel;

        [Header("Combat Modifiers (Chỉ Số Cộng Dồn Từ Thẻ Bài)")]
        [SerializeField] private int _maxHealthBonus = 0;             // Máu tối đa cộng thêm
        [SerializeField] private float _moveSpeedModifier = 1f;       // Hệ số tốc độ chạy (1.0 = 100%)
        [SerializeField] private float _fireRateModifier = 1f;        // Hệ số tốc độ bắn (1.0 = 100%)
        [SerializeField] private int _damageBonus = 0;                // Sát thương cộng thêm
        [SerializeField] private int _projectilesBonus = 0;           // Số viên đạn bắn thêm mỗi loạt
        [SerializeField] private float _pickupRadiusBonus = 0f;       // Tăng cự ly hút nam châm

        // Observer Pattern: UI Thanh Exp và Bảng Level Up lắng nghe
        public static event Action<int, int, int> OnExpChanged; // currentExp, expToNextLevel, currentLevel
        public static event Action<int> OnLevelUp;               // newLevel

        public int CurrentLevel => _currentLevel;
        public int CurrentExp => _currentExp;
        public int ExpToNextLevel => _expToNextLevel;

        // --- Properties Tổng Hợp Chỉ Số (Base + Modifiers) ---
        public int MaxHealth => _baseMaxHealth + _maxHealthBonus;
        public float MoveSpeed => _baseMoveSpeed * _moveSpeedModifier;
        public float PickupRadius => _basePickupRadius + _pickupRadiusBonus;
        public float FireRateModifier => _fireRateModifier;
        public int DamageBonus => _damageBonus;
        public int ProjectilesBonus => _projectilesBonus;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CalculateExpToNextLevel();
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
            _expToNextLevel = _baseExpToNextLevel + (_currentLevel - 1) * _expIncreasePerLevel;
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
            Debug.Log($"<color=green>[UPGRADE]</color> Tốc độ bắn tăng +{percent * 100}%! Hệ số: {_fireRateModifier:F2}x");
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
            if (TryGetComponent<PlayerHealth>(out var health))
            {
                health.IncreaseMaxHealth(amount, true);
            }
            Debug.Log($"<color=green>[UPGRADE]</color> Máu tối đa tăng +{amount}! Máu tối đa mới: {MaxHealth}");
        }
    }
}

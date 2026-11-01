using System;
using System.Collections;
using UnityEngine;

namespace Project_D.Player
{
    [SelectionBase]
    [DisallowMultipleComponent]
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Stats Source (Liên kết PlayerStats)")]
        [Tooltip("Tham chiếu trực tiếp tới PlayerStats trên cùng nhân vật. Tự động liên kết nếu để trống.")]
        [SerializeField] private PlayerStats _playerStats;

        [Header("Health Status (Trạng Thái Sinh Lực)")]
        [Tooltip("Lượng máu tối đa dự phòng (chỉ áp dụng khi không có PlayerStats)")]
        [SerializeField] private int _fallbackMaxHealth = 1000;
        [Tooltip("Lượng máu hiện tại")]
        [SerializeField] private int _currentHealth;
        [Tooltip("Máu tối đa hiệu dụng đang áp dụng")]
        [SerializeField] private int _runtimeMaxHealth;

        [Header("Invulnerability Frames (I-Frames)")]
        [SerializeField] private float _iFrameDuration = 0.5f;
        [SerializeField] private float _flashInterval = 0.08f;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        // Observer Pattern: UI Máu hoặc GameStateManager chỉ cần lắng nghe sự kiện này
        public static event Action<int, int> OnHealthChanged; // currentHealth, maxHealth
        public static event Action OnPlayerDied;

        private bool _isInitialized;
        private bool _isInvulnerable;
        private Coroutine _iFrameCoroutine;
        private Color _originalColor = Color.white;
        private float _regenAccumulator = 0f;

        public static PlayerHealth Instance { get; private set; }

        public int CurrentHealth => _currentHealth;
        // Single Source of Truth: MaxHealth luôn query từ PlayerStats nếu có
        public int MaxHealth => _playerStats != null ? _playerStats.MaxHealth : (PlayerStats.Instance != null ? PlayerStats.Instance.MaxHealth : _fallbackMaxHealth);
        public bool IsAlive => _currentHealth > 0;
        public bool IsInvulnerable => _isInvulnerable;
        public PlayerStats LinkedStats => _playerStats;

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
            if (_playerStats == null)
            {
                _playerStats = GetComponent<PlayerStats>();
            }

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            // Đồng bộ giá trị dự phòng theo PlayerDataSO nếu có
            if (_playerStats != null && _playerStats.PlayerData != null)
            {
                _fallbackMaxHealth = _playerStats.PlayerData.BaseMaxHealth;
            }

            if (!Application.isPlaying)
            {
                _runtimeMaxHealth = MaxHealth;
                if (_currentHealth <= 0 || _currentHealth > _runtimeMaxHealth)
                {
                    _currentHealth = _runtimeMaxHealth;
                }
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            AutoWireComponents();

            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
            }

            // Khởi tạo máu ban đầu ngay trong Awake để an toàn trước mọi race condition
            InitializeHealth();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            // Đảm bảo dữ liệu máu đã khởi tạo và bắn sự kiện cập nhật HUD
            if (!_isInitialized)
            {
                InitializeHealth();
            }

            _runtimeMaxHealth = MaxHealth;
            OnHealthChanged?.Invoke(_currentHealth, _runtimeMaxHealth);
        }

        private void Update()
        {
            HandleHealthRegen();
        }

        private void InitializeHealth()
        {
            _runtimeMaxHealth = MaxHealth;
            _currentHealth = _runtimeMaxHealth;
            _isInitialized = true;
        }

        // Hồi máu thụ động theo thời gian dựa trên chỉ số HealthRegen từ PlayerStats
        private void HandleHealthRegen()
        {
            if (!IsAlive) return;

            float regenRate = _playerStats != null ? _playerStats.HealthRegen : (PlayerStats.Instance != null ? PlayerStats.Instance.HealthRegen : 0f);
            if (regenRate > 0f && _currentHealth < MaxHealth)
            {
                _regenAccumulator += regenRate * Time.deltaTime;
                if (_regenAccumulator >= 1f)
                {
                    int healAmount = Mathf.FloorToInt(_regenAccumulator);
                    _regenAccumulator -= healAmount;
                    Heal(healAmount);
                }
            }
        }

        private void OnDisable()
        {
            // Dọn dẹp Coroutine an toàn nếu Player bị vô hiệu hóa
            if (_iFrameCoroutine != null)
            {
                StopCoroutine(_iFrameCoroutine);
                _iFrameCoroutine = null;
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _originalColor;
            }

            _isInvulnerable = false;
        }

        // Nhận sát thương từ quái hoặc bẫy
        public void TakeDamage(int damage)
        {
            if (!IsAlive || _isInvulnerable || damage <= 0) return;

            _currentHealth = Mathf.Max(0, _currentHealth - damage);
            _runtimeMaxHealth = MaxHealth;
            Debug.Log($"<color=magenta>[PLAYER HIT]</color> Player bị trừ {damage} máu! Còn: {_currentHealth}/{_runtimeMaxHealth}");

            OnHealthChanged?.Invoke(_currentHealth, _runtimeMaxHealth);

            if (_currentHealth <= 0)
            {
                Die();
            }
            else
            {
                // Kích hoạt thời gian bất tử tạm thời
                if (_iFrameCoroutine != null)
                {
                    StopCoroutine(_iFrameCoroutine);
                }
                _iFrameCoroutine = StartCoroutine(IFrameRoutine());
            }
        }

        // Hồi máu (nhặt bình máu hoặc kỹ năng hồi phục)
        public void Heal(int amount)
        {
            if (!IsAlive || amount <= 0) return;

            _runtimeMaxHealth = MaxHealth;
            _currentHealth = Mathf.Min(_runtimeMaxHealth, _currentHealth + amount);
            Debug.Log($"<color=green>[PLAYER HEAL]</color> Hồi phục {amount} máu! Hiện tại: {_currentHealth}/{_runtimeMaxHealth}");

            OnHealthChanged?.Invoke(_currentHealth, _runtimeMaxHealth);
        }

        // Hồi máu theo % máu tối đa (ví dụ 0.3f = hồi 30% máu)
        public void HealPercent(float percent)
        {
            int healAmount = Mathf.RoundToInt(MaxHealth * Mathf.Clamp01(percent));
            Heal(healAmount);
        }

        // Hồi đầy 100% máu
        public void HealFull()
        {
            Heal(MaxHealth);
        }

        // Nhận thông báo khi máu tối đa tăng từ PlayerStats
        public void OnMaxHealthBonusAdded(int amount, bool healEquivalent = true)
        {
            if (amount <= 0) return;

            _runtimeMaxHealth = MaxHealth;
            if (healEquivalent)
            {
                _currentHealth = Mathf.Min(_runtimeMaxHealth, _currentHealth + amount);
            }

            OnHealthChanged?.Invoke(_currentHealth, _runtimeMaxHealth);
        }

        // Mở rộng máu tối đa (Tương thích ngược nếu các script bên ngoài gọi trực tiếp)
        public void IncreaseMaxHealth(int amount, bool healEquivalent = true)
        {
            if (_playerStats != null)
            {
                _playerStats.AddMaxHealthBonus(amount);
            }
            else if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.AddMaxHealthBonus(amount);
            }
            else
            {
                _fallbackMaxHealth += amount;
                _runtimeMaxHealth = _fallbackMaxHealth;
                if (healEquivalent)
                {
                    _currentHealth += amount;
                }
                OnHealthChanged?.Invoke(_currentHealth, _runtimeMaxHealth);
            }
        }

        private IEnumerator IFrameRoutine()
        {
            _isInvulnerable = true;

            float elapsed = 0f;
            bool isRed = false;

            while (elapsed < _iFrameDuration)
            {
                if (_spriteRenderer != null)
                {
                    // Nhấp nháy giữa màu đỏ và màu gốc
                    _spriteRenderer.color = isRed ? _originalColor : new Color(1f, 0.3f, 0.3f, 0.8f);
                    isRed = !isRed;
                }

                yield return new WaitForSeconds(_flashInterval);
                elapsed += _flashInterval;
            }

            // Trả lại trạng thái bình thường
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _originalColor;
            }

            _isInvulnerable = false;
            _iFrameCoroutine = null;
        }

        private void Die()
        {
            Debug.Log("<color=red><b>[GAME OVER]</b></color> Player đã ngã xuống!");
            OnPlayerDied?.Invoke();

            // Vô hiệu hóa điều khiển để nhân vật dừng lại
            if (TryGetComponent<PlayerController>(out var controller))
            {
                controller.enabled = false;
            }
        }
    }
}

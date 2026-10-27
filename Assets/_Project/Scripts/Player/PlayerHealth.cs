using System;
using System.Collections;
using UnityEngine;

namespace Project_D.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private int _maxHealth = 100;
        private int _currentHealth;

        [Header("Invulnerability Frames (I-Frames)")]
        [SerializeField] private float _iFrameDuration = 0.5f;
        [SerializeField] private float _flashInterval = 0.08f;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        // Observer Pattern: UI Máu hoặc GameStateManager chỉ cần lắng nghe sự kiện này
        public static event Action<int, int> OnHealthChanged; // currentHealth, maxHealth
        public static event Action OnPlayerDied;

        private bool _isInvulnerable;
        private Coroutine _iFrameCoroutine;
        private Color _originalColor = Color.white;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public bool IsAlive => _currentHealth > 0;
        public bool IsInvulnerable => _isInvulnerable;

        private void Awake()
        {
            _currentHealth = _maxHealth;

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
            }
        }

        private void Start()
        {
            if (PlayerStats.Instance != null)
            {
                _maxHealth = PlayerStats.Instance.MaxHealth;
                _currentHealth = _maxHealth;
            }

            // Bắn tín hiệu ban đầu để cập nhật UI lúc mới vào game
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
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

        // Nhận sát thương từ quái
        public void TakeDamage(int damage)
        {
            if (!IsAlive || _isInvulnerable || damage <= 0) return;

            _currentHealth = Mathf.Max(0, _currentHealth - damage);
            Debug.Log($"<color=magenta>[PLAYER HIT]</color> Player bị trừ {damage} máu! Còn: {_currentHealth}/{_maxHealth}");

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

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

            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            Debug.Log($"<color=green>[PLAYER HEAL]</color> Hồi phục {amount} máu! Hiện tại: {_currentHealth}/{_maxHealth}");

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        // Hồi máu theo % máu tối đa (ví dụ 0.3f = hồi 30% máu)
        public void HealPercent(float percent)
        {
            int healAmount = Mathf.RoundToInt(_maxHealth * Mathf.Clamp01(percent));
            Heal(healAmount);
        }

        // Hồi đầy 100% máu
        public void HealFull()
        {
            Heal(_maxHealth);
        }

        // Mở rộng máu tối đa (cho thẻ nâng cấp ở Pillar 3)
        public void IncreaseMaxHealth(int amount, bool healEquivalent = true)
        {
            if (amount <= 0) return;

            _maxHealth += amount;
            if (healEquivalent)
            {
                _currentHealth += amount;
            }

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
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

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project_D.Player;

namespace Project_D.UI
{
    public class HUDController : MonoBehaviour
    {
        [Header("Health Bar References")]
        [SerializeField] private Image _hpFillImage;
        [SerializeField] private TextMeshProUGUI _hpTextTMP;
        [SerializeField] private Text _hpTextLegacy;
        [SerializeField] private bool _changeHpColorWithHealth = true;

        [Header("Experience Bar & Level References")]
        [SerializeField] private Image _expFillImage;
        [SerializeField] private TextMeshProUGUI _levelTextTMP;
        [SerializeField] private Text _levelTextLegacy;
        [SerializeField] private TextMeshProUGUI _expTextTMP;

        [Header("Survival Timer References")]
        [SerializeField] private TextMeshProUGUI _timerTextTMP;
        [SerializeField] private Text _timerTextLegacy;

        private float _elapsedTime;
        private int _lastSeconds = -1;

        private void OnEnable()
        {
            // Đăng ký nhận sự kiện từ PlayerHealth và PlayerStats (Observer Pattern)
            PlayerHealth.OnHealthChanged += HandleHealthChanged;
            PlayerStats.OnExpChanged += HandleExpChanged;
        }

        private void OnDisable()
        {
            // BẮT BUỘC hủy đăng ký chống rò rỉ bộ nhớ (Memory Leak)
            PlayerHealth.OnHealthChanged -= HandleHealthChanged;
            PlayerStats.OnExpChanged -= HandleExpChanged;
        }

        private void Start()
        {
            // Khởi tạo trạng thái ban đầu nếu các Component đã có sẵn
            if (PlayerStats.Instance != null)
            {
                HandleExpChanged(PlayerStats.Instance.CurrentExp, PlayerStats.Instance.ExpToNextLevel, PlayerStats.Instance.CurrentLevel);
            }

            var playerHealth = FindAnyObjectByType<PlayerHealth>();
            if (playerHealth != null)
            {
                HandleHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }

            UpdateTimerText(0);
        }

        private void Update()
        {
            // Bộ đếm thời gian: Khi game tạm dừng (Time.timeScale = 0), Time.deltaTime = 0 -> Đồng hồ tự dừng!
            _elapsedTime += Time.deltaTime;
            int currentSeconds = (int)_elapsedTime;

            // Tối ưu Mobile: Chỉ cập nhật chuỗi text khi bước giây thay đổi (1 lần/giây, Zero GC Spike)
            if (currentSeconds != _lastSeconds)
            {
                _lastSeconds = currentSeconds;
                UpdateTimerText(currentSeconds);
            }
        }

        private void UpdateTimerText(int totalSeconds)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            string timeString = $"{minutes:00}:{seconds:00}";

            if (_timerTextTMP != null) _timerTextTMP.text = timeString;
            if (_timerTextLegacy != null) _timerTextLegacy.text = timeString;
        }

        // Cập nhật Thanh Máu
        private void HandleHealthChanged(int currentHealth, int maxHealth)
        {
            float fillRatio = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

            if (_hpFillImage != null)
            {
                _hpFillImage.fillAmount = fillRatio;

                // Tự động đổi màu thanh máu: Xanh lá (>50%) -> Vàng (25-50%) -> Đỏ (<25%)
                if (_changeHpColorWithHealth)
                {
                    if (fillRatio > 0.5f)
                        _hpFillImage.color = new Color(0.2f, 0.85f, 0.3f); // Xanh lá
                    else if (fillRatio > 0.25f)
                        _hpFillImage.color = new Color(0.95f, 0.8f, 0.1f); // Vàng cảnh báo
                    else
                        _hpFillImage.color = new Color(0.9f, 0.15f, 0.15f); // Đỏ nguy kịch
                }
            }

            string hpString = $"{currentHealth} / {maxHealth}";
            if (_hpTextTMP != null) _hpTextTMP.text = hpString;
            if (_hpTextLegacy != null) _hpTextLegacy.text = hpString;
        }

        // Cập nhật Thanh Exp và Cấp độ
        private void HandleExpChanged(int currentExp, int expToNextLevel, int currentLevel)
        {
            float fillRatio = expToNextLevel > 0 ? (float)currentExp / expToNextLevel : 0f;

            if (_expFillImage != null)
            {
                _expFillImage.fillAmount = fillRatio;
            }

            string levelString = $"Lv. {currentLevel}";
            if (_levelTextTMP != null) _levelTextTMP.text = levelString;
            if (_levelTextLegacy != null) _levelTextLegacy.text = levelString;

            if (_expTextTMP != null)
            {
                _expTextTMP.text = $"{currentExp} / {expToNextLevel}";
            }
        }
    }
}

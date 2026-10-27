using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Project_D.Player;

namespace Project_D.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [Header("UI Panel Reference")]
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Summary Text References")]
        [SerializeField] private TextMeshProUGUI _survivalTimeTextTMP;
        [SerializeField] private Text _survivalTimeTextLegacy;
        [SerializeField] private TextMeshProUGUI _levelReachedTextTMP;
        [SerializeField] private Text _levelReachedTextLegacy;

        [Header("Button References")]
        [SerializeField] private Button _restartButton;

        private void Awake()
        {
            if (_canvasGroup == null && _gameOverPanel != null)
            {
                _canvasGroup = _gameOverPanel.GetComponent<CanvasGroup>();
            }

            // Mặc định ẩn bảng Game Over lúc mới vào trận
            HidePanel();

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(OnRestartClicked);
                _restartButton.onClick.AddListener(OnRestartClicked);
            }
        }

        private void HidePanel()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
            else if (_gameOverPanel != null && _gameOverPanel != gameObject)
            {
                _gameOverPanel.SetActive(false);
            }
        }

        private void ShowPanel()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }
            else if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);
            }
        }

        private void OnEnable()
        {
            // Lắng nghe sự kiện người chơi hết máu từ PlayerHealth
            PlayerHealth.OnPlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            // BẮT BUỘC hủy đăng ký chống rò rỉ bộ nhớ
            PlayerHealth.OnPlayerDied -= HandlePlayerDied;
        }

        private void HandlePlayerDied()
        {
            ShowPanel();

            // 1. TẠM DỪNG THẾ GIỚI GAME
            Time.timeScale = 0f;

            // 2. TÍNH TOÁN THỜI GIAN SỐNG SÓT
            int totalSeconds = (int)Time.timeSinceLevelLoad;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            string timeString = $"Thời gian sống sót: {minutes:00}:{seconds:00}";

            if (_survivalTimeTextTMP != null) _survivalTimeTextTMP.text = timeString;
            if (_survivalTimeTextLegacy != null) _survivalTimeTextLegacy.text = timeString;

            // 3. TỔNG KẾT CẤP ĐỘ ĐẠT ĐƯỢC
            int finalLevel = PlayerStats.Instance != null ? PlayerStats.Instance.CurrentLevel : 1;
            string levelString = $"Cấp độ đạt được: Cấp {finalLevel}";

            if (_levelReachedTextTMP != null) _levelReachedTextTMP.text = levelString;
            if (_levelReachedTextLegacy != null) _levelReachedTextLegacy.text = levelString;

            Debug.Log($"<color=red><b>[GAME OVER POPUP]</b></color> {timeString} | {levelString}");
        }

        // Bấm nút "Chơi Lại"
        public void OnRestartClicked()
        {
            // BẮT BUỘC PHẢI KHÔI PHỤC timeScale = 1 TRƯỚC KHI LOAD LẠI SCENE!
            // (Nếu không khôi phục, màn chơi mới sẽ bị bất động vĩnh viễn)
            Time.timeScale = 1f;

            Debug.Log("<color=green>[GAME RESTART]</color> Đang tải lại trận đấu mới...");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}

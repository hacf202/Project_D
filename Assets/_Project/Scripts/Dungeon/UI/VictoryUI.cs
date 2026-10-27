using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Project_D.Player;
using Project_D.Dungeon.Run;

namespace Project_D.Dungeon.UI
{
    public class VictoryUI : MonoBehaviour
    {
        public static VictoryUI Instance { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Summary Info")]
        [SerializeField] private TMP_Text _timeSummaryText;
        [SerializeField] private TMP_Text _levelSummaryText;
        [SerializeField] private TMP_Text _soulsSummaryText;

        [Header("Buttons")]
        [SerializeField] private Button _restartRunButton;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_canvasGroup == null && _panel != null)
            {
                _canvasGroup = _panel.GetComponent<CanvasGroup>();
            }

            HidePanel();

            if (_restartRunButton != null)
            {
                _restartRunButton.onClick.AddListener(OnRestartRunClicked);
            }
        }

        private void OnEnable()
        {
            RunManager.OnRunCompleted += HandleRunCompleted;
        }

        private void OnDisable()
        {
            RunManager.OnRunCompleted -= HandleRunCompleted;
        }

        private void HandleRunCompleted()
        {
            ShowPanel();

            // Tạm dừng thế giới game để người chơi chiêm ngưỡng chiến thắng
            Time.timeScale = 0f;

            // 1. Thời gian hoàn thành
            int totalSeconds = (int)Time.timeSinceLevelLoad;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            if (_timeSummaryText != null)
            {
                _timeSummaryText.text = $"Thời Gian: {minutes:00}:{seconds:00}";
            }

            // 2. Cấp độ đạt được
            int level = PlayerStats.Instance != null ? PlayerStats.Instance.CurrentLevel : 1;
            if (_levelSummaryText != null)
            {
                _levelSummaryText.text = $"Cấp Độ: Cấp {level}";
            }

            // 3. Tổng số Souls
            int souls = RunManager.Instance != null ? RunManager.Instance.CurrentSouls : 0;
            if (_soulsSummaryText != null)
            {
                _soulsSummaryText.text = $"Linh Hồn Thu Thập: {souls} Souls";
            }

            Debug.Log("<color=gold><b>[VICTORY POPUP]</b></color> Hiển thị bảng vinh danh chiến thắng Run 13 tầng!");
        }

        private void OnRestartRunClicked()
        {
            // BẮT BUỘC khôi phục timeScale = 1 trước khi load lại Scene
            Time.timeScale = 1f;

            Debug.Log("<color=green>[NEW RUN]</color> Tải lại Scene để bắt đầu lượt chơi 13 tầng mới...");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void ShowPanel()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }
            else if (_panel != null)
            {
                _panel.SetActive(true);
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
            else if (_panel != null && _panel != gameObject)
            {
                _panel.SetActive(false);
            }
        }
    }
}

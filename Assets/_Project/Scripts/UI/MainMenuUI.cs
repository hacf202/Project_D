using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Project_D.Core;

namespace Project_D.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        // Hằng số PlayerPrefs đồng bộ 100% với SettingsPanelUI.cs
        private const string PREF_BGM_VOL = "Setting_BGMVol";
        private const string PREF_SFX_VOL = "Setting_SFXVol";
        private const string PREF_SCREEN_SHAKE = "Setting_ScreenShake";
        private const string PREF_DAMAGE_NUMBERS = "Setting_DamageNumbers";
        private const string PREF_HIGH_QUALITY = "Setting_HighQuality";

        [Header("Scene Navigation")]
        [Tooltip("Tên Scene màn chơi chính trong Build Settings")]
        [SerializeField] private string _gameplaySceneName = "SampleScene";

        [Header("Main Menu Buttons")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;

        [Header("Settings Modal Panel")]
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private CanvasGroup _settingsCanvasGroup;
        [SerializeField] private Button _closeSettingsButton;

        [Header("Settings Controls (Đồng bộ với In-Game)")]
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private TMP_Text _bgmValueText;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private TMP_Text _sfxValueText;
        [SerializeField] private Toggle _screenShakeToggle;
        [SerializeField] private Toggle _damageNumbersToggle;
        [SerializeField] private Toggle _highQualityToggle;

        private bool _isLoadingScene = false;

        private void Awake()
        {
            // Đảm bảo thời gian luôn chạy bình thường khi ở Main Menu
            if (PanelQueueManager.HasInstance)
            {
                PanelQueueManager.Instance.ForceResumeAll("MainMenuAwake");
            }
            else
            {
                Time.timeScale = 1f;
            }

            if (_settingsCanvasGroup == null && _settingsPanel != null)
            {
                _settingsCanvasGroup = _settingsPanel.GetComponent<CanvasGroup>();
            }

            SetSettingsModalVisible(false);
            LoadSavedSettings();
        }

        private void OnEnable()
        {
            BindListeners();
        }

        private void OnDisable()
        {
            UnbindListeners();
        }

        private void BindListeners()
        {
            if (_playButton != null) _playButton.onClick.AddListener(OnPlayClicked);
            if (_settingsButton != null) _settingsButton.onClick.AddListener(OnOpenSettingsClicked);
            if (_quitButton != null) _quitButton.onClick.AddListener(OnQuitClicked);
            if (_closeSettingsButton != null) _closeSettingsButton.onClick.AddListener(OnCloseSettingsClicked);

            if (_bgmSlider != null) _bgmSlider.onValueChanged.AddListener(OnBgmChanged);
            if (_sfxSlider != null) _sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            if (_screenShakeToggle != null) _screenShakeToggle.onValueChanged.AddListener(OnScreenShakeChanged);
            if (_damageNumbersToggle != null) _damageNumbersToggle.onValueChanged.AddListener(OnDamageNumbersChanged);
            if (_highQualityToggle != null) _highQualityToggle.onValueChanged.AddListener(OnHighQualityChanged);
        }

        private void UnbindListeners()
        {
            if (_playButton != null) _playButton.onClick.RemoveListener(OnPlayClicked);
            if (_settingsButton != null) _settingsButton.onClick.RemoveListener(OnOpenSettingsClicked);
            if (_quitButton != null) _quitButton.onClick.RemoveListener(OnQuitClicked);
            if (_closeSettingsButton != null) _closeSettingsButton.onClick.RemoveListener(OnCloseSettingsClicked);

            if (_bgmSlider != null) _bgmSlider.onValueChanged.RemoveListener(OnBgmChanged);
            if (_sfxSlider != null) _sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
            if (_screenShakeToggle != null) _screenShakeToggle.onValueChanged.RemoveListener(OnScreenShakeChanged);
            if (_damageNumbersToggle != null) _damageNumbersToggle.onValueChanged.RemoveListener(OnDamageNumbersChanged);
            if (_highQualityToggle != null) _highQualityToggle.onValueChanged.RemoveListener(OnHighQualityChanged);
        }

        // =====================================================================
        // 1. ĐIỀU HƯỚNG NÚT CHÍNH (PLAY / SETTINGS / QUIT)
        // =====================================================================

        private void OnPlayClicked()
        {
            // Chặn spam click nhiều lần gây load scene chồng chéo
            if (_isLoadingScene) return;

            if (string.IsNullOrWhiteSpace(_gameplaySceneName) || !Application.CanStreamedLevelBeLoaded(_gameplaySceneName))
            {
                Debug.LogError($"[MainMenuUI] Không thể tải Scene '{_gameplaySceneName}'. Hãy kiểm tra tên Scene trong File > Build Profiles!");
                return;
            }

            _isLoadingScene = true;
            if (_playButton != null) _playButton.interactable = false;

            PlayerPrefs.Save();
            SceneManager.LoadScene(_gameplaySceneName);
        }

        private void OnOpenSettingsClicked()
        {
            LoadSavedSettings();
            SetSettingsModalVisible(true);
        }

        private void OnCloseSettingsClicked()
        {
            PlayerPrefs.Save();
            SetSettingsModalVisible(false);
        }

        private void OnQuitClicked()
        {
            PlayerPrefs.Save();
            Debug.Log("[MainMenuUI] Thoát trò chơi.");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // =====================================================================
        // 2. XỬ LÝ LƯU & ĐỒNG BỘ SETTINGS QUA PLAYERPREFS
        // =====================================================================

        private void LoadSavedSettings()
        {
            float bgm = Mathf.Clamp01(PlayerPrefs.GetFloat(PREF_BGM_VOL, 0.8f));
            float sfx = Mathf.Clamp01(PlayerPrefs.GetFloat(PREF_SFX_VOL, 0.9f));
            bool shake = PlayerPrefs.GetInt(PREF_SCREEN_SHAKE, 1) == 1;
            bool dmgNum = PlayerPrefs.GetInt(PREF_DAMAGE_NUMBERS, 1) == 1;
            bool highQ = PlayerPrefs.GetInt(PREF_HIGH_QUALITY, 1) == 1;

            if (_bgmSlider != null) _bgmSlider.SetValueWithoutNotify(bgm);
            if (_sfxSlider != null) _sfxSlider.SetValueWithoutNotify(sfx);
            if (_screenShakeToggle != null) _screenShakeToggle.SetIsOnWithoutNotify(shake);
            if (_damageNumbersToggle != null) _damageNumbersToggle.SetIsOnWithoutNotify(dmgNum);
            if (_highQualityToggle != null) _highQualityToggle.SetIsOnWithoutNotify(highQ);

            UpdatePercentageText(_bgmValueText, bgm);
            UpdatePercentageText(_sfxValueText, sfx);
        }

        private void OnBgmChanged(float value)
        {
            float clamped = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PREF_BGM_VOL, clamped);
            UpdatePercentageText(_bgmValueText, clamped);
        }

        private void OnSfxChanged(float value)
        {
            float clamped = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PREF_SFX_VOL, clamped);
            UpdatePercentageText(_sfxValueText, clamped);
        }

        private void OnScreenShakeChanged(bool isOn)
        {
            PlayerPrefs.SetInt(PREF_SCREEN_SHAKE, isOn ? 1 : 0);
        }

        private void OnDamageNumbersChanged(bool isOn)
        {
            PlayerPrefs.SetInt(PREF_DAMAGE_NUMBERS, isOn ? 1 : 0);
        }

        private void OnHighQualityChanged(bool isOn)
        {
            PlayerPrefs.SetInt(PREF_HIGH_QUALITY, isOn ? 1 : 0);
        }

        private void UpdatePercentageText(TMP_Text targetText, float normalizedValue)
        {
            if (targetText != null)
            {
                targetText.text = $"{Mathf.RoundToInt(normalizedValue * 100f)}%";
            }
        }

        private void SetSettingsModalVisible(bool visible)
        {
            if (_settingsCanvasGroup != null)
            {
                _settingsCanvasGroup.alpha = visible ? 1f : 0f;
                _settingsCanvasGroup.interactable = visible;
                _settingsCanvasGroup.blocksRaycasts = visible;
            }
            else if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(visible);
            }
        }
    }
}
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project_D.Player;
using Project_D.Dungeon.Run;

namespace Project_D.Dungeon.UI
{
    public class RestSiteUI : MonoBehaviour
    {
        public static RestSiteUI Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Buttons")]
        [SerializeField] private Button _freeRestButton;     // Hồi 30% Máu miễn phí
        [SerializeField] private Button _fullHealSoulsButton; // Dùng Souls hồi 100% Máu
        [SerializeField] private Button _closeButton;

        [Header("Texts")]
        [SerializeField] private TMP_Text _soulsCostText;
        [SerializeField] private TMP_Text _playerHealthText;

        [Header("Cost Configuration")]
        [SerializeField] private int _fullHealSoulCost = 50;

        private Action _onFinishedCallback;

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

            if (_freeRestButton != null) _freeRestButton.onClick.AddListener(OnFreeRestClicked);
            if (_fullHealSoulsButton != null) _fullHealSoulsButton.onClick.AddListener(OnFullHealClicked);
            if (_closeButton != null) _closeButton.onClick.AddListener(Close);
        }

        public void OpenRestSite(Action onFinished = null)
        {
            _onFinishedCallback = onFinished;
            ShowPanel();
            UpdateUI();
        }

        private void UpdateUI()
        {
            var health = FindAnyObjectByType<PlayerHealth>();
            if (health != null && _playerHealthText != null)
            {
                _playerHealthText.text = $"Máu hiện tại: {health.CurrentHealth} / {health.MaxHealth}";
            }

            if (_soulsCostText != null)
            {
                _soulsCostText.text = $"Hồi Đầy Máu (Tốn {_fullHealSoulCost} Souls)";
            }

            if (_fullHealSoulsButton != null && RunManager.Instance != null)
            {
                // Chỉ cho phép bấm hồi đầy máu nếu có đủ Souls
                _fullHealSoulsButton.interactable = RunManager.Instance.CurrentSouls >= _fullHealSoulCost;
            }
        }

        private void OnFreeRestClicked()
        {
            var health = FindAnyObjectByType<PlayerHealth>();
            if (health != null)
            {
                health.HealPercent(0.3f);
                Debug.Log("<color=green>[REST SITE]</color> Nghỉ ngơi miễn phí: Hồi phục 30% máu tối đa!");
            }

            FinishAndClose();
        }

        private void OnFullHealClicked()
        {
            if (RunManager.Instance != null && RunManager.Instance.SpendSouls(_fullHealSoulCost))
            {
                var health = FindAnyObjectByType<PlayerHealth>();
                if (health != null)
                {
                    health.HealFull();
                    Debug.Log("<color=green>[REST SITE]</color> Dùng Souls hồi phục 100% sinh lực!");
                }
                FinishAndClose();
            }
        }

        public void Close()
        {
            HidePanel();
        }

        private void FinishAndClose()
        {
            HidePanel();
            _onFinishedCallback?.Invoke();
            _onFinishedCallback = null;
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

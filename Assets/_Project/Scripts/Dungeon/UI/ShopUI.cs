using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project_D.Player;
using Project_D.Dungeon.Run;

namespace Project_D.Dungeon.UI
{
    public class ShopUI : MonoBehaviour
    {
        public static ShopUI Instance { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Header Info")]
        [SerializeField] private TMP_Text _soulsBalanceText;
        [SerializeField] private Button _leaveShopButton;

        [Header("Item 1: Bình Máu (+40 HP)")]
        [SerializeField] private Button _healPotionButton;
        [SerializeField] private int _healCost = 20;

        [Header("Item 2: Tăng Sát Thương (+5 Dmg)")]
        [SerializeField] private Button _damageButton;
        [SerializeField] private int _damageCost = 45;

        [Header("Item 3: Tăng Tốc Độ Bắn (+20%)")]
        [SerializeField] private Button _fireRateButton;
        [SerializeField] private int _fireRateCost = 35;

        [Header("Item 4: Tăng Đạn Chùm (+1 Đạn)")]
        [SerializeField] private Button _multiShotButton;
        [SerializeField] private int _multiShotCost = 60;

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

            if (_leaveShopButton != null) _leaveShopButton.onClick.AddListener(CloseShop);
            if (_healPotionButton != null) _healPotionButton.onClick.AddListener(BuyHealPotion);
            if (_damageButton != null) _damageButton.onClick.AddListener(BuyDamageUpgrade);
            if (_fireRateButton != null) _fireRateButton.onClick.AddListener(BuyFireRateUpgrade);
            if (_multiShotButton != null) _multiShotButton.onClick.AddListener(BuyMultiShotUpgrade);
        }

        private void OnEnable()
        {
            RunManager.OnSoulsChanged += HandleSoulsChanged;
        }

        private void OnDisable()
        {
            RunManager.OnSoulsChanged -= HandleSoulsChanged;
        }

        public void OpenShop(Action onFinished = null)
        {
            _onFinishedCallback = onFinished;
            ShowPanel();
            UpdateBalanceUI();
        }

        private void HandleSoulsChanged(int newSouls)
        {
            UpdateBalanceUI();
        }

        private void UpdateBalanceUI()
        {
            int currentSouls = RunManager.Instance != null ? RunManager.Instance.CurrentSouls : 0;
            if (_soulsBalanceText != null)
            {
                _soulsBalanceText.text = $"Linh Hồn: {currentSouls} Souls";
            }

            // Cập nhật trạng thái bấm của các nút tùy thuộc vào số Souls
            if (_healPotionButton != null) _healPotionButton.interactable = currentSouls >= _healCost;
            if (_damageButton != null) _damageButton.interactable = currentSouls >= _damageCost;
            if (_fireRateButton != null) _fireRateButton.interactable = currentSouls >= _fireRateCost;
            if (_multiShotButton != null) _multiShotButton.interactable = currentSouls >= _multiShotCost;
        }

        private void BuyHealPotion()
        {
            if (RunManager.Instance != null && RunManager.Instance.SpendSouls(_healCost))
            {
                var health = FindAnyObjectByType<PlayerHealth>();
                if (health != null)
                {
                    health.Heal(40);
                }
                Debug.Log("<color=green>[SHOP PURCHASE]</color> Đã mua Bình Máu (+40 HP)!");
            }
        }

        private void BuyDamageUpgrade()
        {
            if (RunManager.Instance != null && RunManager.Instance.SpendSouls(_damageCost))
            {
                PlayerStats.Instance?.AddDamageBonus(5);
                Debug.Log("<color=green>[SHOP PURCHASE]</color> Đã mua Tăng Sát Thương (+5 Damage)!");
            }
        }

        private void BuyFireRateUpgrade()
        {
            if (RunManager.Instance != null && RunManager.Instance.SpendSouls(_fireRateCost))
            {
                PlayerStats.Instance?.AddFireRateBonus(0.2f);
                Debug.Log("<color=green>[SHOP PURCHASE]</color> Đã mua Tăng Tốc Độ Bắn (+20%)!");
            }
        }

        private void BuyMultiShotUpgrade()
        {
            if (RunManager.Instance != null && RunManager.Instance.SpendSouls(_multiShotCost))
            {
                PlayerStats.Instance?.AddProjectilesBonus(1);
                Debug.Log("<color=green>[SHOP PURCHASE]</color> Đã mua Tăng Đạn Chùm (+1 Viên)! Bắn thêm 1 tia.");
            }
        }

        public void CloseShop()
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

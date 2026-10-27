using System.Collections.Generic;
using UnityEngine;
using Project_D.Player;
using Project_D.UI;

namespace Project_D.Upgrades
{
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        [Header("UI Configuration")]
        [SerializeField] private GameObject _levelUpPanel;          // Bảng UI tổng (nền mờ)
        [SerializeField] private Transform _cardsContainer;         // Vùng chứa các thẻ (có gắn HorizontalLayoutGroup)
        [SerializeField] private GameObject _cardPrefab;            // Khuôn Prefab của 1 tấm thẻ (UpgradeCard.prefab)
        [SerializeField] private int _cardsToDisplay = 3;           // Số thẻ hiển thị (dễ dàng chỉnh thành 4, 5 thẻ)

        [Header("Upgrade Database")]
        [SerializeField] private List<UpgradeDataSO> _availableUpgrades = new List<UpgradeDataSO>();

        // Mini UI Pool: Tái sử dụng các thẻ để đạt Zero GC và triệt tiêu 100% lỗi Missing Reference
        private readonly List<UpgradeCardUI> _cardPool = new List<UpgradeCardUI>();
        private int _pendingLevelUps = 0;
        private bool _isPanelOpen = false;

        public bool IsPanelOpen => _isPanelOpen;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Nếu chưa gán Container thì lấy chính Transform của LevelUpPanel
            if (_cardsContainer == null && _levelUpPanel != null)
            {
                _cardsContainer = _levelUpPanel.transform;
            }

            // Khởi tạo sẵn Mini Pool các thẻ bài lúc khởi động game
            InitializeCardPool();

            if (_levelUpPanel != null)
            {
                _levelUpPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            PlayerStats.OnLevelUp += HandleLevelUp;
        }

        private void OnDisable()
        {
            PlayerStats.OnLevelUp -= HandleLevelUp;
        }

        // Tự động gom hoặc sinh sẵn các thẻ bài vào Mini Pool
        private void InitializeCardPool()
        {
            _cardPool.Clear();

            // 1. Tương thích ngược: Nếu trong Container đã có sẵn các thẻ cũ trong Scene, gom lại dùng luôn!
            if (_cardsContainer != null)
            {
                var existingCards = _cardsContainer.GetComponentsInChildren<UpgradeCardUI>(true);
                foreach (var card in existingCards)
                {
                    if (card != null && !_cardPool.Contains(card))
                    {
                        card.gameObject.SetActive(false);
                        _cardPool.Add(card);
                    }
                }
            }

            // 2. Nếu thiếu thẻ và có Prefab: Tự động sinh thêm cho đủ số lượng _cardsToDisplay
            EnsurePoolSize(_cardsToDisplay);
        }

        private void EnsurePoolSize(int requiredCount)
        {
            if (_cardsContainer == null || _cardPrefab == null) return;

            while (_cardPool.Count < requiredCount)
            {
                GameObject cardObj = Instantiate(_cardPrefab, _cardsContainer);
                cardObj.name = $"Card_Dynamic_{_cardPool.Count + 1}";
                cardObj.SetActive(false);

                if (cardObj.TryGetComponent<UpgradeCardUI>(out var cardUI))
                {
                    _cardPool.Add(cardUI);
                }
            }
        }

        private void HandleLevelUp(int newLevel)
        {
            _pendingLevelUps++;

            if (!_isPanelOpen)
            {
                ShowNextLevelUp();
            }
        }

        private void ShowNextLevelUp()
        {
            if (_availableUpgrades == null || _availableUpgrades.Count == 0)
            {
                Debug.LogWarning("<color=orange>[UPGRADE MANAGER]</color> Danh sách _availableUpgrades đang trống! Vui lòng gán các thẻ nâng cấp vào Inspector.");
                _pendingLevelUps = 0;
                return;
            }

            // Đảm bảo số lượng thẻ trong Pool đủ đáp ứng
            EnsurePoolSize(_cardsToDisplay);

            if (_cardPool.Count == 0)
            {
                Debug.LogError("<color=red>[UPGRADE MANAGER]</color> Không thể hiển thị thẻ: Chưa có Card nào trong Pool và chưa gán _cardPrefab!");
                ClosePanel();
                return;
            }

            _isPanelOpen = true;

            // 1. TẠM DỪNG THỜI GIAN THẾ GIỚI GAME (Pause)
            Time.timeScale = 0f;

            // 2. BẬT BẢNG UI
            if (_levelUpPanel != null)
            {
                _levelUpPanel.SetActive(true);
            }

            // 3. BỐC NGẪU NHIÊN CÁC THẺ KHÔNG TRÙNG NHAU TỪ KHO
            int countToPick = Mathf.Min(_cardsToDisplay, _cardPool.Count);
            List<UpgradeDataSO> chosenUpgrades = PickRandomDistinctUpgrades(countToPick);

            // 4. HIỂN THỊ DỮ LIỆU LÊN TỪNG THẺ TRONG POOL
            for (int i = 0; i < _cardPool.Count; i++)
            {
                if (_cardPool[i] == null) continue;

                if (i < chosenUpgrades.Count && chosenUpgrades[i] != null)
                {
                    _cardPool[i].gameObject.SetActive(true);
                    _cardPool[i].Setup(chosenUpgrades[i], OnUpgradeSelected);
                }
                else
                {
                    _cardPool[i].gameObject.SetActive(false);
                }
            }
        }

        // Người chơi chạm chọn 1 thẻ bài
        private void OnUpgradeSelected(UpgradeDataSO selectedUpgrade)
        {
            if (selectedUpgrade == null) return;

            // 1. Áp dụng hiệu ứng thẻ bài lên nhân vật
            selectedUpgrade.ApplyUpgrade();

            // 2. Trừ bớt 1 cấp trong hàng đợi
            _pendingLevelUps = Mathf.Max(0, _pendingLevelUps - 1);

            // 3. Hỗ trợ Multi-Level Queue: Nếu còn cấp chờ thì mở tiếp lượt bốc mới
            if (_pendingLevelUps > 0)
            {
                Debug.Log($"<color=yellow>[UPGRADE]</color> Vẫn còn {_pendingLevelUps} lần chọn thẻ! Mở tiếp lượt bốc tiếp theo.");
                ShowNextLevelUp();
            }
            else
            {
                // Hết cấp chờ -> Tắt bảng và TIẾP TỤC TRẬN ĐẤU
                ClosePanel();
            }
        }

        private void ClosePanel()
        {
            // Tắt toàn bộ các thẻ đang mở trong Pool
            foreach (var card in _cardPool)
            {
                if (card != null)
                {
                    card.gameObject.SetActive(false);
                }
            }

            if (_levelUpPanel != null)
            {
                _levelUpPanel.SetActive(false);
            }

            _isPanelOpen = false;

            // KHÔI PHỤC THỜI GIAN GAME (Resume)
            Time.timeScale = 1f;
        }

        // Thuật toán bốc ngẫu nhiên không trùng lặp và loại bỏ phần tử null
        private List<UpgradeDataSO> PickRandomDistinctUpgrades(int count)
        {
            var pool = new List<UpgradeDataSO>();
            foreach (var item in _availableUpgrades)
            {
                if (item != null) pool.Add(item);
            }

            var result = new List<UpgradeDataSO>();
            int targetCount = Mathf.Min(count, pool.Count);

            for (int i = 0; i < targetCount; i++)
            {
                int randomIndex = Random.Range(0, pool.Count);
                result.Add(pool[randomIndex]);
                pool.RemoveAt(randomIndex);
            }

            return result;
        }
    }
}

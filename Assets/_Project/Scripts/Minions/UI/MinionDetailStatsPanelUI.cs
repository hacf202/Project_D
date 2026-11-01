using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project_D.Core;

namespace Project_D.Minions.UI
{
    public class MinionDetailStatsPanelUI : MonoBehaviour
    {
        public static MinionDetailStatsPanelUI Instance { get; private set; }

        [Header("Panel Structure References")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _backdropButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _bottomCloseButton;

        [Header("Header Elements")]
        [SerializeField] private Image _tierHeaderBar;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _iconInitialFallback;
        [SerializeField] private TMP_Text _minionNameText;
        [SerializeField] private TMP_Text _tierTagText;
        [SerializeField] private TMP_Text _classTagText;
        [SerializeField] private TMP_Text _rosterSummaryText;

        [Header("Runtime Instance Status")]
        [SerializeField] private GameObject _instanceTabsContainer;
        [SerializeField] private TMP_Text _currentInstanceTitleText;
        [SerializeField] private Image _hpBarFill;
        [SerializeField] private TMP_Text _hpText;
        [SerializeField] private TMP_Text _stateBadgeText;
        [SerializeField] private Image _stateBadgeBg;

        [Header("Combat Stats Elements")]
        [SerializeField] private TMP_Text _damageValueText;
        [SerializeField] private TMP_Text _attackSpeedValueText;
        [SerializeField] private TMP_Text _attackRangeValueText;
        [SerializeField] private TMP_Text _moveSpeedValueText;
        [SerializeField] private TMP_Text _attackMethodValueText;
        [SerializeField] private TMP_Text _knockbackValueText;
        [SerializeField] private TMP_Text _respawnValueText;

        [Header("Description & Lore")]
        [SerializeField] private TMP_Text _descriptionText;

        [Header("Settings")]
        [SerializeField] private bool _pauseTimeWhenOpen = true;

        private MinionDataSO _currentData;
        private List<MinionController> _currentMinionInstances = new List<MinionController>();
        private int _selectedInstanceIndex = 0;
        private bool _isOpen = false;
        private float _lastUpdateTimer = 0f;

        // Cache trạng thái để tối ưu hóa Zero GC
        private int _lastCachedHp = -1;
        private MinionState _lastCachedState = (MinionState)(-1);
        private float _lastCachedRespawn = -1f;

        public bool IsOpen => _isOpen;
        public MinionDataSO CurrentData => _currentData;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            AutoFindReferences();
            SetupButtonListeners();
            HidePanel();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static void OpenForMinion(MinionDataSO data)
        {
            if (Instance == null)
            {
                Instance = UnityEngine.Object.FindAnyObjectByType<MinionDetailStatsPanelUI>(FindObjectsInactive.Include);
                if (Instance == null)
                {
                    var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
                    if (canvas != null)
                    {
                        Instance = CreateDynamicDetailPanel(canvas.transform);
                    }
                }
            }

            if (Instance != null)
            {
                Instance.Open(data);
            }
        }

        public void AutoFindReferences()
        {
            if (_panelRoot == null) _panelRoot = gameObject;
            if (_canvasGroup == null)
            {
                _canvasGroup = _panelRoot.GetComponent<CanvasGroup>();
            }

            if (_backdropButton == null)
            {
                var backdrop = transform.Find("Backdrop");
                if (backdrop != null) _backdropButton = backdrop.GetComponent<Button>();
            }

            if (_closeButton == null)
            {
                var closeObj = transform.Find("ModalWindow/Header/CloseButton");
                if (closeObj == null) closeObj = transform.Find("ModalWindow/CloseButton");
                if (closeObj != null) _closeButton = closeObj.GetComponent<Button>();
            }

            if (_bottomCloseButton == null)
            {
                var bCloseObj = transform.Find("ModalWindow/BottomCloseButton");
                if (bCloseObj != null) _bottomCloseButton = bCloseObj.GetComponent<Button>();
            }

            if (_tierHeaderBar == null)
            {
                var tb = transform.Find("ModalWindow/TierHeaderBar");
                if (tb != null) _tierHeaderBar = tb.GetComponent<Image>();
            }

            if (_iconImage == null)
            {
                var iconObj = transform.Find("ModalWindow/Header/AvatarContainer/Icon");
                if (iconObj != null) _iconImage = iconObj.GetComponent<Image>();
            }

            if (_iconInitialFallback == null && _iconImage != null)
            {
                _iconInitialFallback = _iconImage.GetComponentInChildren<TMP_Text>(true);
            }

            if (_minionNameText == null)
            {
                var nameObj = transform.Find("ModalWindow/Header/TitleContainer/MinionName");
                if (nameObj != null) _minionNameText = nameObj.GetComponent<TMP_Text>();
            }

            if (_tierTagText == null)
            {
                var tierObj = transform.Find("ModalWindow/Header/TitleContainer/TagsContainer/TierTag");
                if (tierObj != null) _tierTagText = tierObj.GetComponent<TMP_Text>();
            }

            if (_classTagText == null)
            {
                var classObj = transform.Find("ModalWindow/Header/TitleContainer/TagsContainer/ClassTag");
                if (classObj != null) _classTagText = classObj.GetComponent<TMP_Text>();
            }

            if (_rosterSummaryText == null)
            {
                var rosterObj = transform.Find("ModalWindow/Header/TitleContainer/RosterSummaryText");
                if (rosterObj != null) _rosterSummaryText = rosterObj.GetComponent<TMP_Text>();
            }

            if (_instanceTabsContainer == null)
            {
                var tabsObj = transform.Find("ModalWindow/RuntimeStatus/InstanceTabsContainer");
                if (tabsObj != null) _instanceTabsContainer = tabsObj.gameObject;
            }

            if (_hpBarFill == null)
            {
                var hpFill = transform.Find("ModalWindow/RuntimeStatus/HPContainer/HPBar/Fill");
                if (hpFill != null) _hpBarFill = hpFill.GetComponent<Image>();
            }

            if (_hpText == null)
            {
                var hpTxt = transform.Find("ModalWindow/RuntimeStatus/HPContainer/HPText");
                if (hpTxt != null) _hpText = hpTxt.GetComponent<TMP_Text>();
            }

            if (_stateBadgeText == null)
            {
                var stateTxt = transform.Find("ModalWindow/RuntimeStatus/Badge/StateText");
                if (stateTxt != null) _stateBadgeText = stateTxt.GetComponent<TMP_Text>();
            }

            if (_stateBadgeBg == null)
            {
                var stateBg = transform.Find("ModalWindow/RuntimeStatus/Badge");
                if (stateBg != null) _stateBadgeBg = stateBg.GetComponent<Image>();
            }

            if (_descriptionText == null)
            {
                var descObj = transform.Find("ModalWindow/DescriptionBox/DescriptionText");
                if (descObj != null) _descriptionText = descObj.GetComponent<TMP_Text>();
            }
        }

        private void SetupButtonListeners()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(Close);
                _closeButton.onClick.AddListener(Close);
            }

            if (_bottomCloseButton != null)
            {
                _bottomCloseButton.onClick.RemoveListener(Close);
                _bottomCloseButton.onClick.AddListener(Close);
            }

            if (_backdropButton != null)
            {
                _backdropButton.onClick.RemoveListener(Close);
                _backdropButton.onClick.AddListener(Close);
            }
        }

        public void Open(MinionDataSO data, int targetIndex = 0)
        {
            if (data == null) return;

            AutoFindReferences();
            SetupButtonListeners();

            _currentData = data;
            _selectedInstanceIndex = targetIndex;
            _lastCachedHp = -1;
            _lastCachedState = (MinionState)(-1);
            _lastCachedRespawn = -1f;

            QueryCurrentMinionInstances();
            PopulateStaticData();
            UpdateRuntimeInstanceStatus(true);
            BuildInstanceTabs();

            ShowPanel();

            if (_pauseTimeWhenOpen && PanelQueueManager.Instance != null)
            {
                PanelQueueManager.Instance.RequestPause(this, "MinionDetailStatsPanel");
            }
        }

        public void Close()
        {
            if (!_isOpen) return;

            HidePanel();

            if (_pauseTimeWhenOpen && PanelQueueManager.Instance != null)
            {
                PanelQueueManager.Instance.ReleasePause(this);
            }
        }

        private void ShowPanel()
        {
            _isOpen = true;
            if (_panelRoot != null && !_panelRoot.activeSelf)
            {
                _panelRoot.SetActive(true);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }
        }

        private void HidePanel()
        {
            _isOpen = false;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        private void Update()
        {
            if (!_isOpen || _currentData == null) return;

            _lastUpdateTimer += Time.unscaledDeltaTime;
            if (_lastUpdateTimer >= 0.1f)
            {
                _lastUpdateTimer = 0f;
                UpdateRuntimeInstanceStatus(false);
            }
        }

        private void QueryCurrentMinionInstances()
        {
            _currentMinionInstances.Clear();
            if (MinionManager.Instance != null && _currentData != null)
            {
                var minions = MinionManager.Instance.GetMinionsByData(_currentData);
                if (minions != null)
                {
                    _currentMinionInstances.AddRange(minions);
                }
            }

            if (_selectedInstanceIndex >= _currentMinionInstances.Count)
            {
                _selectedInstanceIndex = Mathf.Max(0, _currentMinionInstances.Count - 1);
            }
        }

        // =========================================================================
        // NẠP THÔNG TIN CỐ ĐỊNH & THÔNG SỐ CƠ BẢN
        // =========================================================================

        private void PopulateStaticData()
        {
            if (_currentData == null) return;

            // 1. Tier Bar
            if (_tierHeaderBar != null)
            {
                _tierHeaderBar.color = GetTierColor(_currentData.Tier);
            }

            // 2. Icon & Avatar
            if (_iconImage != null)
            {
                Sprite targetSprite = ResolveSprite(_currentData);
                if (targetSprite != null)
                {
                    _iconImage.sprite = targetSprite;
                    _iconImage.preserveAspect = true;
                    _iconImage.color = Color.white;
                    if (_iconInitialFallback != null) _iconInitialFallback.gameObject.SetActive(false);
                }
                else
                {
                    _iconImage.sprite = null;
                    _iconImage.color = GetClassColor(_currentData.Class);
                    if (_iconInitialFallback != null)
                    {
                        _iconInitialFallback.gameObject.SetActive(true);
                        _iconInitialFallback.text = !string.IsNullOrEmpty(_currentData.MinionName) ? _currentData.MinionName.Substring(0, 1) : "L";
                    }
                }
            }

            // 3. Name & Badges
            if (_minionNameText != null)
            {
                _minionNameText.text = $"<b>{_currentData.MinionName}</b>";
            }

            if (_tierTagText != null)
            {
                _tierTagText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(GetTierColor(_currentData.Tier))}><b>{GetTierDisplayName(_currentData.Tier)}</b></color>";
            }

            if (_classTagText != null)
            {
                _classTagText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(GetClassColor(_currentData.Class))}><b>{GetClassDisplayName(_currentData.Class)}</b></color>";
            }

            // 4. Combat Stats (Hiển thị chỉ số thực tế sau khi tính Team Buffs)
            int effectiveDmg = _currentData.AttackDamage;
            float effectiveAtkSpd = _currentData.AttackSpeed;
            float effectiveRespawn = _currentData.RespawnDuration;

            if (MinionManager.Instance != null)
            {
                effectiveDmg = Mathf.Max(1, Mathf.RoundToInt(_currentData.AttackDamage * MinionManager.Instance.MinionDamageMultiplier));
                effectiveAtkSpd = Mathf.Max(0.1f, _currentData.AttackSpeed * MinionManager.Instance.MinionAttackSpeedMultiplier);
                effectiveRespawn = Mathf.Max(1.0f, _currentData.RespawnDuration * MinionManager.Instance.MinionRespawnTimeMultiplier);
            }

            if (_damageValueText != null)
            {
                string bonus = (MinionManager.Instance != null && MinionManager.Instance.MinionDamageMultiplier > 1f)
                    ? $" <color=#00FF88>(+{Mathf.RoundToInt((MinionManager.Instance.MinionDamageMultiplier - 1f) * 100)}%)</color>"
                    : "";
                _damageValueText.text = $"<b>{effectiveDmg}</b>{bonus} <size=80%><color=#A0AAB8>sát thương</color></size>";
            }

            if (_attackSpeedValueText != null)
            {
                float interval = effectiveAtkSpd > 0f ? (1f / effectiveAtkSpd) : 0f;
                string bonus = (MinionManager.Instance != null && MinionManager.Instance.MinionAttackSpeedMultiplier > 1f)
                    ? $" <color=#00FF88>(+{Mathf.RoundToInt((MinionManager.Instance.MinionAttackSpeedMultiplier - 1f) * 100)}%)</color>"
                    : "";
                _attackSpeedValueText.text = $"<b>{effectiveAtkSpd:0.#}</b>{bonus} <size=80%><color=#A0AAB8>đòn/s ({interval:0.##}s/đòn)</color></size>";
            }

            if (_attackRangeValueText != null)
            {
                _attackRangeValueText.text = $"<b>{_currentData.AttackRange:0.0}m</b> <size=80%><color=#A0AAB8>tầm đánh</color></size>";
            }

            if (_moveSpeedValueText != null)
            {
                _moveSpeedValueText.text = $"<b>{_currentData.MoveSpeed:0.0}</b> <size=80%><color=#A0AAB8>m/s</color></size>";
            }

            if (_attackMethodValueText != null)
            {
                _attackMethodValueText.text = GetAttackMethodDescription(_currentData);
            }

            if (_knockbackValueText != null)
            {
                _knockbackValueText.text = $"<b>{_currentData.KnockbackForce:0.#}</b> <size=80%><color=#A0AAB8>lực đẩy lùi</color></size>";
            }

            if (_respawnValueText != null)
            {
                string buffText = (MinionManager.Instance != null && MinionManager.Instance.MinionRespawnTimeMultiplier < 1f)
                    ? $" <color=#00FF88>(-{Mathf.RoundToInt((1f - MinionManager.Instance.MinionRespawnTimeMultiplier) * 100)}%)</color>"
                    : "";
                _respawnValueText.text = $"<b>{effectiveRespawn:0.#}s</b>{buffText} <size=80%><color=#A0AAB8>khi tử trận</color></size>";
            }

            // 5. Description
            if (_descriptionText != null)
            {
                _descriptionText.text = !string.IsNullOrEmpty(_currentData.Description)
                    ? _currentData.Description
                    : "Lính đồng minh sát cánh chiến đấu và bảo vệ người chơi.";
            }
        }

        // =========================================================================
        // CẬP NHẬT TRẠNG THÁI HIỆN TẠI (RUNTIME LIVE STATS)
        // =========================================================================

        private void UpdateRuntimeInstanceStatus(bool forceRefresh)
        {
            QueryCurrentMinionInstances();

            int total = _currentMinionInstances.Count;
            int alive = 0;
            for (int i = 0; i < total; i++)
            {
                if (_currentMinionInstances[i] != null && _currentMinionInstances[i].IsAlive)
                {
                    alive++;
                }
            }

            if (_rosterSummaryText != null)
            {
                string countColorHex = alive > 0 ? "#00FF66" : "#FF5555";
                _rosterSummaryText.text = $"Đang có: <color={countColorHex}><b>{alive}/{total}</b></color> con trên sân";
            }

            if (total == 0)
            {
                if (_currentInstanceTitleText != null) _currentInstanceTitleText.text = "Không có cá thể lính nào trên sân";
                if (_hpBarFill != null) _hpBarFill.fillAmount = 0f;
                if (_hpText != null) _hpText.text = $"Sinh Lực: 0 / {_currentData.MaxHealth}";
                if (_stateBadgeText != null) _stateBadgeText.text = "CHƯA TRIỆU HỒI";
                if (_stateBadgeBg != null) _stateBadgeBg.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);
                return;
            }

            if (_selectedInstanceIndex >= total)
            {
                _selectedInstanceIndex = total - 1;
            }

            var currentMinion = _currentMinionInstances[_selectedInstanceIndex];
            if (currentMinion == null) return;

            int curHp = currentMinion.CurrentHealth;
            int maxHp = currentMinion.MaxHealth;
            MinionState state = currentMinion.State;
            float remainingRespawn = (MinionManager.Instance != null && !currentMinion.IsAlive)
                ? MinionManager.Instance.GetRemainingRespawnTime(currentMinion)
                : 0f;

            bool changed = forceRefresh || (curHp != _lastCachedHp) || (state != _lastCachedState) || (Mathf.Abs(remainingRespawn - _lastCachedRespawn) > 0.2f);
            if (!changed) return;

            _lastCachedHp = curHp;
            _lastCachedState = state;
            _lastCachedRespawn = remainingRespawn;

            // 1. Tiêu đề cá thể
            if (_currentInstanceTitleText != null)
            {
                _currentInstanceTitleText.text = $"Cá Thể #{_selectedInstanceIndex + 1} / {total}";
            }

            // 2. Máu hiện tại
            float hpPercent = maxHp > 0 ? Mathf.Clamp01((float)curHp / maxHp) : 0f;
            if (_hpBarFill != null)
            {
                _hpBarFill.fillAmount = hpPercent;
                _hpBarFill.color = hpPercent > 0.5f
                    ? Color.Lerp(new Color(1f, 0.8f, 0.2f), new Color(0.2f, 0.9f, 0.3f), (hpPercent - 0.5f) * 2f)
                    : Color.Lerp(new Color(0.9f, 0.25f, 0.25f), new Color(1f, 0.8f, 0.2f), hpPercent * 2f);
            }

            if (_hpText != null)
            {
                _hpText.text = $"<b>{Mathf.Max(0, curHp)}</b> / {maxHp} HP <size=80%><color=#B0BEC5>({Mathf.RoundToInt(hpPercent * 100)}%)</color></size>";
            }

            // 3. Trạng thái hành vi (Dùng text thuần không chứa emoji Unicode để font hiển thị chuẩn)
            if (_stateBadgeText != null && _stateBadgeBg != null)
            {
                if (!currentMinion.IsAlive)
                {
                    _stateBadgeText.text = remainingRespawn > 0f
                        ? $"HỒI SINH ({remainingRespawn:0.0}s)"
                        : "ĐÃ TỬ TRẬN";
                    _stateBadgeBg.color = new Color(0.6f, 0.15f, 0.15f, 0.95f);
                }
                else if (state == MinionState.Combat)
                {
                    _stateBadgeText.text = "CHIẾN ĐẤU";
                    _stateBadgeBg.color = new Color(0.85f, 0.45f, 0.15f, 0.95f);
                }
                else
                {
                    _stateBadgeText.text = "THEO SAU";
                    _stateBadgeBg.color = new Color(0.18f, 0.65f, 0.35f, 0.95f);
                }
            }
        }

        // =========================================================================
        // DỰNG CÁC TAB CHỌN CÁ THỂ LÍNH
        // =========================================================================

        private void BuildInstanceTabs()
        {
            if (_instanceTabsContainer == null) return;

            int count = _currentMinionInstances.Count;
            for (int i = _instanceTabsContainer.transform.childCount - 1; i >= 0; i--)
            {
                var child = _instanceTabsContainer.transform.GetChild(i);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }

            if (count <= 1)
            {
                _instanceTabsContainer.SetActive(false);
                return;
            }

            _instanceTabsContainer.SetActive(true);

            for (int i = 0; i < count; i++)
            {
                int index = i;
                var tabObj = new GameObject($"Tab_{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
                tabObj.transform.SetParent(_instanceTabsContainer.transform, false);

                var rt = tabObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(110f, 36f);

                var img = tabObj.GetComponent<Image>();
                bool isSelected = (index == _selectedInstanceIndex);
                var minion = _currentMinionInstances[index];
                bool isAlive = minion != null && minion.IsAlive;

                Color activeColor = isSelected ? new Color(0.25f, 0.65f, 1f, 1f) : new Color(0.18f, 0.22f, 0.3f, 0.9f);
                if (!isAlive) activeColor = isSelected ? new Color(0.8f, 0.3f, 0.3f, 1f) : new Color(0.25f, 0.15f, 0.15f, 0.9f);
                img.color = activeColor;

                var btn = tabObj.GetComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    _selectedInstanceIndex = index;
                    UpdateRuntimeInstanceStatus(true);
                    BuildInstanceTabs();
                });

                var txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtObj.transform.SetParent(tabObj.transform, false);
                var trt = txtObj.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.sizeDelta = Vector2.zero;

                var tmp = txtObj.GetComponent<TextMeshProUGUI>();
                tmp.fontSize = 20;
                tmp.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.text = isAlive ? $"Lính #{i + 1}" : $"#{i + 1} (Chết)";
                tmp.color = isSelected ? Color.white : new Color(0.8f, 0.85f, 0.9f, 0.85f);
                tmp.raycastTarget = false;
            }
        }

        // =========================================================================
        // HELPER UTILITIES CHO MÀU SẮC & TÊN GỌI
        // =========================================================================

        public static Color GetTierColor(MinionTier tier)
        {
            switch (tier)
            {
                case MinionTier.Tier1: return new Color(0.85f, 0.88f, 0.92f);      // Bạc sáng
                case MinionTier.Tier2: return new Color(0.3f, 0.85f, 0.45f);      // Xanh lục tươi
                case MinionTier.Tier3: return new Color(0.85f, 0.45f, 1f);        // Tím huyền bí
                case MinionTier.Tier4_Special: return new Color(1f, 0.65f, 0.15f); // Vàng kim cam
                default: return Color.white;
            }
        }

        private Sprite ResolveSprite(MinionDataSO data)
        {
            if (data != null && data.Icon != null)
            {
                return data.Icon;
            }

            if (data != null && data.Prefab != null)
            {
                var sr = data.Prefab.GetComponentInChildren<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    return sr.sprite;
                }
            }

            if (_iconImage != null && _iconImage.sprite != null)
            {
                return _iconImage.sprite;
            }

            return null;
        }

        public static Color GetClassColor(MinionClass mClass)
        {
            switch (mClass)
            {
                case MinionClass.Warrior: return new Color(0.25f, 0.75f, 1f);
                case MinionClass.Archer: return new Color(0.35f, 0.95f, 0.45f);
                case MinionClass.Mage: return new Color(0.8f, 0.45f, 1f);
                case MinionClass.Tank: return new Color(1f, 0.65f, 0.25f);
                case MinionClass.Support: return new Color(1f, 0.9f, 0.25f);
                default: return Color.white;
            }
        }

        public static string GetTierDisplayName(MinionTier tier)
        {
            switch (tier)
            {
                case MinionTier.Tier1: return "TIER 1 • CƠ BẢN";
                case MinionTier.Tier2: return "TIER 2 • NÂNG CAO";
                case MinionTier.Tier3: return "TIER 3 • TINH ANH";
                case MinionTier.Tier4_Special: return "TIER 4 • HUYỀN THOẠI";
                default: return tier.ToString().ToUpper();
            }
        }

        public static string GetClassDisplayName(MinionClass mClass)
        {
            switch (mClass)
            {
                case MinionClass.Warrior: return "CHIẾN BINH";
                case MinionClass.Archer: return "CUNG THỦ";
                case MinionClass.Mage: return "PHÁP SƯ";
                case MinionClass.Tank: return "ĐỠ ĐÒN";
                case MinionClass.Support: return "HỖ TRỢ";
                default: return mClass.ToString().ToUpper();
            }
        }

        private static string GetAttackMethodDescription(MinionDataSO data)
        {
            if (data == null) return "Cận chiến";
            switch (data.AttackMethod)
            {
                case AttackMethod.Melee:
                    return "<b>Cận Chiến</b> <size=80%><color=#A0AAB8>(Tấn công trực tiếp)</color></size>";
                case AttackMethod.RangedProjectile:
                    return "<b>Bắn Tầm Xa</b> <size=80%><color=#A0AAB8>(Bắn đạn projectile)</color></size>";
                case AttackMethod.AoE:
                    return $"<b>Diện Rộng (AoE)</b> <size=80%><color=#A0AAB8>(Bán kính {data.AoERadius:0.0}m)</color></size>";
                default:
                    return data.AttackMethod.ToString();
            }
        }

        // =========================================================================
        // TẠO DYNAMIC DETAIL PANEL ĐẦY ĐỦ CẤU TRÚC VỚI KÍCH THƯỚC CHỮ LỚN
        // =========================================================================

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureDetailPanelInScene()
        {
            if (Instance != null) return;

            var existing = UnityEngine.Object.FindAnyObjectByType<MinionDetailStatsPanelUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                Instance = existing;
                return;
            }

            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            CreateDynamicDetailPanel(canvas.transform);
        }

        public static MinionDetailStatsPanelUI CreateDynamicDetailPanel(Transform canvasTransform)
        {
            // 1. Root Modal Object
            var panelObj = new GameObject("MinionDetailStatsPanelModal", typeof(RectTransform), typeof(CanvasGroup));
            panelObj.transform.SetParent(canvasTransform, false);

            var panelRt = panelObj.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.sizeDelta = Vector2.zero;

            var cg = panelObj.GetComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;

            // 2. Backdrop button
            var backdropObj = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            backdropObj.transform.SetParent(panelObj.transform, false);
            var bdRt = backdropObj.GetComponent<RectTransform>();
            bdRt.anchorMin = Vector2.zero;
            bdRt.anchorMax = Vector2.one;
            bdRt.sizeDelta = Vector2.zero;

            var bdImg = backdropObj.GetComponent<Image>();
            bdImg.color = new Color(0.04f, 0.05f, 0.08f, 0.8f);
            var bdBtn = backdropObj.GetComponent<Button>();

            // 3. Modal Window Box (Rộng 720f, Cao 1050f cho màn hình dọc)
            var windowObj = new GameObject("ModalWindow", typeof(RectTransform), typeof(Image));
            windowObj.transform.SetParent(panelObj.transform, false);
            var winRt = windowObj.GetComponent<RectTransform>();
            winRt.anchorMin = new Vector2(0.5f, 0.5f);
            winRt.anchorMax = new Vector2(0.5f, 0.5f);
            winRt.pivot = new Vector2(0.5f, 0.5f);
            winRt.sizeDelta = new Vector2(720f, 1050f);

            var winImg = windowObj.GetComponent<Image>();
            winImg.color = new Color(0.12f, 0.14f, 0.18f, 0.98f);

            // Tier Header Bar
            var tbObj = new GameObject("TierHeaderBar", typeof(RectTransform), typeof(Image));
            tbObj.transform.SetParent(windowObj.transform, false);
            var tbRt = tbObj.GetComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0f, 1f);
            tbRt.anchorMax = new Vector2(1f, 1f);
            tbRt.pivot = new Vector2(0.5f, 1f);
            tbRt.sizeDelta = new Vector2(0f, 8f);
            var tbImg = tbObj.GetComponent<Image>();
            tbImg.color = new Color(0.3f, 0.85f, 0.45f);

            // Close button (X) ở góc phải dùng chữ 'X' ASCII tiêu chuẩn
            var closeObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeObj.transform.SetParent(windowObj.transform, false);
            var closeRt = closeObj.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-15f, -15f);
            closeRt.sizeDelta = new Vector2(48f, 48f);

            var closeImg = closeObj.GetComponent<Image>();
            closeImg.color = new Color(0.25f, 0.28f, 0.35f, 0.95f);
            var closeBtn = closeObj.GetComponent<Button>();

            var closeTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeTxtObj.transform.SetParent(closeObj.transform, false);
            var ctrt = closeTxtObj.GetComponent<RectTransform>();
            ctrt.anchorMin = Vector2.zero;
            ctrt.anchorMax = Vector2.one;
            ctrt.sizeDelta = Vector2.zero;
            var closeTmp = closeTxtObj.GetComponent<TextMeshProUGUI>();
            closeTmp.fontSize = 28;
            closeTmp.fontStyle = FontStyles.Bold;
            closeTmp.alignment = TextAlignmentOptions.Center;
            closeTmp.text = "X";
            closeTmp.color = Color.white;
            closeTmp.raycastTarget = false;

            // 4. Header: Avatar + Title & Tags
            var headerObj = new GameObject("Header", typeof(RectTransform));
            headerObj.transform.SetParent(windowObj.transform, false);
            var hRt = headerObj.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = new Vector2(0f, -25f);
            hRt.sizeDelta = new Vector2(-50f, 130f);

            // Avatar Container
            var avatarObj = new GameObject("AvatarContainer", typeof(RectTransform), typeof(Image));
            avatarObj.transform.SetParent(headerObj.transform, false);
            var avRt = avatarObj.GetComponent<RectTransform>();
            avRt.anchorMin = new Vector2(0f, 0.5f);
            avRt.anchorMax = new Vector2(0f, 0.5f);
            avRt.pivot = new Vector2(0f, 0.5f);
            avRt.anchoredPosition = Vector2.zero;
            avRt.sizeDelta = new Vector2(120f, 120f);
            var avImg = avatarObj.GetComponent<Image>();
            avImg.color = new Color(0.18f, 0.20f, 0.25f, 0.95f);

            var iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(avatarObj.transform, false);
            var iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.sizeDelta = new Vector2(-12f, -12f);
            var iconImg = iconObj.GetComponent<Image>();
            iconImg.preserveAspect = true;

            var fallbackTxtObj = new GameObject("InitialText", typeof(RectTransform), typeof(TextMeshProUGUI));
            fallbackTxtObj.transform.SetParent(iconObj.transform, false);
            var fbRt = fallbackTxtObj.GetComponent<RectTransform>();
            fbRt.anchorMin = Vector2.zero;
            fbRt.anchorMax = Vector2.one;
            fbRt.sizeDelta = Vector2.zero;
            var fbTmp = fallbackTxtObj.GetComponent<TextMeshProUGUI>();
            fbTmp.fontSize = 48;
            fbTmp.fontStyle = FontStyles.Bold;
            fbTmp.alignment = TextAlignmentOptions.Center;
            fbTmp.text = "L";

            // Title Container
            var titleObj = new GameObject("TitleContainer", typeof(RectTransform));
            titleObj.transform.SetParent(headerObj.transform, false);
            var tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0.5f);
            tRt.anchorMax = new Vector2(1f, 0.5f);
            tRt.pivot = new Vector2(0f, 0.5f);
            tRt.anchoredPosition = new Vector2(145f, 0f);
            tRt.sizeDelta = new Vector2(-170f, 120f);

            var nameTxtObj = new GameObject("MinionName", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameTxtObj.transform.SetParent(titleObj.transform, false);
            var nRt = nameTxtObj.GetComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0f, 1f);
            nRt.anchorMax = new Vector2(1f, 1f);
            nRt.pivot = new Vector2(0f, 1f);
            nRt.sizeDelta = new Vector2(0f, 44f);
            var nameTmp = nameTxtObj.GetComponent<TextMeshProUGUI>();
            nameTmp.fontSize = 32;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.alignment = TextAlignmentOptions.Left;
            nameTmp.color = Color.white;
            nameTmp.text = "Tên Lính";

            var tagsObj = new GameObject("TagsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tagsObj.transform.SetParent(titleObj.transform, false);
            var tgRt = tagsObj.GetComponent<RectTransform>();
            tgRt.anchorMin = new Vector2(0f, 0.5f);
            tgRt.anchorMax = new Vector2(1f, 0.5f);
            tgRt.pivot = new Vector2(0f, 0.5f);
            tgRt.anchoredPosition = new Vector2(0f, -6f);
            tgRt.sizeDelta = new Vector2(0f, 32f);

            var hlg = tagsObj.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 15f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            var tierTagObj = new GameObject("TierTag", typeof(RectTransform), typeof(TextMeshProUGUI));
            tierTagObj.transform.SetParent(tagsObj.transform, false);
            var ttTmp = tierTagObj.GetComponent<TextMeshProUGUI>();
            ttTmp.fontSize = 22;
            ttTmp.fontStyle = FontStyles.Bold;
            ttTmp.text = "TIER 1";

            var classTagObj = new GameObject("ClassTag", typeof(RectTransform), typeof(TextMeshProUGUI));
            classTagObj.transform.SetParent(tagsObj.transform, false);
            var ctTmp = classTagObj.GetComponent<TextMeshProUGUI>();
            ctTmp.fontSize = 22;
            ctTmp.fontStyle = FontStyles.Bold;
            ctTmp.text = "CHIẾN BINH";

            var rosterTxtObj = new GameObject("RosterSummaryText", typeof(RectTransform), typeof(TextMeshProUGUI));
            rosterTxtObj.transform.SetParent(titleObj.transform, false);
            var rRt = rosterTxtObj.GetComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0f, 0f);
            rRt.anchorMax = new Vector2(1f, 0f);
            rRt.pivot = new Vector2(0f, 0f);
            rRt.sizeDelta = new Vector2(0f, 30f);
            var rTmp = rosterTxtObj.GetComponent<TextMeshProUGUI>();
            rTmp.fontSize = 22;
            rTmp.color = new Color(0.72f, 0.78f, 0.88f);
            rTmp.text = "Đang có: 1/1 con trên sân";

            // 5. Runtime Status Section (Thanh máu, Tabs và Badge Trạng Thái)
            var runtimeObj = new GameObject("RuntimeStatus", typeof(RectTransform), typeof(Image));
            runtimeObj.transform.SetParent(windowObj.transform, false);
            var runRt = runtimeObj.GetComponent<RectTransform>();
            runRt.anchorMin = new Vector2(0.5f, 1f);
            runRt.anchorMax = new Vector2(0.5f, 1f);
            runRt.pivot = new Vector2(0.5f, 1f);
            runRt.anchoredPosition = new Vector2(0f, -170f);
            runRt.sizeDelta = new Vector2(660f, 150f);
            var runImg = runtimeObj.GetComponent<Image>();
            runImg.color = new Color(0.08f, 0.10f, 0.13f, 0.95f);

            // Container Tabs chọn cá thể
            var tabsContainerObj = new GameObject("InstanceTabsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabsContainerObj.transform.SetParent(runtimeObj.transform, false);
            var tabsRt = tabsContainerObj.GetComponent<RectTransform>();
            tabsRt.anchorMin = new Vector2(0f, 1f);
            tabsRt.anchorMax = new Vector2(1f, 1f);
            tabsRt.pivot = new Vector2(0.5f, 1f);
            tabsRt.anchoredPosition = new Vector2(0f, -8f);
            tabsRt.sizeDelta = new Vector2(-30f, 38f);
            var tabsHlg = tabsContainerObj.GetComponent<HorizontalLayoutGroup>();
            tabsHlg.spacing = 10f;
            tabsHlg.childAlignment = TextAnchor.MiddleLeft;

            // State Badge
            var stateBadgeObj = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            stateBadgeObj.transform.SetParent(runtimeObj.transform, false);
            var sbRt = stateBadgeObj.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(1f, 0f);
            sbRt.anchorMax = new Vector2(1f, 0f);
            sbRt.pivot = new Vector2(1f, 0f);
            sbRt.anchoredPosition = new Vector2(-15f, 15f);
            sbRt.sizeDelta = new Vector2(230f, 42f);
            var sbImg = stateBadgeObj.GetComponent<Image>();
            sbImg.color = new Color(0.18f, 0.65f, 0.35f, 0.95f);

            var sbTxtObj = new GameObject("StateText", typeof(RectTransform), typeof(TextMeshProUGUI));
            sbTxtObj.transform.SetParent(stateBadgeObj.transform, false);
            var sbtRt = sbTxtObj.GetComponent<RectTransform>();
            sbtRt.anchorMin = Vector2.zero;
            sbtRt.anchorMax = Vector2.one;
            sbtRt.sizeDelta = Vector2.zero;
            var sbTmp = sbTxtObj.GetComponent<TextMeshProUGUI>();
            sbTmp.fontSize = 20;
            sbTmp.fontStyle = FontStyles.Bold;
            sbTmp.alignment = TextAlignmentOptions.Center;
            sbTmp.text = "THEO SAU";
            sbTmp.color = Color.white;

            // HP Bar & Text
            var hpContainer = new GameObject("HPContainer", typeof(RectTransform));
            hpContainer.transform.SetParent(runtimeObj.transform, false);
            var hpcRt = hpContainer.GetComponent<RectTransform>();
            hpcRt.anchorMin = new Vector2(0f, 0f);
            hpcRt.anchorMax = new Vector2(0.62f, 0f);
            hpcRt.pivot = new Vector2(0f, 0f);
            hpcRt.anchoredPosition = new Vector2(15f, 12f);
            hpcRt.sizeDelta = new Vector2(0f, 75f);

            var hpTextObj = new GameObject("HPText", typeof(RectTransform), typeof(TextMeshProUGUI));
            hpTextObj.transform.SetParent(hpContainer.transform, false);
            var hptRt = hpTextObj.GetComponent<RectTransform>();
            hptRt.anchorMin = new Vector2(0f, 1f);
            hptRt.anchorMax = new Vector2(1f, 1f);
            hptRt.pivot = new Vector2(0f, 1f);
            hptRt.sizeDelta = new Vector2(0f, 32f);
            var hptTmp = hpTextObj.GetComponent<TextMeshProUGUI>();
            hptTmp.fontSize = 24;
            hptTmp.text = "100 / 100 HP (100%)";
            hptTmp.color = Color.white;

            var hpBarBgObj = new GameObject("HPBar", typeof(RectTransform), typeof(Image));
            hpBarBgObj.transform.SetParent(hpContainer.transform, false);
            var hpbRt = hpBarBgObj.GetComponent<RectTransform>();
            hpbRt.anchorMin = new Vector2(0f, 0f);
            hpbRt.anchorMax = new Vector2(1f, 0f);
            hpbRt.pivot = new Vector2(0f, 0f);
            hpbRt.sizeDelta = new Vector2(0f, 26f);
            var hpbImg = hpBarBgObj.GetComponent<Image>();
            hpbImg.color = new Color(0.18f, 0.20f, 0.24f, 1f);

            var hpFillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            hpFillObj.transform.SetParent(hpBarBgObj.transform, false);
            var hpfRt = hpFillObj.GetComponent<RectTransform>();
            hpfRt.anchorMin = Vector2.zero;
            hpfRt.anchorMax = Vector2.one;
            hpfRt.sizeDelta = Vector2.zero;
            var hpfImg = hpFillObj.GetComponent<Image>();
            hpfImg.type = Image.Type.Filled;
            hpfImg.fillMethod = Image.FillMethod.Horizontal;
            hpfImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            hpfImg.fillAmount = 1f;
            hpfImg.color = new Color(0.2f, 0.9f, 0.35f);

            // 6. Combat Stats Grid (Bảng các dòng chỉ số - Font lớn x2)
            var statsPanelObj = new GameObject("StatsGrid", typeof(RectTransform), typeof(VerticalLayoutGroup));
            statsPanelObj.transform.SetParent(windowObj.transform, false);
            var spRt = statsPanelObj.GetComponent<RectTransform>();
            spRt.anchorMin = new Vector2(0.5f, 1f);
            spRt.anchorMax = new Vector2(0.5f, 1f);
            spRt.pivot = new Vector2(0.5f, 1f);
            spRt.anchoredPosition = new Vector2(0f, -335f);
            spRt.sizeDelta = new Vector2(660f, 375f);

            var vlg = statsPanelObj.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            var damageRow = CreateStatRow(statsPanelObj.transform, "Sát Thương Đòn Đánh", "15 sát thương");
            var speedRow = CreateStatRow(statsPanelObj.transform, "Tần Suất Tấn Công", "1.2 đòn/giây");
            var rangeRow = CreateStatRow(statsPanelObj.transform, "Tầm Tấn Công", "1.2m");
            var moveRow = CreateStatRow(statsPanelObj.transform, "Tốc Độ Di Chuyển", "4.5 m/s");
            var methodRow = CreateStatRow(statsPanelObj.transform, "Phương Thức Đánh", "Cận chiến");
            var knockbackRow = CreateStatRow(statsPanelObj.transform, "Lực Đẩy Lùi Quái", "1.5");
            var respawnRow = CreateStatRow(statsPanelObj.transform, "Thời Gian Hồi Sinh", "10 giây");

            // 7. Description Box
            var descBoxObj = new GameObject("DescriptionBox", typeof(RectTransform), typeof(Image));
            descBoxObj.transform.SetParent(windowObj.transform, false);
            var dbRt = descBoxObj.GetComponent<RectTransform>();
            dbRt.anchorMin = new Vector2(0.5f, 0f);
            dbRt.anchorMax = new Vector2(0.5f, 0f);
            dbRt.pivot = new Vector2(0.5f, 0f);
            dbRt.anchoredPosition = new Vector2(0f, 90f);
            dbRt.sizeDelta = new Vector2(660f, 175f);
            var dbImg = descBoxObj.GetComponent<Image>();
            dbImg.color = new Color(0.08f, 0.10f, 0.13f, 0.95f);

            var descTxtObj = new GameObject("DescriptionText", typeof(RectTransform), typeof(TextMeshProUGUI));
            descTxtObj.transform.SetParent(descBoxObj.transform, false);
            var dtRt = descTxtObj.GetComponent<RectTransform>();
            dtRt.anchorMin = Vector2.zero;
            dtRt.anchorMax = Vector2.one;
            dtRt.sizeDelta = new Vector2(-28f, -24f);
            var dtTmp = descTxtObj.GetComponent<TextMeshProUGUI>();
            dtTmp.fontSize = 24;
            dtTmp.alignment = TextAlignmentOptions.TopLeft;
            dtTmp.color = new Color(0.85f, 0.88f, 0.92f);
            dtTmp.text = "Lính đồng minh sát cánh chiến đấu và bảo vệ người chơi.";

            // 8. Footer Close Button
            var bCloseObj = new GameObject("BottomCloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            bCloseObj.transform.SetParent(windowObj.transform, false);
            var bcRt = bCloseObj.GetComponent<RectTransform>();
            bcRt.anchorMin = new Vector2(0.5f, 0f);
            bcRt.anchorMax = new Vector2(0.5f, 0f);
            bcRt.pivot = new Vector2(0.5f, 0f);
            bcRt.anchoredPosition = new Vector2(0f, 20f);
            bcRt.sizeDelta = new Vector2(260f, 55f);
            var bcImg = bCloseObj.GetComponent<Image>();
            bcImg.color = new Color(0.22f, 0.25f, 0.32f, 0.95f);
            var bcBtn = bCloseObj.GetComponent<Button>();

            var bcTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            bcTxtObj.transform.SetParent(bCloseObj.transform, false);
            var bctRt = bcTxtObj.GetComponent<RectTransform>();
            bctRt.anchorMin = Vector2.zero;
            bctRt.anchorMax = Vector2.one;
            bctRt.sizeDelta = Vector2.zero;
            var bcTmp = bcTxtObj.GetComponent<TextMeshProUGUI>();
            bcTmp.fontSize = 26;
            bcTmp.fontStyle = FontStyles.Bold;
            bcTmp.alignment = TextAlignmentOptions.Center;
            bcTmp.text = "ĐÓNG";
            bcTmp.color = Color.white;
            bcTmp.raycastTarget = false;

            // Gắn và cấu hình component MinionDetailStatsPanelUI
            var panelUI = panelObj.AddComponent<MinionDetailStatsPanelUI>();
            panelUI._panelRoot = panelObj;
            panelUI._canvasGroup = cg;
            panelUI._backdropButton = bdBtn;
            panelUI._closeButton = closeBtn;
            panelUI._bottomCloseButton = bcBtn;
            panelUI._tierHeaderBar = tbImg;
            panelUI._iconImage = iconImg;
            panelUI._iconInitialFallback = fbTmp;
            panelUI._minionNameText = nameTmp;
            panelUI._tierTagText = ttTmp;
            panelUI._classTagText = ctTmp;
            panelUI._rosterSummaryText = rTmp;

            panelUI._instanceTabsContainer = tabsContainerObj;
            panelUI._hpBarFill = hpfImg;
            panelUI._hpText = hptTmp;
            panelUI._stateBadgeText = sbTmp;
            panelUI._stateBadgeBg = sbImg;

            panelUI._damageValueText = damageRow;
            panelUI._attackSpeedValueText = speedRow;
            panelUI._attackRangeValueText = rangeRow;
            panelUI._moveSpeedValueText = moveRow;
            panelUI._attackMethodValueText = methodRow;
            panelUI._knockbackValueText = knockbackRow;
            panelUI._respawnValueText = respawnRow;

            panelUI._descriptionText = dtTmp;

            panelUI.SetupButtonListeners();
            panelUI.HidePanel();

            Instance = panelUI;
            return panelUI;
        }

        private static TMP_Text CreateStatRow(Transform parent, string label, string defaultValue)
        {
            var rowObj = new GameObject("Row", typeof(RectTransform), typeof(Image));
            rowObj.transform.SetParent(parent, false);
            var rRt = rowObj.GetComponent<RectTransform>();
            rRt.sizeDelta = new Vector2(0f, 44f);
            var rImg = rowObj.GetComponent<Image>();
            rImg.color = new Color(0.15f, 0.17f, 0.22f, 0.65f);

            var labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(rowObj.transform, false);
            var lRt = labelObj.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0f, 0f);
            lRt.anchorMax = new Vector2(0.48f, 1f);
            lRt.pivot = new Vector2(0f, 0.5f);
            lRt.anchoredPosition = new Vector2(14f, 0f);
            lRt.sizeDelta = Vector2.zero;
            var lTmp = labelObj.GetComponent<TextMeshProUGUI>();
            lTmp.fontSize = 23;
            lTmp.color = new Color(0.72f, 0.78f, 0.88f);
            lTmp.alignment = TextAlignmentOptions.MidlineLeft;
            lTmp.text = label;
            lTmp.raycastTarget = false;

            var valObj = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
            valObj.transform.SetParent(rowObj.transform, false);
            var vRt = valObj.GetComponent<RectTransform>();
            vRt.anchorMin = new Vector2(0.5f, 0f);
            vRt.anchorMax = new Vector2(1f, 1f);
            vRt.pivot = new Vector2(1f, 0.5f);
            vRt.anchoredPosition = new Vector2(-14f, 0f);
            vRt.sizeDelta = Vector2.zero;
            var vTmp = valObj.GetComponent<TextMeshProUGUI>();
            vTmp.fontSize = 23;
            vTmp.color = Color.white;
            vTmp.alignment = TextAlignmentOptions.MidlineRight;
            vTmp.text = defaultValue;
            vTmp.raycastTarget = false;

            return vTmp;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Project_D/UI/Attach Minion Detail Stats Panel to Canvas", false, 30)]
        private static void AttachDetailPanelMenuItem()
        {
            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[UI] Không tìm thấy Canvas trong Scene hiện tại!");
                return;
            }

            var existing = canvas.GetComponentInChildren<MinionDetailStatsPanelUI>(true);
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing.gameObject);
                else DestroyImmediate(existing.gameObject);
            }

            var panel = CreateDynamicDetailPanel(canvas.transform);
            UnityEditor.EditorUtility.SetDirty(canvas.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Debug.Log($"<color=green>[UI]</color> Đã tạo mới và cập nhật MinionDetailStatsPanelModal với font chữ lớn x2 trong Canvas thành công!");
        }
#endif
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Project_D.Core;
using Project_D.Player;
using Project_D.Combat;
using Project_D.Minions;
using Project_D.Dungeon.Run;

namespace Project_D.UI.Settings
{
    public class SettingsPanelUI : MonoBehaviour
    {
        public static SettingsPanelUI Instance { get; private set; }

        // =========================================================================
        // EVENTS (CHO AUDIO VÀ VFX SUBSYSTEMS HOOK VÀO)
        // =========================================================================
        public static event Action<float> OnBGMVolumeChanged;
        public static event Action<float> OnSFXVolumeChanged;
        public static event Action<bool> OnScreenShakeToggled;
        public static event Action<bool> OnDamageNumbersToggled;
        public static event Action<bool> OnHighQualityToggled;
        public static event Action<bool> OnSettingsPanelToggled;
        public static event Action OnAbandonRunRequested;

        // PlayerPrefs Constants
        private const string PREF_BGM_VOL = "Setting_BGMVol";
        private const string PREF_SFX_VOL = "Setting_SFXVol";
        private const string PREF_SCREEN_SHAKE = "Setting_ScreenShake";
        private const string PREF_DAMAGE_NUMBERS = "Setting_DamageNumbers";
        private const string PREF_HIGH_QUALITY = "Setting_HighQuality";

        [Header("Panel Root & Navigation")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _openButton;           // Nút mở trên HUD (icon Menu / Bánh răng)
        [SerializeField] private Button _closeButton;          // Nút X
        [SerializeField] private Button _resumeButton;         // Nút "Tiếp Tục Chiến Đấu"
        [SerializeField] private Button _retreatButton;        // Nút "Rút Lui"
        [SerializeField] private Button _backdropButton;       // Nền đen mờ
        [SerializeField] private bool _pauseTimeWhenOpen = true;

        [Header("Phần 1: Dungeon Mini Map Graph")]
        [SerializeField] private GameObject _mapSection;
        [SerializeField] private ScrollRect _mapScrollRect;
        [SerializeField] private RectTransform _mapGraphContainer;
        [SerializeField] private TMP_Text _floorBadgeText;
        [SerializeField] private RectTransform _playerPin;
        [SerializeField] private RunMapConfigSO _fallbackMapConfig;
        [SerializeField] private float _floorSpacingY = 135f;
        [SerializeField] private float _nodeSpacingX = 160f;

        [Header("Phần 2: Option (Tùy Chọn)")]
        [SerializeField] private Slider _bgmVolumeSlider;
        [SerializeField] private TMP_Text _bgmVolumeText;
        [SerializeField] private Slider _sfxVolumeSlider;
        [SerializeField] private TMP_Text _sfxVolumeText;
        [SerializeField] private Toggle _damageNumbersToggle;
        [SerializeField] private Toggle _highQualityToggle;
        [SerializeField] private Toggle _screenShakeToggle;

        [Header("Phần 3: Sát Thương Gây Cho Quái Vật (Damage Bar)")]
        [SerializeField] private Transform _damageEntriesContainer;
        [SerializeField] private TMP_Text _damageBarTitleText;
        [SerializeField] private GameObject _statTooltipBox;
        [SerializeField] private TMP_Text _statTooltipText;

        // Pools & Caches (Zero GC)
        private readonly List<GameObject> _mapNodePool = new List<GameObject>();
        private readonly List<GameObject> _mapLinePool = new List<GameObject>();
        private readonly List<DamageEntryCard> _damageCardPool = new List<DamageEntryCard>();
        private readonly Dictionary<int, Vector2> _nodePositionsCache = new Dictionary<int, Vector2>();

        private bool _isOpen = false;
        public bool IsOpen => _isOpen;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            AutoFindReferences();
            InitializeAudioVfxListeners();
            LoadSettingsPreferences();

            if (_openButton != null)
            {
                _openButton.onClick.RemoveListener(Open);
                _openButton.onClick.AddListener(Open);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(Close);
                _closeButton.onClick.AddListener(Close);
            }

            if (_resumeButton != null)
            {
                _resumeButton.onClick.RemoveListener(Close);
                _resumeButton.onClick.AddListener(Close);
            }

            if (_retreatButton != null)
            {
                _retreatButton.onClick.RemoveListener(HandleRetreatClicked);
                _retreatButton.onClick.AddListener(HandleRetreatClicked);
            }

            if (_backdropButton != null)
            {
                _backdropButton.onClick.RemoveListener(Close);
                _backdropButton.onClick.AddListener(Close);
            }

            SetPanelVisibility(false);
        }

        private void Start()
        {
            ApplyInitialSettings();
        }

        private void Update()
        {
            // Tương thích New Input System
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Toggle();
            }
        }

        // =========================================================================
        // 1. QUẢN LÝ BẬT / TẮT & TẠM DỪNG GAME AN TOÀN
        // =========================================================================

        public void Toggle()
        {
            if (_isOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (_isOpen) return;
            _isOpen = true;

            SetPanelVisibility(true);

            if (_pauseTimeWhenOpen && PanelQueueManager.Instance != null)
            {
                PanelQueueManager.Instance.RequestPause(this, "Open Settings Panel");
            }

            if (_statTooltipBox != null)
            {
                _statTooltipBox.SetActive(false);
            }

            RefreshAllSections();
            OnSettingsPanelToggled?.Invoke(true);
        }

        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;

            if (_statTooltipBox != null)
            {
                _statTooltipBox.SetActive(false);
            }

            SetPanelVisibility(false);

            if (_pauseTimeWhenOpen && PanelQueueManager.Instance != null)
            {
                PanelQueueManager.Instance.ReleasePause(this);
            }

            OnSettingsPanelToggled?.Invoke(false);
        }

        private void SetPanelVisibility(bool visible)
        {
            // PanelRoot, MapSection và Backdrop luôn active để CanvasGroup kiểm soát độ trong suốt mượt mà
            if (_panelRoot != null && !_panelRoot.activeSelf)
            {
                _panelRoot.SetActive(true);
            }

            if (_mapSection != null && !_mapSection.activeSelf)
            {
                _mapSection.SetActive(true);
            }

            if (_backdropButton != null && !_backdropButton.gameObject.activeSelf)
            {
                _backdropButton.gameObject.SetActive(true);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.interactable = visible;
                _canvasGroup.blocksRaycasts = visible;
            }
            else
            {
                if (_panelRoot != null) _panelRoot.SetActive(visible);
                if (_backdropButton != null) _backdropButton.gameObject.SetActive(visible);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Xem Trước: BẬT Panel (Editor Preview)")]
        private void EditorPreviewShow()
        {
            SetPanelVisibility(true);
            RefreshAllSections();
            UnityEditor.EditorUtility.SetDirty(gameObject);
        }

        [ContextMenu("Xem Trước: TẮT Panel (Editor Preview)")]
        private void EditorPreviewHide()
        {
            SetPanelVisibility(false);
            UnityEditor.EditorUtility.SetDirty(gameObject);
        }

        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                if (_panelRoot != null && !_panelRoot.activeSelf)
                {
                    _panelRoot.SetActive(true);
                }
                if (_mapSection != null && !_mapSection.activeSelf)
                {
                    _mapSection.SetActive(true);
                }
                if (_backdropButton != null && !_backdropButton.gameObject.activeSelf)
                {
                    _backdropButton.gameObject.SetActive(true);
                }
            };
        }
#endif

        public void RefreshAllSections()
        {
            RefreshDungeonMapGraph();
            RefreshDamageBreakdownBar();
        }

        // =========================================================================
        // 2. PHẦN 1: MINI MAP GRAPH (GRAPH DẠNG CÂY TRỰC QUAN NHƯ HÌNH MẪU)
        // =========================================================================

        private void EnsureMapSectionExists()
        {
            if (_mapSection != null && _mapGraphContainer != null) return;

            Transform parent = _panelRoot != null ? _panelRoot.transform : transform;
            Transform existingMap = FindElementTransform("MapSection") ?? parent.Find("MapSection");
            if (existingMap != null)
            {
                _mapSection = existingMap.gameObject;
                _mapScrollRect = existingMap.GetComponentInChildren<ScrollRect>(true);
                var content = existingMap.Find("MapScrollView/Viewport/MapGraphContent");
                if (content != null) _mapGraphContainer = content as RectTransform;
                _floorBadgeText = existingMap.GetComponentInChildren<TMP_Text>(true);
                var pin = existingMap.Find("MapScrollView/Viewport/MapGraphContent/PlayerPin");
                if (pin != null) _playerPin = pin as RectTransform;
                if (_mapGraphContainer != null) return;
            }

            // Tự động dựng MapSection runtime nếu Scene đang chạy bản cũ chưa kịp đồng bộ Prefab
            var mapSection = new GameObject("MapSection", typeof(RectTransform), typeof(Image));
            mapSection.layer = 5;
            mapSection.transform.SetParent(parent, false);
            mapSection.transform.SetAsFirstSibling();

            var msRt = mapSection.GetComponent<RectTransform>();
            msRt.anchorMin = new Vector2(0.5f, 1f);
            msRt.anchorMax = new Vector2(0.5f, 1f);
            msRt.pivot = new Vector2(0.5f, 1f);
            msRt.anchoredPosition = new Vector2(0f, -70f);
            msRt.sizeDelta = new Vector2(980f, 580f);

            var msImg = mapSection.GetComponent<Image>();
            msImg.color = new Color(0.08f, 0.10f, 0.14f, 0.85f);
            var msOutline = mapSection.AddComponent<Outline>();
            msOutline.effectColor = new Color(0.25f, 0.30f, 0.40f, 0.6f);
            msOutline.effectDistance = new Vector2(2f, -2f);

            TMP_FontAsset font = _bgmVolumeText != null ? _bgmVolumeText.font : null;

            // Floor Badge Text
            var badgeObj = new GameObject("FloorBadgeText", typeof(RectTransform), typeof(TextMeshProUGUI));
            badgeObj.layer = 5;
            badgeObj.transform.SetParent(mapSection.transform, false);
            var fbRt = badgeObj.GetComponent<RectTransform>();
            fbRt.anchorMin = new Vector2(0f, 1f);
            fbRt.anchorMax = new Vector2(0f, 1f);
            fbRt.pivot = new Vector2(0f, 1f);
            fbRt.anchoredPosition = new Vector2(25f, -14f);
            fbRt.sizeDelta = new Vector2(500f, 38f);
            var fbTmp = badgeObj.GetComponent<TextMeshProUGUI>();
            if (font != null) fbTmp.font = font;
            fbTmp.text = "TIẾN TRÌNH HẦM NGỤC (TẦNG 1 / 13)";
            fbTmp.fontSize = 24;
            fbTmp.fontStyle = FontStyles.Bold;
            fbTmp.color = new Color(0.35f, 0.85f, 1f);

            // ScrollView
            var scrollObj = new GameObject("MapScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollObj.layer = 5;
            scrollObj.transform.SetParent(mapSection.transform, false);
            var svRt = scrollObj.GetComponent<RectTransform>();
            svRt.anchorMin = Vector2.zero;
            svRt.anchorMax = Vector2.one;
            svRt.offsetMin = new Vector2(15f, 15f);
            svRt.offsetMax = new Vector2(-15f, -56f);

            var sRect = scrollObj.GetComponent<ScrollRect>();
            sRect.horizontal = false;
            sRect.vertical = true;
            sRect.scrollSensitivity = 35f;

            // Viewport
            var vpObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            vpObj.layer = 5;
            vpObj.transform.SetParent(scrollObj.transform, false);
            var vpRt = vpObj.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.sizeDelta = Vector2.zero;
            sRect.viewport = vpRt;

            // Content
            var contentObj = new GameObject("MapGraphContent", typeof(RectTransform));
            contentObj.layer = 5;
            contentObj.transform.SetParent(vpObj.transform, false);
            var cRt = contentObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 1f);
            cRt.anchorMax = new Vector2(0.5f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(950f, 1200f);
            sRect.content = cRt;

            // Player Pin
            var pinObj = new GameObject("PlayerPin", typeof(RectTransform), typeof(Image));
            pinObj.layer = 5;
            pinObj.transform.SetParent(contentObj.transform, false);
            var pinRt = pinObj.GetComponent<RectTransform>();
            pinRt.anchorMin = new Vector2(0.5f, 1f);
            pinRt.anchorMax = new Vector2(0.5f, 1f);
            pinRt.pivot = new Vector2(0.5f, 0.5f);
            pinRt.sizeDelta = new Vector2(56f, 56f);
            var pinImg = pinObj.GetComponent<Image>();
            pinImg.color = new Color(1f, 0.85f, 0.25f, 1f);
            var pinOutline = pinObj.AddComponent<Outline>();
            pinOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            pinOutline.effectDistance = new Vector2(2.5f, -2.5f);

            var pinTxtObj = new GameObject("PinIcon", typeof(RectTransform), typeof(TextMeshProUGUI));
            pinTxtObj.layer = 5;
            pinTxtObj.transform.SetParent(pinObj.transform, false);
            var ptRt = pinTxtObj.GetComponent<RectTransform>();
            ptRt.anchorMin = Vector2.zero;
            ptRt.anchorMax = Vector2.one;
            ptRt.sizeDelta = Vector2.zero;
            var ptTmp = pinTxtObj.GetComponent<TextMeshProUGUI>();
            if (font != null) ptTmp.font = font;
            ptTmp.text = "P";
            ptTmp.fontSize = 28;
            ptTmp.fontStyle = FontStyles.Bold;
            ptTmp.alignment = TextAlignmentOptions.Center;
            ptTmp.color = Color.black;

            _mapSection = mapSection;
            _floorBadgeText = fbTmp;
            _mapScrollRect = sRect;
            _mapGraphContainer = cRt;
            _playerPin = pinRt;
        }

        private void RefreshDungeonMapGraph()
        {
            EnsureMapSectionExists();

            var runMgr = RunManager.Instance ?? FindAnyObjectByType<RunManager>();
            var mapConfig = runMgr != null ? runMgr.MapConfig : null;
            if (mapConfig == null) mapConfig = _fallbackMapConfig;
#if UNITY_EDITOR
            if (mapConfig == null)
            {
                mapConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<RunMapConfigSO>("Assets/_Project/ScriptableObjects/Dungeon/RunMap_13Floors.asset");
            }
#endif
            if (mapConfig == null)
            {
                mapConfig = ScriptableObject.CreateInstance<RunMapConfigSO>();
                mapConfig.BuildDefault13FloorRunMap();
            }

            if (_mapSection != null && !_mapSection.activeSelf)
            {
                _mapSection.SetActive(true);
            }

            if (_mapGraphContainer == null) return;
            if (!_mapGraphContainer.gameObject.activeSelf) _mapGraphContainer.gameObject.SetActive(true);
            if (_mapScrollRect != null && !_mapScrollRect.gameObject.activeSelf) _mapScrollRect.gameObject.SetActive(true);

            var currentNode = (runMgr != null && runMgr.CurrentNode != null) ? runMgr.CurrentNode : mapConfig.GetRootNode();
            int currentFloor = (runMgr != null && runMgr.CurrentFloor > 0) ? runMgr.CurrentFloor : (currentNode != null ? currentNode.FloorNumber : 1);

            if (_floorBadgeText != null)
            {
                _floorBadgeText.text = $"TẦNG {currentFloor} / {mapConfig.TotalFloors}";
            }

            // Tạm ẩn các node và line trong pool
            foreach (var nodeObj in _mapNodePool) if (nodeObj != null) nodeObj.SetActive(false);
            foreach (var lineObj in _mapLinePool) if (lineObj != null) lineObj.SetActive(false);
            _nodePositionsCache.Clear();

            // 1. Tính toán tọa độ đối xứng cho từng Node (Tầng 1 ở trên cùng, các tầng sau chạy xuống dưới)
            float startY = -45f;
            for (int f = 1; f <= mapConfig.TotalFloors; f++)
            {
                var floorNodes = mapConfig.GetNodesForFloor(f);
                int count = floorNodes != null ? floorNodes.Count : 0;
                if (count == 0) continue;

                float y = startY - (f - 1) * _floorSpacingY;
                for (int i = 0; i < count; i++)
                {
                    var node = floorNodes[i];
                    float x = (i - (count - 1) * 0.5f) * _nodeSpacingX;
                    _nodePositionsCache[node.Id] = new Vector2(x, y);
                }
            }

            // Đặt chiều cao container để scroll được mượt mà
            float totalHeight = Mathf.Abs(startY - mapConfig.TotalFloors * _floorSpacingY) + 120f;
            _mapGraphContainer.sizeDelta = new Vector2(_mapGraphContainer.sizeDelta.x, totalHeight);

            // 2. Vẽ các đường nối (Connectors) giữa các Node
            int lineIndex = 0;
            foreach (var node in mapConfig.AllNodes)
            {
                if (node.NextNodeIds == null || !_nodePositionsCache.ContainsKey(node.Id)) continue;
                Vector2 startPos = _nodePositionsCache[node.Id];

                foreach (int nextId in node.NextNodeIds)
                {
                    if (!_nodePositionsCache.ContainsKey(nextId)) continue;
                    Vector2 endPos = _nodePositionsCache[nextId];

                    var lineObj = GetOrCreateLine(lineIndex++);
                    lineObj.SetActive(true);

                    // Thiết lập vị trí, chiều dài và góc xoay cho đường thẳng
                    var lineRt = lineObj.GetComponent<RectTransform>();
                    lineRt.anchorMin = new Vector2(0.5f, 1f);
                    lineRt.anchorMax = new Vector2(0.5f, 1f);
                    lineRt.pivot = new Vector2(0.5f, 0.5f);

                    Vector2 diff = endPos - startPos;
                    float dist = diff.magnitude;
                    float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

                    lineRt.anchoredPosition = startPos + diff * 0.5f;
                    lineRt.sizeDelta = new Vector2(dist, 6f);
                    lineRt.localRotation = Quaternion.Euler(0f, 0f, angle);

                    // Màu đường nối: Vàng nếu là đường đã đi / đang đứng, xám đậm nếu là tương lai
                    var lineImg = lineObj.GetComponent<Image>();
                    bool isTraversed = (currentNode != null && (node.Id == currentNode.Id || nextId == currentNode.Id));
                    lineImg.color = isTraversed ? new Color(1f, 0.85f, 0.35f, 0.95f) : new Color(0.32f, 0.36f, 0.44f, 0.7f);
                }
            }

            // 3. Vẽ từng Node phòng (Square Tile)
            int nodeIndex = 0;
            Vector2 currentPlayerPos = Vector2.zero;
            bool foundPlayer = false;

            foreach (var node in mapConfig.AllNodes)
            {
                if (!_nodePositionsCache.ContainsKey(node.Id)) continue;
                Vector2 pos = _nodePositionsCache[node.Id];

                var nodeObj = GetOrCreateNode(nodeIndex++);
                nodeObj.SetActive(true);
                var nRt = nodeObj.GetComponent<RectTransform>();
                nRt.anchorMin = new Vector2(0.5f, 1f);
                nRt.anchorMax = new Vector2(0.5f, 1f);
                nRt.pivot = new Vector2(0.5f, 0.5f);
                nRt.anchoredPosition = pos;

                bool isCurrent = (currentNode != null && node.Id == currentNode.Id);
                bool isPast = (node.FloorNumber < currentFloor);
                bool isNext = (currentNode != null && currentNode.NextNodeIds != null && currentNode.NextNodeIds.Contains(node.Id));

                SetupNodeVisual(nodeObj, node, isCurrent, isPast, isNext);

                if (isCurrent)
                {
                    currentPlayerPos = pos;
                    foundPlayer = true;
                }
            }

            // 4. Định vị Player Pin (Avatar chỉ điểm người chơi)
            if (_playerPin != null)
            {
                _playerPin.anchorMin = new Vector2(0.5f, 1f);
                _playerPin.anchorMax = new Vector2(0.5f, 1f);
                _playerPin.pivot = new Vector2(0.5f, 0.5f);
                _playerPin.gameObject.SetActive(foundPlayer);
                if (foundPlayer)
                {
                    _playerPin.SetAsLastSibling();
                    _playerPin.anchoredPosition = currentPlayerPos + new Vector2(0f, 44f);
                }
            }

            // 5. Tự động cuộn khung nhìn về vị trí phòng của người chơi
            if (_mapScrollRect != null && foundPlayer && totalHeight > 0f)
            {
                float normY = 1f - Mathf.Clamp01(Mathf.Abs(currentPlayerPos.y) / totalHeight);
                _mapScrollRect.verticalNormalizedPosition = normY;
            }
        }

        private GameObject GetOrCreateNode(int index)
        {
            if (index < _mapNodePool.Count) return _mapNodePool[index];

            var obj = new GameObject($"MapNode_{index}", typeof(RectTransform), typeof(Image));
            obj.layer = _mapGraphContainer != null ? _mapGraphContainer.gameObject.layer : 5;
            obj.transform.SetParent(_mapGraphContainer, false);
            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(84f, 84f);

            // Viền ngoài
            var outline = obj.AddComponent<Outline>();
            outline.effectDistance = new Vector2(3f, -3f);

            // Text icon loại phòng
            var txtObj = new GameObject("IconText", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.layer = obj.layer;
            txtObj.transform.SetParent(obj.transform, false);
            var tRt = txtObj.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;
            var tmp = txtObj.GetComponent<TextMeshProUGUI>();
            if (_floorBadgeText != null && _floorBadgeText.font != null)
            {
                tmp.font = _floorBadgeText.font;
            }
            tmp.fontSize = 24;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            _mapNodePool.Add(obj);
            return obj;
        }

        private GameObject GetOrCreateLine(int index)
        {
            if (index < _mapLinePool.Count) return _mapLinePool[index];

            var lineObj = new GameObject($"MapLine_{index}", typeof(RectTransform), typeof(Image));
            lineObj.layer = _mapGraphContainer != null ? _mapGraphContainer.gameObject.layer : 5;
            lineObj.transform.SetParent(_mapGraphContainer, false);
            var lineRt = lineObj.GetComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0.5f, 1f);
            lineRt.anchorMax = new Vector2(0.5f, 1f);
            lineRt.pivot = new Vector2(0.5f, 0.5f);

            var img = lineObj.GetComponent<Image>();
            img.color = new Color(0.32f, 0.36f, 0.44f, 0.7f);
            img.raycastTarget = false;

            // Đảm bảo Line nằm dưới các Node trong hierarchy
            lineObj.transform.SetAsFirstSibling();

            _mapLinePool.Add(lineObj);
            return lineObj;
        }

        private void SetupNodeVisual(GameObject nodeObj, RunNodeData node, bool isCurrent, bool isPast, bool isNext)
        {
            var bgImg = nodeObj.GetComponent<Image>();
            var outline = nodeObj.GetComponent<Outline>();
            var txt = nodeObj.GetComponentInChildren<TextMeshProUGUI>();

            // Màu nền và viền theo trạng thái
            if (isCurrent)
            {
                bgImg.color = new Color(0.22f, 0.28f, 0.40f, 1f);
                outline.effectColor = new Color(1f, 0.85f, 0.25f, 1f);
                outline.effectDistance = new Vector2(3.5f, -3.5f);
            }
            else if (isNext)
            {
                bgImg.color = new Color(0.16f, 0.20f, 0.28f, 1f);
                outline.effectColor = new Color(0f, 0.85f, 1f, 0.9f);
                outline.effectDistance = new Vector2(2.5f, -2.5f);
            }
            else if (isPast)
            {
                bgImg.color = new Color(0.10f, 0.11f, 0.14f, 0.8f);
                outline.effectColor = new Color(0.22f, 0.25f, 0.30f, 0.5f);
                outline.effectDistance = new Vector2(2f, -2f);
            }
            else
            {
                bgImg.color = new Color(0.13f, 0.15f, 0.20f, 0.95f);
                outline.effectColor = new Color(0.28f, 0.32f, 0.40f, 0.8f);
                outline.effectDistance = new Vector2(2f, -2f);
            }

            // Ký hiệu loại phòng trực quan (như đầu lâu Boss ở hình mẫu)
            if (txt != null)
            {
                switch (node.NodeType)
                {
                    case RunNodeType.BossBattle:
                        txt.fontSize = 22;
                        txt.text = "<color=#FF3333><b>BOSS</b></color>";
                        outline.effectColor = new Color(0.95f, 0.2f, 0.2f, 1f);
                        outline.effectDistance = new Vector2(3.5f, -3.5f);
                        break;
                    case RunNodeType.MiniBoss:
                        txt.fontSize = 20;
                        txt.text = "<color=#FF9100><b>MINI</b></color>";
                        outline.effectColor = new Color(1f, 0.55f, 0f, 1f);
                        break;
                    case RunNodeType.EliteBattle:
                        txt.fontSize = 20;
                        txt.text = "<color=#FFC107><b>ELITE</b></color>";
                        outline.effectColor = new Color(1f, 0.75f, 0.1f, 1f);
                        break;
                    case RunNodeType.Shop:
                        txt.fontSize = 32;
                        txt.text = "<color=#FFD54F><b>$</b></color>";
                        break;
                    case RunNodeType.RestSite:
                        txt.fontSize = 34;
                        txt.text = "<color=#69F0AE><b>+</b></color>";
                        break;
                    case RunNodeType.StarterReward:
                    case RunNodeType.BigReward:
                    case RunNodeType.BuffReward:
                        txt.fontSize = 20;
                        txt.text = "<color=#E040FB><b>BUFF</b></color>";
                        break;
                    default:
                        txt.fontSize = 26;
                        txt.text = isPast ? "<color=#90A4AE>✓</color>" : "";
                        break;
                }
            }
        }

        // =========================================================================
        // 3. PHẦN 2: OPTION (TÙY CHỌN BGM, SFX, DAMAGE NUMBERS, QUALITY)
        // =========================================================================

        private void InitializeAudioVfxListeners()
        {
            if (_bgmVolumeSlider != null)
            {
                _bgmVolumeSlider.onValueChanged.RemoveListener(HandleBGMVolumeChanged);
                _bgmVolumeSlider.onValueChanged.AddListener(HandleBGMVolumeChanged);
            }

            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.onValueChanged.RemoveListener(HandleSFXVolumeChanged);
                _sfxVolumeSlider.onValueChanged.AddListener(HandleSFXVolumeChanged);
            }

            if (_damageNumbersToggle != null)
            {
                _damageNumbersToggle.onValueChanged.RemoveListener(HandleDamageNumbersToggled);
                _damageNumbersToggle.onValueChanged.AddListener(HandleDamageNumbersToggled);
            }

            if (_highQualityToggle != null)
            {
                _highQualityToggle.onValueChanged.RemoveListener(HandleHighQualityToggled);
                _highQualityToggle.onValueChanged.AddListener(HandleHighQualityToggled);
            }

            if (_screenShakeToggle != null)
            {
                _screenShakeToggle.onValueChanged.RemoveListener(HandleScreenShakeToggled);
                _screenShakeToggle.onValueChanged.AddListener(HandleScreenShakeToggled);
            }
        }

        private void LoadSettingsPreferences()
        {
            float bgm = PlayerPrefs.GetFloat(PREF_BGM_VOL, 0.8f);
            float sfx = PlayerPrefs.GetFloat(PREF_SFX_VOL, 0.9f);
            bool dmgNumbers = PlayerPrefs.GetInt(PREF_DAMAGE_NUMBERS, 1) == 1;
            bool highQ = PlayerPrefs.GetInt(PREF_HIGH_QUALITY, 1) == 1;
            bool screenShake = PlayerPrefs.GetInt(PREF_SCREEN_SHAKE, 1) == 1;

            if (_bgmVolumeSlider != null) _bgmVolumeSlider.value = bgm;
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.value = sfx;
            if (_damageNumbersToggle != null) _damageNumbersToggle.isOn = dmgNumbers;
            if (_highQualityToggle != null) _highQualityToggle.isOn = highQ;
            if (_screenShakeToggle != null) _screenShakeToggle.isOn = screenShake;

            UpdateVolumeLabels(bgm, sfx);
        }

        private void ApplyInitialSettings()
        {
            float bgm = PlayerPrefs.GetFloat(PREF_BGM_VOL, 0.8f);
            float sfx = PlayerPrefs.GetFloat(PREF_SFX_VOL, 0.9f);
            bool dmgNumbers = PlayerPrefs.GetInt(PREF_DAMAGE_NUMBERS, 1) == 1;
            bool highQ = PlayerPrefs.GetInt(PREF_HIGH_QUALITY, 1) == 1;
            bool screenShake = PlayerPrefs.GetInt(PREF_SCREEN_SHAKE, 1) == 1;

            OnBGMVolumeChanged?.Invoke(bgm);
            OnSFXVolumeChanged?.Invoke(sfx);
            OnDamageNumbersToggled?.Invoke(dmgNumbers);
            OnHighQualityToggled?.Invoke(highQ);
            OnScreenShakeToggled?.Invoke(screenShake);
        }

        private void HandleBGMVolumeChanged(float val)
        {
            PlayerPrefs.SetFloat(PREF_BGM_VOL, val);
            if (_bgmVolumeText != null) _bgmVolumeText.text = $"{Mathf.RoundToInt(val * 100)}%";
            OnBGMVolumeChanged?.Invoke(val);
        }

        private void HandleSFXVolumeChanged(float val)
        {
            PlayerPrefs.SetFloat(PREF_SFX_VOL, val);
            if (_sfxVolumeText != null) _sfxVolumeText.text = $"{Mathf.RoundToInt(val * 100)}%";
            OnSFXVolumeChanged?.Invoke(val);
        }

        private void HandleDamageNumbersToggled(bool isOn)
        {
            PlayerPrefs.SetInt(PREF_DAMAGE_NUMBERS, isOn ? 1 : 0);
            OnDamageNumbersToggled?.Invoke(isOn);
        }

        private void HandleHighQualityToggled(bool isOn)
        {
            PlayerPrefs.SetInt(PREF_HIGH_QUALITY, isOn ? 1 : 0);
            OnHighQualityToggled?.Invoke(isOn);
        }

        private void HandleScreenShakeToggled(bool isOn)
        {
            PlayerPrefs.SetInt(PREF_SCREEN_SHAKE, isOn ? 1 : 0);
            OnScreenShakeToggled?.Invoke(isOn);
        }

        private void UpdateVolumeLabels(float bgm, float sfx)
        {
            if (_bgmVolumeText != null) _bgmVolumeText.text = $"{Mathf.RoundToInt(bgm * 100)}%";
            if (_sfxVolumeText != null) _sfxVolumeText.text = $"{Mathf.RoundToInt(sfx * 100)}%";
        }

        private void HandleRetreatClicked()
        {
            Debug.Log("[Settings] Người chơi bấm Rút Lui.");
            OnAbandonRunRequested?.Invoke();
            Close();
        }

        // =========================================================================
        // 4. PHẦN 3: SÁT THƯƠNG GÂY CHO QUÁI VẬT (THANH Ô VUÔNG TỶ LỆ SÁT THƯƠNG)
        // =========================================================================

        private void RefreshDamageBreakdownBar()
        {
            if (_damageEntriesContainer == null) return;

            // 1. Tính toán DPS người chơi
            var pStats = PlayerStats.Instance;
            var weaponCtrl = FindAnyObjectByType<WeaponController>();

            float playerDamage = 10f;
            float playerFireRate = 1.0f;
            int playerProjectiles = 1;

            if (weaponCtrl != null && weaponCtrl.WeaponData != null)
            {
                playerDamage = weaponCtrl.WeaponData.Damage + (pStats != null ? pStats.DamageBonus : 0);
                playerFireRate = weaponCtrl.WeaponData.FireRate * (pStats != null ? pStats.FireRateModifier : 1f);
                playerProjectiles = 1 + (pStats != null ? pStats.ProjectilesBonus : 0);
            }
            else if (pStats != null)
            {
                playerDamage = 10 + pStats.DamageBonus;
                playerFireRate = pStats.FireRateModifier;
                playerProjectiles = 1 + pStats.ProjectilesBonus;
            }

            float playerDPS = playerDamage * playerFireRate * playerProjectiles;

            // 2. Tính toán DPS của từng nhóm lính
            var minionMgr = MinionManager.Instance;
            var groups = minionMgr != null ? minionMgr.GetRosterGroups() : null;
            int groupCount = groups != null ? groups.Count : 0;

            float totalTeamDPS = playerDPS;
            float[] minionDPSArray = new float[groupCount];

            for (int i = 0; i < groupCount; i++)
            {
                var g = groups[i];
                if (g.Data != null && g.AliveCount > 0)
                {
                    float dps = g.Data.AttackDamage * g.Data.AttackSpeed * g.AliveCount;
                    minionDPSArray[i] = dps;
                    totalTeamDPS += dps;
                }
            }

            if (totalTeamDPS <= 0.001f) totalTeamDPS = 1f;

            // Tạm ẩn các card trong pool
            foreach (var card in _damageCardPool) if (card != null) card.gameObject.SetActive(false);

            int cardIndex = 0;

            // 3. Card 1: Người chơi (Player Hero Card)
            float playerPercent = (playerDPS / totalTeamDPS) * 100f;
            var playerCard = GetOrCreateDamageCard(cardIndex++);
            playerCard.gameObject.SetActive(true);

            Sprite playerSprite = null;
            if (pStats != null && pStats.PlayerData != null && pStats.PlayerData.Portrait != null)
            {
                playerSprite = pStats.PlayerData.Portrait;
            }
            else
            {
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    var sr = playerObj.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null) playerSprite = sr.sprite;
                }
            }

            string playerTooltip = $"<b>ANH HÙNG (PLAYER)</b>\nSát thương: {playerDamage}\nTốc bắn: {playerFireRate:0.1f} đòn/s\nSố tia: {playerProjectiles}\nDPS: {playerDPS:0.1f}";
            playerCard.Setup(playerSprite, $"{playerPercent:0.0}%", true, 1, new Color(1f, 0.85f, 0.35f, 1f), playerTooltip, ShowStatTooltip);

            // 4. Các Card tiếp theo: Từng loại lính
            for (int i = 0; i < groupCount; i++)
            {
                var g = groups[i];
                if (g.Data == null) continue;

                float minionPercent = (minionDPSArray[i] / totalTeamDPS) * 100f;
                var card = GetOrCreateDamageCard(cardIndex++);
                card.gameObject.SetActive(true);

                Sprite mSprite = g.Data.Icon;
                if (mSprite == null && g.Data.Prefab != null)
                {
                    var sr = g.Data.Prefab.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null) mSprite = sr.sprite;
                }

                string methodStr = g.Data.AttackMethod == AttackMethod.RangedProjectile ? "Bắn tỉa" : (g.Data.AttackMethod == AttackMethod.AoE ? "Sát thương lan" : "Cận chiến");
                string minionTooltip = $"<b>{g.Data.MinionName} (x{g.TotalCount})</b>\nSát thương: {g.Data.AttackDamage}\nTốc đánh: {g.Data.AttackSpeed:0.1f}/s\nKiểu đánh: {methodStr}\nDPS nhóm: {minionDPSArray[i]:0.1f}";

                Color tierColor = (g.Data.Tier == MinionTier.Tier2) ? new Color(0.3f, 0.85f, 0.45f) : ((g.Data.Tier == MinionTier.Tier3) ? new Color(0.85f, 0.45f, 1f) : new Color(0.7f, 0.75f, 0.85f));
                card.Setup(mSprite, $"{minionPercent:0.0}%", false, g.TotalCount, tierColor, minionTooltip, ShowStatTooltip);
            }
        }

        private DamageEntryCard GetOrCreateDamageCard(int index)
        {
            if (index < _damageCardPool.Count) return _damageCardPool[index];

            var cardObj = new GameObject($"DamageCard_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
            cardObj.transform.SetParent(_damageEntriesContainer, false);
            var rt = cardObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(130f, 175f);

            var bgImg = cardObj.GetComponent<Image>();
            bgImg.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

            var outline = cardObj.AddComponent<Outline>();
            outline.effectDistance = new Vector2(2.5f, -2.5f);

            // Icon chân dung
            var iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(cardObj.transform, false);
            var iRt = iconObj.GetComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0.5f, 1f);
            iRt.anchorMax = new Vector2(0.5f, 1f);
            iRt.pivot = new Vector2(0.5f, 1f);
            iRt.anchoredPosition = new Vector2(0f, -8f);
            iRt.sizeDelta = new Vector2(100f, 100f);
            var iconImg = iconObj.GetComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // Text số lượng lính (Badge x2)
            var countObj = new GameObject("CountBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
            countObj.transform.SetParent(cardObj.transform, false);
            var cRt = countObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(1f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(1f, 1f);
            cRt.anchoredPosition = new Vector2(-4f, -4f);
            cRt.sizeDelta = new Vector2(46f, 26f);
            var countTmp = countObj.GetComponent<TextMeshProUGUI>();
            countTmp.fontSize = 18;
            countTmp.fontStyle = FontStyles.Bold;
            countTmp.alignment = TextAlignmentOptions.TopRight;
            countTmp.color = Color.white;
            countTmp.raycastTarget = false;

            // Text phần trăm sát thương bên dưới
            var pctObj = new GameObject("PercentText", typeof(RectTransform), typeof(TextMeshProUGUI));
            pctObj.transform.SetParent(cardObj.transform, false);
            var pRt = pctObj.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0f, 0f);
            pRt.anchorMax = new Vector2(1f, 0f);
            pRt.pivot = new Vector2(0.5f, 0f);
            pRt.anchoredPosition = new Vector2(0f, 6f);
            pRt.sizeDelta = new Vector2(0f, 32f);
            var pctTmp = pctObj.GetComponent<TextMeshProUGUI>();
            pctTmp.fontSize = 24;
            pctTmp.fontStyle = FontStyles.Bold;
            pctTmp.alignment = TextAlignmentOptions.Center;
            pctTmp.color = new Color(0.9f, 0.92f, 0.95f, 1f);
            pctTmp.raycastTarget = false;

            var entryCard = cardObj.AddComponent<DamageEntryCard>();
            entryCard.Init(bgImg, outline, iconImg, countTmp, pctTmp);

            _damageCardPool.Add(entryCard);
            return entryCard;
        }

        private void ShowStatTooltip(string tooltipContent)
        {
            if (_statTooltipBox == null || _statTooltipText == null) return;
            _statTooltipText.text = tooltipContent;
            _statTooltipBox.SetActive(true);
        }

        // =========================================================================
        // 5. TỰ ĐỘNG BẮT LINK COMPONENT (AUTO-FIND REFERENCES)
        // =========================================================================

        private void Reset()
        {
            AutoFindReferences();
        }

        public void AutoFindReferences()
        {
#if UNITY_EDITOR
            if (_fallbackMapConfig == null)
            {
                _fallbackMapConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<RunMapConfigSO>("Assets/_Project/ScriptableObjects/Dungeon/RunMap_13Floors.asset");
            }
#endif
            if (_panelRoot == null)
            {
                var pr = transform.Find("PanelRoot");
                _panelRoot = pr != null ? pr.gameObject : gameObject;
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>() ?? _panelRoot?.GetComponent<CanvasGroup>();
            }

            if (_openButton == null)
            {
                var ob = GameObject.Find("Btn_Settings") ?? GameObject.Find("SettingsButton") ?? GameObject.Find("Btn_Menu");
                if (ob != null) _openButton = ob.GetComponent<Button>();
            }

            if (_closeButton == null)
            {
                _closeButton = FindChildComponent<Button>("CloseButton");
            }

            if (_resumeButton == null)
            {
                _resumeButton = FindChildComponent<Button>("ResumeButton");
            }

            if (_retreatButton == null)
            {
                _retreatButton = FindChildComponent<Button>("RetreatButton");
            }

            if (_backdropButton == null)
            {
                _backdropButton = FindChildComponent<Button>("Backdrop");
            }

            // Phần 1: Map
            if (_mapSection == null)
            {
                var ms = FindElementTransform("MapSection");
                if (ms != null) _mapSection = ms.gameObject;
            }
            if (_mapScrollRect == null) _mapScrollRect = FindChildComponent<ScrollRect>("MapScrollView");
            if (_mapGraphContainer == null)
            {
                var mg = FindElementTransform("MapGraphContent");
                _mapGraphContainer = mg as RectTransform;
            }
            if (_mapGraphContainer != null)
            {
                _mapNodePool.Clear();
                _mapLinePool.Clear();
                for (int i = 0; i < _mapGraphContainer.childCount; i++)
                {
                    var child = _mapGraphContainer.GetChild(i).gameObject;
                    if (child.name.StartsWith("MapNode_")) _mapNodePool.Add(child);
                    else if (child.name.StartsWith("MapLine_")) _mapLinePool.Add(child);
                }
            }
            if (_floorBadgeText == null) _floorBadgeText = FindChildComponent<TMP_Text>("FloorBadgeText");
            if (_playerPin == null)
            {
                var pin = FindElementTransform("PlayerPin");
                _playerPin = pin as RectTransform;
            }

            // Phần 2: Options
            if (_bgmVolumeSlider == null) _bgmVolumeSlider = FindChildComponent<Slider>("BGMVolumeSlider");
            if (_bgmVolumeText == null) _bgmVolumeText = FindChildComponent<TMP_Text>("BGMVolumeText");
            if (_sfxVolumeSlider == null) _sfxVolumeSlider = FindChildComponent<Slider>("SFXVolumeSlider");
            if (_sfxVolumeText == null) _sfxVolumeText = FindChildComponent<TMP_Text>("SFXVolumeText");
            if (_damageNumbersToggle == null) _damageNumbersToggle = FindChildComponent<Toggle>("DamageNumbersToggle");
            if (_highQualityToggle == null) _highQualityToggle = FindChildComponent<Toggle>("HighQualityToggle");
            if (_screenShakeToggle == null) _screenShakeToggle = FindChildComponent<Toggle>("ScreenShakeToggle");

            // Phần 3: Damage Bar
            if (_damageEntriesContainer == null)
            {
                _damageEntriesContainer = FindElementTransform("DamageEntriesContainer");
            }
            if (_damageEntriesContainer != null)
            {
                _damageCardPool.Clear();
                for (int i = 0; i < _damageEntriesContainer.childCount; i++)
                {
                    var card = _damageEntriesContainer.GetChild(i).GetComponent<DamageEntryCard>();
                    if (card != null) _damageCardPool.Add(card);
                }
            }
            if (_damageBarTitleText == null) _damageBarTitleText = FindChildComponent<TMP_Text>("DamageBarTitleText");

            var tipBox = FindElementTransform("StatTooltipBox");
            if (tipBox != null)
            {
                _statTooltipBox = tipBox.gameObject;
                _statTooltipText = tipBox.GetComponentInChildren<TMP_Text>(true);
            }
        }

        private Transform FindElementTransform(string childName)
        {
            var found = transform.Find(childName);
            if (found != null) return found;

            var all = GetComponentsInChildren<Transform>(true);
            foreach (var item in all)
            {
                if (item.gameObject.name.Equals(childName, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }
            return null;
        }

        private T FindChildComponent<T>(string childName) where T : Component
        {
            var found = transform.Find(childName);
            if (found != null && found.TryGetComponent<T>(out var comp)) return comp;

            var all = GetComponentsInChildren<T>(true);
            foreach (var item in all)
            {
                if (item.gameObject.name.Equals(childName, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }
            return null;
        }
    }
}

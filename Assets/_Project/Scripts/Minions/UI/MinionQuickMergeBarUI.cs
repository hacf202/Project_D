using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Project_D.Minions.UI
{
    public class MinionQuickMergeBarUI : MonoBehaviour
    {
        public static MinionQuickMergeBarUI Instance { get; private set; }

        [Header("Structure References")]
        [Tooltip("Thanh cột dọc xếp các icon lính có thể hợp nhất từ dưới lên trên")]
        [SerializeField] private RectTransform _slotsContainer;
        [Tooltip("Nút mở danh sách toàn bộ công thức merge (Sổ Công Thức)")]
        [SerializeField] private Button _btnOpenRecipes;
        [Tooltip("Bảng danh sách công thức chi tiết (Modal Dialog)")]
        [SerializeField] private MinionMergePanelUI _mergePanelModal;
        [Tooltip("Hình nền của toàn bộ thanh merge")]
        [SerializeField] private Image _barBackground;

        [Header("Slot Configuration")]
        [Tooltip("Kích thước icon slot (0 = Tự động lấy khớp theo chiều rộng của MinionQuickMergeBar)")]
        [SerializeField] private float _slotSizeOverride = 0f;

        [Header("Legacy / Optional (Nút Hợp Nhất Nhanh cũ - có thể để trống hoặc xóa)")]
        [SerializeField] private Button _btnQuickMerge;
        [SerializeField] private GameObject _quickMergeBadge;
        [SerializeField] private TMP_Text _quickMergeBadgeText;

        [Header("Animation")]
        [SerializeField] private bool _enablePulse = true;
        [SerializeField] private float _pulseSpeed = 4f;

        private readonly List<MinionQuickMergeSlotUI> _activeSlots = new List<MinionQuickMergeSlotUI>();
        private int _readyToMergeCount = 0;
        private Vector3 _mergeBtnOriginalScale = Vector3.one;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_barBackground == null)
            {
                _barBackground = GetComponent<Image>();
            }

            CollectExistingSlots();
            SetupButtonListeners();
        }

        private void CollectExistingSlots()
        {
            if (_slotsContainer != null && _activeSlots.Count == 0)
            {
                var existing = _slotsContainer.GetComponentsInChildren<MinionQuickMergeSlotUI>(true);
                for (int i = 0; i < existing.Length; i++)
                {
                    _activeSlots.Add(existing[i]);
                }
            }
        }

        private void SetupButtonListeners()
        {
            if (_btnOpenRecipes != null)
            {
                _btnOpenRecipes.onClick.RemoveListener(OnOpenRecipesClicked);
                _btnOpenRecipes.onClick.AddListener(OnOpenRecipesClicked);
            }

            if (_btnQuickMerge != null)
            {
                _btnQuickMerge.onClick.RemoveListener(OnQuickMergeButtonClicked);
                _btnQuickMerge.onClick.AddListener(OnQuickMergeButtonClicked);
                _mergeBtnOriginalScale = _btnQuickMerge.transform.localScale;
            }
        }

        private void Start()
        {
            SetupButtonListeners();

            if (_mergePanelModal == null)
            {
                _mergePanelModal = Object.FindAnyObjectByType<MinionMergePanelUI>(FindObjectsInactive.Include);
                if (_mergePanelModal == null)
                {
                    var canvas = GetComponentInParent<Canvas>();
                    if (canvas != null)
                    {
                        _mergePanelModal = CreateDynamicMergePanelModal(canvas.transform);
                    }
                }
            }

            // Đảm bảo modal luôn Active để CanvasGroup điều khiển hiển thị mượt mà, tránh bẫy lifecycle của Awake
            if (_mergePanelModal != null && !_mergePanelModal.gameObject.activeSelf)
            {
                _mergePanelModal.gameObject.SetActive(true);
            }

            RefreshUI();
        }

        private void OnEnable()
        {
            MinionManager.OnAvailableMergesChanged += RefreshUI;
            MinionManager.OnMinionSummoned += HandleMinionEvent;
            MinionManager.OnMinionMerged += HandleMinionEvent;
            RefreshUI();
        }

        private void OnDisable()
        {
            MinionManager.OnAvailableMergesChanged -= RefreshUI;
            MinionManager.OnMinionSummoned -= HandleMinionEvent;
            MinionManager.OnMinionMerged -= HandleMinionEvent;
        }

        private void HandleMinionEvent(MinionDataSO data)
        {
            RefreshUI();
        }

        private void Update()
        {
            if (_enablePulse && _btnQuickMerge != null && _readyToMergeCount > 0)
            {
                float scale = 1f + Mathf.Sin(Time.unscaledTime * _pulseSpeed) * 0.08f;
                _btnQuickMerge.transform.localScale = _mergeBtnOriginalScale * scale;
            }
            else if (_btnQuickMerge != null)
            {
                _btnQuickMerge.transform.localScale = _mergeBtnOriginalScale;
            }
        }

        // =========================================================================
        // CẬP NHẬT CỘT DỌC VÀ CÁC NÚT TƯƠNG TÁC
        // =========================================================================

        public void RefreshUI()
        {
            if (MinionManager.Instance == null) return;

            var availableMerges = MinionManager.Instance.GetAvailableMerges();

            // 1. Lọc ra các công thức ĐỦ ĐIỀU KIỆN hợp nhất (CanMerge == true)
            var readyMerges = new List<MergeAvailableInfo>();
            if (availableMerges != null)
            {
                for (int i = 0; i < availableMerges.Count; i++)
                {
                    var m = availableMerges[i];
                    if (m != null && m.CanMerge && !m.IsLockedBySpecialCondition)
                    {
                        readyMerges.Add(m);
                    }
                }
            }

            _readyToMergeCount = readyMerges.Count;
            bool hasReady = _readyToMergeCount > 0;

            // 2. Tắt các slot cũ trong cột
            for (int i = 0; i < _activeSlots.Count; i++)
            {
                if (_activeSlots[i] != null) _activeSlots[i].gameObject.SetActive(false);
            }

            // 3. SlotsColumn CHỈ hiện icon của các con lính ĐỦ ĐIỀU KIỆN hợp nhất
            if (_slotsContainer != null && hasReady)
            {
                float slotSize = GetSlotTargetSize();

                for (int i = 0; i < readyMerges.Count; i++)
                {
                    MinionQuickMergeSlotUI slotUI;
                    if (i < _activeSlots.Count)
                    {
                        slotUI = _activeSlots[i];
                        slotUI.gameObject.SetActive(true);
                    }
                    else
                    {
                        slotUI = MinionQuickMergeSlotUI.CreateDynamicSlot(_slotsContainer, slotSize);
                        _activeSlots.Add(slotUI);
                    }

                    slotUI.SetSize(slotSize);
                    slotUI.Setup(readyMerges[i], OnExecuteMergeFromSlot);
                }
            }

            // 4. Hỗ trợ an toàn nếu còn giữ nút QuickMerge cũ trong Scene
            if (_btnQuickMerge != null)
            {
                _btnQuickMerge.gameObject.SetActive(hasReady);
                _btnQuickMerge.interactable = hasReady;
            }

            if (_quickMergeBadge != null)
            {
                _quickMergeBadge.SetActive(hasReady);
            }

            if (_quickMergeBadgeText != null)
            {
                _quickMergeBadgeText.text = _readyToMergeCount.ToString();
            }

            // 5. Ẩn/Hiện nền thanh bar: Khi không có lính sẵn sàng, ẩn nền để không chắn tầm nhìn sàn dungeon
            if (_barBackground != null)
            {
                _barBackground.enabled = hasReady;
            }
        }

        // Tính toán kích thước icon slot tự động khớp với chiều rộng thanh MinionQuickMergeBar
        public float GetSlotTargetSize()
        {
            if (_slotSizeOverride > 0f) return _slotSizeOverride;

            if (_slotsContainer != null && _slotsContainer.rect.width > 0f)
            {
                return _slotsContainer.rect.width;
            }

            var rt = transform as RectTransform;
            if (rt != null && rt.rect.width > 0f)
            {
                return rt.rect.width;
            }

            return 80f; // Mặc định chuẩn chiều rộng thanh QuickMergeBar
        }

        // Khi người chơi ấn chọn trực tiếp vào icon lính trong cột để hợp nhất ngay lập tức
        private void OnExecuteMergeFromSlot(MergeRecipeSO recipe)
        {
            if (MinionManager.Instance != null && recipe != null)
            {
                bool success = MinionManager.Instance.ExecuteMerge(recipe);
                if (success)
                {
                    RefreshUI();
                }
            }
        }

        // Nút Hợp Nhất Nhanh cũ (nếu còn dùng)
        private void OnQuickMergeButtonClicked()
        {
            if (MinionManager.Instance != null)
            {
                bool success = MinionManager.Instance.QuickMergeFirstAvailable();
                if (success)
                {
                    RefreshUI();
                }
            }
        }

        // Khi người chơi ấn nút mở danh sách toàn bộ công thức
        private void OnOpenRecipesClicked()
        {
            if (_mergePanelModal != null)
            {
                _mergePanelModal.Open();
            }
        }

        // =========================================================================
        // TỰ ĐỘNG KHỞI TẠO NẾU SCENE CHƯA CÓ SẴN (ZERO MANUAL SETUP)
        // =========================================================================

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureMergeBarInScene()
        {
            if (Instance != null) return;

            var existing = Object.FindAnyObjectByType<MinionQuickMergeBarUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return;
            }

            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            CreateDynamicMergeBar(canvas.transform);
        }

        public static MinionQuickMergeBarUI CreateDynamicMergeBar(Transform canvasTransform)
        {
            // 1. Root của Thanh Merge (Căn mép phải màn hình, kích thước 80x660)
            var root = new GameObject("MinionQuickMergeBar", typeof(RectTransform));
            root.transform.SetParent(canvasTransform, false);

            var rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(1f, 0.5f);
            rootRt.anchorMax = new Vector2(1f, 0.5f);
            rootRt.pivot = new Vector2(1f, 0.5f);
            rootRt.anchoredPosition = new Vector2(-20f, 0f);
            rootRt.sizeDelta = new Vector2(80f, 660f);

            var rootBg = root.AddComponent<Image>();
            rootBg.color = new Color(0.08f, 0.09f, 0.15f, 0.65f);

            // 2. Cột chứa các slot lính (1 CỘT TỪ DƯỚI LÊN TRÊN, CĂN GIỮA Ô 80PX)
            var columnObj = new GameObject("SlotsColumn", typeof(RectTransform), typeof(VerticalLayoutGroup));
            columnObj.transform.SetParent(root.transform, false);

            var colRt = columnObj.GetComponent<RectTransform>();
            colRt.anchorMin = Vector2.zero;
            colRt.anchorMax = Vector2.one;
            colRt.pivot = new Vector2(1f, 0f);
            colRt.anchoredPosition = Vector2.zero;
            colRt.sizeDelta = Vector2.zero;

            var vlg = columnObj.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.LowerCenter;
            vlg.spacing = 10f;
            vlg.reverseArrangement = true;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            // Gắn controller
            var barUI = root.AddComponent<MinionQuickMergeBarUI>();
            barUI._slotsContainer = colRt;
            barUI._barBackground = rootBg;
            barUI._mergePanelModal = CreateDynamicMergePanelModal(canvasTransform);

            barUI.RefreshUI();
            return barUI;
        }

        public static MinionMergePanelUI CreateDynamicMergePanelModal(Transform canvasTransform)
        {
            var existing = Object.FindAnyObjectByType<MinionMergePanelUI>(FindObjectsInactive.Include);
            if (existing != null) return existing;

            var modalObj = new GameObject("MinionMergePanelModal", typeof(RectTransform));
            modalObj.transform.SetParent(canvasTransform, false);

            var mRt = modalObj.GetComponent<RectTransform>();
            mRt.anchorMin = Vector2.zero;
            mRt.anchorMax = Vector2.one;
            mRt.sizeDelta = Vector2.zero;

            var darkBg = new GameObject("DarkOverlay", typeof(RectTransform), typeof(Image));
            darkBg.transform.SetParent(modalObj.transform, false);
            var dbRt = darkBg.GetComponent<RectTransform>();
            dbRt.anchorMin = Vector2.zero;
            dbRt.anchorMax = Vector2.one;
            dbRt.sizeDelta = Vector2.zero;
            var dbImg = darkBg.GetComponent<Image>();
            dbImg.color = new Color(0f, 0f, 0f, 0.75f);

            var boxObj = new GameObject("DialogBox", typeof(RectTransform), typeof(Image));
            boxObj.transform.SetParent(modalObj.transform, false);
            var boxRt = boxObj.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.5f, 0.5f);
            boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.sizeDelta = new Vector2(880f, 1200f);
            var boxImg = boxObj.GetComponent<Image>();
            boxImg.color = new Color(0.12f, 0.12f, 0.18f, 0.98f);

            var titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(boxObj.transform, false);
            var tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.anchoredPosition = new Vector2(0f, -24f);
            tRt.sizeDelta = new Vector2(-40f, 50f);
            var tTMP = titleObj.GetComponent<TextMeshProUGUI>();
            tTMP.fontSize = 32;
            tTMP.fontStyle = FontStyles.Bold;
            tTMP.alignment = TextAlignmentOptions.Center;
            tTMP.color = new Color(1f, 0.85f, 0.2f);
            tTMP.text = "CÔNG THỨC HỢP NHẤT";

            var scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollObj.transform.SetParent(boxObj.transform, false);
            var sRt = scrollObj.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0f, 0f);
            sRt.anchorMax = new Vector2(1f, 1f);
            sRt.anchoredPosition = new Vector2(0f, -20f);
            sRt.sizeDelta = new Vector2(-40f, -170f);
            var scrollRect = scrollObj.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            var viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObj.transform.SetParent(scrollObj.transform, false);
            var vRt = viewportObj.GetComponent<RectTransform>();
            vRt.anchorMin = Vector2.zero;
            vRt.anchorMax = Vector2.one;
            vRt.sizeDelta = Vector2.zero;
            viewportObj.GetComponent<Image>().color = Color.white;
            viewportObj.GetComponent<Mask>().showMaskGraphic = false;

            var contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObj.transform.SetParent(viewportObj.transform, false);
            var cRt = contentObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.sizeDelta = new Vector2(0f, 0f);

            var cvlg = contentObj.GetComponent<VerticalLayoutGroup>();
            cvlg.spacing = 12f;
            cvlg.childControlWidth = true;
            cvlg.childControlHeight = false;
            cvlg.childForceExpandWidth = true;

            var csf = contentObj.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = cRt;
            scrollRect.viewport = vRt;

            var closeBtnObj = new GameObject("Btn_Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtnObj.transform.SetParent(boxObj.transform, false);
            var cbRt = closeBtnObj.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0.5f, 0f);
            cbRt.anchorMax = new Vector2(0.5f, 0f);
            cbRt.pivot = new Vector2(0.5f, 0f);
            cbRt.anchoredPosition = new Vector2(0f, 20f);
            cbRt.sizeDelta = new Vector2(240f, 60f);
            closeBtnObj.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.38f, 1f);

            var cbTxtObj = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
            cbTxtObj.transform.SetParent(closeBtnObj.transform, false);
            var cbtRt = cbTxtObj.GetComponent<RectTransform>();
            cbtRt.anchorMin = Vector2.zero;
            cbtRt.anchorMax = Vector2.one;
            cbtRt.sizeDelta = Vector2.zero;
            var cbTMP = cbTxtObj.GetComponent<TextMeshProUGUI>();
            cbTMP.fontSize = 22;
            cbTMP.fontStyle = FontStyles.Bold;
            cbTMP.alignment = TextAlignmentOptions.Center;
            cbTMP.color = Color.white;
            cbTMP.text = "ĐÓNG";

            var cg = modalObj.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;

            var panelUI = modalObj.AddComponent<MinionMergePanelUI>();
            panelUI.SetupReferences(modalObj, cRt, closeBtnObj.GetComponent<Button>(), cg);

            modalObj.SetActive(true);
            return panelUI;
        }
    }
}

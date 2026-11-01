using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project_D.Minions.UI
{
    public class MinionRosterBarUI : MonoBehaviour
    {
        public static MinionRosterBarUI Instance { get; private set; }

        [Header("UI Structure References")]
        [Tooltip("Thanh cuộn ngang chứa danh sách các thẻ lính")]
        [SerializeField] private ScrollRect _scrollRect;
        [Tooltip("Vùng chứa (Content) của ScrollView")]
        [SerializeField] private RectTransform _contentContainer;
        [Tooltip("Prefab khuôn mẫu của 1 thẻ lính (Tùy chọn. Nếu để trống, script sẽ tự lấy chính thẻ mẫu MinionCard có sẵn trong Content)")]
        [SerializeField] private GameObject _cardPrefab;
        [Tooltip("Hình nền của toàn bộ thanh cuộn")]
        [SerializeField] private Image _barBackground;

        [Header("Display Settings")]
        [SerializeField] private bool _hideWhenEmpty = false;

        private readonly List<MinionRosterCardUI> _cardPool = new List<MinionRosterCardUI>();
        private GameObject _templateCardObject;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            AutoFindBarReferences();
            DetectTemplateCard();
        }

        private void AutoFindBarReferences()
        {
            if (_barBackground == null)
            {
                _barBackground = GetComponent<Image>();
            }

            if (_scrollRect == null)
            {
                _scrollRect = GetComponentInChildren<ScrollRect>(true);
            }

            if (_contentContainer == null)
            {
                if (_scrollRect != null && _scrollRect.content != null)
                {
                    _contentContainer = _scrollRect.content;
                }
                else
                {
                    var contentTransform = transform.Find("ScrollView/Viewport/Content");
                    if (contentTransform != null)
                    {
                        _contentContainer = contentTransform as RectTransform;
                    }
                }
            }
        }

        private void DetectTemplateCard()
        {
            // 1. Tự động nạp Prefab chuẩn nếu chưa gán
            if (_cardPrefab == null)
            {
#if UNITY_EDITOR
                _cardPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/MinionCard.prefab");
#endif
                if (_cardPrefab == null)
                {
                    _cardPrefab = Resources.Load<GameObject>("UI/MinionCard");
                }
            }

            if (_cardPrefab != null)
            {
                _templateCardObject = _cardPrefab;
            }

            // 2. Dọn dẹp hoặc đồng bộ các thẻ mẫu có sẵn trong Content
            if (_contentContainer != null)
            {
                var existingCards = _contentContainer.GetComponentsInChildren<MinionRosterCardUI>(true);
                if (existingCards.Length > 0)
                {
                    if (_templateCardObject == null)
                    {
                        _templateCardObject = existingCards[0].gameObject;
                    }

                    // Lưu toàn bộ thẻ đã có sẵn trong Content vào Pool và kích hoạt AutoFindReferences tự chữa lành
                    for (int i = 0; i < existingCards.Length; i++)
                    {
                        existingCards[i].AutoFindReferences();
                        if (!_cardPool.Contains(existingCards[i]))
                        {
                            _cardPool.Add(existingCards[i]);
                        }
                    }
                }
            }
        }

        private void Start()
        {
            RefreshRoster();
        }

        private void OnEnable()
        {
            MinionManager.OnMinionRosterChanged += RefreshRoster;
            MinionManager.OnMinionSummoned += HandleMinionEvent;
            MinionManager.OnMinionMerged += HandleMinionEvent;
            RefreshRoster();
        }

        private void OnDisable()
        {
            MinionManager.OnMinionRosterChanged -= RefreshRoster;
            MinionManager.OnMinionSummoned -= HandleMinionEvent;
            MinionManager.OnMinionMerged -= HandleMinionEvent;
        }

        private void HandleMinionEvent(MinionDataSO data)
        {
            RefreshRoster();
        }

        // =========================================================================
        // CẬP NHẬT DANH SÁCH THẺ LÍNH TRÊN THANH CUỘN NGANG
        // =========================================================================

        public void RefreshRoster()
        {
            if (MinionManager.Instance == null) return;
            if (_contentContainer == null)
            {
                AutoFindBarReferences();
                if (_contentContainer == null) return;
            }

            if (_templateCardObject == null)
            {
                DetectTemplateCard();
            }

            var groups = MinionManager.Instance.GetRosterGroups();
            int count = groups != null ? groups.Count : 0;

            if (_hideWhenEmpty && _barBackground != null)
            {
                _barBackground.enabled = (count > 0);
            }

            // 1. Tắt toàn bộ thẻ cũ trong Pool
            for (int i = 0; i < _cardPool.Count; i++)
            {
                if (_cardPool[i] != null)
                {
                    _cardPool[i].gameObject.SetActive(false);
                }
            }

            // 2. Cập nhật hoặc sinh mới từng thẻ lính theo từng loại
            // Hoàn toàn giữ nguyên kích thước, bố cục và thông số của Prefab/Thẻ mẫu đã tạo
            for (int i = 0; i < count; i++)
            {
                var group = groups[i];
                MinionRosterCardUI cardUI;

                if (i < _cardPool.Count && _cardPool[i] != null)
                {
                    cardUI = _cardPool[i];
                    cardUI.gameObject.SetActive(true);
                }
                else
                {
                    if (_templateCardObject != null)
                    {
                        var go = Instantiate(_templateCardObject, _contentContainer);
                        cardUI = go.GetComponent<MinionRosterCardUI>();
                        if (cardUI == null)
                        {
                            cardUI = go.AddComponent<MinionRosterCardUI>();
                        }
                    }
                    else
                    {
                        // Fallback chỉ khi người dùng hoàn toàn không tạo thẻ mẫu nào
                        cardUI = MinionRosterCardUI.CreateDynamicCard(_contentContainer);
                    }

                    _cardPool.Add(cardUI);
                }

                cardUI.AutoFindReferences();
                cardUI.Setup(group.Data, group.AliveCount, group.TotalCount);
            }
        }

        // =========================================================================
        // TỰ ĐỘNG NHẬN DIỆN HOẶC KHỞI TẠO NẾU SCENE CHƯA CÓ
        // =========================================================================

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureRosterBarInScene()
        {
            if (Instance != null) return;

            // 1. Kiểm tra xem Component đã tồn tại trong Scene chưa
            var existing = Object.FindAnyObjectByType<MinionRosterBarUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return;
            }

            // 2. Nếu người dùng đã tự dựng GameObject "MinionRosterBar" trong Canvas mà chưa gắn script
            var existingBarObj = GameObject.Find("MinionRosterBar");
            if (existingBarObj != null)
            {
                var barUI = existingBarObj.AddComponent<MinionRosterBarUI>();
#if UNITY_EDITOR
                barUI._cardPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/MinionCard.prefab");
#endif
                return;
            }

            // 3. Nếu chưa có bất kỳ GameObject nào trong Scene thì mới tạo dynamic
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            CreateDynamicRosterBar(canvas.transform);
        }

        public static MinionRosterBarUI CreateDynamicRosterBar(Transform canvasTransform)
        {
            var barObj = new GameObject("MinionRosterBar", typeof(RectTransform), typeof(Image));
            barObj.transform.SetParent(canvasTransform, false);

            var barRt = barObj.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 0f);
            barRt.anchorMax = new Vector2(1f, 0f);
            barRt.pivot = new Vector2(0.5f, 0f);
            barRt.anchoredPosition = new Vector2(0f, 10f);
            barRt.sizeDelta = new Vector2(0f, 260f);

            var bgImg = barObj.GetComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.4f);

            var scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollObj.transform.SetParent(barObj.transform, false);
            var sRt = scrollObj.GetComponent<RectTransform>();
            sRt.anchorMin = Vector2.zero;
            sRt.anchorMax = Vector2.one;
            sRt.sizeDelta = new Vector2(-20f, 0f);
            sRt.anchoredPosition = Vector2.zero;

            var sr = scrollObj.GetComponent<ScrollRect>();
            sr.horizontal = true;
            sr.vertical = false;
            sr.movementType = ScrollRect.MovementType.Elastic;

            var vpObj = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            vpObj.transform.SetParent(scrollObj.transform, false);
            var vpRt = vpObj.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.sizeDelta = Vector2.zero;

            vpObj.GetComponent<Image>().color = Color.white;
            vpObj.GetComponent<Mask>().showMaskGraphic = false;

            var contentObj = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            contentObj.transform.SetParent(vpObj.transform, false);
            var cRt = contentObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 0.5f);
            cRt.anchorMax = new Vector2(0f, 0.5f);
            cRt.pivot = new Vector2(0f, 0.5f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(0f, 240f);

            var hlg = contentObj.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.spacing = 15f;
            hlg.padding = new RectOffset(10, 10, 0, 0);
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var csf = contentObj.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            sr.viewport = vpRt;
            sr.content = cRt;

            var barUI = barObj.AddComponent<MinionRosterBarUI>();
            barUI._scrollRect = sr;
            barUI._contentContainer = cRt;
            barUI._barBackground = bgImg;

            barUI.RefreshRoster();
            return barUI;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Project_D/UI/Attach Minion Roster Bar UI to Selected", false, 25)]
        private static void AttachRosterBarMenuItem()
        {
            var selected = UnityEditor.Selection.activeGameObject;
            if (selected == null)
            {
                var existing = GameObject.Find("MinionRosterBar");
                if (existing != null) selected = existing;
            }

            if (selected != null)
            {
                if (!selected.TryGetComponent<MinionRosterBarUI>(out var barUI))
                {
                    barUI = selected.AddComponent<MinionRosterBarUI>();
                }
                barUI.AutoFindBarReferences();
                barUI.DetectTemplateCard();
                UnityEditor.EditorUtility.SetDirty(selected);
                Debug.Log($"<color=green>[UI]</color> Đã gắn và kết nối MinionRosterBarUI với GameObject '{selected.name}' thành công!");
            }
        }
#endif
    }
}

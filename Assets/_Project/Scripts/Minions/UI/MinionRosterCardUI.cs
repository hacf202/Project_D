using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Project_D.Minions.UI
{
    public class MinionRosterCardUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI Component References")]
        [SerializeField] private Button _cardButton;
        [SerializeField] private Image _cardBackground;
        [SerializeField] private Image _borderImage;
        [SerializeField] private Image _tierBarImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _initialFallbackText;
        [SerializeField] private Image _countBoxImage;
        [SerializeField] private TMP_Text _countText;

        [Header("Color Styling")]
        [SerializeField] private Color _normalBgColor = new Color(0.18f, 0.20f, 0.25f, 0.95f);
        [SerializeField] private Color _deadBgColor = new Color(0.08f, 0.09f, 0.11f, 0.95f);
        [SerializeField] private Color _normalCountBoxColor = new Color(0.92f, 0.94f, 0.96f, 1f);
        [SerializeField] private Color _deadCountBoxColor = new Color(0.15f, 0.16f, 0.19f, 1f);
        [SerializeField] private Color _normalCountTextColor = new Color(0.1f, 0.12f, 0.15f, 1f);
        [SerializeField] private Color _deadCountTextColor = new Color(0.75f, 0.25f, 0.25f, 1f);

        private MinionDataSO _minionData;
        private int _aliveCount;
        private int _totalCount;

        public MinionDataSO MinionData => _minionData;
        public int AliveCount => _aliveCount;
        public int TotalCount => _totalCount;

        private void Awake()
        {
            AutoFindReferences();
        }

        private void OnValidate()
        {
            AutoFindReferences();
        }

        /// <summary>
        /// Tự động tìm kiếm các thành phần con theo tên chuẩn nếu chưa kéo thả trong Inspector
        /// </summary>
        public void AutoFindReferences()
        {
            if (_cardBackground == null)
            {
                var inner = transform.Find("InnerBg");
                _cardBackground = inner != null ? inner.GetComponent<Image>() : GetComponent<Image>();
            }

            if (_cardButton == null)
            {
                _cardButton = GetComponent<Button>();
            }

            if (_cardButton != null)
            {
                _cardButton.onClick.RemoveListener(OnCardClicked);
                _cardButton.onClick.AddListener(OnCardClicked);
            }

            if (_cardBackground != null)
            {
                _cardBackground.raycastTarget = true;
            }

            if (_borderImage == null)
            {
                var border = transform.Find("Border");
                if (border != null) _borderImage = border.GetComponent<Image>();
            }

            if (_tierBarImage == null)
            {
                var tb = transform.Find("TierBar");
                if (tb != null)
                {
                    _tierBarImage = tb.GetComponent<Image>();
                    if (_tierBarImage == null) _tierBarImage = tb.gameObject.AddComponent<Image>();
                }
            }

            if (_iconImage == null)
            {
                var icon = transform.Find("Icon");
                if (icon != null)
                {
                    _iconImage = icon.GetComponent<Image>();
                    if (_iconImage == null)
                    {
                        _iconImage = icon.gameObject.AddComponent<Image>();
                    }
                }
                else
                {
                    // Fallback nếu không có child tên "Icon": tìm Image con nào không phải background/border/tierBar/countBox
                    var allImages = GetComponentsInChildren<Image>(true);
                    foreach (var img in allImages)
                    {
                        if (img != _cardBackground && img != _borderImage && img != _tierBarImage && img != _countBoxImage)
                        {
                            _iconImage = img;
                            break;
                        }
                    }
                }
            }

            if (_initialFallbackText == null && _iconImage != null)
            {
                _initialFallbackText = _iconImage.GetComponentInChildren<TMP_Text>(true);
            }

            if (_countBoxImage == null)
            {
                var cb = transform.Find("CountBox");
                if (cb != null)
                {
                    _countBoxImage = cb.GetComponent<Image>();
                    if (_countBoxImage == null) _countBoxImage = cb.gameObject.AddComponent<Image>();
                }
            }

            if (_countText == null)
            {
                if (_countBoxImage != null)
                {
                    _countText = _countBoxImage.GetComponentInChildren<TMP_Text>(true);
                }
                else
                {
                    _countText = GetComponentInChildren<TMP_Text>(true);
                }
            }

            // Đảm bảo các thành phần con không nuốt raycast để Button nhận diện click mượt mà
            if (_borderImage != null) _borderImage.raycastTarget = false;
            if (_tierBarImage != null) _tierBarImage.raycastTarget = false;
            if (_iconImage != null) _iconImage.raycastTarget = false;
            if (_countBoxImage != null) _countBoxImage.raycastTarget = false;
            if (_countText != null) _countText.raycastTarget = false;
            if (_initialFallbackText != null) _initialFallbackText.raycastTarget = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Nếu đã có Button đang hoạt động, Button.onClick sẽ xử lý để tránh gọi 2 lần
            if (_cardButton != null && _cardButton.isActiveAndEnabled && _cardButton.interactable)
            {
                return;
            }
            OnCardClicked();
        }

        private void OnCardClicked()
        {
            if (_minionData != null)
            {
                MinionDetailStatsPanelUI.OpenForMinion(_minionData);
            }
        }

        public void Setup(MinionDataSO data, int aliveCount, int totalCount)
        {
            AutoFindReferences();

            _minionData = data;
            _aliveCount = aliveCount;
            _totalCount = totalCount;

            if (data == null) return;

            bool isAllDead = (aliveCount <= 0);

            // 1. Đường gạch trên thể hiện Tier
            if (_tierBarImage != null)
            {
                _tierBarImage.color = GetTierColor(data.Tier);
            }

            // 2. Ảnh lính (Icon) - Ưu tiên data.Icon -> Prefab SpriteRenderer -> Giữ nguyên Sprite gốc trên Prefab UI
            if (_iconImage != null)
            {
                _iconImage.enabled = true;
                _iconImage.gameObject.SetActive(true);
                Sprite targetSprite = ResolveSprite(data);

                if (targetSprite != null)
                {
                    _iconImage.sprite = targetSprite;
                    _iconImage.preserveAspect = true;
                    _iconImage.color = isAllDead ? new Color(0.35f, 0.35f, 0.35f, 0.5f) : Color.white;
                    if (_initialFallbackText != null) _initialFallbackText.gameObject.SetActive(false);
                }
                else
                {
                    // Chỉ fallback về ô màu nếu hoàn toàn không có sprite nào
                    _iconImage.sprite = null;
                    Color classColor = GetClassColor(data.Class);
                    _iconImage.color = isAllDead
                        ? new Color(classColor.r * 0.3f, classColor.g * 0.3f, classColor.b * 0.3f, 0.5f)
                        : classColor;

                    if (_initialFallbackText != null)
                    {
                        _initialFallbackText.gameObject.SetActive(true);
                        _initialFallbackText.text = !string.IsNullOrEmpty(data.MinionName) ? data.MinionName.Substring(0, 1) : "M";
                        _initialFallbackText.color = isAllDead ? new Color(0.6f, 0.6f, 0.6f, 0.6f) : Color.white;
                    }
                }
            }

            // 3. Số lượng lính (Ví dụ: 3/5)
            if (_countText != null)
            {
                _countText.text = $"<b>{aliveCount}/{totalCount}</b>";
                _countText.color = isAllDead ? _deadCountTextColor : _normalCountTextColor;
            }

            // 4. Trạng thái nền: Tối đi khi toàn bộ lính loại đó bị hạ gục hết
            if (_cardBackground != null)
            {
                _cardBackground.color = isAllDead ? _deadBgColor : _normalBgColor;
            }

            if (_countBoxImage != null)
            {
                _countBoxImage.color = isAllDead ? _deadCountBoxColor : _normalCountBoxColor;
            }

            if (_borderImage != null)
            {
                _borderImage.color = isAllDead ? new Color(0.18f, 0.18f, 0.22f, 0.5f) : new Color(0.35f, 0.38f, 0.45f, 0.9f);
            }
        }

        private Color GetTierColor(MinionTier tier)
        {
            switch (tier)
            {
                case MinionTier.Tier1: return new Color(0.85f, 0.88f, 0.92f);      // Bạc sáng / Xám ngọc
                case MinionTier.Tier2: return new Color(0.3f, 0.85f, 0.45f);      // Xanh lục tươi
                case MinionTier.Tier3: return new Color(0.85f, 0.45f, 1f);        // Tím huyền bí
                case MinionTier.Tier4_Special: return new Color(1f, 0.65f, 0.15f); // Vàng kim cam rực
                default: return Color.white;
            }
        }

        private Color GetClassColor(MinionClass mClass)
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

            // Fallback giữ nguyên sprite đã gán trên Image component của Prefab
            if (_iconImage != null && _iconImage.sprite != null)
            {
                return _iconImage.sprite;
            }

            return null;
        }

        // Tạo nhanh cấu trúc UI cho thẻ lính nếu hoàn toàn không có Prefab hay thẻ mẫu trong Scene
        public static MinionRosterCardUI CreateDynamicCard(Transform parent)
        {
            var cardObj = new GameObject("MinionCard", typeof(RectTransform), typeof(Image), typeof(Button));
            cardObj.transform.SetParent(parent, false);

            var rt = cardObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120f, 200f);

            var bgImg = cardObj.GetComponent<Image>();
            bgImg.color = new Color(0.18f, 0.20f, 0.25f, 0.95f);
            bgImg.raycastTarget = true;

            var btn = cardObj.GetComponent<Button>();
            btn.targetGraphic = bgImg;
            btn.transition = Selectable.Transition.ColorTint;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            btn.colors = colors;

            var tierBarObj = new GameObject("TierBar", typeof(RectTransform), typeof(Image));
            tierBarObj.transform.SetParent(cardObj.transform, false);
            var tbRt = tierBarObj.GetComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0f, 1f);
            tbRt.anchorMax = new Vector2(1f, 1f);
            tbRt.pivot = new Vector2(0.5f, 1f);
            tbRt.anchoredPosition = Vector2.zero;
            tbRt.sizeDelta = new Vector2(0f, 6f);
            var tbImg = tierBarObj.GetComponent<Image>();
            tbImg.color = new Color(0.85f, 0.88f, 0.92f);
            tbImg.raycastTarget = false;

            var iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(cardObj.transform, false);
            var iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(0f, 10f);
            iconRt.sizeDelta = new Vector2(100f, 100f);
            var iconImg = iconObj.GetComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            var countBoxObj = new GameObject("CountBox", typeof(RectTransform), typeof(Image));
            countBoxObj.transform.SetParent(cardObj.transform, false);
            var cbRt = countBoxObj.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0f, 0f);
            cbRt.anchorMax = new Vector2(1f, 0f);
            cbRt.pivot = new Vector2(0.5f, 0f);
            cbRt.anchoredPosition = Vector2.zero;
            cbRt.sizeDelta = new Vector2(0f, 32f);
            var cbImg = countBoxObj.GetComponent<Image>();
            cbImg.color = new Color(0.92f, 0.94f, 0.96f, 1f);
            cbImg.raycastTarget = false;

            var countTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            countTxtObj.transform.SetParent(countBoxObj.transform, false);
            var ctRt = countTxtObj.GetComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero;
            ctRt.anchorMax = Vector2.one;
            ctRt.sizeDelta = Vector2.zero;
            var countTMP = countTxtObj.GetComponent<TextMeshProUGUI>();
            countTMP.fontSize = 18;
            countTMP.fontStyle = FontStyles.Bold;
            countTMP.alignment = TextAlignmentOptions.Center;
            countTMP.color = new Color(0.1f, 0.12f, 0.15f, 1f);
            countTMP.raycastTarget = false;
            countTMP.text = "1/1";

            var cardUI = cardObj.AddComponent<MinionRosterCardUI>();
            cardUI._cardButton = btn;
            cardUI._cardBackground = bgImg;
            cardUI._tierBarImage = tbImg;
            cardUI._iconImage = iconImg;
            cardUI._countBoxImage = cbImg;
            cardUI._countText = countTMP;

            return cardUI;
        }
    }
}

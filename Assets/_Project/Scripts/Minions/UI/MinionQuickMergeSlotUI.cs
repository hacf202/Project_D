using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Project_D.Minions.UI
{
    public class MinionQuickMergeSlotUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _bgImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _nameText;     // Tùy chọn (dành cho prefab đầy đủ)
        [SerializeField] private TMP_Text _countText;    // Tùy chọn (dành cho prefab đầy đủ)
        [SerializeField] private Image _glowBorder;
        [SerializeField] private GameObject _readyBadge; // Tùy chọn
        [SerializeField] private TMP_Text _tierText;     // Huy hiệu bậc lính (T2, T3...)
        [SerializeField] private Button _button;

        private MergeAvailableInfo _info;
        private Action<MergeRecipeSO> _onMergeCallback;
        private Vector3 _originalScale = Vector3.one;

        private void Awake()
        {
            EnsureButtonListener();
            _originalScale = transform.localScale;
        }

        private void EnsureButtonListener()
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClickSlot);
                _button.onClick.AddListener(OnClickSlot);
            }
        }

        private void Update()
        {
            // Hiệu ứng nhịp đập vàng kim khi lính đã sẵn sàng hợp nhất
            if (_info != null && _info.CanMerge)
            {
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.05f;

                if (_glowBorder != null)
                {
                    float alpha = 0.5f + Mathf.Sin(Time.unscaledTime * 5f) * 0.4f;
                    Color c = _glowBorder.color;
                    c.a = alpha;
                    _glowBorder.color = c;
                }

                if (_readyBadge != null)
                {
                    _readyBadge.transform.localScale = Vector3.one * pulse;
                }

                transform.localScale = _originalScale * pulse;
            }
            else
            {
                transform.localScale = _originalScale;
            }
        }

        public void SetSize(float size)
        {
            var rt = transform as RectTransform;
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(size, size);
            }

            if (TryGetComponent<LayoutElement>(out var le))
            {
                le.preferredWidth = size;
                le.preferredHeight = size;
                le.minWidth = size;
                le.minHeight = size;
            }
        }

        public void Setup(MergeAvailableInfo info, Action<MergeRecipeSO> onMergeCallback)
        {
            EnsureButtonListener();
            _info = info;
            _onMergeCallback = onMergeCallback;

            if (info == null || info.Recipe == null) return;

            var recipe = info.Recipe;
            var output = recipe.OutputMinion;

            // 1. Icon lính kết quả
            if (_iconImage != null)
            {
                var txt = _iconImage.GetComponentInChildren<TMP_Text>(true);
                if (output != null && output.Icon != null)
                {
                    _iconImage.sprite = output.Icon;
                    _iconImage.color = Color.white;
                    if (txt != null) txt.gameObject.SetActive(false);
                }
                else
                {
                    _iconImage.sprite = null;
                    Color classColor = output != null ? GetClassColor(output.Class) : new Color(0.25f, 0.35f, 0.5f, 0.95f);
                    _iconImage.color = classColor;
                    if (txt != null)
                    {
                        txt.gameObject.SetActive(true);
                        string outName = output != null ? output.MinionName : recipe.RecipeName;
                        txt.text = outName.Length > 0 ? outName.Substring(0, 1) : "L";
                    }
                }
            }

            // 2. Tên & Bậc (nếu có UI text)
            if (_nameText != null)
            {
                string outName = output != null ? output.MinionName : recipe.RecipeName;
                _nameText.text = $"<b>{outName}</b>";
            }

            // 3. Tiến độ số lượng (nếu có UI text)
            if (_countText != null)
            {
                if (info.IsLockedBySpecialCondition)
                {
                    _countText.text = "<color=#FF5555>ĐÃ KHÓA</color>";
                }
                else if (info.CanMerge)
                {
                    _countText.text = $"<color=#00FF66><b>{info.CurrentCount}/{info.RequiredCount}</b></color>";
                }
                else
                {
                    _countText.text = $"<color=#FFD700>{info.CurrentCount}/{info.RequiredCount}</color>";
                }
            }

            // 4. Huy hiệu bậc lính kết quả (T2, T3, T4...)
            if (_tierText != null)
            {
                if (output != null)
                {
                    _tierText.text = GetTierLabel(output.Tier);
                    _tierText.color = GetTierColor(output.Tier);
                }
                else
                {
                    _tierText.text = "UP";
                    _tierText.color = Color.yellow;
                }
            }

            // 5. Khung viền phát sáng & Huy hiệu Sẵn sàng
            bool isReady = info.CanMerge && !info.IsLockedBySpecialCondition;
            if (_glowBorder != null)
            {
                _glowBorder.gameObject.SetActive(isReady);
            }

            if (_readyBadge != null)
            {
                _readyBadge.SetActive(isReady);
            }

            if (_bgImage != null)
            {
                _bgImage.color = isReady ? new Color(0.12f, 0.16f, 0.24f, 0.95f) : new Color(0.1f, 0.1f, 0.14f, 0.85f);
            }
        }

        private void OnClickSlot()
        {
            if (_info == null || _info.Recipe == null) return;

            if (_info.CanMerge)
            {
                // Hợp nhất đơn vị lính ngay khi chạm vào icon
                _onMergeCallback?.Invoke(_info.Recipe);
            }
            else if (_info.IsLockedBySpecialCondition)
            {
                Debug.Log($"<color=orange>[MergeSlot]</color> {_info.Recipe.LockedHint}");
            }
            else
            {
                Debug.Log($"<color=yellow>[MergeSlot]</color> Chưa đủ số lượng để hợp nhất ({_info.CurrentCount}/{_info.RequiredCount})");
            }
        }

        private string GetTierLabel(MinionTier tier)
        {
            switch (tier)
            {
                case MinionTier.Tier1: return "T1";
                case MinionTier.Tier2: return "T2";
                case MinionTier.Tier3: return "T3";
                case MinionTier.Tier4_Special: return "T4";
                default: return "★";
            }
        }

        private Color GetTierColor(MinionTier tier)
        {
            switch (tier)
            {
                case MinionTier.Tier1: return new Color(0.85f, 0.85f, 0.85f);     // Bạc sáng
                case MinionTier.Tier2: return new Color(0.3f, 0.85f, 1f);        // Lam ngọc
                case MinionTier.Tier3: return new Color(0.9f, 0.45f, 1f);        // Tím huyền thoại
                case MinionTier.Tier4_Special: return new Color(1f, 0.82f, 0.15f); // Vàng kim tối thượng
                default: return Color.white;
            }
        }

        private Color GetClassColor(MinionClass mClass)
        {
            switch (mClass)
            {
                case MinionClass.Warrior: return new Color(0.2f, 0.8f, 1f); // Cyan
                case MinionClass.Archer: return new Color(0.3f, 1f, 0.4f);  // Lime Green
                case MinionClass.Mage: return new Color(0.8f, 0.4f, 1f);    // Purple
                case MinionClass.Tank: return new Color(1f, 0.6f, 0.2f);    // Orange
                case MinionClass.Support: return new Color(1f, 0.9f, 0.2f); // Gold
                default: return Color.white;
            }
        }

        // Tạo nhanh cấu trúc UI Slot Icon chuẩn vuông khớp với chiều rộng thanh QuickMergeBar
        public static MinionQuickMergeSlotUI CreateDynamicSlot(Transform parent, float size = 80f)
        {
            var go = new GameObject("QuickMergeSlot", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);

            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = size;
            le.preferredHeight = size;
            le.minWidth = size;
            le.minHeight = size;

            // 1. Nền slot hình vuông bo góc/sẫm màu
            var bg = go.GetComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.18f, 0.95f);

            // 2. Viền phát sáng nhịp đập khi sẵn sàng hợp nhất
            var glowObj = new GameObject("GlowBorder", typeof(RectTransform), typeof(Image));
            glowObj.transform.SetParent(go.transform, false);
            var glowRt = glowObj.GetComponent<RectTransform>();
            glowRt.anchorMin = Vector2.zero;
            glowRt.anchorMax = Vector2.one;
            glowRt.sizeDelta = new Vector2(4, 4);
            var glowImg = glowObj.GetComponent<Image>();
            glowImg.color = new Color(1f, 0.82f, 0.15f, 0.85f);
            glowImg.raycastTarget = false;

            // 3. Icon Lính lọt lòng giữa ô vuông (padding ~6px)
            var iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(go.transform, false);
            var iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = new Vector2(-12, -12);
            var iconImg = iconObj.GetComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // 3.1 Chữ cái thay thế nếu chưa có Sprite Icon
            var iconTxtObj = new GameObject("Initial", typeof(RectTransform), typeof(TextMeshProUGUI));
            iconTxtObj.transform.SetParent(iconObj.transform, false);
            var iconTxtRt = iconTxtObj.GetComponent<RectTransform>();
            iconTxtRt.anchorMin = Vector2.zero;
            iconTxtRt.anchorMax = Vector2.one;
            iconTxtRt.sizeDelta = Vector2.zero;
            var iconTxtTMP = iconTxtObj.GetComponent<TextMeshProUGUI>();
            iconTxtTMP.fontSize = Mathf.RoundToInt(size * 0.35f);
            iconTxtTMP.fontStyle = FontStyles.Bold;
            iconTxtTMP.alignment = TextAlignmentOptions.Center;
            iconTxtTMP.color = Color.white;
            iconTxtTMP.raycastTarget = false;

            // 4. Huy hiệu bậc lính (Tier Badge góc trên phải)
            var tierBadgeObj = new GameObject("TierBadge", typeof(RectTransform), typeof(Image));
            tierBadgeObj.transform.SetParent(go.transform, false);
            var tbRt = tierBadgeObj.GetComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(1, 1);
            tbRt.anchorMax = new Vector2(1, 1);
            tbRt.pivot = new Vector2(1, 1);
            tbRt.anchoredPosition = new Vector2(-2, -2);
            tbRt.sizeDelta = new Vector2(24, 18);
            var tbImg = tierBadgeObj.GetComponent<Image>();
            tbImg.color = new Color(0.06f, 0.08f, 0.12f, 0.88f);
            tbImg.raycastTarget = false;

            var tbTxtObj = new GameObject("TierText", typeof(RectTransform), typeof(TextMeshProUGUI));
            tbTxtObj.transform.SetParent(tierBadgeObj.transform, false);
            var tbtRt = tbTxtObj.GetComponent<RectTransform>();
            tbtRt.anchorMin = Vector2.zero;
            tbtRt.anchorMax = Vector2.one;
            tbtRt.sizeDelta = Vector2.zero;
            var tbTMP = tbTxtObj.GetComponent<TextMeshProUGUI>();
            tbTMP.fontSize = 10;
            tbTMP.fontStyle = FontStyles.Bold;
            tbTMP.alignment = TextAlignmentOptions.Center;
            tbTMP.color = new Color(1f, 0.85f, 0.2f);
            tbTMP.raycastTarget = false;
            tbTMP.text = "T2";

            // 5. Mũi tên xanh báo hiệu nâng cấp ở góc dưới trái
            var hintObj = new GameObject("MergeHint", typeof(RectTransform), typeof(TextMeshProUGUI));
            hintObj.transform.SetParent(go.transform, false);
            var hintRt = hintObj.GetComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0, 0);
            hintRt.anchorMax = new Vector2(0, 0);
            hintRt.pivot = new Vector2(0, 0);
            hintRt.anchoredPosition = new Vector2(4, 2);
            hintRt.sizeDelta = new Vector2(20, 16);
            var hintTMP = hintObj.GetComponent<TextMeshProUGUI>();
            hintTMP.fontSize = 11;
            hintTMP.fontStyle = FontStyles.Bold;
            hintTMP.alignment = TextAlignmentOptions.Left;
            hintTMP.color = new Color(0.2f, 1f, 0.4f, 0.95f);
            hintTMP.text = "▲";
            hintTMP.raycastTarget = false;

            var slotUI = go.AddComponent<MinionQuickMergeSlotUI>();
            slotUI._bgImage = bg;
            slotUI._iconImage = iconImg;
            slotUI._glowBorder = glowImg;
            slotUI._tierText = tbTMP;
            slotUI._button = go.GetComponent<Button>();

            return slotUI;
        }
    }
}

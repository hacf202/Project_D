using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Project_D.Minions.UI
{
    public class MinionMergeEntryUI : MonoBehaviour
    {
        [Header("Input Minion Elements")]
        [SerializeField] private Image _inputIcon;
        [SerializeField] private TMP_Text _inputNameText;
        [SerializeField] private TMP_Text _countProgressText;

        [Header("Output Minion Elements")]
        [SerializeField] private Image _outputIcon;
        [SerializeField] private TMP_Text _outputNameText;
        [SerializeField] private TMP_Text _outputStatsText;

        [Header("Action Button")]
        [SerializeField] private Button _mergeButton;
        [SerializeField] private TMP_Text _buttonLabel;

        private MergeRecipeSO _recipe;
        private Action<MergeRecipeSO> _onMergeCallback;

        public bool IsInitialized => _mergeButton != null && _buttonLabel != null;

        private void Awake()
        {
            EnsureButtonListener();
        }

        private void EnsureButtonListener()
        {
            if (_mergeButton == null) _mergeButton = GetComponentInChildren<Button>(true);
            if (_mergeButton != null)
            {
                _mergeButton.onClick.RemoveListener(OnButtonClicked);
                _mergeButton.onClick.AddListener(OnButtonClicked);
            }
        }

        public void Setup(MergeAvailableInfo info, Action<MergeRecipeSO> onMergeCallback)
        {
            EnsureButtonListener();

            if (info == null || info.Recipe == null) return;

            _recipe = info.Recipe;
            _onMergeCallback = onMergeCallback;

            // 1. Thông tin lính nguyên liệu đầu vào
            string inName = _recipe.RecipeName;
            if (_inputNameText != null)
            {
                _inputNameText.text = $"<b>{inName}</b>";
            }

            if (_inputIcon != null)
            {
                Sprite ingIcon = null;
                var ings = _recipe.Ingredients;
                if (ings.Count > 0 && ings[0].MinionData != null)
                {
                    ingIcon = ings[0].MinionData.Icon;
                }

                if (ingIcon != null)
                {
                    _inputIcon.sprite = ingIcon;
                    _inputIcon.color = Color.white;
                    var txt = _inputIcon.GetComponentInChildren<TMP_Text>(true);
                    if (txt != null) txt.gameObject.SetActive(false);
                }
                else
                {
                    _inputIcon.sprite = null;
                    _inputIcon.color = new Color(0.2f, 0.25f, 0.38f, 0.95f);
                    var txt = _inputIcon.GetComponentInChildren<TMP_Text>(true);
                    if (txt != null)
                    {
                        txt.gameObject.SetActive(true);
                        txt.text = inName.Length > 0 ? inName.Substring(0, 1) : "L";
                    }
                }
            }

            if (_countProgressText != null)
            {
                string colorHex = info.CanMerge ? "#00FF66" : (info.CurrentCount > 0 ? "#FFD700" : "#FF6666");
                if (!string.IsNullOrEmpty(info.IngredientsDescription))
                {
                    _countProgressText.text = $"<color={colorHex}><b>{info.IngredientsDescription}</b></color>";
                }
                else
                {
                    _countProgressText.text = $"<color=#80D8FF>Tier {_recipe.InputTier}</color> • <color={colorHex}><b>{info.CurrentCount}/{info.RequiredCount}</b></color>";
                }
            }

            // 2. Thông tin lính mới đầu ra sau khi hợp nhất
            if (_recipe.OutputMinion != null)
            {
                var outM = _recipe.OutputMinion;
                if (_outputIcon != null)
                {
                    if (outM.Icon != null)
                    {
                        _outputIcon.sprite = outM.Icon;
                        _outputIcon.color = Color.white;
                        var txt = _outputIcon.GetComponentInChildren<TMP_Text>(true);
                        if (txt != null) txt.gameObject.SetActive(false);
                    }
                    else
                    {
                        _outputIcon.sprite = null;
                        _outputIcon.color = new Color(0.35f, 0.28f, 0.12f, 0.95f);
                        var txt = _outputIcon.GetComponentInChildren<TMP_Text>(true);
                        if (txt != null)
                        {
                            txt.gameObject.SetActive(true);
                            txt.text = outM.MinionName.Length > 0 ? outM.MinionName.Substring(0, 1) : "★";
                        }
                    }
                }

                if (_outputNameText != null)
                {
                    _outputNameText.text = $"<color=#FFD700><b>{outM.MinionName}</b></color>";
                }

                if (_outputStatsText != null)
                {
                    _outputStatsText.text = $"<color=#00E5FF>Tier {outM.Tier} ({outM.Class})</color> • HP: <color=#00FF66>{outM.MaxHealth}</color> | ATK: <color=#FF5555>{outM.AttackDamage}</color>";
                }
            }

            // 3. Trạng thái nút bấm
            if (_mergeButton != null)
            {
                if (info.IsLockedBySpecialCondition)
                {
                    _mergeButton.interactable = false;
                    if (_buttonLabel != null) _buttonLabel.text = "<color=#FF6666>ĐÃ KHÓA</color>";
                }
                else if (info.CanMerge)
                {
                    _mergeButton.interactable = true;
                    if (_buttonLabel != null) _buttonLabel.text = "<color=#FFFFFF><b>HỢP NHẤT</b></color>";
                }
                else
                {
                    _mergeButton.interactable = false;
                    if (_buttonLabel != null) _buttonLabel.text = "<color=#888888>Chưa đủ</color>";
                }
            }
        }

        private void OnButtonClicked()
        {
            if (_recipe != null && _onMergeCallback != null)
            {
                _onMergeCallback(_recipe);
            }
        }

        public static MinionMergeEntryUI CreateDynamicEntry(Transform parent)
        {
            var go = new GameObject("MergeEntry", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 108f);

            var le = go.GetComponent<LayoutElement>();
            le.minHeight = 108f;
            le.preferredHeight = 108f;
            le.flexibleWidth = 1f;

            var bg = go.GetComponent<Image>();
            bg.color = new Color(0.12f, 0.14f, 0.22f, 0.95f);

            // 1. Icon lính nguyên liệu (Input Icon)
            var inIconObj = new GameObject("InputIcon", typeof(RectTransform), typeof(Image));
            inIconObj.transform.SetParent(go.transform, false);
            var inIconRt = inIconObj.GetComponent<RectTransform>();
            inIconRt.anchorMin = new Vector2(0f, 0.5f);
            inIconRt.anchorMax = new Vector2(0f, 0.5f);
            inIconRt.pivot = new Vector2(0.5f, 0.5f);
            inIconRt.anchoredPosition = new Vector2(38f, 0f);
            inIconRt.sizeDelta = new Vector2(52f, 52f);
            var inIconImg = inIconObj.GetComponent<Image>();
            inIconImg.color = new Color(0.2f, 0.25f, 0.38f, 0.95f);

            var inTxtObj = new GameObject("Initial", typeof(RectTransform), typeof(TextMeshProUGUI));
            inTxtObj.transform.SetParent(inIconObj.transform, false);
            var inTxtRt = inTxtObj.GetComponent<RectTransform>();
            inTxtRt.anchorMin = Vector2.zero;
            inTxtRt.anchorMax = Vector2.one;
            inTxtRt.sizeDelta = Vector2.zero;
            var inTxtTMP = inTxtObj.GetComponent<TextMeshProUGUI>();
            inTxtTMP.fontSize = 24;
            inTxtTMP.fontStyle = FontStyles.Bold;
            inTxtTMP.alignment = TextAlignmentOptions.Center;
            inTxtTMP.color = Color.white;

            // 2. Tên lính nguyên liệu & Tier (Input Name)
            var inNameObj = new GameObject("InputName", typeof(RectTransform), typeof(TextMeshProUGUI));
            inNameObj.transform.SetParent(go.transform, false);
            var inNameRt = inNameObj.GetComponent<RectTransform>();
            inNameRt.anchorMin = new Vector2(0f, 0.5f);
            inNameRt.anchorMax = new Vector2(0f, 0.5f);
            inNameRt.pivot = new Vector2(0f, 0.5f);
            inNameRt.anchoredPosition = new Vector2(76f, 16f);
            inNameRt.sizeDelta = new Vector2(170f, 32f);
            var inNameTMP = inNameObj.GetComponent<TextMeshProUGUI>();
            inNameTMP.fontSize = 15f;
            inNameTMP.fontStyle = FontStyles.Bold;
            inNameTMP.color = Color.white;

            // 3. Tiến độ số lượng (Count Progress)
            var countObj = new GameObject("CountProgress", typeof(RectTransform), typeof(TextMeshProUGUI));
            countObj.transform.SetParent(go.transform, false);
            var countRt = countObj.GetComponent<RectTransform>();
            countRt.anchorMin = new Vector2(0f, 0.5f);
            countRt.anchorMax = new Vector2(0f, 0.5f);
            countRt.pivot = new Vector2(0f, 0.5f);
            countRt.anchoredPosition = new Vector2(76f, -16f);
            countRt.sizeDelta = new Vector2(170f, 26f);
            var countTMP = countObj.GetComponent<TextMeshProUGUI>();
            countTMP.fontSize = 14f;
            countTMP.color = new Color(1f, 0.85f, 0.2f);

            // 4. Mũi tên chuyển hóa (Arrow)
            var arrowObj = new GameObject("Arrow", typeof(RectTransform), typeof(TextMeshProUGUI));
            arrowObj.transform.SetParent(go.transform, false);
            var arrRt = arrowObj.GetComponent<RectTransform>();
            arrRt.anchorMin = new Vector2(0f, 0.5f);
            arrRt.anchorMax = new Vector2(0f, 0.5f);
            arrRt.pivot = new Vector2(0.5f, 0.5f);
            arrRt.anchoredPosition = new Vector2(265f, 0f);
            arrRt.sizeDelta = new Vector2(40f, 36f);
            var arrTMP = arrowObj.GetComponent<TextMeshProUGUI>();
            arrTMP.fontSize = 22f;
            arrTMP.fontStyle = FontStyles.Bold;
            arrTMP.alignment = TextAlignmentOptions.Center;
            arrTMP.color = new Color(0f, 0.9f, 1f);
            arrTMP.text = "->";

            // 5. Icon lính kết quả (Output Icon)
            var outIconObj = new GameObject("OutputIcon", typeof(RectTransform), typeof(Image));
            outIconObj.transform.SetParent(go.transform, false);
            var outIconRt = outIconObj.GetComponent<RectTransform>();
            outIconRt.anchorMin = new Vector2(0f, 0.5f);
            outIconRt.anchorMax = new Vector2(0f, 0.5f);
            outIconRt.pivot = new Vector2(0.5f, 0.5f);
            outIconRt.anchoredPosition = new Vector2(325f, 0f);
            outIconRt.sizeDelta = new Vector2(52f, 52f);
            var outIconImg = outIconObj.GetComponent<Image>();
            outIconImg.color = new Color(0.35f, 0.28f, 0.12f, 0.95f);

            var outTxtObj = new GameObject("Initial", typeof(RectTransform), typeof(TextMeshProUGUI));
            outTxtObj.transform.SetParent(outIconObj.transform, false);
            var outTxtRt = outTxtObj.GetComponent<RectTransform>();
            outTxtRt.anchorMin = Vector2.zero;
            outTxtRt.anchorMax = Vector2.one;
            outTxtRt.sizeDelta = Vector2.zero;
            var outTxtTMP = outTxtObj.GetComponent<TextMeshProUGUI>();
            outTxtTMP.fontSize = 24;
            outTxtTMP.fontStyle = FontStyles.Bold;
            outTxtTMP.alignment = TextAlignmentOptions.Center;
            outTxtTMP.color = new Color(1f, 0.85f, 0.2f);

            // 6. Tên lính kết quả (Output Name)
            var outNameObj = new GameObject("OutputName", typeof(RectTransform), typeof(TextMeshProUGUI));
            outNameObj.transform.SetParent(go.transform, false);
            var outNameRt = outNameObj.GetComponent<RectTransform>();
            outNameRt.anchorMin = new Vector2(0f, 0.5f);
            outNameRt.anchorMax = new Vector2(0f, 0.5f);
            outNameRt.pivot = new Vector2(0f, 0.5f);
            outNameRt.anchoredPosition = new Vector2(365f, 16f);
            outNameRt.sizeDelta = new Vector2(250f, 32f);
            var outNameTMP = outNameObj.GetComponent<TextMeshProUGUI>();
            outNameTMP.fontSize = 16f;
            outNameTMP.fontStyle = FontStyles.Bold;
            outNameTMP.color = new Color(1f, 0.85f, 0.2f);

            // 7. Chỉ số lính kết quả (Output Stats)
            var outStatsObj = new GameObject("OutputStats", typeof(RectTransform), typeof(TextMeshProUGUI));
            outStatsObj.transform.SetParent(go.transform, false);
            var outStatsRt = outStatsObj.GetComponent<RectTransform>();
            outStatsRt.anchorMin = new Vector2(0f, 0.5f);
            outStatsRt.anchorMax = new Vector2(0f, 0.5f);
            outStatsRt.pivot = new Vector2(0f, 0.5f);
            outStatsRt.anchoredPosition = new Vector2(365f, -16f);
            outStatsRt.sizeDelta = new Vector2(260f, 26f);
            var outStatsTMP = outStatsObj.GetComponent<TextMeshProUGUI>();
            outStatsTMP.fontSize = 13f;
            outStatsTMP.color = new Color(0.9f, 0.9f, 0.95f);

            // 8. Nút Hợp Nhất (Action Button)
            var btnObj = new GameObject("Btn_Action", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(go.transform, false);
            var btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 0.5f);
            btnRt.anchorMax = new Vector2(1f, 0.5f);
            btnRt.pivot = new Vector2(1f, 0.5f);
            btnRt.anchoredPosition = new Vector2(-15f, 0f);
            btnRt.sizeDelta = new Vector2(150f, 54f);
            var btnImg = btnObj.GetComponent<Image>();
            btnImg.color = new Color(0.95f, 0.6f, 0.1f, 1f);

            var btnTxtObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            var btnTxtRt = btnTxtObj.GetComponent<RectTransform>();
            btnTxtRt.anchorMin = Vector2.zero;
            btnTxtRt.anchorMax = Vector2.one;
            btnTxtRt.sizeDelta = Vector2.zero;
            var btnTMP = btnTxtObj.GetComponent<TextMeshProUGUI>();
            btnTMP.fontSize = 16f;
            btnTMP.fontStyle = FontStyles.Bold;
            btnTMP.alignment = TextAlignmentOptions.Center;
            btnTMP.color = Color.white;
            btnTMP.text = "HỢP NHẤT";

            var entryUI = go.AddComponent<MinionMergeEntryUI>();
            entryUI._inputIcon = inIconImg;
            entryUI._inputNameText = inNameTMP;
            entryUI._countProgressText = countTMP;
            entryUI._outputIcon = outIconImg;
            entryUI._outputNameText = outNameTMP;
            entryUI._outputStatsText = outStatsTMP;
            entryUI._mergeButton = btnObj.GetComponent<Button>();
            entryUI._buttonLabel = btnTMP;

            return entryUI;
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project_D.Upgrades;

namespace Project_D.UI
{
    public class UpgradeCardUI : MonoBehaviour
    {
        [Header("TextMeshPro References (Khuyên Dùng)")]
        [SerializeField] private TextMeshProUGUI _titleTMP;
        [SerializeField] private TextMeshProUGUI _descriptionTMP;

        [Header("Legacy Text References (Dự Phòng)")]
        [SerializeField] private Text _titleLegacy;
        [SerializeField] private Text _descriptionLegacy;

        [Header("General Elements")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private Button _selectButton;

        private UpgradeDataSO _currentData;
        private Action<UpgradeDataSO> _onSelectCallback;

        private void Awake()
        {
            if (_selectButton == null)
            {
                _selectButton = GetComponent<Button>();
            }

            if (_selectButton != null)
            {
                _selectButton.onClick.RemoveListener(HandleClick);
                _selectButton.onClick.AddListener(HandleClick);
            }
        }

        public void Setup(UpgradeDataSO data, Action<UpgradeDataSO> onSelectCallback)
        {
            _currentData = data;
            _onSelectCallback = onSelectCallback;

            if (_currentData == null) return;

            // 1. Cập nhật Tiêu đề thẻ
            if (_titleTMP != null) _titleTMP.text = _currentData.Title;
            if (_titleLegacy != null) _titleLegacy.text = _currentData.Title;

            // 2. Cập nhật Mô tả hiệu ứng thẻ
            if (_descriptionTMP != null) _descriptionTMP.text = _currentData.Description;
            if (_descriptionLegacy != null) _descriptionLegacy.text = _currentData.Description;

            // 3. Cập nhật Icon minh họa
            if (_iconImage != null)
            {
                if (_currentData.Icon != null)
                {
                    _iconImage.sprite = _currentData.Icon;
                    _iconImage.gameObject.SetActive(true);
                }
                else
                {
                    _iconImage.gameObject.SetActive(false);
                }
            }
        }

        private void HandleClick()
        {
            if (_currentData != null)
            {
                _onSelectCallback?.Invoke(_currentData);
            }
        }
    }
}

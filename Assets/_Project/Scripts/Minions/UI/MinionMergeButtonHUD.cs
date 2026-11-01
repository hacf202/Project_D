using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Project_D.Minions.UI
{
    public class MinionMergeButtonHUD : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _visualContainer;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _mergeButton;
        [SerializeField] private GameObject _badgeContainer;
        [SerializeField] private TMP_Text _badgeText;
        [SerializeField] private MinionMergePanelUI _mergePanel;

        [Header("Animation Settings")]
        [SerializeField] private bool _enablePulse = true;
        [SerializeField] private float _pulseSpeed = 4f;
        [SerializeField] private float _pulseAmount = 0.1f;

        private Vector3 _originalScale;
        private int _availableMergesCount = 0;

        private void Awake()
        {
            if (_visualContainer == null && transform.childCount > 0)
            {
                _visualContainer = transform.GetChild(0).gameObject;
            }
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_mergeButton == null) _mergeButton = GetComponentInChildren<Button>(true);
            _originalScale = transform.localScale;

            if (_mergeButton != null)
            {
                _mergeButton.onClick.AddListener(OnClickButton);
            }
        }

        private void OnEnable()
        {
            MinionManager.OnAvailableMergesChanged += RefreshStatus;
            RefreshStatus();
        }

        private void OnDisable()
        {
            MinionManager.OnAvailableMergesChanged -= RefreshStatus;
        }

        private void Update()
        {
            if (_enablePulse && _availableMergesCount > 0)
            {
                float scale = 1f + Mathf.Sin(Time.unscaledTime * _pulseSpeed) * _pulseAmount;
                transform.localScale = _originalScale * scale;
            }
            else
            {
                transform.localScale = _originalScale;
            }
        }

        public void RefreshStatus()
        {
            if (MinionManager.Instance == null)
            {
                SetVisible(false);
                return;
            }

            var merges = MinionManager.Instance.GetAvailableMerges();
            int readyToMergeCount = 0;
            if (merges != null)
            {
                foreach (var m in merges)
                {
                    if (m != null && m.CanMerge) readyToMergeCount++;
                }
            }

            _availableMergesCount = readyToMergeCount;

            // Nếu không có công thức nào đủ điều kiện thì ẩn visual nút trên HUD
            if (_availableMergesCount <= 0)
            {
                SetVisible(false);
                return;
            }

            // Có ít nhất 1 công thức hợp nhất sẵn sàng: Bật nút và cập nhật badge
            SetVisible(true);

            if (_badgeContainer != null)
            {
                _badgeContainer.SetActive(true);
            }

            if (_badgeText != null)
            {
                _badgeText.text = _availableMergesCount.ToString();
            }
        }

        private void SetVisible(bool visible)
        {
            if (_visualContainer != null)
            {
                _visualContainer.SetActive(visible);
            }
            else if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.interactable = visible;
                _canvasGroup.blocksRaycasts = visible;
            }
            else if (_mergeButton != null && _mergeButton.gameObject != this.gameObject)
            {
                _mergeButton.gameObject.SetActive(visible);
            }
            else
            {
                // Fallback nếu gắn trực tiếp
                if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.interactable = visible;
                _canvasGroup.blocksRaycasts = visible;
            }
        }

        private void OnClickButton()
        {
            if (_mergePanel != null)
            {
                _mergePanel.Open();
            }
            else
            {
                // Tự động tìm kiếm nếu chưa gán
                var panel = UnityEngine.Object.FindAnyObjectByType<MinionMergePanelUI>(FindObjectsInactive.Include);
                if (panel != null)
                {
                    _mergePanel = panel;
                    _mergePanel.Open();
                }
                else
                {
                    Debug.LogWarning("[MinionMergeButtonHUD] Chưa gán MinionMergePanelUI trong Scene!");
                }
            }
        }
    }
}

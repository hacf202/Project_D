using System;
using UnityEngine;
using TMPro;
using Project_D.Dungeon.Run;
using Project_D.Player;

namespace Project_D.Dungeon.Room
{
    [RequireComponent(typeof(Collider2D))]
    public class RoomPortal : MonoBehaviour
    {
        [Header("Portal Visuals")]
        [SerializeField] private SpriteRenderer _portalRenderer;
        [SerializeField] private SpriteRenderer _iconRenderer;
        [SerializeField] private TMP_Text _symbolText;
        [SerializeField] private TMP_Text _labelText;

        [Header("Portal Animation")]
        [SerializeField] private float _pulseSpeed = 3f;
        [SerializeField] private float _pulseAmount = 0.08f;

        [Header("Teleport Standstill Settings")]
        [Tooltip("Thời gian người chơi cần đứng yên liên tục trong cổng để kích hoạt dịch chuyển (giây, khuyến nghị 0.75s - 1.0s)")]
        [SerializeField, Range(0.5f, 2.0f)] private float _standstillDuration = 0.85f;
        [Tooltip("Vận tốc tối đa của Player để tính là đang 'đứng yên'")]
        [SerializeField] private float _velocityThreshold = 0.2f;
        [Tooltip("Hiển thị hướng dẫn và thanh tiến trình trên nhãn cổng")]
        [SerializeField] private bool _showProgressVisual = true;

        private RunNodeData _targetNode;
        private bool _isActivated = false;
        private Vector3 _initialScale;
        private Collider2D _col;

        // Trạng thái người chơi đứng trong cổng
        private bool _isPlayerInside = false;
        private PlayerController _playerController;
        private Rigidbody2D _playerRb;
        private float _standstillTimer = 0f;
        private string _defaultTitle = "";
        private string _defaultLabelText = "";

        public RunNodeData TargetNode => _targetNode;
        public float StandstillDuration => _standstillDuration;
        public float CurrentStandstillTimer => _standstillTimer;

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
            _col.isTrigger = true;
            _initialScale = transform.localScale;

            if (_portalRenderer == null)
            {
                _portalRenderer = GetComponent<SpriteRenderer>();
            }
            if (_portalRenderer != null)
            {
                _portalRenderer.enabled = true;
            }

            if (_symbolText == null || _labelText == null)
            {
                var texts = GetComponentsInChildren<TMP_Text>(true);
                foreach (var t in texts)
                {
                    if (t.name.IndexOf("Symbol", StringComparison.OrdinalIgnoreCase) >= 0 && _symbolText == null)
                        _symbolText = t;
                    else if (t.name.IndexOf("Label", StringComparison.OrdinalIgnoreCase) >= 0 && _labelText == null)
                        _labelText = t;
                }
            }
        }

        private void OnDisable()
        {
            ResetStandstill();
        }

        // Cấu hình Cổng theo dữ liệu của Node tầng kế tiếp
        public void SetupPortal(RunNodeData nodeData, PortalVisualConfigSO visualConfig = null)
        {
            _targetNode = nodeData;
            _isActivated = true;
            _standstillTimer = 0f;
            _isPlayerInside = false;
            gameObject.SetActive(true);

            if (_col != null) _col.enabled = true;
            if (_portalRenderer != null) _portalRenderer.enabled = true;

            if (_targetNode == null) return;

            NodeVisualEntry visual = visualConfig != null ? visualConfig.GetVisualForType(_targetNode.NodeType) : null;
            Color themeColor = visual != null ? visual.ThemeColor : _targetNode.NodeColor;

            // 1. Tô màu Cổng theo loại phòng
            if (_portalRenderer != null)
            {
                _portalRenderer.color = themeColor;
            }

            // 2. Gán Icon riêng biệt hoặc Ký tự đại diện
            if (visual != null && visual.CustomIcon != null && _iconRenderer != null)
            {
                _iconRenderer.sprite = visual.CustomIcon;
                _iconRenderer.gameObject.SetActive(true);
                if (_symbolText != null) _symbolText.gameObject.SetActive(false);
            }
            else if (_symbolText != null)
            {
                _symbolText.text = visual != null ? visual.Symbol : "GATE";
                _symbolText.enableAutoSizing = true;
                _symbolText.fontSizeMin = 2f;
                _symbolText.fontSizeMax = 7f;
                _symbolText.gameObject.SetActive(true);
                if (_iconRenderer != null) _iconRenderer.gameObject.SetActive(false);
            }

            // 3. Hiển thị nhãn tên phòng và lưu text gốc
            string title = visual != null ? visual.Title : _targetNode.DisplayName;
            _defaultTitle = title;
            _defaultLabelText = $"Tầng {_targetNode.FloorNumber}\n{title}";

            if (_labelText != null)
            {
                _labelText.text = _defaultLabelText;
                _labelText.color = themeColor;
            }

            Debug.Log($"<color=cyan>[PORTAL OPEN]</color> Đã mở cổng sang: Tầng {_targetNode.FloorNumber} - {_targetNode.DisplayName}");
        }

        private void Update()
        {
            if (!_isActivated || _targetNode == null) return;

            // 1. Hiệu ứng nhịp đập nhẹ nhàng (Breathing / Pulse). Khi đang sạc đứng yên thì tăng tốc nhẹ tạo hiệu ứng tích tụ năng lượng
            float currentPulseSpeed = _pulseSpeed;
            float pulseMultiplier = 1f;

            if (_isPlayerInside && _standstillTimer > 0f)
            {
                float progress = Mathf.Clamp01(_standstillTimer / _standstillDuration);
                currentPulseSpeed = Mathf.Lerp(_pulseSpeed, _pulseSpeed * 2.5f, progress);
                pulseMultiplier = 1f + progress * 0.12f;
            }

            float scaleMultiplier = (1f + Mathf.Sin(Time.time * currentPulseSpeed) * _pulseAmount) * pulseMultiplier;
            transform.localScale = _initialScale * scaleMultiplier;

            // 2. Xử lý cơ chế đứng yên trong cổng
            if (_isPlayerInside && _playerController != null)
            {
                if (!_playerController.gameObject.activeInHierarchy)
                {
                    ResetStandstill();
                    return;
                }

                // Tiêu chí đứng yên: Không có input di chuyển và vận tốc thực tế <= ngưỡng
                bool isMoving = _playerController.IsMoving;
                if (!isMoving && _playerRb != null)
                {
                    isMoving = _playerRb.linearVelocity.magnitude > _velocityThreshold;
                }

                if (!isMoving)
                {
                    // Người chơi đang đứng yên -> Tích lũy thời gian
                    _standstillTimer += Time.deltaTime;
                    float progress = Mathf.Clamp01(_standstillTimer / _standstillDuration);

                    if (_showProgressVisual && _labelText != null)
                    {
                        int totalBars = 6;
                        int filledBars = Mathf.RoundToInt(progress * totalBars);
                        string barVisual = new string('■', filledBars) + new string('□', totalBars - filledBars);
                        float remaining = Mathf.Max(0f, _standstillDuration - _standstillTimer);
                        _labelText.text = $"Tầng {_targetNode.FloorNumber} - {_defaultTitle}\n<color=#00FFAA>[{barVisual}] {remaining:0.0}s</color>";
                    }

                    if (_standstillTimer >= _standstillDuration)
                    {
                        EnterPortal();
                    }
                }
                else
                {
                    // Người chơi đang di chuyển trong cổng -> Reset bộ đếm và nhắc nhở đứng yên
                    if (_standstillTimer > 0f)
                    {
                        _standstillTimer = 0f;
                    }

                    if (_showProgressVisual && _labelText != null)
                    {
                        _labelText.text = $"Tầng {_targetNode.FloorNumber} - {_defaultTitle}\n<color=#FFCC00>Đứng yên để qua tầng</color>";
                    }
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryRegisterPlayer(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!_isPlayerInside)
            {
                TryRegisterPlayer(other);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (_playerController != null && (other.gameObject == _playerController.gameObject || other.transform.IsChildOf(_playerController.transform)))
            {
                ResetStandstill();
            }
        }

        private void TryRegisterPlayer(Collider2D other)
        {
            if (!_isActivated || _targetNode == null) return;

            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null || other.GetComponentInParent<PlayerController>() != null)
            {
                var pc = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
                if (pc != null)
                {
                    _playerController = pc;
                    _playerRb = pc.GetComponent<Rigidbody2D>();
                    _isPlayerInside = true;
                    _standstillTimer = 0f;
                }
            }
        }

        private void ResetStandstill()
        {
            _isPlayerInside = false;
            _playerController = null;
            _playerRb = null;
            _standstillTimer = 0f;

            if (_labelText != null && !string.IsNullOrEmpty(_defaultLabelText))
            {
                _labelText.text = _defaultLabelText;
            }
        }

        private void EnterPortal()
        {
            if (!_isActivated || _targetNode == null) return;

            // Khóa cổng để không kích hoạt trùng lặp
            _isActivated = false;
            _isPlayerInside = false;
            if (_col != null) _col.enabled = false;

            if (_labelText != null)
            {
                _labelText.text = $"<color=#00FFAA>Đang dịch chuyển...</color>";
            }

            Debug.Log($"<color=green><b>[PORTAL ENTERED]</b></color> Người chơi đã đứng yên và kích hoạt cổng: {_targetNode.DisplayName}!");

            // Chuyển tầng trong RunManager
            if (RunManager.Instance != null)
            {
                RunManager.Instance.AdvanceToNode(_targetNode.Id);
            }
        }

        public void ClosePortal()
        {
            ResetStandstill();
            _isActivated = false;
            if (_col != null) _col.enabled = false;
            gameObject.SetActive(false);
        }
    }
}

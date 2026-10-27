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

        private RunNodeData _targetNode;
        private bool _isActivated = false;
        private Vector3 _initialScale;
        private Collider2D _col;

        public RunNodeData TargetNode => _targetNode;

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
            _col.isTrigger = true;
            _initialScale = transform.localScale;

            if (_portalRenderer == null)
            {
                _portalRenderer = GetComponent<SpriteRenderer>();
            }
        }

        // Cấu hình Cổng theo dữ liệu của Node tầng kế tiếp
        public void SetupPortal(RunNodeData nodeData, PortalVisualConfigSO visualConfig = null)
        {
            _targetNode = nodeData;
            _isActivated = true;
            gameObject.SetActive(true);

            if (_col != null) _col.enabled = true;

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

            // 3. Hiển thị nhãn tên phòng
            if (_labelText != null)
            {
                string title = visual != null ? visual.Title : _targetNode.DisplayName;
                _labelText.text = $"Tầng {_targetNode.FloorNumber}\n{title}";
                _labelText.color = themeColor;
            }

            Debug.Log($"<color=cyan>[PORTAL OPEN]</color> Đã mở cổng sang: Tầng {_targetNode.FloorNumber} - {_targetNode.DisplayName}");
        }

        private void Update()
        {
            if (!_isActivated) return;

            // Hiệu ứng nhịp đập nhẹ nhàng (Breathing / Pulse) tạo cảm giác sống động cho Cổng
            float scaleMultiplier = 1f + Mathf.Sin(Time.time * _pulseSpeed) * _pulseAmount;
            transform.localScale = _initialScale * scaleMultiplier;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Kiểm tra người chơi bước vào cổng
            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                EnterPortal();
            }
        }

        private void EnterPortal()
        {
            if (!_isActivated || _targetNode == null) return;

            // Khóa cổng để không kích hoạt trùng lặp
            _isActivated = false;
            if (_col != null) _col.enabled = false;

            Debug.Log($"<color=green><b>[PORTAL ENTERED]</b></color> Người chơi đã bước vào cổng: {_targetNode.DisplayName}!");

            // Chuyển tầng trong RunManager
            if (RunManager.Instance != null)
            {
                RunManager.Instance.AdvanceToNode(_targetNode.Id);
            }
        }

        public void ClosePortal()
        {
            _isActivated = false;
            if (_col != null) _col.enabled = false;
            gameObject.SetActive(false);
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using Project_D.Player;
namespace Project_D.UI
{
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform _joystickBackground;
        [SerializeField] private RectTransform _joystickHandle;
        [SerializeField] private PlayerController _playerController;
        [Header("Settings")]
        [SerializeField] private float _handleRange = 100f; // Bán kính kéo tối đa của cần gạt
        private Canvas _canvas;
        private Camera _uiCamera;
        private Vector2 _pointerDownPosition;
        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                _uiCamera = _canvas.worldCamera;
            }
            // Mặc định ẩn joystick đi
            if (_joystickBackground != null)
            {
                _joystickBackground.gameObject.SetActive(false);
            }
        }
        // 1. Khi ngón tay bắt đầu chạm xuống màn hình
        public void OnPointerDown(PointerEventData eventData)
        {
            if (_joystickBackground == null) return;
            // Bật joystick lên
            _joystickBackground.gameObject.SetActive(true);
            // Chuyển đổi tọa độ pixel màn hình sang tọa độ Canvas Local Point
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                eventData.position,
                _uiCamera,
                out Vector2 localPoint
            );
            // Đặt tâm Joystick tại vị trí vừa chạm
            _joystickBackground.anchoredPosition = localPoint;
            _joystickHandle.anchoredPosition = Vector2.zero;
            _pointerDownPosition = eventData.position;
        }
        // 2. Khi ngón tay đang kéo rê
        public void OnDrag(PointerEventData eventData)
        {
            Vector2 delta = eventData.position - _pointerDownPosition;
            // Chặn biên không cho Handle bay ra ngoài bán kính _handleRange
            Vector2 clampedDelta = Vector2.ClampMagnitude(delta, _handleRange);
            _joystickHandle.anchoredPosition = clampedDelta;
            // Chuẩn hóa vector thành giá trị [0..1] gửi cho Player
            Vector2 inputDirection = clampedDelta / _handleRange;
            if (_playerController != null)
            {
                _playerController.SetMoveInput(inputDirection);
            }
        }
        // 3. Khi ngón tay rời khỏi màn hình
        public void OnPointerUp(PointerEventData eventData)
        {
            // Trả cần gạt về tâm
            _joystickHandle.anchoredPosition = Vector2.zero;
            // Dừng nhân vật lại
            if (_playerController != null)
            {
                _playerController.SetMoveInput(Vector2.zero);
            }
            // Ẩn joystick đi
            if (_joystickBackground != null)
            {
                _joystickBackground.gameObject.SetActive(false);
            }
        }
    }
}

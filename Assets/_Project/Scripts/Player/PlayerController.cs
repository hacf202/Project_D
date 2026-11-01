using UnityEngine;
using UnityEngine.InputSystem;

namespace Project_D.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5f;

        [Header("Minion Follow Zone")]
        [Tooltip("Bán kính vòng theo sau của người chơi dành cho lính đồng minh")]
        [SerializeField] private float _minionFollowRadius = 3.5f;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private Rigidbody2D _rb;
        private Vector2 _moveInput;
        private Vector2 _joystickInput;

        public float MinionFollowRadius => _minionFollowRadius;
        public Vector2 CurrentVelocity => _rb != null ? _rb.linearVelocity : Vector2.zero;
        public float CurrentSpeed => PlayerStats.Instance != null ? PlayerStats.Instance.MoveSpeed : _moveSpeed;
        public Vector2 MoveInput => _moveInput;
        public bool IsMoving => _moveInput.sqrMagnitude > 0.01f || (_rb != null && _rb.linearVelocity.sqrMagnitude > 0.04f);

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.mass = 1000f; // Khối lượng áp đảo, không bị quái đẩy xê dịch

            if (_spriteRenderer == null) 
                _spriteRenderer = GetComponent<SpriteRenderer>();

            // Đảm bảo không loại trừ bất kỳ layer nào để nhận va chạm và gây sát thương chuẩn
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.excludeLayers = 0;
            }
        }

        private void Update()
        {
            ReadKeyboardInput();
            HandleFlip();
        }

        private void FixedUpdate()
        {
            Move();
            Project_D.Dungeon.Room.RoomBoundaryValidator.ValidateAndUnstuck(transform, 0.4f, _rb);
        }

        private void ReadKeyboardInput()
        {
            float x = 0f;
            float y = 0f;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) y += 1f;
                if (Keyboard.current.sKey.isPressed) y -= 1f;
                if (Keyboard.current.aKey.isPressed) x -= 1f;
                if (Keyboard.current.dKey.isPressed) x += 1f;
            }

            Vector2 keyboardInput = new Vector2(x, y).normalized;

            // Nếu người chơi bấm phím thì lấy phím, không bấm thì lấy từ Joystick
            if (keyboardInput.sqrMagnitude > 0.01f)
            {
                _moveInput = keyboardInput;
            }
            else
            {
                _moveInput = _joystickInput;
            }
        }

        private void Move()
        {
            float speed = PlayerStats.Instance != null ? PlayerStats.Instance.MoveSpeed : _moveSpeed;
            // Vận tốc tịnh tiến = vector hướng * tốc độ
            _rb.linearVelocity = _moveInput * speed;
        }

        private void HandleFlip()
        {
            if (_spriteRenderer == null) return;

            if (_moveInput.x > 0.01f)
                _spriteRenderer.flipX = false;
            else if (_moveInput.x < -0.01f)
                _spriteRenderer.flipX = true;
        }

        // VirtualJoystick sẽ gọi hàm này và nạp vector hướng vào biến 'input'
        public void SetMoveInput(Vector2 input)
        {
            _joystickInput = input;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, _minionFollowRadius);
        }
    }
}

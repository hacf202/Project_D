using UnityEngine;
using Project_D.Player;
using Project_D.Combat;

namespace Project_D.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyController : MonoBehaviour
    {
        [Header("Data Configuration")]
        [SerializeField] private EnemyDataSO _data;
        private GameObject _prefabOrigin;

        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 3f;

        [Header("Combat & Juice")]
        [SerializeField] private Material _flashMaterial;
        [SerializeField] private float _flashDuration = 0.08f;
        [SerializeField] private float _knockbackDecay = 18f;

        [Header("Health Settings")]
        [SerializeField] private int _maxHealth = 30;
        private int _currentHealth;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private Rigidbody2D _rb;
        private Transform _playerTransform;
        private Material _originalMaterial;
        private Coroutine _flashCoroutine;
        private Vector2 _knockbackVelocity;
        private float _knockbackTimer;
        private Collider2D _col;
        private bool _isDead = false;

        // Observer Pattern: RoomController lắng nghe để đếm tiến độ Wave
        public static event System.Action<EnemyController> OnEnemyDied;

        public EnemyDataSO Data => _data;
        public bool IsDead => _isDead;

        public void Initialize(EnemyDataSO data, GameObject prefabOrigin)
        {
            _data = data;
            _prefabOrigin = prefabOrigin;
            _isDead = false;

            if (_col != null) _col.enabled = true;

            if (_data != null)
            {
                _moveSpeed = _data.MoveSpeed;
                _maxHealth = _data.MaxHealth;
                _currentHealth = _maxHealth;
            }
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;

            _col = GetComponent<Collider2D>();
            // Đảm bảo vật lý trơn trượt (Zero Friction) để không bao giờ bị ma sát dính tường
            if (_col != null)
            {
                var frictionlessMat = new PhysicsMaterial2D("Frictionless_Enemy")
                {
                    friction = 0f,
                    bounciness = 0f
                };
                _col.sharedMaterial = frictionlessMat;
            }

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (_spriteRenderer != null)
            {
                _originalMaterial = _spriteRenderer.sharedMaterial;
            }

            // Tự động tìm Shader Flash nếu _flashMaterial chưa gán trong Inspector
            if (_flashMaterial == null)
            {
                var flashShader = Shader.Find("Project_D/SpriteWhiteFlash");
                if (flashShader != null)
                {
                    _flashMaterial = new Material(flashShader);
                }
            }

            if (_data != null)
            {
                _moveSpeed = _data.MoveSpeed;
                _maxHealth = _data.MaxHealth;
            }
        }

        private void OnEnable()
        {
            _isDead = false;
            if (_col != null) _col.enabled = true;

            if (_data != null)
            {
                _moveSpeed = _data.MoveSpeed;
                _maxHealth = _data.MaxHealth;
                _currentHealth = _maxHealth;
            }
            else
            {
                _currentHealth = _maxHealth;
            }

            _knockbackTimer = 0f;
            _knockbackVelocity = Vector2.zero;

            if (_spriteRenderer != null && _originalMaterial != null)
            {
                _spriteRenderer.sharedMaterial = _originalMaterial;
            }

            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
            }
        }

        private void OnDisable()
        {
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _flashCoroutine = null;
            }

            if (_spriteRenderer != null && _originalMaterial != null)
            {
                _spriteRenderer.sharedMaterial = _originalMaterial;
            }

            if (_col != null)
            {
                _col.enabled = true;
            }
        }

        private void Start()
        {
            // Tìm Player 1 lần duy nhất lúc khởi động
            FindPlayer();
        }

        private void FindPlayer()
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                _playerTransform = player.transform;
            }
        }

        private void FixedUpdate()
        {
            // Nếu đang trong thời gian bị đẩy lùi thì nhận lực knockback, sau đó tự hồi phục di chuyển
            if (_knockbackTimer > 0f)
            {
                _knockbackTimer -= Time.fixedDeltaTime;
                _rb.linearVelocity = _knockbackVelocity;
                _knockbackVelocity = Vector2.MoveTowards(_knockbackVelocity, Vector2.zero, _knockbackDecay * Time.fixedDeltaTime);
            }
            else
            {
                Move();
            }
        }

        private void Update()
        {
            HandleFlip();
        }

        private void Move()
        {
            if (_playerTransform == null)
            {
                FindPlayer();
                return;
            }

            Vector2 moveDirection = Vector2.zero;

            // 1. Ưu tiên lấy hướng từ dòng chảy FlowField (tự biết lách qua mọi góc tường)
            if (FlowField.FlowFieldManager.Instance != null)
            {
                Vector2 flowDir = FlowField.FlowFieldManager.Instance.GetFlowDirection(transform.position);
                if (flowDir.sqrMagnitude > 0.01f)
                {
                    moveDirection = flowDir;
                }
            }

            // 2. Dự phòng: Nếu quái đứng ngoài tầm lưới FlowField thì chạy thẳng về phía Player
            if (moveDirection == Vector2.zero)
            {
                Vector2 toPlayer = (Vector2)_playerTransform.position - (Vector2)transform.position;
                moveDirection = toPlayer.normalized;
            }

            // Đẩy quái di chuyển bằng vận tốc vật lý chuẩn Unity 6
            _rb.linearVelocity = moveDirection * _moveSpeed;
        }

        private void HandleFlip()
        {
            if (_spriteRenderer == null) return;

            // Lật mặt Sprite theo chiều di chuyển X
            if (_rb.linearVelocity.x > 0.05f)
            {
                _spriteRenderer.flipX = false;
            }
            else if (_rb.linearVelocity.x < -0.05f)
            {
                _spriteRenderer.flipX = true;
            }
        }

        // Tương thích ngược nếu chỉ truyền lượng sát thương
        public void TakeDamage(int damage)
        {
            TakeDamage(damage, Vector2.zero, 0f);
        }

        // Hàm nhận sát thương đầy đủ (sát thương, hướng đẩy lùi, lực giật)
        public void TakeDamage(int damage, Vector2 hitDirection, float knockbackForce = 0f)
        {
            // BẢO VỆ CHỐNG SOFTLOCK: Nếu quái đã chết hoặc đang trong trạng thái hủy, bỏ qua mọi sát thương tiếp theo
            if (_isDead || _currentHealth <= 0) return;

            _currentHealth -= damage;
            Debug.Log($"<color=red>[ENEMY HIT]</color> {gameObject.name} bị trừ {damage} máu! Còn: {_currentHealth}/{_maxHealth}");

            // 1. Áp dụng lực đẩy lùi (Knockback) nếu có
            if (knockbackForce > 0f && hitDirection.sqrMagnitude > 0.001f)
            {
                _knockbackVelocity = hitDirection.normalized * knockbackForce;
                _knockbackTimer = 0.1f;
            }

            // 2. Chớp trắng khi trúng đòn (Hit Flash White)
            FlashWhite();

            // 3. Hiển thị số sát thương bay lên (Floating Damage Numbers)
            if (UI.DamageTextManager.Instance != null)
            {
                UI.DamageTextManager.Instance.Spawn(damage, transform.position);
            }

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        private void FlashWhite()
        {
            if (_spriteRenderer == null || _flashMaterial == null) return;

            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
            }
            _flashCoroutine = StartCoroutine(FlashRoutine());
        }

        private System.Collections.IEnumerator FlashRoutine()
        {
            _spriteRenderer.sharedMaterial = _flashMaterial;
            yield return new WaitForSeconds(_flashDuration);
            if (_originalMaterial != null)
            {
                _spriteRenderer.sharedMaterial = _originalMaterial;
            }
            _flashCoroutine = null;
        }

        private void Die()
        {
            // BẢO VỆ CHỐNG SOFTLOCK: Chỉ được chết 1 lần duy nhất!
            if (_isDead) return;
            _isDead = true;

            // Khóa va chạm vật lý ngay lập tức để không nhận thêm bất kỳ viên đạn nào khác trong cùng frame
            if (_col != null) _col.enabled = false;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;

            // Đảm bảo khôi phục lại Material trước khi trả về Pool
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _flashCoroutine = null;
            }
            if (_spriteRenderer != null && _originalMaterial != null)
            {
                _spriteRenderer.sharedMaterial = _originalMaterial;
            }

            Debug.Log($"<color=orange>[ENEMY DEAD]</color> {gameObject.name} đã bị tiêu diệt!");

            // 1. Cộng thẳng Souls cho người chơi (Zero Pickup Delay)
            int souls = _data != null ? _data.SoulReward : 2;
            if (Dungeon.Run.RunManager.Instance != null)
            {
                Dungeon.Run.RunManager.Instance.AddSouls(souls);
            }

            // 2. Bắn sự kiện quái chết để RoomController theo dõi tiến độ Wave
            OnEnemyDied?.Invoke(this);

            // 3. Rơi ngọc kinh nghiệm tại vị trí quái chết
            DropExpGem();

            // Trả quái về PoolManager để tái sử dụng (Zero GC Allocation)
            if (Core.PoolManager.Instance != null && _prefabOrigin != null)
            {
                Core.PoolManager.Instance.ReturnToPool(_prefabOrigin, gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void DropExpGem()
        {
            if (_data == null || _data.ExpGemPrefab == null || Core.PoolManager.Instance == null) return;

            GameObject gemObj = Core.PoolManager.Instance.Get(_data.ExpGemPrefab, transform.position, Quaternion.identity);
            if (gemObj != null && gemObj.TryGetComponent<ExpGem>(out var gem))
            {
                gem.Initialize(_data.ExpReward, _data.ExpGemPrefab);
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            AttackPlayer(collision.gameObject);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            AttackPlayer(other.gameObject);
        }

        private void AttackPlayer(GameObject target)
        {
            if (target.TryGetComponent<PlayerHealth>(out var playerHealth))
            {
                int damage = _data != null ? _data.ContactDamage : 5;
                playerHealth.TakeDamage(damage);
            }
        }
    }
}

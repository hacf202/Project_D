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

        [Header("Attack Settings")]
        [Tooltip("Khoảng thời gian tối thiểu giữa 2 lần gây sát thương va chạm (giây)")]
        [SerializeField] private float _attackInterval = 1f;
        private float _attackTimer = 0f;
        private float _specialSkillTimer = 3f;
        private bool _isExploding = false;
        private Coroutine _explodeCoroutine;

        [Header("Health Settings")]
        [SerializeField] private int _maxHealth = 30;
        private int _currentHealth;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private Rigidbody2D _rb;
        private Transform _playerTransform;
        private Player.PlayerController _playerController;
        private Material _originalMaterial;
        private Coroutine _flashCoroutine;
        private Vector2 _knockbackVelocity;
        private float _knockbackTimer;
        private Collider2D _col;
        private CircleCollider2D _hitboxTrigger;
        private Collider2D _playerCol;
        private bool _isDead = false;

        // TRỤ CỘT 2: KHÓA MỤC TIÊU CÓ ĐỘ TRỄ (STICKY TARGET / ANTI-JITTER)
        private Project_D.Minions.MinionController _lockedMinion;
        private float _targetLockTimer = 0f;

        // Observer Pattern: RoomController lắng nghe để đếm tiến độ Wave
        public static event System.Action<EnemyController> OnEnemyDied;

        public EnemyDataSO Data => _data;
        public bool IsDead => _isDead;

        public void Initialize(EnemyDataSO data, GameObject prefabOrigin)
        {
            _data = data;
            _prefabOrigin = prefabOrigin;
            _isDead = false;
            _isExploding = false;

            if (_col != null) _col.enabled = true;
            if (_hitboxTrigger != null) _hitboxTrigger.enabled = true;

            if (_data != null)
            {
                _moveSpeed = _data.MoveSpeed;
                _maxHealth = _data.MaxHealth;
                _currentHealth = _maxHealth;
                _attackInterval = _data.AttackInterval;
            }
            _attackTimer = 0f;
            _specialSkillTimer = 3f;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.mass = 1f;

            // Khởi tạo Collider vật lý:
            // Tiếp xúc va chạm chuẩn, không loại trừ layer nào để Player và Enemy chạm nhau mà không đi xuyên qua
            var circleCols = GetComponents<CircleCollider2D>();
            _col = circleCols.Length > 0 ? circleCols[0] : GetComponent<Collider2D>();
            if (_col != null)
            {
                var frictionlessMat = new PhysicsMaterial2D("Frictionless_Enemy")
                {
                    friction = 0f,
                    bounciness = 0f
                };
                _col.sharedMaterial = frictionlessMat;
                _col.excludeLayers = 0;
            }

            // Nếu Prefab có sẵn collider thứ 2 làm Trigger thì hỗ trợ tương thích
            if (circleCols.Length > 1)
            {
                _hitboxTrigger = circleCols[1];
                _hitboxTrigger.isTrigger = true;
                _hitboxTrigger.excludeLayers = 0;
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
            _isExploding = false;
            _specialSkillTimer = 3f;

            if (_explodeCoroutine != null)
            {
                StopCoroutine(_explodeCoroutine);
                _explodeCoroutine = null;
            }

            if (_col != null) _col.enabled = true;
            if (_hitboxTrigger != null) _hitboxTrigger.enabled = true;

            if (_data != null)
            {
                _moveSpeed = _data.MoveSpeed;
                _maxHealth = _data.MaxHealth;
                _currentHealth = _maxHealth;
                _attackInterval = _data.AttackInterval;
            }
            else
            {
                _currentHealth = _maxHealth;
            }

            _attackTimer = 0f;

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

            _lockedMinion = null;
            _targetLockTimer = 0f;
        }

        private void OnDisable()
        {
            if (_lockedMinion != null)
            {
                _lockedMinion.UnregisterAttacker(this);
                _lockedMinion = null;
            }

            if (_explodeCoroutine != null)
            {
                StopCoroutine(_explodeCoroutine);
                _explodeCoroutine = null;
            }
            _isExploding = false;

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
                _playerController = player;
                _playerCol = player.GetComponent<Collider2D>();
            }
        }

        private void FixedUpdate()
        {
            // 0. Giảm thời gian hồi chiêu
            if (_attackTimer > 0f)
            {
                _attackTimer -= Time.fixedDeltaTime;
            }

            if (_specialSkillTimer > 0f)
            {
                _specialSkillTimer -= Time.fixedDeltaTime;
            }

            // Đếm ngược thời gian khóa mục tiêu chống rung lắc (Sticky Target)
            if (_targetLockTimer > 0f)
            {
                _targetLockTimer -= Time.fixedDeltaTime;
            }

            // 1. Nếu đang nổ (Shadow Bomber fuse) hoặc đã chết: đứng yên
            if (_isExploding || _isDead)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            // 2. Nếu đang trong thời gian bị đẩy lùi thì nhận lực knockback, sau đó tự hồi phục di chuyển
            if (_knockbackTimer > 0f)
            {
                _knockbackTimer -= Time.fixedDeltaTime;
                _rb.linearVelocity = _knockbackVelocity;
                _knockbackVelocity = Vector2.MoveTowards(_knockbackVelocity, Vector2.zero, _knockbackDecay * Time.fixedDeltaTime);
            }
            // 3. Nếu người chơi di chuyển đâm vào quái (trừ Boss/MiniBoss không bị đẩy)
            else if (!IsBossOrMiniBoss && HandlePlayerPushAside())
            {
                // Unstuck and slide if against wall
            }
            else
            {
                Move();
            }

            // 4. Kỹ năng đặc biệt của Boss / Mini-Boss
            HandleBossSkills();

            // Chống kẹt tường và ngăn quái văng ra ngoài phòng (kể cả khi bị knockback cực mạnh)
            Project_D.Dungeon.Room.RoomBoundaryValidator.ValidateAndUnstuck(transform, 0.4f, _rb);
        }

        private bool IsBossOrMiniBoss => _data != null && _data.IsBossOrMiniBoss;

        private void Update()
        {
            HandleFlip();
        }

        private Transform GetNearestTarget()
        {
            // 1. Kiểm tra mục tiêu đã khóa (Sticky Target): Giữ vững mục tiêu trong 1.5s để chống rung lắc (anti-jitter)
            if (_lockedMinion != null)
            {
                if (_lockedMinion.IsAlive && _lockedMinion.gameObject.activeInHierarchy && _targetLockTimer > 0f)
                {
                    float distSqr = ((Vector2)_lockedMinion.transform.position - (Vector2)transform.position).sqrMagnitude;
                    if (distSqr <= 64f) // Trong cự ly 8m, tiếp tục kiên định truy đuổi con lính này
                    {
                        return _lockedMinion.transform;
                    }
                }

                _lockedMinion.UnregisterAttacker(this);
                _lockedMinion = null;
            }

            Transform nearest = _playerTransform;
            float minSqrDist = _playerTransform != null ? ((Vector2)_playerTransform.position - (Vector2)transform.position).sqrMagnitude : float.MaxValue;
            Project_D.Minions.MinionController bestMinion = null;

            // 2. Tìm kiếm mục tiêu - ƯU TIÊN LÍNH TỐI ĐA (Minions Taunt Priority)
            // Lính sinh ra để thu hút quái và phân tán áp lực cho người chơi nên được nhân hệ số ưu tiên cao
            if (Project_D.Minions.MinionManager.Instance != null)
            {
                var minions = Project_D.Minions.MinionManager.Instance.ActiveMinions;
                for (int i = 0; i < minions.Count; i++)
                {
                    var m = minions[i];
                    if (m != null && m.IsAlive)
                    {
                        float sqrD = ((Vector2)m.transform.position - (Vector2)transform.position).sqrMagnitude;

                        // Lính còn chỗ nhận quái được giảm 55% khoảng cách biểu kiến (hút quái mạnh hơn Player gấp đôi)
                        float weightedDist = m.CanAcceptMoreAttackers ? sqrD * 0.45f : sqrD;

                        if (weightedDist < minSqrDist)
                        {
                            minSqrDist = weightedDist;
                            bestMinion = m;
                            nearest = m.transform;
                        }
                    }
                }
            }

            // 3. Đăng ký nhận vé và khóa mục tiêu nếu chọn được Lính
            if (bestMinion != null)
            {
                if (_lockedMinion != bestMinion)
                {
                    if (_lockedMinion != null) _lockedMinion.UnregisterAttacker(this);
                    _lockedMinion = bestMinion;
                    _lockedMinion.RegisterAttacker(this);
                }
                _targetLockTimer = 1.5f; // Khóa 1.5s
                return _lockedMinion.transform;
            }
            else
            {
                if (_lockedMinion != null)
                {
                    _lockedMinion.UnregisterAttacker(this);
                    _lockedMinion = null;
                }
            }

            return nearest;
        }

        private void Move()
        {
            if (_playerTransform == null)
            {
                FindPlayer();
                return;
            }

            Transform nearestTarget = GetNearestTarget();
            if (nearestTarget == null) return;

            float distToTarget = Vector2.Distance(transform.position, nearestTarget.position);

            // A. Quái cảm tử phát nổ (Shadow Bomber)
            if (_data != null && _data.AttackMethod == EnemyAttackMethod.Explode)
            {
                float triggerRadius = _data.AttackRange > 0f ? _data.AttackRange : 1.2f;
                if (distToTarget <= triggerRadius && !_isExploding)
                {
                    _explodeCoroutine = StartCoroutine(ExplodeSequenceRoutine());
                    return;
                }
            }

            // B. Quái bắn xa (Ranged Cultist, Void Watcher, Necromancer King...)
            if (_data != null && _data.AttackMethod == EnemyAttackMethod.Ranged && _data.AttackRange > 0f)
            {
                if (distToTarget <= _data.AttackRange)
                {
                    // Đủ cự ly: Tấn công tầm xa khi hết cooldown
                    if (_attackTimer <= 0f)
                    {
                        FireProjectileAtTarget(nearestTarget);
                        _attackTimer = _attackInterval;
                    }

                    // Nếu mục tiêu áp sát quá gần (< 45% cự ly bắn): lùi nhẹ để giữ khoảng cách (Kite)
                    if (distToTarget < _data.AttackRange * 0.45f)
                    {
                        Vector2 retreatDir = ((Vector2)transform.position - (Vector2)nearestTarget.position).normalized;
                        _rb.linearVelocity = retreatDir * (_moveSpeed * 0.6f);
                    }
                    else
                    {
                        _rb.linearVelocity = Vector2.zero;
                    }
                    return;
                }
            }

            Vector2 moveDirection = Vector2.zero;

            // 1. Khi ở cự ly giáp mặt cận chiến (<= 3.0m):
            // - Nếu mục tiêu là Lính: Di chuyển về vị trí VỆ TINH BAO VÂY (Surround Slot) để quây tròn quanh lính, chống đè lên nhau
            // - Nếu mục tiêu là Player: Di chuyển trực diện áp sát
            if (nearestTarget != null && distToTarget <= 3.0f)
            {
                if (_lockedMinion != null && nearestTarget == _lockedMinion.transform)
                {
                    Vector2 surroundSlot = _lockedMinion.GetSurroundSlot(GetHashCode());
                    Vector2 toSlot = surroundSlot - (Vector2)transform.position;
                    moveDirection = toSlot.sqrMagnitude > 0.05f ? toSlot.normalized : ((Vector2)nearestTarget.position - (Vector2)transform.position).normalized;
                }
                else
                {
                    moveDirection = ((Vector2)nearestTarget.position - (Vector2)transform.position).normalized;
                }
            }
            // 2. Khi ở ngoài tầm cận chiến (> 3.0m) hoặc di chuyển từ xa trong hầm ngục:
            // 100% đi theo Lưới Quái toàn cục (dẫn tự nhiên qua mọi ngóc ngách tới Lính hoặc Player)
            else if (FlowField.FlowFieldManager.Instance != null)
            {
                Vector2 flowDir = FlowField.FlowFieldManager.Instance.GetFlowDirection(transform.position);
                if (flowDir.sqrMagnitude > 0.01f)
                {
                    moveDirection = flowDir;
                }
            }

            // 3. Dự phòng: Nếu quái đứng ngoài tầm lưới FlowField thì chạy về phía mục tiêu gần nhất (Người chơi hoặc Lính)
            if (moveDirection == Vector2.zero)
            {
                if (nearestTarget != null)
                {
                    Vector2 toTarget = (Vector2)nearestTarget.position - (Vector2)transform.position;
                    moveDirection = toTarget.normalized;
                }
            }

            // 3. Ngăn quái đâm xuyên tâm hoặc đẩy mục tiêu khi đã tiếp xúc mép collider (Người chơi hoặc Lính)
            Transform currentTarget = nearestTarget;
            if (currentTarget != null)
            {
                Collider2D targetCol = currentTarget.GetComponent<Collider2D>();
                float targetRadius = 0.5f;
                Vector2 targetColOffset = Vector2.zero;
                if (targetCol is CircleCollider2D tCircle)
                {
                    targetRadius = tCircle.radius * currentTarget.lossyScale.x;
                    targetColOffset = tCircle.offset;
                }
                else if (targetCol != null)
                {
                    targetRadius = targetCol.bounds.extents.x;
                }

                float myRadius = 0.15f;
                Vector2 myColOffset = Vector2.zero;
                if (_col is CircleCollider2D myCircle)
                {
                    myRadius = myCircle.radius * transform.lossyScale.x;
                    myColOffset = myCircle.offset;
                }
                else if (_col != null)
                {
                    myRadius = _col.bounds.extents.x;
                }

                // Khoảng cách chạm mép collider thực tế của 2 thực thể
                float contactDistance = targetRadius + myRadius + 0.05f;

                Vector2 enemyCenter = (Vector2)transform.position + myColOffset;
                Vector2 targetCenter = (Vector2)currentTarget.position + targetColOffset;
                Vector2 toTarget = targetCenter - enemyCenter;
                float currentDistToTarget = toTarget.magnitude;

                if (currentDistToTarget <= contactDistance && currentDistToTarget > 0.001f)
                {
                    Vector2 dirToTarget = toTarget / currentDistToTarget;
                    float forwardDot = Vector2.Dot(moveDirection, dirToTarget);
                    if (forwardDot > 0f)
                    {
                        // Triệt tiêu thành phần vector hướng vào mục tiêu: Quái dừng lực đẩy xô, duy trì tiếp xúc và tấn công
                        moveDirection -= dirToTarget * forwardDot;
                    }
                }
            }

            // Đẩy quái di chuyển bằng vận tốc vật lý chuẩn Unity 6
            _rb.linearVelocity = moveDirection * _moveSpeed;
        }

        // Tự động dạt sang hai bên khi người chơi di chuyển đâm vào (chống kẹt, chống chặn đường)
        private bool HandlePlayerPushAside()
        {
            if (_playerTransform == null) return false;

            if (_playerController == null)
            {
                _playerController = _playerTransform.GetComponent<Player.PlayerController>();
            }

            Vector2 playerVel = _playerController != null ? _playerController.CurrentVelocity : Vector2.zero;
            if (playerVel.sqrMagnitude < 0.1f) return false;

            Vector2 toEnemy = (Vector2)transform.position - (Vector2)_playerTransform.position;
            float dist = toEnemy.magnitude;

            // Bán kính nhường đường tính theo kích thước thực tế của người chơi và quái
            float playerRadius = (_playerCol is CircleCollider2D pc) ? pc.radius : 1.0f;
            float myRadius = (_col is CircleCollider2D mc) ? mc.radius : 0.15f;
            float pushRadius = playerRadius + myRadius + 0.45f;

            if (dist < pushRadius && dist > 0.001f)
            {
                if (Vector2.Dot(playerVel.normalized, toEnemy.normalized) > 0.1f)
                {
                    Vector2 lateralDir = Vector2.Perpendicular(playerVel.normalized);
                    if (Vector2.Dot(lateralDir, toEnemy) < 0f) lateralDir = -lateralDir;

                    Vector2 pushDir = (lateralDir * 0.85f + toEnemy.normalized * 0.35f).normalized;
                    float pushSpeed = Mathf.Max(playerVel.magnitude * 1.25f, 4.2f);
                    _rb.linearVelocity = pushDir * pushSpeed;
                    return true;
                }
            }
            return false;
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

            // 1. Áp dụng lực đẩy lùi (Knockback) có tính toán kháng đẩy lùi
            if (knockbackForce > 0f && hitDirection.sqrMagnitude > 0.001f)
            {
                float resistance = _data != null ? _data.KnockbackResistance : 0f;
                float effectiveForce = knockbackForce * (1f - resistance);
                if (effectiveForce > 0.05f)
                {
                    _knockbackVelocity = hitDirection.normalized * effectiveForce;
                    _knockbackTimer = 0.1f;
                }
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

            // Hủy đăng ký tấn công Lính để giải phóng slot cho quái khác
            if (_lockedMinion != null)
            {
                _lockedMinion.UnregisterAttacker(this);
                _lockedMinion = null;
            }

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

            // Rải nhẹ vị trí ngọc ngẫu nhiên (bán kính 0.25m) để tránh nhiều quái chết cùng lúc rơi đè trùng khít 100% tọa độ
            Vector2 scatter = UnityEngine.Random.insideUnitCircle * 0.25f;
            Vector3 dropPos = transform.position + new Vector3(scatter.x, scatter.y, 0f);

            GameObject gemObj = Core.PoolManager.Instance.Get(_data.ExpGemPrefab, dropPos, Quaternion.identity);
            if (gemObj != null && gemObj.TryGetComponent<ExpGem>(out var gem))
            {
                gem.Initialize(_data.ExpReward, _data.ExpGemPrefab);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            AttackTarget(collision.gameObject);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            AttackTarget(collision.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            AttackTarget(other.gameObject);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            AttackTarget(other.gameObject);
        }

        // =========================================================================
        // COMBAT BEHAVIORS (RANGED, EXPLODE, BOSS SKILLS)
        // =========================================================================

        private void FireProjectileAtTarget(Transform target)
        {
            if (target == null || _data == null || _data.ProjectilePrefab == null || Core.PoolManager.Instance == null) return;

            Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Quaternion rot = Quaternion.Euler(0f, 0f, angle);

            Vector3 spawnPos = transform.position + (Vector3)(dir * 0.45f);
            GameObject projObj = Core.PoolManager.Instance.Get(_data.ProjectilePrefab, spawnPos, rot);
            if (projObj != null && projObj.TryGetComponent<EnemyProjectile>(out var proj))
            {
                proj.Initialize(_data.ProjectilePrefab, _data.ProjectileDamage, _data.ProjectileSpeed);
            }
        }

        private System.Collections.IEnumerator ExplodeSequenceRoutine()
        {
            _isExploding = true;
            _rb.linearVelocity = Vector2.zero;

            // Nhấp nháy đỏ báo hiệu tích nổ trong 0.4s
            float fuseTime = 0.4f;
            float elapsed = 0f;
            bool isRed = false;

            while (elapsed < fuseTime)
            {
                if (_spriteRenderer != null)
                {
                    _spriteRenderer.color = isRed ? Color.white : new Color(1f, 0.2f, 0.2f, 1f);
                    isRed = !isRed;
                }
                yield return new WaitForSeconds(0.08f);
                elapsed += 0.08f;
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.white;
            }

            ExplodeAoE();
        }

        private void ExplodeAoE()
        {
            if (_isDead) return;

            float radius = _data != null ? _data.ExplosionRadius : 2.5f;
            int dmg = _data != null ? _data.ContactDamage : 25;

            // Gây sát thương diện rộng lên Người chơi
            if (_playerTransform != null && Vector2.Distance(transform.position, _playerTransform.position) <= radius)
            {
                if (_playerTransform.TryGetComponent<PlayerHealth>(out var ph))
                {
                    ph.TakeDamage(dmg);
                }
            }

            // Gây sát thương lên Lính triệu hồi
            if (Project_D.Minions.MinionManager.Instance != null)
            {
                var minions = Project_D.Minions.MinionManager.Instance.ActiveMinions;
                for (int i = 0; i < minions.Count; i++)
                {
                    var m = minions[i];
                    if (m != null && m.IsAlive && Vector2.Distance(transform.position, m.transform.position) <= radius)
                    {
                        m.TakeDamage(dmg);
                    }
                }
            }

            Debug.Log($"<color=orange>[EXPLOSION]</color> {gameObject.name} nổ tung gây {dmg} sát thương diện rộng!");
            Die();
        }

        private void HandleBossSkills()
        {
            if (_data == null || !_data.IsBossOrMiniBoss || _specialSkillTimer > 0f || _isDead) return;

            Transform target = GetNearestTarget();
            if (target == null) return;

            string enemyName = _data.EnemyName;

            // 1. Đại Boss Lửa (Inferno Overlord): Bắn cầu lửa 360 độ (Flame Nova)
            if (enemyName.Contains("Inferno"))
            {
                CastFlameNova();
                _specialSkillTimer = 5.0f;
            }
            // 2. Đại Boss Chiêu Hồn (Necromancer King): Triệu hồi bầy Skeleton Swarmer
            else if (enemyName.Contains("Necromancer"))
            {
                CastSummonMinions();
                _specialSkillTimer = 6.5f;
            }
            // 3. Mini Boss Void Watcher: Bắn loạt đạn chùm 3 viên hư không
            else if (enemyName.Contains("Void"))
            {
                CastSpreadShot(target);
                _specialSkillTimer = 2.8f;
            }
            // 4. Mini Boss Bone Golem: Dậm đất chấn động diện rộng
            else if (enemyName.Contains("Bone") || enemyName.Contains("Golem"))
            {
                CastGroundSlam();
                _specialSkillTimer = 4.2f;
            }
        }

        private void CastFlameNova()
        {
            if (_data == null || _data.ProjectilePrefab == null || Core.PoolManager.Instance == null) return;

            int count = 8;
            float step = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = i * step;
                Quaternion rot = Quaternion.Euler(0f, 0f, angle);
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                Vector3 spawnPos = transform.position + (Vector3)(dir * 0.7f);

                GameObject projObj = Core.PoolManager.Instance.Get(_data.ProjectilePrefab, spawnPos, rot);
                if (projObj != null && projObj.TryGetComponent<EnemyProjectile>(out var proj))
                {
                    proj.Initialize(_data.ProjectilePrefab, _data.ProjectileDamage, _data.ProjectileSpeed * 0.9f);
                }
            }
            Debug.Log($"<color=red><b>[BOSS SKILL]</b></color> {gameObject.name} thi triển Flame Nova 8 hướng!");
        }

        private void CastSpreadShot(Transform target)
        {
            if (target == null || _data == null || _data.ProjectilePrefab == null || Core.PoolManager.Instance == null) return;

            Vector2 baseDir = ((Vector2)target.position - (Vector2)transform.position).normalized;
            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

            float[] angleOffsets = new float[] { -20f, 0f, 20f };
            for (int i = 0; i < angleOffsets.Length; i++)
            {
                float angle = baseAngle + angleOffsets[i];
                Quaternion rot = Quaternion.Euler(0f, 0f, angle);
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                Vector3 spawnPos = transform.position + (Vector3)(dir * 0.5f);

                GameObject projObj = Core.PoolManager.Instance.Get(_data.ProjectilePrefab, spawnPos, rot);
                if (projObj != null && projObj.TryGetComponent<EnemyProjectile>(out var proj))
                {
                    proj.Initialize(_data.ProjectilePrefab, _data.ProjectileDamage, _data.ProjectileSpeed);
                }
            }
        }

        private void CastSummonMinions()
        {
            var swarmerData = Resources.Load<EnemyDataSO>("Enemies/SkeletonSwarmer_Data");
#if UNITY_EDITOR
            if (swarmerData == null)
            {
                swarmerData = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyDataSO>("Assets/_Project/ScriptableObjects/Enemies/SkeletonSwarmer_Data.asset");
            }
#endif
            if (swarmerData != null && swarmerData.Prefab != null && Core.PoolManager.Instance != null)
            {
                int summonCount = 2;
                for (int i = 0; i < summonCount; i++)
                {
                    Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(1.2f, 2.5f);
                    Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0f);
                    spawnPos = Project_D.Dungeon.Room.RoomBoundaryValidator.GetNearestWalkablePosition(spawnPos);

                    GameObject minionEnemy = Core.PoolManager.Instance.Get(swarmerData.Prefab, spawnPos, Quaternion.identity);
                    if (minionEnemy != null && minionEnemy.TryGetComponent<EnemyController>(out var ctrl))
                    {
                        ctrl.Initialize(swarmerData, swarmerData.Prefab);
                    }
                }
                Debug.Log($"<color=cyan><b>[BOSS SKILL]</b></color> {gameObject.name} triệu hồi {summonCount} Skeleton Swarmer!");
            }
        }

        private void CastGroundSlam()
        {
            float radius = 3.6f;
            int damage = Mathf.RoundToInt((_data != null ? _data.ContactDamage : 15) * 1.5f);

            // Sát thương lên Player
            if (_playerTransform != null && Vector2.Distance(transform.position, _playerTransform.position) <= radius)
            {
                if (_playerTransform.TryGetComponent<PlayerHealth>(out var ph))
                {
                    ph.TakeDamage(damage);
                }
            }

            // Sát thương lên Minions
            if (Project_D.Minions.MinionManager.Instance != null)
            {
                var minions = Project_D.Minions.MinionManager.Instance.ActiveMinions;
                for (int i = 0; i < minions.Count; i++)
                {
                    var m = minions[i];
                    if (m != null && m.IsAlive && Vector2.Distance(transform.position, m.transform.position) <= radius)
                    {
                        m.TakeDamage(damage);
                    }
                }
            }

            FlashWhite();
            Debug.Log($"<color=yellow><b>[MINI BOSS SKILL]</b></color> {gameObject.name} kích hoạt Ground Slam diện rộng {radius}m!");
        }

        private void AttackTarget(GameObject target)
        {
            if (_attackTimer > 0f || _isDead || target == null) return;

            bool dealtDamage = false;

            if (target.TryGetComponent<PlayerHealth>(out var playerHealth) || (playerHealth = target.GetComponentInParent<PlayerHealth>()) != null)
            {
                // Chỉ tính đòn đánh khi người chơi còn sống và không ở trạng thái bất tử tạm thời (I-Frames)
                if (playerHealth.IsAlive && !playerHealth.IsInvulnerable)
                {
                    int damage = _data != null ? _data.ContactDamage : 5;
                    playerHealth.TakeDamage(damage);
                    dealtDamage = true;
                }
            }
            else if (target.TryGetComponent<Project_D.Minions.MinionController>(out var minion) || (minion = target.GetComponentInParent<Project_D.Minions.MinionController>()) != null)
            {
                if (minion.IsAlive)
                {
                    int damage = _data != null ? _data.ContactDamage : 5;
                    minion.TakeDamage(damage);
                    dealtDamage = true;
                }
            }

            if (dealtDamage)
            {
                _attackTimer = _attackInterval;
            }
        }
    }
}

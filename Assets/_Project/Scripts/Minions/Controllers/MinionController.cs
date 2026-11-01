using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Project_D.Core;
using Project_D.Enemies;
using Project_D.Enemies.FlowField;
using Project_D.Player;
using Project_D.Dungeon.Room;

namespace Project_D.Minions
{
    public enum MinionState
    {
        Following,  // Theo sau người chơi trong vòng follow
        Combat,     // Giao chiến với quái vật
        Dead        // Đang trong hàng đợi hồi sinh 10 giây
    }

    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class MinionController : MonoBehaviour
    {
        [Header("Minion Profile")]
        [SerializeField] private MinionDataSO _data;
        [SerializeField] private MinionState _state = MinionState.Following;

        [Header("Movement & Separation Settings")]
        [Tooltip("Bán kính tác động của lực tách bầy (đã giảm nhỏ để tránh đẩy nhau mạnh)")]
        [SerializeField] private float _separationRadius = 0.55f;
        [SerializeField] private float _separationWeight = 0.8f;
        [SerializeField] private float _playerPushAsideRadius = 1.0f;
        [SerializeField] private LayerMask _enemyLayerMask;
        [SerializeField] private LayerMask _allyLayerMask;
        [SerializeField] private LayerMask _obstacleLayerMask;

        [Header("Visual Feedback")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Material _flashMaterial;
        [SerializeField] private float _flashDuration = 0.12f;

        private Rigidbody2D _rb;
        private Collider2D _col;
        private CircleCollider2D _hitboxTrigger;
        private Transform _playerTransform;
        private PlayerController _playerController;
        private EnemyController _targetEnemy;

        private int _currentHealth;
        private float _nextAttackTime = 0f;
        private Vector2 _targetFollowPoint;
        private bool _isCatchingUp = false;
        private Material _originalMaterial;
        private Coroutine _flashCoroutine;

        private ContactFilter2D _enemyFilter;
        private ContactFilter2D _allyFilter;

        // Bộ đệm tĩnh Zero GC cho OverlapCircle
        private static readonly List<Collider2D> _enemyBuffer = new List<Collider2D>(16);
        private static readonly List<Collider2D> _neighborBuffer = new List<Collider2D>(16);

        public MinionDataSO Data => _data;
        public MinionState State => _state;
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _data != null 
            ? Mathf.Max(1, Mathf.RoundToInt(_data.MaxHealth * (1f + (MinionManager.Instance != null ? MinionManager.Instance.MinionMaxHealthBonusPercent : 0f)))) 
            : 100;
        public int CurrentAttackDamage => _data != null 
            ? Mathf.Max(1, Mathf.RoundToInt(_data.AttackDamage * (MinionManager.Instance != null ? MinionManager.Instance.MinionDamageMultiplier : 1f))) 
            : 10;
        public float CurrentAttackSpeed => _data != null 
            ? Mathf.Max(0.1f, _data.AttackSpeed * (MinionManager.Instance != null ? MinionManager.Instance.MinionAttackSpeedMultiplier : 1f)) 
            : 1f;
        public float CurrentRespawnDuration => _data != null 
            ? Mathf.Max(1f, _data.RespawnDuration * (MinionManager.Instance != null ? MinionManager.Instance.MinionRespawnTimeMultiplier : 1f)) 
            : 10f;
        public bool IsAlive => _state != MinionState.Dead && _currentHealth > 0;

        // =========================================================================
        // TRỤ CỘT 2: QUẢN LÝ SỨC CHỨA AGGRO (COMBAT TOKENS) - LÍNH HÚT QUÁI TỐI ĐA
        // =========================================================================
        public int MaxAggroCapacity
        {
            get
            {
                if (_data == null) return 8;
                return _data.Class switch
                {
                    MinionClass.Tank => 12,    // Hộ vệ tanker hút tối đa 12 quái
                    MinionClass.Warrior => 8,  // Chiến binh hút 8 quái
                    MinionClass.Support => 6,  // Hỗ trợ hút 6 quái
                    MinionClass.Mage => 5,     // Pháp sư hút 5 quái
                    MinionClass.Archer => 5,   // Cung thủ hút 5 quái
                    _ => 8
                };
            }
        }

        private readonly HashSet<EnemyController> _engagedAttackers = new HashSet<EnemyController>();

        public int CurrentAttackerCount
        {
            get
            {
                CleanDeadAttackers();
                return _engagedAttackers.Count;
            }
        }

        public bool CanAcceptMoreAttackers => IsAlive && CurrentAttackerCount < MaxAggroCapacity;

        public bool RegisterAttacker(EnemyController enemy)
        {
            if (enemy == null || !IsAlive) return false;
            CleanDeadAttackers();
            if (_engagedAttackers.Count < MaxAggroCapacity)
            {
                _engagedAttackers.Add(enemy);
                return true;
            }
            return false;
        }

        public void UnregisterAttacker(EnemyController enemy)
        {
            if (enemy != null)
            {
                _engagedAttackers.Remove(enemy);
            }
        }

        private void CleanDeadAttackers()
        {
            _engagedAttackers.RemoveWhere(e => e == null || e.IsDead || !e.gameObject.activeInHierarchy);
        }

        // =========================================================================
        // TRỤ CỘT 3: VÒNG VỆ TINH TIẾP XÚC BAO VÂY (MULTI-TIER SURROUND SLOTS)
        // Quái chia đều theo góc tỏa tròn quanh lính (0.75m vòng trong, 1.35m vòng ngoài), không đè chồng lên nhau
        // =========================================================================
        public Vector2 GetSurroundSlot(int attackerId)
        {
            int slotIndex = Mathf.Abs(attackerId) % 8;
            float angle = slotIndex * (Mathf.PI * 2f / 8f);
            float radius = 0.75f;

            if (_engagedAttackers.Count > 8)
            {
                slotIndex = Mathf.Abs(attackerId) % 4;
                angle = (slotIndex * (Mathf.PI * 2f / 4f)) + (Mathf.PI / 4f);
                radius = 1.35f;
            }

            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            return (Vector2)transform.position + offset;
        }

        public void RefreshMaxHealth()
        {
            int max = MaxHealth;
            if (_currentHealth > max) _currentHealth = max;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_spriteRenderer != null)
            {
                _originalMaterial = _spriteRenderer.sharedMaterial;
                _spriteRenderer.sortingOrder = 5;
            }

            // Rigidbody 2D trơn trượt chuẩn ARPG
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Khởi tạo LayerMask
            if (_enemyLayerMask.value == 0)
            {
                _enemyLayerMask = LayerMask.GetMask("Enemy");
                if (_enemyLayerMask.value == 0) _enemyLayerMask = 1 << 6;
            }

            if (_allyLayerMask.value == 0)
            {
                int minionLayer = LayerMask.NameToLayer("Minion");
                if (minionLayer >= 0) _allyLayerMask = 1 << minionLayer;
                else _allyLayerMask = 1 << gameObject.layer;
            }

            if (_obstacleLayerMask.value == 0)
            {
                _obstacleLayerMask = LayerMask.GetMask("Obstacle", "Obstacle ");
                if (_obstacleLayerMask.value == 0) _obstacleLayerMask = 1 << 7;
            }

            // HỆ THỐNG COLLIDER KÉP (DUAL COLLIDER):
            // 1. Collider rắn chặn tường (Solid Obstacle Collider):
            //    Chỉ tương tác với Obstacle (Tường). Tuyệt đối không đi xuyên tường!
            //    Loại trừ Default (Player), Enemy, Minion để không bao giờ cản trở người chơi hay đẩy quái.
            var circleCols = GetComponents<CircleCollider2D>();
            _col = circleCols.Length > 0 ? circleCols[0] : gameObject.AddComponent<CircleCollider2D>();
            if (_col is CircleCollider2D solidCircle)
            {
                solidCircle.radius = 0.38f;
            }
            _col.isTrigger = false;
            _col.includeLayers = _obstacleLayerMask;
            _col.excludeLayers = LayerMask.GetMask("Default", "Enemy", "Minion");

            var frictionlessMat = new PhysicsMaterial2D("Frictionless_Minion")
            {
                friction = 0f,
                bounciness = 0f
            };
            _col.sharedMaterial = frictionlessMat;

            // 2. Collider Trigger nhận đòn (Hitbox Trigger):
            //    Dùng để phát hiện quái vật và nhận sát thương va chạm OnTriggerStay2D từ quái
            //    Vì là Trigger nên không đẩy quái, triệt tiêu 100% xung đột collider.
            _hitboxTrigger = circleCols.Length > 1 ? circleCols[1] : gameObject.AddComponent<CircleCollider2D>();
            _hitboxTrigger.radius = 0.5f;
            _hitboxTrigger.isTrigger = true;
            _hitboxTrigger.includeLayers = _enemyLayerMask;

            _enemyFilter = new ContactFilter2D();
            _enemyFilter.SetLayerMask(_enemyLayerMask);
            _enemyFilter.useLayerMask = true;
            _enemyFilter.useTriggers = true;

            _allyFilter = new ContactFilter2D();
            _allyFilter.SetLayerMask(_allyLayerMask);
            _allyFilter.useLayerMask = true;
            _allyFilter.useTriggers = true;
        }

        public void Initialize(MinionDataSO data, Transform playerTransform)
        {
            _data = data;
            _playerTransform = playerTransform;
            if (_playerTransform != null)
            {
                _playerController = _playerTransform.GetComponent<PlayerController>();
            }

            if (_data != null)
            {
                _currentHealth = _data.MaxHealth;
            }

            _targetEnemy = null;
            _state = MinionState.Following;
            _isCatchingUp = false;
            _nextAttackTime = Time.time;

            if (_playerTransform != null)
            {
                float radius = _playerController != null ? _playerController.MinionFollowRadius : 3.5f;
                _targetFollowPoint = MinionManager.Instance != null
                    ? MinionManager.Instance.GetFollowSlot(this, _playerTransform, radius)
                    : (Vector2)_playerTransform.position;
            }

            if (_col != null) _col.enabled = true;
            if (_spriteRenderer != null) _spriteRenderer.enabled = true;
        }

        private void Update()
        {
            if (!IsAlive) return;

            if (_playerTransform == null)
            {
                FindPlayer();
                if (_playerTransform == null) return;
            }

            switch (_state)
            {
                case MinionState.Following:
                    UpdateFollowingState();
                    break;

                case MinionState.Combat:
                    UpdateCombatState();
                    break;
            }
        }

        private void FixedUpdate()
        {
            if (!IsAlive)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            // 1. Chống kẹt tường và ngăn lính văng ra ngoài phòng tuyệt đối
            Project_D.Dungeon.Room.RoomBoundaryValidator.ValidateAndUnstuck(transform, 0.35f, _rb);

            // 2. KIỂM TRA LỰC ĐẨY DẠT SANG 2 BÊN TỪ NGƯỜI CHƠI (PLAYER PUSH-ASIDE)
            // Chỉ người chơi di chuyển mới đẩy lính dạt sang 2 bên, lính không bao giờ đẩy lại người chơi!
            if (HandlePlayerPushAside())
            {
                ApplyWallSlideProtection();
                return;
            }

            switch (_state)
            {
                case MinionState.Following:
                    FixedUpdateFollowing();
                    break;

                case MinionState.Combat:
                    FixedUpdateCombat();
                    break;
            }

            // 3. Bảo vệ trượt dọc theo mép tường khi di chuyển
            ApplyWallSlideProtection();
        }

        private void FindPlayer()
        {
            var pc = Object.FindAnyObjectByType<PlayerController>();
            if (pc != null)
            {
                _playerController = pc;
                _playerTransform = pc.transform;
            }
        }

        // =========================================================================
        // 1. NGƯỜI CHƠI ĐẨY LÍNH DẠT SANG 2 BÊN (PLAYER EVASION STEERING)
        // =========================================================================

        private bool HandlePlayerPushAside()
        {
            if (_playerTransform == null) return false;

            Vector2 toMinion = (Vector2)transform.position - (Vector2)_playerTransform.position;
            float distToPlayer = toMinion.magnitude;

            if (distToPlayer < _playerPushAsideRadius && distToPlayer > 0.001f)
            {
                Vector2 playerVel = _playerController != null ? _playerController.CurrentVelocity : Vector2.zero;

                // Nếu người chơi đang di chuyển tiến tới -> lính chủ động dạt sang 2 bên
                if (playerVel.sqrMagnitude > 0.05f)
                {
                    Vector2 playerDir = playerVel.normalized;
                    // Vector vuông góc (tiếp tuyến 2 bên)
                    Vector2 perp = new Vector2(-playerDir.y, playerDir.x);
                    float side = Vector2.Dot(toMinion, perp) >= 0f ? 1f : -1f;

                    // Tổng hợp hướng né: 75% dạt ngang + 25% tản ra xa tâm người chơi
                    Vector2 pushDir = (perp * side * 0.75f + toMinion.normalized * 0.25f).normalized;
                    float pushSpeed = Mathf.Max(playerVel.magnitude * 1.3f, 4.5f);

                    _rb.linearVelocity = pushDir * pushSpeed;
                    HandleFacingDirection(_rb.linearVelocity.x);
                    return true;
                }
            }

            return false;
        }

        // =========================================================================
        // 2. CƠ CHẾ FOLLOW (VÒNG BÁN KÍNH TRÊN NGƯỜI CHƠI & CHỐNG ĐẨY NHAU VÔ HẠN)
        // =========================================================================

        private void UpdateFollowingState()
        {
            // Ưu tiên cao nhất: Kiểm tra quái trong tầm Aggro
            EnemyController detectedEnemy = ScanForEnemyInAggro();
            if (detectedEnemy != null)
            {
                _targetEnemy = detectedEnemy;
                _state = MinionState.Combat;
                _isCatchingUp = false;
                return;
            }

            float followRadius = _playerController != null ? _playerController.MinionFollowRadius : 3.5f;
            float distToPlayer = Vector2.Distance(transform.position, _playerTransform.position);
            bool hasLineOfSight = RoomBoundaryValidator.HasLineOfSight(transform.position, _playerTransform.position);

            // Lính tiến hành follow khi nằm NGOÀI vòng follow của người chơi HOẶC bị tường cản tầm nhìn với người chơi!
            // Khi người chơi di chuyển nhẹ bên trong vòng và không bị tường cản, lính ĐỨNG YÊN không tịnh tiến theo!
            if (distToPlayer > followRadius || !hasLineOfSight)
            {
                if (!_isCatchingUp)
                {
                    _isCatchingUp = true;
                    // Lấy vị trí đích đến phân bổ thông minh từ MinionManager (đảm bảo cách xa các lính khác >= 0.8m)
                    if (MinionManager.Instance != null)
                    {
                        _targetFollowPoint = MinionManager.Instance.GetFollowSlot(this, _playerTransform, followRadius);
                    }
                    else
                    {
                        _targetFollowPoint = (Vector2)_playerTransform.position;
                    }
                }
            }
        }

        private void FixedUpdateFollowing()
        {
            float followRadius = _playerController != null ? _playerController.MinionFollowRadius : 3.5f;
            float distToPlayer = Vector2.Distance(transform.position, _playerTransform.position);

            if (_isCatchingUp)
            {
                // Nếu người chơi chạy tiếp khiến điểm neo cũ bị lệch ra ngoài vòng follow, hoặc điểm neo bị tường chắn
                bool needNewSlot = Vector2.Distance(_playerTransform.position, _targetFollowPoint) > followRadius;
                if (!needNewSlot && !RoomBoundaryValidator.HasLineOfSight(_playerTransform.position, _targetFollowPoint))
                {
                    needNewSlot = true;
                }

                if (needNewSlot)
                {
                    if (MinionManager.Instance != null)
                    {
                        _targetFollowPoint = MinionManager.Instance.GetFollowSlot(this, _playerTransform, followRadius);
                    }
                }

                Vector2 toTarget = _targetFollowPoint - (Vector2)transform.position;
                float distToTarget = toTarget.magnitude;
                bool minionHasLOS = RoomBoundaryValidator.HasLineOfSight(transform.position, _playerTransform.position);

                // Dừng lại nghỉ chân khi đã tới slot hoặc đã vào vòng follow an toàn và có Line of Sight trực tiếp với người chơi
                if (minionHasLOS && (distToTarget <= 0.45f || (distToPlayer <= followRadius * 0.85f && distToTarget <= 0.75f)))
                {
                    _isCatchingUp = false;
                    _rb.linearVelocity = Vector2.zero;
                    return;
                }

                Vector2 moveDir = toTarget.normalized;

                // NẾU BỊ TƯỜNG NGĂN CÁCH VỚI NGƯỜI CHƠI HOẶC VỚI ĐÍCH ĐẾN:
                // Sử dụng Lưới Lính riêng biệt (100% dẫn về Player, không bị hố tự thân) để tự động lách qua mọi góc tường!
                bool isBlockedByWall = !minionHasLOS || !RoomBoundaryValidator.HasLineOfSight(transform.position, _targetFollowPoint);
                if (isBlockedByWall && FlowFieldManager.Instance != null)
                {
                    Vector2 flowDir = FlowFieldManager.Instance.GetMinionFlowDirection(transform.position);
                    if (flowDir.sqrMagnitude > 0.01f)
                    {
                        moveDir = flowDir;
                    }
                }

                // CẢNH BÁO VA CHẠM THÂN LÍNH VỚI TƯỜNG (CircleCast quét theo bán kính thân 0.32m):
                // Bất kể đang đi thẳng hay theo FlowField, nếu thân lính tiến sát mép tường trong cự ly 0.45m,
                // lập tức bẻ lái mượt mà theo tiếp tuyến mặt tường + lực đẩy nhẹ 0.2m ra xa tường để lách góc cua hoàn hảo!
                RaycastHit2D wallHit = Physics2D.CircleCast(transform.position, 0.32f, moveDir, 0.45f, _obstacleLayerMask);
                if (wallHit.collider != null)
                {
                    Vector2 wallNormal = wallHit.normal;
                    Vector2 tangent = new Vector2(-wallNormal.y, wallNormal.x);
                    Vector2 guideVec = isBlockedByWall ? moveDir : toTarget;
                    float dot = Vector2.Dot(tangent, guideVec);
                    Vector2 slideTangent = dot >= 0 ? tangent : -tangent;
                    moveDir = (slideTangent + wallNormal * 0.2f).normalized;
                }

                // Di chuyển về phía điểm đích + lực tách bầy mượt mà (smooth acceleration, chống rung giật)
                float speed = _data != null ? _data.MoveSpeed : 4f;
                Vector2 desiredVel = moveDir * speed;
                Vector2 separation = CalculateSeparationForce(isMoving: true);
                Vector2 targetVel = desiredVel + separation * _separationWeight;
                targetVel = Vector2.ClampMagnitude(targetVel, speed);

                _rb.linearVelocity = Vector2.MoveTowards(_rb.linearVelocity, targetVel, speed * 8f * Time.fixedDeltaTime);
                HandleFacingDirection(_rb.linearVelocity.x);
            }
            else
            {
                // ĐANG NGHỈ CHÂN BÊN TRONG VÒNG FOLLOW CỦA NGƯỜI CHƠI:
                // Nếu hai con lính vô tình quá sát nhau (< 0.45m), chỉ đẩy nhẹ dạt ra mượt mà rồi triệt tiêu vận tốc
                Vector2 separation = CalculateSeparationForce(isMoving: false);
                if (separation.sqrMagnitude > 0.01f)
                {
                    Vector2 targetSepVel = separation * (_data != null ? _data.MoveSpeed * 0.35f : 1.2f);
                    _rb.linearVelocity = Vector2.MoveTowards(_rb.linearVelocity, targetSepVel, 6f * Time.fixedDeltaTime);
                }
                else
                {
                    // Triệt tiêu vận tốc hoàn toàn -> Không bao giờ có hiện tượng đẩy nhau vô hạn!
                    _rb.linearVelocity = Vector2.MoveTowards(_rb.linearVelocity, Vector2.zero, 10f * Time.fixedDeltaTime);
                }
            }
        }

        // =========================================================================
        // 3. THUẬT TOÁN TÁCH BẦY (FLOCKING SEPARATION: PHẠM VI NHỎ, SUY GIẢM TUYẾN TÍNH)
        // =========================================================================

        private Vector2 CalculateSeparationForce(bool isMoving)
        {
            _neighborBuffer.Clear();
            int count = Physics2D.OverlapCircle(transform.position, _separationRadius, _allyFilter, _neighborBuffer);
            if (count <= 1) return Vector2.zero;

            Vector2 separationForce = Vector2.zero;
            int neighbors = 0;

            for (int i = 0; i < count; i++)
            {
                var col = _neighborBuffer[i];
                if (col == null || col.gameObject == gameObject) continue;

                // Bỏ qua Trigger hitbox để không tính trùng 2 collider trên cùng 1 lính
                if (col.isTrigger) continue;

                // CHỐNG RUNG LẮC: Tuyệt đối chỉ tính lực tách bầy với ĐỒNG ĐỘI LÍNH, không tính Player hay vật thể khác
                if (!col.TryGetComponent<MinionController>(out var otherMinion) || !otherMinion.IsAlive) continue;

                Vector2 diff = (Vector2)transform.position - (Vector2)col.transform.position;
                float dist = diff.magnitude;

                // Deadband: Nếu đang đứng yên và khoảng cách >= 0.45m thì ngừng đẩy hoàn toàn
                if (!isMoving && dist >= 0.45f) continue;

                if (dist > 0.001f && dist < _separationRadius)
                {
                    // Lực suy giảm tuyến tính: (1 - dist / radius) thay vì 1/dist
                    // Triệt tiêu 100% hiện tượng nhảy lực đột ngột gây rung lắc
                    float strength = 1f - (dist / _separationRadius);
                    separationForce += diff.normalized * strength;
                    neighbors++;
                }
            }

            if (neighbors > 0)
            {
                separationForce /= neighbors;
            }

            return separationForce;
        }

        // =========================================================================
        // 4. CƠ CHẾ CHIẾN ĐẤU & TÌM ĐƯỜNG
        // =========================================================================

        private void UpdateCombatState()
        {
            // 1. Kiểm tra mục tiêu hiện tại còn sống không
            if (_targetEnemy == null || _targetEnemy.IsDead || !_targetEnemy.gameObject.activeInHierarchy)
            {
                EnemyController nextEnemy = ScanForEnemyInAggro();
                if (nextEnemy != null)
                {
                    _targetEnemy = nextEnemy;
                }
                else
                {
                    // Dọn sạch quái -> Quay lại Follow người chơi
                    _targetEnemy = null;
                    _state = MinionState.Following;
                    _isCatchingUp = false;
                    return;
                }
            }

            // 2. Tiếp tục tấn công quái
            float distToEnemy = Vector2.Distance(transform.position, _targetEnemy.transform.position);
            float attackRange = _data != null ? _data.AttackRange : 1.2f;

            if (distToEnemy <= attackRange)
            {
                if (Time.time >= _nextAttackTime)
                {
                    ExecuteAttack(_targetEnemy);
                    float fireInterval = 1f / Mathf.Max(0.1f, CurrentAttackSpeed);
                    _nextAttackTime = Time.time + fireInterval;
                }
            }
        }

        private void FixedUpdateCombat()
        {
            if (_targetEnemy == null)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 toEnemyVec = (Vector2)_targetEnemy.transform.position - (Vector2)transform.position;
            float distToEnemy = toEnemyVec.magnitude;
            float attackRange = _data != null ? _data.AttackRange : 1.2f;

            if (distToEnemy > attackRange * 0.9f)
            {
                Vector2 moveDir = toEnemyVec.normalized;

                // Kiểm tra xem có góc tường chắn giữa lính và quái không
                RaycastHit2D wallHit = Physics2D.Raycast(transform.position, moveDir, Mathf.Min(distToEnemy, 2.5f), _obstacleLayerMask);
                if (wallHit.collider != null)
                {
                    // Lách tường theo tiếp tuyến của bức tường
                    Vector2 wallNormal = wallHit.normal;
                    Vector2 tangent = new Vector2(-wallNormal.y, wallNormal.x);
                    moveDir = (Vector2.Dot(tangent, toEnemyVec) >= 0 ? tangent : -tangent).normalized;
                }

                float speed = _data != null ? _data.MoveSpeed : 4.5f;
                Vector2 desiredVel = moveDir * speed;
                Vector2 separation = CalculateSeparationForce(isMoving: true);
                Vector2 finalVel = desiredVel + separation * _separationWeight;

                _rb.linearVelocity = Vector2.ClampMagnitude(finalVel, speed);
                HandleFacingDirection(moveDir.x);
            }
            else
            {
                // Đã vào tầm đánh: Dừng chân ra đòn, không đẩy chen lấn quái
                _rb.linearVelocity = Vector2.zero;
                HandleFacingDirection(toEnemyVec.x);
            }
        }

        private void ExecuteAttack(EnemyController enemy)
        {
            if (enemy == null || _data == null) return;

            Vector2 hitDirection = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
            int damage = CurrentAttackDamage;

            switch (_data.AttackMethod)
            {
                case AttackMethod.Melee:
                    enemy.TakeDamage(damage, hitDirection, _data.KnockbackForce);
                    break;

                case AttackMethod.RangedProjectile:
                    if (_data.ProjectilePrefab != null && PoolManager.Instance != null)
                    {
                        float angle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;
                        Quaternion rot = Quaternion.Euler(0, 0, angle);

                        var projObj = PoolManager.Instance.Get(_data.ProjectilePrefab, transform.position, rot);
                        if (projObj != null && projObj.TryGetComponent<MinionProjectile>(out var minionProj))
                        {
                            minionProj.Initialize(_data.ProjectilePrefab, damage, _data.KnockbackForce);
                        }
                    }
                    else
                    {
                        enemy.TakeDamage(damage, hitDirection, _data.KnockbackForce);
                    }
                    break;

                case AttackMethod.AoE:
                    Vector2 impactCenter = enemy.transform.position;
                    _enemyBuffer.Clear();
                    int count = Physics2D.OverlapCircle(impactCenter, _data.AoERadius, _enemyFilter, _enemyBuffer);
                    for (int i = 0; i < count; i++)
                    {
                        if (_enemyBuffer[i] != null && _enemyBuffer[i].TryGetComponent<EnemyController>(out var aoeEnemy))
                        {
                            Vector2 dir = ((Vector2)aoeEnemy.transform.position - impactCenter).normalized;
                            aoeEnemy.TakeDamage(damage, dir, _data.KnockbackForce);
                        }
                    }
                    break;
            }
        }

        private EnemyController ScanForEnemyInAggro()
        {
            float aggroRadius = _data != null ? _data.AggroRange : 6f;
            _enemyBuffer.Clear();
            int count = Physics2D.OverlapCircle(transform.position, aggroRadius, _enemyFilter, _enemyBuffer);
            if (count == 0) return null;

            EnemyController nearest = null;
            float minSqrDist = float.MaxValue;
            Vector2 myPos = transform.position;

            for (int i = 0; i < count; i++)
            {
                var c = _enemyBuffer[i];
                if (c == null) continue;

                if (c.TryGetComponent<EnemyController>(out var enemy))
                {
                    if (enemy.IsDead || !enemy.gameObject.activeInHierarchy) continue;

                    float sqrDist = ((Vector2)enemy.transform.position - myPos).sqrMagnitude;
                    if (sqrDist < minSqrDist)
                    {
                        minSqrDist = sqrDist;
                        nearest = enemy;
                    }
                }
            }

            return nearest;
        }

        private void HandleFacingDirection(float horizontalVelocity)
        {
            if (_spriteRenderer == null) return;

            // Deadband chống rung lật mặt: Chỉ lật mặt khi vận tốc ngang có chủ đích rõ rệt (|vx| > 0.25f)
            if (horizontalVelocity > 0.25f)
            {
                _spriteRenderer.flipX = false;
            }
            else if (horizontalVelocity < -0.25f)
            {
                _spriteRenderer.flipX = true;
            }
        }

        // =========================================================================
        // 5. QUẢN LÝ MÁU, NHẬN SÁT THƯƠNG & TỬ TRẬN
        // =========================================================================

        public void TakeDamage(int damage)
        {
            if (!IsAlive) return;

            _currentHealth -= damage;
            TriggerDamageFlash();

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        private void TriggerDamageFlash()
        {
            if (_spriteRenderer == null || _flashMaterial == null) return;

            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            _spriteRenderer.sharedMaterial = _flashMaterial;
            yield return new WaitForSeconds(_flashDuration);
            if (_spriteRenderer != null && _originalMaterial != null)
            {
                _spriteRenderer.sharedMaterial = _originalMaterial;
            }
        }

        private void ApplyWallSlideProtection()
        {
            if (_rb.linearVelocity.sqrMagnitude > 0.05f)
            {
                // CircleCast quét theo thể tích thực của lính (0.32m) thay vì tia Ray siêu mỏng
                RaycastHit2D hit = Physics2D.CircleCast(transform.position, 0.32f, _rb.linearVelocity.normalized, 0.35f, _obstacleLayerMask);
                if (hit.collider != null)
                {
                    // Chiếu vận tốc lên tiếp tuyến của bức tường để trượt dọc tường
                    Vector2 normal = hit.normal;
                    Vector2 tangent = new Vector2(-normal.y, normal.x);
                    float proj = Vector2.Dot(_rb.linearVelocity, tangent);

                    if (Mathf.Abs(proj) >= 0.1f)
                    {
                        _rb.linearVelocity = tangent * proj + normal * 0.15f;
                    }
                    else
                    {
                        // Nếu đâm vuông góc vào tường (proj xấp xỉ 0), ưu tiên hỏi Lưới Lính để trượt mượt mà
                        if (FlowFieldManager.Instance != null)
                        {
                            Vector2 flowDir = FlowFieldManager.Instance.GetMinionFlowDirection(transform.position);
                            if (flowDir.sqrMagnitude > 0.01f)
                            {
                                float dotNormal = Vector2.Dot(flowDir, normal);
                                if (dotNormal < 0f)
                                {
                                    float fTangent = Vector2.Dot(flowDir, tangent);
                                    flowDir = (tangent * fTangent + normal * 0.15f).normalized;
                                }
                                _rb.linearVelocity = flowDir * (_data != null ? _data.MoveSpeed * 0.7f : 2.5f);
                                return;
                            }
                        }

                        Vector2 toTarget = _targetFollowPoint - (Vector2)transform.position;
                        float dot = Vector2.Dot(tangent, toTarget);
                        Vector2 slideTangent = dot >= 0 ? tangent : -tangent;
                        _rb.linearVelocity = (slideTangent + normal * 0.15f).normalized * (_data != null ? _data.MoveSpeed * 0.7f : 2.5f);
                    }
                }
            }
        }

        private void OnDisable()
        {
            _engagedAttackers.Clear();
        }

        private void Die()
        {
            _state = MinionState.Dead;
            _rb.linearVelocity = Vector2.zero;
            _engagedAttackers.Clear();

            if (_col != null) _col.enabled = false;
            if (_hitboxTrigger != null) _hitboxTrigger.enabled = false;
            if (_spriteRenderer != null) _spriteRenderer.enabled = false;

            if (MinionManager.Instance != null)
            {
                MinionManager.Instance.ReportMinionDied(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void Respawn(Vector3 respawnPosition)
        {
            transform.position = respawnPosition;
            if (_data != null)
            {
                _currentHealth = _data.MaxHealth;
            }

            _state = MinionState.Following;
            _isCatchingUp = false;

            if (_col != null) _col.enabled = true;
            if (_hitboxTrigger != null) _hitboxTrigger.enabled = true;
            if (_spriteRenderer != null) _spriteRenderer.enabled = true;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Dịch chuyển lính đến vị trí chỉ định khi chuyển sang phòng mới.
        /// </summary>
        public void TeleportTo(Vector3 pos)
        {
            transform.position = pos;
            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
            }
            _state = MinionState.Following;
            _isCatchingUp = false;
            _targetFollowPoint = pos;
            _targetEnemy = null;
        }
    }
}

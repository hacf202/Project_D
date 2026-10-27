using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Project_D.Core;
using Project_D.Combat;
using Project_D.Enemies;
using Project_D.Dungeon.Run;
using Project_D.Dungeon.Interactions;

namespace Project_D.Dungeon.Room
{
    public enum RoomCombatState
    {
        Inactive,
        SpawningWave,
        FightingWave,
        WaveIntermission,
        RoomCompleted
    }

    public class RoomController : MonoBehaviour
    {
        public static RoomController Instance { get; private set; }

        [Header("Wave & Combat Configurations")]
        [SerializeField] private RoomWaveConfigSO _normalBattleWaveConfig;
        [SerializeField] private RoomWaveConfigSO _eliteBattleWaveConfig;
        [SerializeField] private EnemyDataSO[] _fallbackEnemyTypes;
        [SerializeField] private float _minSpawnRadius = 8f;
        [SerializeField] private float _maxSpawnRadius = 12f;

        [Header("Portals (Cổng Dịch Chuyển Tầng Kế Tiếp)")]
        [SerializeField] private RoomPortal[] _roomPortals;
        [SerializeField] private PortalVisualConfigSO _portalVisualConfig;

        [Header("Interaction Points (Điểm Tương Tác Phi Chiến Đấu)")]
        [SerializeField] private InteractionPoint[] _interactionPoints;

        [Header("Room State")]
        private RunNodeData _currentNode;
        private RoomWaveConfigSO _currentWaveConfig;
        private int _currentWaveIndex = 0;
        private int _totalEnemiesInCurrentWave = 0;
        private int _enemiesDefeatedInCurrentWave = 0;
        private int _enemiesSpawnedInCurrentWave = 0;
        private RoomCombatState _combatState = RoomCombatState.Inactive;
        private readonly HashSet<EnemyController> _currentWaveEnemies = new HashSet<EnemyController>();
        private Coroutine _spawnWaveCoroutine;
        private Coroutine _delayedWaveCoroutine;
        private Transform _playerTransform;

        public static event Action OnRoomCombatStarted;
        public static event Action<int, int, int, int> OnWaveProgressChanged; // currentWave, totalWaves, remainingEnemies, totalInWave
        public static event Action OnRoomCleared;

        public int CurrentWave => _currentWaveIndex + 1;
        public int TotalWaves => _currentWaveConfig != null ? _currentWaveConfig.TotalWaves : 3;
        public int RemainingEnemies => Mathf.Max(0, _totalEnemiesInCurrentWave - _enemiesDefeatedInCurrentWave);
        public int AliveEnemies => _currentWaveEnemies.Count;
        public bool IsCombatActive => _combatState == RoomCombatState.SpawningWave || _combatState == RoomCombatState.FightingWave || _combatState == RoomCombatState.WaveIntermission;
        public RoomCombatState CombatState => _combatState;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            // Vô hiệu hóa ngay EnemySpawner vô tận cũ nếu có trong Scene để tránh xung đột
            var legacySpawner = FindAnyObjectByType<EnemySpawner>();
            if (legacySpawner != null)
            {
                legacySpawner.StopSpawning();
                legacySpawner.enabled = false;
            }

            // Khởi tạo các cấu hình sóng mặc định nếu chưa gán
            if (_normalBattleWaveConfig == null)
            {
                _normalBattleWaveConfig = ScriptableObject.CreateInstance<RoomWaveConfigSO>();
                _normalBattleWaveConfig.CreateDefault3WaveNormal();
            }

            if (_eliteBattleWaveConfig == null)
            {
                _eliteBattleWaveConfig = ScriptableObject.CreateInstance<RoomWaveConfigSO>();
                _eliteBattleWaveConfig.CreateDefault4WaveElite();
            }

            if (_portalVisualConfig == null)
            {
                _portalVisualConfig = ScriptableObject.CreateInstance<PortalVisualConfigSO>();
                _portalVisualConfig.PopulateDefaultDistinctIcons();
            }

            // Tự động tìm các Portal và InteractionPoint trong phòng nếu mảng bị trống
            if (_roomPortals == null || _roomPortals.Length == 0)
            {
                _roomPortals = GetComponentsInChildren<RoomPortal>(true);
            }

            if (_interactionPoints == null || _interactionPoints.Length == 0)
            {
                _interactionPoints = GetComponentsInChildren<InteractionPoint>(true);
            }

            // Tự động đảm bảo RoomManager hiện diện trong Scene để nạp phòng động
            if (RoomManager.Instance == null)
            {
                var existingManager = FindAnyObjectByType<RoomManager>();
                if (existingManager == null)
                {
                    var mgrGo = new GameObject("RoomManager");
                    if (transform.parent != null)
                    {
                        mgrGo.transform.SetParent(transform.parent);
                    }
                    mgrGo.AddComponent<RoomManager>();
                }
            }
        }

        private void Start()
        {
            FindPlayer();

            // Khóa Spawner cũ thêm 1 lần nữa trong Start
            var legacySpawner = FindAnyObjectByType<EnemySpawner>();
            if (legacySpawner != null)
            {
                legacySpawner.StopSpawning();
                legacySpawner.enabled = false;
            }

            PrewarmWaveEnemies();

            // Dự phòng khởi động: nếu RunManager đã phát OnRoomEntered trước khi RoomController đăng ký
            if (_currentNode == null && RunManager.Instance != null && RunManager.Instance.CurrentNode != null)
            {
                HandleRoomEntered(RunManager.Instance.CurrentNode);
            }
        }

        private void PrewarmWaveEnemies()
        {
            if (PoolManager.Instance == null) return;
            var prewarmed = new HashSet<GameObject>();

            PrewarmFromConfig(_normalBattleWaveConfig, prewarmed);
            PrewarmFromConfig(_eliteBattleWaveConfig, prewarmed);

            if (_fallbackEnemyTypes != null)
            {
                foreach (var data in _fallbackEnemyTypes)
                {
                    if (data != null && data.Prefab != null && prewarmed.Add(data.Prefab))
                    {
                        PoolManager.Instance.Prewarm(data.Prefab, 15);
                    }
                    if (data != null && data.ExpGemPrefab != null && prewarmed.Add(data.ExpGemPrefab))
                    {
                        PoolManager.Instance.Prewarm(data.ExpGemPrefab, 30);
                    }
                }
            }
        }

        private void PrewarmFromConfig(RoomWaveConfigSO config, HashSet<GameObject> prewarmed)
        {
            if (config == null || config.Waves == null || PoolManager.Instance == null) return;
            foreach (var wave in config.Waves)
            {
                if (wave.EnemyTypes != null)
                {
                    foreach (var data in wave.EnemyTypes)
                    {
                        if (data != null && data.Prefab != null && prewarmed.Add(data.Prefab))
                        {
                            PoolManager.Instance.Prewarm(data.Prefab, 15);
                        }
                        if (data != null && data.ExpGemPrefab != null && prewarmed.Add(data.ExpGemPrefab))
                        {
                            PoolManager.Instance.Prewarm(data.ExpGemPrefab, 30);
                        }
                    }
                }
            }
        }

        private void OnEnable()
        {
            RunManager.OnRoomEntered += HandleRoomEntered;
            EnemyController.OnEnemyDied += HandleEnemyDied;
        }

        private void OnDisable()
        {
            RunManager.OnRoomEntered -= HandleRoomEntered;
            EnemyController.OnEnemyDied -= HandleEnemyDied;

            if (_spawnWaveCoroutine != null)
            {
                StopCoroutine(_spawnWaveCoroutine);
                _spawnWaveCoroutine = null;
            }
            if (_delayedWaveCoroutine != null)
            {
                StopCoroutine(_delayedWaveCoroutine);
                _delayedWaveCoroutine = null;
            }
            _currentWaveEnemies.Clear();
        }

        private void FindPlayer()
        {
            var player = FindAnyObjectByType<Player.PlayerController>();
            if (player != null)
            {
                _playerTransform = player.transform;
            }
        }

        private RoomLayout _currentRoomLayout;

        public RoomLayout CurrentRoomLayout => _currentRoomLayout;

        public void BindRoomLayout(RoomLayout layout)
        {
            if (layout == null) return;

            _currentRoomLayout = layout;

            // 1. Tự động ẩn các Cổng tĩnh cũ trong Scene nếu có để tránh trùng lặp
            if (layout.Portals != null && layout.Portals.Length > 0)
            {
                if (_roomPortals != null)
                {
                    foreach (var oldPortal in _roomPortals)
                    {
                        if (oldPortal != null && !Array.Exists(layout.Portals, p => p == oldPortal))
                        {
                            oldPortal.gameObject.SetActive(false);
                        }
                    }
                }
                _roomPortals = layout.Portals;
            }

            // 2. Tự động ẩn các Điểm tương tác tĩnh cũ trong Scene
            if (layout.InteractionPoints != null && layout.InteractionPoints.Length > 0)
            {
                if (_interactionPoints != null)
                {
                    foreach (var oldPoint in _interactionPoints)
                    {
                        if (oldPoint != null && !Array.Exists(layout.InteractionPoints, p => p == oldPoint))
                        {
                            oldPoint.gameObject.SetActive(false);
                        }
                    }
                }
                _interactionPoints = layout.InteractionPoints;
            }

            // 3. Ẩn tường tĩnh thử nghiệm cũ nếu có
            var staticWall = GameObject.Find("Wall_Test");
            if (staticWall != null)
            {
                staticWall.SetActive(false);
            }

            Debug.Log($"<color=cyan>[RoomController]</color> Đã liên kết RoomLayout '{layout.RoomName}' ({_roomPortals?.Length ?? 0} Cổng, {_interactionPoints?.Length ?? 0} Điểm tương tác).");
        }

        // Nhận tín hiệu khi người chơi bước vào một Node mới trong Run
        private void HandleRoomEntered(RunNodeData nodeData)
        {
            _currentNode = nodeData;
            FindPlayer();

            if (_spawnWaveCoroutine != null)
            {
                StopCoroutine(_spawnWaveCoroutine);
                _spawnWaveCoroutine = null;
            }
            if (_delayedWaveCoroutine != null)
            {
                StopCoroutine(_delayedWaveCoroutine);
                _delayedWaveCoroutine = null;
            }
            _currentWaveEnemies.Clear();

            // 1. Tải và thiết lập phòng động thông qua RoomManager nếu có trong Scene
            if (RoomManager.Instance != null)
            {
                RoomLayout layout = RoomManager.Instance.LoadRoomForNode(_currentNode);
                if (layout != null)
                {
                    BindRoomLayout(layout);
                }
            }
            else
            {
                // Fallback nếu chưa gắn RoomManager
                if (_playerTransform != null)
                {
                    _playerTransform.position = Vector3.zero;
                }
            }

            if (_currentNode.IsCombatRoom)
            {
                StartCombatRoom();
            }
            else
            {
                StartNonCombatRoom();
            }
        }

        // ----------------- PHÒNG CHIẾN ĐẤU (3-4 WAVES) -----------------
        private void StartCombatRoom()
        {
            _combatState = RoomCombatState.SpawningWave;
            _currentWaveIndex = 0;
            _totalEnemiesInCurrentWave = 0;
            _enemiesDefeatedInCurrentWave = 0;
            _enemiesSpawnedInCurrentWave = 0;
            _currentWaveEnemies.Clear();

            // 1. Đóng toàn bộ các Cổng dịch chuyển khi bắt đầu đánh quái
            CloseAllPortals();

            // 2. Tắt các điểm tương tác phi chiến đấu
            SetInteractionPointsActive(false);

            // 3. Chọn cấu hình Wave phù hợp (Elite / Boss dùng 4 wave hoặc cấu hình riêng)
            if (_currentNode.NodeType == RunNodeType.EliteBattle || _currentNode.NodeType == RunNodeType.MiniBoss || _currentNode.NodeType == RunNodeType.BossBattle)
            {
                _currentWaveConfig = _eliteBattleWaveConfig;
            }
            else
            {
                _currentWaveConfig = _normalBattleWaveConfig;
            }

            Debug.Log($"<color=red><b>[ROOM COMBAT]</b></color> Bắt đầu phòng chiến đấu {_currentNode.DisplayName} gồm {_currentWaveConfig.TotalWaves} Waves!");

            OnRoomCombatStarted?.Invoke();

            // 4. Kích hoạt Wave 1
            _spawnWaveCoroutine = StartCoroutine(SpawnWaveRoutine(_currentWaveIndex));
        }

        private IEnumerator SpawnWaveRoutine(int waveIndex)
        {
            if (_currentWaveConfig == null || waveIndex >= _currentWaveConfig.Waves.Count) yield break;

            _combatState = RoomCombatState.SpawningWave;
            WaveConfig wave = _currentWaveConfig.Waves[waveIndex];
            _totalEnemiesInCurrentWave = wave.EnemyCount;
            _enemiesDefeatedInCurrentWave = 0;
            _enemiesSpawnedInCurrentWave = 0;
            _currentWaveEnemies.Clear();

            Debug.Log($"<color=yellow>[WAVE STARTED]</color> {wave.WaveName}: Cần tiêu diệt {wave.EnemyCount} quái vật!");

            UpdateWaveProgress();

            for (int i = 0; i < wave.EnemyCount; i++)
            {
                if (_combatState != RoomCombatState.SpawningWave) yield break;

                SpawnSingleEnemy(wave);
                _enemiesSpawnedInCurrentWave++;
                UpdateWaveProgress();

                if (wave.SpawnInterval > 0f)
                {
                    yield return new WaitForSeconds(wave.SpawnInterval);
                }
            }

            if (_combatState == RoomCombatState.SpawningWave)
            {
                _combatState = RoomCombatState.FightingWave;
            }

            CheckWaveCompletion();
        }

        private void SpawnSingleEnemy(WaveConfig wave)
        {
            if (_playerTransform == null) FindPlayer();
            if (_playerTransform == null) return;

            // Chọn loại quái từ WaveConfig hoặc fallback
            EnemyDataSO enemyData = null;
            if (wave.EnemyTypes != null && wave.EnemyTypes.Length > 0)
            {
                enemyData = wave.EnemyTypes[UnityEngine.Random.Range(0, wave.EnemyTypes.Length)];
            }
            else if (_fallbackEnemyTypes != null && _fallbackEnemyTypes.Length > 0)
            {
                enemyData = _fallbackEnemyTypes[UnityEngine.Random.Range(0, _fallbackEnemyTypes.Length)];
            }

            if (enemyData == null || enemyData.Prefab == null || PoolManager.Instance == null)
            {
                Debug.LogWarning("[RoomController] Cấu hình quái bị thiếu EnemyData/Prefab/Pool! Bù trừ bộ đếm để chống kẹt game.");
                _enemiesDefeatedInCurrentWave++;
                UpdateWaveProgress();
                return;
            }

            // Tính toán vị trí sinh quái: Ưu tiên chọn từ EnemySpawnPoints của RoomLayout nếu có, hoặc sinh ngẫu nhiên xung quanh Player
            Vector3 spawnPos;
            if (_currentRoomLayout != null && _currentRoomLayout.EnemySpawnPoints != null && _currentRoomLayout.EnemySpawnPoints.Length > 0)
            {
                var pt = _currentRoomLayout.EnemySpawnPoints[UnityEngine.Random.Range(0, _currentRoomLayout.EnemySpawnPoints.Length)];
                spawnPos = pt != null ? pt.position : CalculateFallbackSpawnPos();
            }
            else
            {
                spawnPos = CalculateFallbackSpawnPos();
            }

            GameObject enemyObj = PoolManager.Instance.Get(enemyData.Prefab, spawnPos, Quaternion.identity);
            if (enemyObj != null)
            {
                if (enemyObj.TryGetComponent<EnemyController>(out var controller))
                {
                    controller.Initialize(enemyData, enemyData.Prefab);
                    _currentWaveEnemies.Add(controller);
                }
            }
            else
            {
                Debug.LogWarning("[RoomController] Không thể lấy quái từ Pool! Bù trừ bộ đếm để chống kẹt game.");
                _enemiesDefeatedInCurrentWave++;
                UpdateWaveProgress();
            }
        }

        private Vector3 CalculateFallbackSpawnPos()
        {
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = UnityEngine.Random.Range(_minSpawnRadius, _maxSpawnRadius);
            Vector3 center = _playerTransform != null ? _playerTransform.position : Vector3.zero;
            Vector3 pos = center + new Vector3(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, 0f);

            // Kẹp vị trí quái bên trong tường phòng nếu có RoomLayout
            if (_currentRoomLayout != null && _currentRoomLayout.RoomSize.x > 0 && _currentRoomLayout.RoomSize.y > 0)
            {
                float maxSpawnX = (_currentRoomLayout.RoomSize.x * 0.5f) - 2f;
                float maxSpawnY = (_currentRoomLayout.RoomSize.y * 0.5f) - 2f;
                pos.x = Mathf.Clamp(pos.x, -maxSpawnX, maxSpawnX);
                pos.y = Mathf.Clamp(pos.y, -maxSpawnY, maxSpawnY);
            }

            return pos;
        }

        // Lắng nghe khi 1 quái chết
        private void HandleEnemyDied(EnemyController enemy)
        {
            // BẢO VỆ CHỐNG CLEAR SỚM: Chỉ xử lý quái chết khi đang thực chiến
            if (_combatState != RoomCombatState.SpawningWave && _combatState != RoomCombatState.FightingWave) return;

            // BẢO VỆ CHỐNG LỆCH BỘ ĐẾM: Quái này bắt buộc phải thuộc danh sách quái của Wave hiện tại
            if (!_currentWaveEnemies.Remove(enemy))
            {
                // Quái rác sót lại hoặc sự kiện trùng lặp ngoài mong muốn -> Bỏ qua
                return;
            }

            _enemiesDefeatedInCurrentWave++;
            UpdateWaveProgress();

            // Kiểm tra điều kiện hoàn thành wave
            CheckWaveCompletion();
        }

        private void CheckWaveCompletion()
        {
            if (_combatState != RoomCombatState.SpawningWave && _combatState != RoomCombatState.FightingWave) return;

            PruneInvalidEnemies();

            // Điều kiện hoàn thành Wave:
            // 1. Toàn bộ quái của wave đã được spawn ra sân
            // 2. Không còn bất kỳ quái nào của wave còn sống trong phòng
            bool allSpawned = _enemiesSpawnedInCurrentWave >= _totalEnemiesInCurrentWave;
            bool allDefeated = _currentWaveEnemies.Count == 0;

            if (allSpawned && allDefeated)
            {
                if (_currentWaveIndex + 1 < _currentWaveConfig.TotalWaves)
                {
                    // Chuyển sang trạng thái nghỉ giữa 2 wave (Intermission)
                    _combatState = RoomCombatState.WaveIntermission;
                    _delayedWaveCoroutine = StartCoroutine(DelayedNextWave(_currentWaveConfig.DelayBetweenWaves));
                }
                else
                {
                    // ĐÃ TIÊU DIỆT SẠCH TOÀN BỘ QUÁI Ở WAVE CUỐI CÙNG!
                    CompleteRoomCombat();
                }
            }
        }

        private void PruneInvalidEnemies()
        {
            if (_currentWaveEnemies.Count == 0) return;

            List<EnemyController> invalidEnemies = null;
            foreach (var enemy in _currentWaveEnemies)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.IsDead)
                {
                    if (invalidEnemies == null) invalidEnemies = new List<EnemyController>();
                    invalidEnemies.Add(enemy);
                }
            }

            if (invalidEnemies != null)
            {
                foreach (var enemy in invalidEnemies)
                {
                    _currentWaveEnemies.Remove(enemy);
                    _enemiesDefeatedInCurrentWave++;
                    Debug.LogWarning("[RoomController Watchdog] Tự động giải phóng quái bị hủy bất thường để đảm bảo không bị softlock.");
                }
                UpdateWaveProgress();
            }
        }

        private IEnumerator DelayedNextWave(float delay)
        {
            Debug.Log($"<color=cyan>[WAVE CLEARED]</color> Đã hoàn thành Wave {_currentWaveIndex + 1}! Chuẩn bị đợt tiếp theo sau {delay}s...");
            yield return new WaitForSeconds(delay);

            // CHỈ TĂNG CHỈ SỐ WAVE KHI BẮT ĐẦU WAVE MỚI
            _currentWaveIndex++;
            _spawnWaveCoroutine = StartCoroutine(SpawnWaveRoutine(_currentWaveIndex));
        }

        private void CompleteRoomCombat()
        {
            _combatState = RoomCombatState.RoomCompleted;
            Debug.Log($"<color=green><b>🎉 [ROOM CLEARED]</b></color> Phòng {_currentNode.DisplayName} đã được dọn sạch hoàn toàn!");

            // 1. Kích hoạt siêu nam châm hút toàn bộ ngọc Exp còn sót lại trên sàn
            CollectAllRemainingGems();

            // 2. Mở các Cổng dịch chuyển sang các nhánh rẽ của tầng kế tiếp
            OpenNextFloorPortals();

            OnRoomCleared?.Invoke();

            // 3. Nếu là Tầng 13 (Boss) thì hoàn thành Run!
            if (_currentNode.NodeType == RunNodeType.BossBattle)
            {
                RunManager.Instance?.CompleteRun();
            }
        }

        private void UpdateWaveProgress()
        {
            int remainingToDefeat = Mathf.Max(0, _totalEnemiesInCurrentWave - _enemiesDefeatedInCurrentWave);
            OnWaveProgressChanged?.Invoke(_currentWaveIndex + 1, TotalWaves, remainingToDefeat, _totalEnemiesInCurrentWave);
        }

        // ----------------- PHÒNG PHI CHIẾN ĐẤU (REST, SHOP, REWARD) -----------------
        private void StartNonCombatRoom()
        {
            _combatState = RoomCombatState.Inactive;
            _totalEnemiesInCurrentWave = 0;
            _enemiesDefeatedInCurrentWave = 0;
            _currentWaveEnemies.Clear();

            // 1. Mở sẵn toàn bộ các Cổng dịch chuyển sang tầng sau ngay lập tức
            OpenNextFloorPortals();

            // 2. Kích hoạt đúng Điểm Tương Tác cho loại phòng này
            ActivateMatchingInteractionPoint(_currentNode.NodeType);

            Debug.Log($"<color=green>[NON-COMBAT ROOM]</color> Tiến vào phòng {_currentNode.DisplayName}. Cổng tầng sau đã mở sẵn!");
        }

        private void ActivateMatchingInteractionPoint(RunNodeType nodeType)
        {
            if (_interactionPoints == null) return;

            foreach (var point in _interactionPoints)
            {
                if (point == null) continue;

                bool shouldBeActive = false;
                if (nodeType == RunNodeType.RestSite && point.Type == InteractionType.RestSite) shouldBeActive = true;
                else if (nodeType == RunNodeType.Shop && point.Type == InteractionType.Shop) shouldBeActive = true;
                else if ((nodeType == RunNodeType.StarterReward || nodeType == RunNodeType.BigReward || nodeType == RunNodeType.BuffReward) && point.Type == InteractionType.BuffReward) shouldBeActive = true;

                point.gameObject.SetActive(shouldBeActive);
            }
        }

        private void SetInteractionPointsActive(bool active)
        {
            if (_interactionPoints == null) return;
            foreach (var p in _interactionPoints)
            {
                if (p != null) p.gameObject.SetActive(active);
            }
        }

        // ----------------- ĐIỀU PHỐI CỔNG DỊCH CHUYỂN -----------------
        public void OpenNextFloorPortals()
        {
            CloseAllPortals();

            if (RunManager.Instance == null) return;

            List<RunNodeData> nextNodes = RunManager.Instance.GetCurrentNextNodes();
            if (nextNodes == null || nextNodes.Count == 0)
            {
                Debug.Log("<color=yellow>[PORTAL]</color> Không có nhánh rẽ tiếp theo (Có thể đã đến cuối Run).");
                return;
            }

            if (_roomPortals == null || _roomPortals.Length == 0)
            {
                Debug.LogWarning("[RoomController] Không tìm thấy Cổng dịch chuyển nào trong phòng!");
                return;
            }

            // Lọc ra các cổng hợp lệ và sắp xếp theo vị trí X từ trái qua phải
            var validPortals = new List<RoomPortal>();
            foreach (var p in _roomPortals)
            {
                if (p != null) validPortals.Add(p);
            }
            validPortals.Sort((a, b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));

            if (validPortals.Count == 0) return;

            // Chọn các cổng mở một cách đối xứng thẩm mỹ
            List<RoomPortal> portalsToUse = new List<RoomPortal>();
            int branchCount = nextNodes.Count;

            if (branchCount == 1)
            {
                // 1 nhánh: Chọn cổng ở chính giữa
                int centerIndex = validPortals.Count / 2;
                portalsToUse.Add(validPortals[centerIndex]);
            }
            else if (branchCount == 2 && validPortals.Count >= 2)
            {
                if (validPortals.Count == 3)
                {
                    // 3 cổng (Trái, Giữa, Phải): Mở 2 cổng Trái và Phải đối xứng
                    portalsToUse.Add(validPortals[0]);
                    portalsToUse.Add(validPortals[2]);
                }
                else
                {
                    portalsToUse.Add(validPortals[0]);
                    portalsToUse.Add(validPortals[1]);
                }
            }
            else
            {
                // 3 nhánh trở lên: Mở theo thứ tự từ trái qua phải
                int count = Mathf.Min(branchCount, validPortals.Count);
                for (int i = 0; i < count; i++)
                {
                    portalsToUse.Add(validPortals[i]);
                }
            }

            // Kích hoạt và cấu hình các cổng
            for (int i = 0; i < portalsToUse.Count && i < nextNodes.Count; i++)
            {
                portalsToUse[i].SetupPortal(nextNodes[i], _portalVisualConfig);
            }
        }

        private void CloseAllPortals()
        {
            if (_roomPortals == null) return;

            foreach (var portal in _roomPortals)
            {
                if (portal != null)
                {
                    portal.ClosePortal();
                }
            }
        }

        // Siêu nam châm hút toàn bộ ngọc Exp rơi trên sàn về Player
        private void CollectAllRemainingGems()
        {
            var gems = FindObjectsByType<ExpGem>();
            if (_playerTransform == null) FindPlayer();

            foreach (var gem in gems)
            {
                if (gem != null && gem.gameObject.activeInHierarchy && _playerTransform != null)
                {
                    gem.StartAttract(_playerTransform);
                }
            }
        }
    }
}

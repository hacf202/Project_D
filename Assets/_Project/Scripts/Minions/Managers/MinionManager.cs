using System;
using System.Collections.Generic;
using UnityEngine;
using Project_D.Core;
using Project_D.Player;

namespace Project_D.Minions
{
    public class MinionRespawnTask
    {
        public MinionController Minion;
        public float RespawnTime;
    }

    public class MergeAvailableInfo
    {
        public MergeRecipeSO Recipe;
        public int CurrentCount;
        public int RequiredCount;
        public bool CanMerge;
        public bool IsLockedBySpecialCondition;
        public string IngredientsDescription;
    }

    public class MinionManager : MonoBehaviour
    {
        private static MinionManager _instance;
        public static MinionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindAnyObjectByType<MinionManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("MinionManager");
                        _instance = go.AddComponent<MinionManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Database & Recipes")]
        [SerializeField] private MinionDatabaseSO _database;
        public MinionDatabaseSO Database => _database;

        [Header("Spawn Settings")]
        [SerializeField] private float _summonRadius = 2.0f;

        [Header("Team Buff Modifiers (Chỉ Số Cường Hóa Từ Thẻ Bài)")]
        [SerializeField] private float _minionDamageMultiplier = 1.0f;
        [SerializeField] private float _minionAttackSpeedMultiplier = 1.0f;
        [SerializeField] private float _minionMaxHealthBonusPercent = 0.0f;
        [SerializeField] private float _minionRespawnTimeMultiplier = 1.0f;

        // Quản lý các lính đang sống trên sân
        private readonly List<MinionController> _activeMinions = new List<MinionController>();
        // Hàng đợi các lính đang chờ hồi sinh (10 giây)
        private readonly List<MinionRespawnTask> _respawnTasks = new List<MinionRespawnTask>();
        // Tập hợp các khóa mở khóa đặc biệt (dành cho Tier 4)
        private readonly HashSet<string> _unlockedSpecialConditions = new HashSet<string>();
        // Bảng theo dõi điểm neo đích đến đã phân bổ cho từng lính để triệt tiêu 100% xung đột chen lấn
        private readonly Dictionary<MinionController, Vector2> _assignedFollowOffsets = new Dictionary<MinionController, Vector2>();

        private Transform _playerTransform;

        // Sự kiện thông báo khi đội hình thay đổi hoặc có công thức Merge khả dụng
        public static event Action OnAvailableMergesChanged;
        public static event Action<MinionDataSO> OnMinionSummoned;
        public static event Action<MinionDataSO> OnMinionMerged;
        public static event Action OnMinionRosterChanged;

        public IReadOnlyList<MinionController> ActiveMinions => _activeMinions;
        public IReadOnlyList<MinionRespawnTask> RespawnTasks => _respawnTasks;

        public float MinionDamageMultiplier => _minionDamageMultiplier;
        public float MinionAttackSpeedMultiplier => _minionAttackSpeedMultiplier;
        public float MinionMaxHealthBonusPercent => _minionMaxHealthBonusPercent;
        public float MinionRespawnTimeMultiplier => _minionRespawnTimeMultiplier;

        public void AddMinionDamageBonus(float percent)
        {
            _minionDamageMultiplier += percent;
            Debug.Log($"<color=cyan>[MINION BUFF]</color> Sát thương toàn bộ lính tăng +{percent * 100}%! Hệ số: {_minionDamageMultiplier:F2}x");
        }

        public void AddMinionAttackSpeedBonus(float percent)
        {
            _minionAttackSpeedMultiplier += percent;
            Debug.Log($"<color=cyan>[MINION BUFF]</color> Tốc độ đánh lính tăng +{percent * 100}%! Hệ số: {_minionAttackSpeedMultiplier:F2}x");
        }

        public void AddMinionMaxHealthBonus(float percent)
        {
            _minionMaxHealthBonusPercent += percent;
            Debug.Log($"<color=cyan>[MINION BUFF]</color> Máu tối đa lính tăng +{percent * 100}%!");
            foreach (var m in _activeMinions)
            {
                if (m != null && m.IsAlive)
                {
                    m.RefreshMaxHealth();
                }
            }
        }

        public void AddMinionRespawnSpeedBonus(float percentReduction)
        {
            _minionRespawnTimeMultiplier = Mathf.Max(0.2f, _minionRespawnTimeMultiplier * (1f - percentReduction));
            Debug.Log($"<color=cyan>[MINION BUFF]</color> Thời gian hồi sinh lính giảm {percentReduction * 100}%! Hệ số thời gian: {_minionRespawnTimeMultiplier:F2}x");
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            if (_database == null)
            {
                _database = Resources.Load<MinionDatabaseSO>("MinionDatabase");
#if UNITY_EDITOR
                if (_database == null)
                {
                    string[] guids = UnityEditor.AssetDatabase.FindAssets("t:MinionDatabaseSO");
                    if (guids.Length > 0)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                        _database = UnityEditor.AssetDatabase.LoadAssetAtPath<MinionDatabaseSO>(path);
                    }
                }
#endif
            }
        }

        private void OnEnable()
        {
            Project_D.Dungeon.Room.RoomManager.OnRoomLayoutLoaded += HandleRoomLayoutLoaded;
        }

        private void OnDisable()
        {
            Project_D.Dungeon.Room.RoomManager.OnRoomLayoutLoaded -= HandleRoomLayoutLoaded;
        }

        private void HandleRoomLayoutLoaded(Project_D.Dungeon.Room.RoomLayout layout)
        {
            TeleportAllMinionsToPlayer();
        }

        private void Start()
        {
            FindPlayer();
        }

        private void Update()
        {
            UpdateRespawnQueue();
        }

        private void FindPlayer()
        {
            if (_playerTransform == null)
            {
                var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                if (pc != null) _playerTransform = pc.transform;
            }
        }

        // =========================================================================
        // 1. TRIỆU HỒI LÍNH (SUMMON & OBJECT POOLING)
        // =========================================================================

        public MinionController SummonMinion(MinionDataSO data)
        {
            if (data == null || data.Prefab == null)
            {
                Debug.LogError("[MinionManager] Không thể triệu hồi: MinionDataSO hoặc Prefab bị rỗng!");
                return null;
            }

            FindPlayer();
            Vector3 spawnPos = _playerTransform != null
                ? _playerTransform.position + (Vector3)UnityEngine.Random.insideUnitCircle * _summonRadius
                : Vector3.zero;

            // Đảm bảo điểm triệu hồi không bao giờ rơi vào trong tường hoặc ngoài phòng
            spawnPos = Project_D.Dungeon.Room.RoomBoundaryValidator.GetNearestWalkablePosition(spawnPos);

            GameObject minionObj = null;
            if (PoolManager.Instance != null)
            {
                minionObj = PoolManager.Instance.Get(data.Prefab, spawnPos, Quaternion.identity);
            }
            else
            {
                minionObj = Instantiate(data.Prefab, spawnPos, Quaternion.identity);
            }

            if (minionObj.TryGetComponent<MinionController>(out var minion))
            {
                minion.Initialize(data, _playerTransform);
                if (!_activeMinions.Contains(minion))
                {
                    _activeMinions.Add(minion);
                }

                OnMinionSummoned?.Invoke(data);
                OnAvailableMergesChanged?.Invoke();
                OnMinionRosterChanged?.Invoke();

                Debug.Log($"<color=green>[MINION SUMMON]</color> Đã triệu hồi: <b>{data.MinionName}</b> ({data.Class} - Tier {data.Tier})!");
                return minion;
            }

            Debug.LogError($"[MinionManager] Prefab '{data.Prefab.name}' không có component MinionController!");
            return null;
        }

        // =========================================================================
        // PHÂN BỔ ĐIỂM FOLLOW AN TOÀN (CHỐNG TRÙNG VỊ TRÍ GÂY ĐẨY NHAU VÔ HẠN)
        // =========================================================================

        public Vector2 GetFollowSlot(MinionController requestingMinion, Transform playerTransform, float followRadius)
        {
            if (playerTransform == null) return Vector2.zero;

            int index = _activeMinions.IndexOf(requestingMinion);
            if (index < 0) index = _activeMinions.Count;
            int total = Mathf.Max(1, _activeMinions.Count);

            float baseAngle = (2f * Mathf.PI / total) * index;
            float targetDist = Mathf.Clamp(followRadius * 0.6f, 1.4f, followRadius * 0.85f);

            Vector2 bestOffset = Vector2.zero;
            float maxNearestDist = -1f;
            bool foundValid = false;

            // Thử 8 góc phân bố đều quanh người chơi để tìm vị trí xa nhất so với các lính khác
            for (int step = 0; step < 8; step++)
            {
                float testAngle = baseAngle + (step * (Mathf.PI * 2f / 8f));
                Vector2 candidateOffset = new Vector2(Mathf.Cos(testAngle), Mathf.Sin(testAngle)) * targetDist;
                Vector2 candidateWorldPos = (Vector2)playerTransform.position + candidateOffset;

                // 1. Bỏ qua nếu góc này đâm trúng tường đá hoặc quá sát mép tường (< 0.6m, đảm bảo thân lính 0.38m đứng thoải mái)
                if (Project_D.Dungeon.Room.RoomBoundaryValidator.IsInsideWall(candidateWorldPos, 0.6f))
                {
                    continue;
                }

                // 2. Bỏ qua nếu có tường hoặc góc tường ngăn cách đường đi giữa người chơi và vị trí slot (CircleCast quét theo thân lính)
                if (!Project_D.Dungeon.Room.RoomBoundaryValidator.HasPathClear(playerTransform.position, candidateWorldPos, 0.35f))
                {
                    continue;
                }

                foundValid = true;

                float nearestDist = float.MaxValue;
                foreach (var kvp in _assignedFollowOffsets)
                {
                    if (kvp.Key != requestingMinion && kvp.Key != null && kvp.Key.IsAlive)
                    {
                        float d = Vector2.Distance(candidateOffset, kvp.Value);
                        if (d < nearestDist) nearestDist = d;
                    }
                }

                // Nếu khoảng cách tới mọi lính khác >= 0.8m thì đây là vị trí an toàn lý tưởng
                if (nearestDist >= 0.8f)
                {
                    bestOffset = candidateOffset;
                    break;
                }

                if (nearestDist > maxNearestDist)
                {
                    maxNearestDist = nearestDist;
                    bestOffset = candidateOffset;
                }
            }

            // Nếu cả 8 góc ở bán kính chuẩn đều bị tường chắn, thử co bán kính gần người chơi hơn
            if (!foundValid)
            {
                float closeDist = targetDist * 0.5f;
                for (int step = 0; step < 8; step++)
                {
                    float testAngle = baseAngle + (step * (Mathf.PI * 2f / 8f));
                    Vector2 candidateOffset = new Vector2(Mathf.Cos(testAngle), Mathf.Sin(testAngle)) * closeDist;
                    Vector2 candidateWorldPos = (Vector2)playerTransform.position + candidateOffset;

                    if (!Project_D.Dungeon.Room.RoomBoundaryValidator.IsInsideWall(candidateWorldPos, 0.45f) &&
                        Project_D.Dungeon.Room.RoomBoundaryValidator.HasPathClear(playerTransform.position, candidateWorldPos, 0.3f))
                    {
                        bestOffset = candidateOffset;
                        foundValid = true;
                        break;
                    }
                }
            }

            if (!foundValid)
            {
                bestOffset = Vector2.zero;
            }

            _assignedFollowOffsets[requestingMinion] = bestOffset;
            Vector3 rawSlot = (Vector2)playerTransform.position + bestOffset;
            return Project_D.Dungeon.Room.RoomBoundaryValidator.GetNearestWalkablePosition(rawSlot);
        }

        public void ReleaseFollowSlot(MinionController minion)
        {
            if (minion != null && _assignedFollowOffsets.ContainsKey(minion))
            {
                _assignedFollowOffsets.Remove(minion);
            }
        }

        // =========================================================================
        // 2. HÀNG ĐỢI HỒI SINH (RESPAWN LOOP - 10 GIÂY)
        // =========================================================================

        public void ReportMinionDied(MinionController minion)
        {
            if (minion == null) return;

            ReleaseFollowSlot(minion);

            // Tắt GameObject và đưa vào danh sách chờ hồi sinh
            minion.gameObject.SetActive(false);

            float baseDuration = minion.Data != null ? minion.Data.RespawnDuration : 10f;
            float duration = Mathf.Max(1.0f, baseDuration * _minionRespawnTimeMultiplier);
            _respawnTasks.Add(new MinionRespawnTask
            {
                Minion = minion,
                RespawnTime = Time.time + duration
            });

            OnMinionRosterChanged?.Invoke();

            Debug.Log($"<color=orange>[MINION DIED]</color> {minion.Data?.MinionName} đã tử trận! Sẽ tự động hồi sinh sau {duration:0} giây.");
        }

        private void UpdateRespawnQueue()
        {
            if (_respawnTasks.Count == 0) return;

            FindPlayer();
            float now = Time.time;

            for (int i = _respawnTasks.Count - 1; i >= 0; i--)
            {
                var task = _respawnTasks[i];
                if (task == null || task.Minion == null)
                {
                    _respawnTasks.RemoveAt(i);
                    continue;
                }

                if (now >= task.RespawnTime)
                {
                    // Đã hết thời gian chờ 10s: Hồi sinh lính ngay cạnh người chơi
                    Vector3 respawnPos = _playerTransform != null
                        ? _playerTransform.position + (Vector3)UnityEngine.Random.insideUnitCircle * 1.5f
                        : task.Minion.transform.position;

                    // Đảm bảo vị trí hồi sinh không nằm trong tường
                    respawnPos = Project_D.Dungeon.Room.RoomBoundaryValidator.GetNearestWalkablePosition(respawnPos);

                    task.Minion.Respawn(respawnPos);
                    _respawnTasks.RemoveAt(i);
                    OnMinionRosterChanged?.Invoke();

                    Debug.Log($"<color=cyan>[MINION RESPAWN]</color> <b>{task.Minion.Data?.MinionName}</b> đã hồi sinh tràn đầy sinh lực cạnh chủ nhân!");
                }
            }
        }

        // =========================================================================
        // DANH SÁCH LÍNH ĐANG SỞ HỮU GOM NHÓM THEO LOẠI (DÀNH CHO ROSTER BAR UI)
        // =========================================================================

        public struct MinionRosterGroupInfo
        {
            public MinionDataSO Data;
            public int AliveCount;
            public int TotalCount;
        }

        public List<MinionRosterGroupInfo> GetRosterGroups()
        {
            var result = new List<MinionRosterGroupInfo>();
            if (_activeMinions == null || _activeMinions.Count == 0) return result;

            var groups = new Dictionary<string, MinionRosterGroupInfo>();
            for (int i = 0; i < _activeMinions.Count; i++)
            {
                var m = _activeMinions[i];
                if (m == null || m.Data == null) continue;

                string id = m.Data.Id;
                if (!groups.TryGetValue(id, out var info))
                {
                    info = new MinionRosterGroupInfo
                    {
                        Data = m.Data,
                        AliveCount = 0,
                        TotalCount = 0
                    };
                }

                info.TotalCount++;
                if (m.IsAlive && m.gameObject.activeInHierarchy)
                {
                    info.AliveCount++;
                }

                groups[id] = info;
            }

            foreach (var kvp in groups)
            {
                result.Add(kvp.Value);
            }

            // Sắp xếp theo Tier giảm dần, sau đó theo tên
            result.Sort((a, b) =>
            {
                int tierComparison = ((int)b.Data.Tier).CompareTo((int)a.Data.Tier);
                if (tierComparison != 0) return tierComparison;
                return string.Compare(a.Data.MinionName, b.Data.MinionName, System.StringComparison.Ordinal);
            });

            return result;
        }

        public List<MinionController> GetMinionsByData(MinionDataSO data)
        {
            var result = new List<MinionController>();
            if (data == null || _activeMinions == null) return result;

            for (int i = 0; i < _activeMinions.Count; i++)
            {
                var m = _activeMinions[i];
                if (m != null && m.Data == data)
                {
                    result.Add(m);
                }
            }
            return result;
        }

        public float GetRemainingRespawnTime(MinionController minion)
        {
            if (minion == null || _respawnTasks == null) return 0f;
            for (int i = 0; i < _respawnTasks.Count; i++)
            {
                if (_respawnTasks[i].Minion == minion)
                {
                    return Mathf.Max(0f, _respawnTasks[i].RespawnTime - Time.time);
                }
            }
            return 0f;
        }

        // =========================================================================
        // 3. HỆ THỐNG HỢP NHẤT THỦ CÔNG THEO NÚT BẤM (MERGE SYSTEM - ĐA NGUYÊN LIỆU)
        // =========================================================================

        private Dictionary<string, int> GetMinionCountMap()
        {
            var map = new Dictionary<string, int>();
            foreach (var minion in _activeMinions)
            {
                if (minion == null || minion.Data == null) continue;
                string id = minion.Data.Id;
                map[id] = map.TryGetValue(id, out int c) ? c + 1 : 1;
            }
            return map;
        }

        private MergeAvailableInfo EvaluateRecipe(MergeRecipeSO recipe, Dictionary<string, int> countMap)
        {
            if (recipe == null) return null;

            bool isLocked = recipe.RequiresSpecialUnlock && !IsSpecialConditionUnlocked(recipe.UnlockConditionKey);
            bool canMerge = !isLocked;
            int totalCurrent = 0;
            int totalRequired = 0;
            var parts = new List<string>();

            var ingredients = recipe.Ingredients;
            if (ingredients.Count == 0)
            {
                canMerge = false;
            }

            foreach (var ing in ingredients)
            {
                string id = ing.MinionId;
                int have = countMap.TryGetValue(id, out int c) ? c : 0;
                int need = ing.Count;

                totalCurrent += Mathf.Min(have, need);
                totalRequired += need;

                if (have < need)
                {
                    canMerge = false;
                }

                string name = ing.MinionData != null ? ing.MinionData.MinionName : id;
                parts.Add($"{name} ({have}/{need})");
            }

            return new MergeAvailableInfo
            {
                Recipe = recipe,
                CurrentCount = totalCurrent,
                RequiredCount = totalRequired,
                CanMerge = canMerge,
                IsLockedBySpecialCondition = isLocked,
                IngredientsDescription = string.Join(" + ", parts)
            };
        }

        public List<MergeAvailableInfo> GetAvailableMerges()
        {
            var list = new List<MergeAvailableInfo>();
            if (_database == null || _database.AllRecipes == null) return list;

            var countMap = GetMinionCountMap();

            foreach (var recipe in _database.AllRecipes)
            {
                if (recipe == null) continue;
                var info = EvaluateRecipe(recipe, countMap);
                if (info == null) continue;

                // Chỉ hiện trong thanh Quick Merge nếu có ít nhất 1 lính thành phần hoặc đã sẵn sàng
                if (info.CurrentCount > 0 || info.CanMerge)
                {
                    list.Add(info);
                }
            }

            // Sắp xếp ưu tiên các công thức ĐỦ ĐIỀU KIỆN (CanMerge) lên đầu danh sách
            list.Sort((a, b) => b.CanMerge.CompareTo(a.CanMerge));
            return list;
        }

        // Lấy trạng thái của TOÀN BỘ công thức trong Database (dành cho Sổ Công Thức tra cứu)
        public List<MergeAvailableInfo> GetAllRecipesStatus()
        {
            var list = new List<MergeAvailableInfo>();
            if (_database == null || _database.AllRecipes == null) return list;

            var countMap = GetMinionCountMap();

            foreach (var recipe in _database.AllRecipes)
            {
                if (recipe == null) continue;
                var info = EvaluateRecipe(recipe, countMap);
                if (info != null)
                {
                    list.Add(info);
                }
            }

            list.Sort((a, b) => b.CanMerge.CompareTo(a.CanMerge));
            return list;
        }

        // Hợp nhất nhanh công thức khả dụng đầu tiên (Quick Merge)
        public bool QuickMergeFirstAvailable()
        {
            var merges = GetAvailableMerges();
            foreach (var m in merges)
            {
                if (m != null && m.CanMerge && m.Recipe != null)
                {
                    return ExecuteMerge(m.Recipe);
                }
            }
            return false;
        }

        public bool ExecuteMerge(MergeRecipeSO recipe)
        {
            if (recipe == null) return false;

            // 1. Kiểm tra điều kiện mở khóa đặc biệt cho Tier 4
            if (recipe.RequiresSpecialUnlock && !IsSpecialConditionUnlocked(recipe.UnlockConditionKey))
            {
                Debug.LogWarning($"<color=yellow>[MERGE BLOCKED]</color> Chưa mở khóa điều kiện đặc biệt: '{recipe.UnlockConditionKey}' ({recipe.LockedHint})");
                return false;
            }

            var ingredients = recipe.Ingredients;
            if (ingredients.Count == 0) return false;

            // 2. Thu thập và xác nhận đủ toàn bộ các lính nguyên liệu theo từng thành phần
            var minionsToConsume = new List<MinionController>();
            var usedSet = new HashSet<MinionController>();

            foreach (var ing in ingredients)
            {
                int needed = ing.Count;
                int collected = 0;

                foreach (var m in _activeMinions)
                {
                    if (m != null && m.Data != null && !usedSet.Contains(m) && ing.Matches(m))
                    {
                        minionsToConsume.Add(m);
                        usedSet.Add(m);
                        collected++;
                        if (collected >= needed) break;
                    }
                }

                if (collected < needed)
                {
                    Debug.LogWarning($"<color=yellow>[MERGE FAIL]</color> Không đủ lính nguyên liệu cho thành phần '{ing.MinionId}' ({collected}/{needed})!");
                    return false;
                }
            }

            // 3. Thu hồi toàn bộ lính nguyên liệu đã chọn về Pool
            foreach (var candidate in minionsToConsume)
            {
                ReleaseFollowSlot(candidate);
                _activeMinions.Remove(candidate);

                // Loại khỏi hàng đợi hồi sinh nếu đang chờ hồi sinh
                _respawnTasks.RemoveAll(t => t.Minion == candidate);

                if (PoolManager.Instance != null && candidate.Data.Prefab != null)
                {
                    PoolManager.Instance.ReturnToPool(candidate.Data.Prefab, candidate.gameObject);
                }
                else
                {
                    Destroy(candidate.gameObject);
                }
            }

            // 4. Triệu hồi lính mới thành quả
            if (recipe.OutputMinion != null)
            {
                SummonMinion(recipe.OutputMinion);
                OnMinionMerged?.Invoke(recipe.OutputMinion);
            }

            OnAvailableMergesChanged?.Invoke();
            OnMinionRosterChanged?.Invoke();

            Debug.Log($"<color=magenta><b>[MERGE SUCCESS]</b></color> 🎉 Hợp nhất thành công <b>{recipe.RecipeName}</b> -> <b>{recipe.OutputMinion?.MinionName}</b>!");
            return true;
        }

        // =========================================================================
        // 4. DỊCH CHUYỂN TOÀN BỘ LÍNH VỀ GẦN NGƯỜI CHƠI KHI QUA PHÒNG MỚI
        // =========================================================================

        /// <summary>
        /// Dịch chuyển toàn bộ lính đồng minh về vị trí an toàn xung quanh người chơi
        /// khi chuyển sang phòng hầm ngục mới (thay vì bị kẹt giữa phòng cũ).
        /// </summary>
        public void TeleportAllMinionsToPlayer()
        {
            FindPlayer();
            if (_playerTransform == null) return;

            Vector3 playerPos = _playerTransform.position;
            _assignedFollowOffsets.Clear();

            int count = _activeMinions.Count;
            for (int i = 0; i < count; i++)
            {
                var minion = _activeMinions[i];
                if (minion == null) continue;

                // Phân bố vòng cung quanh người chơi ở bán kính 1.2m - 2.0m
                float angle = (2f * Mathf.PI / Mathf.Max(1, count)) * i;
                float dist = 1.2f + (i % 2) * 0.4f;
                Vector3 candidatePos = playerPos + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * dist;

                // Đảm bảo vị trí rơi vào sàn trống hợp lệ
                candidatePos = Project_D.Dungeon.Room.RoomBoundaryValidator.GetNearestWalkablePosition(candidatePos, 3.5f, 0.35f);

                minion.TeleportTo(candidatePos);
            }

            Debug.Log($"<color=cyan>[MinionManager]</color> Đã dịch chuyển {count} lính về gần Người chơi tại phòng mới ({playerPos}).");
        }

        // =========================================================================
        // 4. MỞ KHÓA ĐIỀU KIỆN ĐẶC BIỆT TIER 4
        // =========================================================================

        public void UnlockSpecialCondition(string conditionKey)
        {
            if (string.IsNullOrEmpty(conditionKey)) return;
            if (_unlockedSpecialConditions.Add(conditionKey))
            {
                Debug.Log($"<color=yellow><b>[TIER 4 UNLOCK]</b></color> Đã mở khóa điều kiện đặc biệt: '{conditionKey}'!");
                OnAvailableMergesChanged?.Invoke();
            }
        }

        public bool IsSpecialConditionUnlocked(string conditionKey)
        {
            if (string.IsNullOrEmpty(conditionKey)) return true;
            return _unlockedSpecialConditions.Contains(conditionKey);
        }

        public void SetDatabase(MinionDatabaseSO database)
        {
            _database = database;
        }
    }
}

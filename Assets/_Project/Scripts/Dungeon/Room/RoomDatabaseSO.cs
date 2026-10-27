using System.Collections.Generic;
using UnityEngine;
using Project_D.Dungeon.Run;

namespace Project_D.Dungeon.Room
{
    [CreateAssetMenu(fileName = "RoomDatabase", menuName = "Project_D/Dungeon/Room Database")]
    public class RoomDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<RoomVariationSO> _allRooms = new List<RoomVariationSO>();

        public IReadOnlyList<RoomVariationSO> AllRooms => _allRooms;

        public List<RoomVariationSO> GetRoomsForType(RunNodeType type)
        {
            var result = new List<RoomVariationSO>();
            if (_allRooms == null) return result;

            foreach (var r in _allRooms)
            {
                if (r != null && r.SupportedType == type && r.RoomPrefab != null)
                {
                    result.Add(r);
                }
            }

            // Fallback nếu không có phòng nào trực tiếp cho loại node này (VD: StarterReward, BigReward, MiniBoss)
            if (result.Count == 0)
            {
                RunNodeType fallbackType = GetFallbackType(type);
                if (fallbackType != type)
                {
                    foreach (var r in _allRooms)
                    {
                        if (r != null && r.SupportedType == fallbackType && r.RoomPrefab != null)
                        {
                            result.Add(r);
                        }
                    }
                }
            }

            // Fallback an toàn: nếu vẫn không tìm thấy, lấy bất kỳ phòng nào có Prefab
            if (result.Count == 0)
            {
                foreach (var r in _allRooms)
                {
                    if (r != null && r.RoomPrefab != null)
                    {
                        result.Add(r);
                    }
                }
            }

            return result;
        }

        private RunNodeType GetFallbackType(RunNodeType type)
        {
            switch (type)
            {
                case RunNodeType.StarterReward:
                case RunNodeType.BigReward:
                    return RunNodeType.BuffReward;
                case RunNodeType.MiniBoss:
                    return RunNodeType.EliteBattle;
                default:
                    return type;
            }
        }

        public RoomVariationSO GetRandomRoomForType(RunNodeType type, RoomVariationSO lastUsed = null)
        {
            var candidates = GetRoomsForType(type);
            if (candidates.Count == 0) return null;
            if (candidates.Count == 1) return candidates[0];

            // Nếu có từ 2 phòng trở lên, loại trừ phòng vừa chơi để tránh trùng lặp liên tiếp
            if (lastUsed != null && candidates.Contains(lastUsed))
            {
                candidates.Remove(lastUsed);
            }

            // Tính tổng trọng số ngẫu nhiên
            int totalWeight = 0;
            foreach (var r in candidates)
            {
                totalWeight += Mathf.Max(1, r.SelectionWeight);
            }

            int randomRoll = Random.Range(0, totalWeight);
            int currentSum = 0;

            foreach (var r in candidates)
            {
                currentSum += Mathf.Max(1, r.SelectionWeight);
                if (randomRoll < currentSum)
                {
                    return r;
                }
            }

            return candidates[0];
        }

        public void RegisterVariation(RoomVariationSO variation)
        {
            if (variation == null) return;
            if (_allRooms == null) _allRooms = new List<RoomVariationSO>();

            if (!_allRooms.Contains(variation))
            {
                _allRooms.Add(variation);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Auto Find All Room Variations In Project")]
        public void AutoFindAllVariationsInProject()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("t:RoomVariationSO");
            if (_allRooms == null) _allRooms = new List<RoomVariationSO>();
            _allRooms.Clear();

            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<RoomVariationSO>(path);
                if (asset != null)
                {
                    _allRooms.Add(asset);
                }
            }

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"<color=green>[RoomDatabase]</color> Đã tự động cập nhật {_allRooms.Count} biến thể phòng vào Database!");
        }
#endif
    }
}

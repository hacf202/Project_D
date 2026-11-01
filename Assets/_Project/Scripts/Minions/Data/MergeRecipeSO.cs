using System.Collections.Generic;
using UnityEngine;

namespace Project_D.Minions
{
    [System.Serializable]
    public class MergeIngredient
    {
        [Tooltip("Kéo trực tiếp ScriptableObject của lính nguyên liệu vào đây")]
        [SerializeField] private MinionDataSO _minionData;
        [Tooltip("Số lượng lính loại này cần để hợp nhất (1, 2, 3...)")]
        [SerializeField] private int _count = 1;

        // Lưu ẩn để tương thích dữ liệu cũ nếu có
        [SerializeField, HideInInspector] private string _minionId = "";

        public MinionDataSO MinionData => _minionData;
        public string MinionId => _minionData != null ? _minionData.Id : _minionId;
        public int Count => Mathf.Max(1, _count);

        public MergeIngredient() { }

        public MergeIngredient(MinionDataSO data, int count = 1)
        {
            _minionData = data;
            _minionId = data != null ? data.Id : "";
            _count = count;
        }

        public MergeIngredient(string minionId, int count = 1)
        {
            _minionId = minionId;
            _count = count;
        }

        public bool Matches(MinionController minion)
        {
            if (minion == null || minion.Data == null) return false;
            if (_minionData != null)
            {
                return minion.Data == _minionData || minion.Data.Id == _minionData.Id;
            }
            return !string.IsNullOrEmpty(_minionId) && minion.Data.Id == _minionId;
        }
    }

    [CreateAssetMenu(fileName = "NewMergeRecipe", menuName = "Project_D/Minions/Merge Recipe")]
    public class MergeRecipeSO : ScriptableObject
    {
        [Header("Recipe Identity")]
        [SerializeField] private string _recipeId = "recipe_warrior_t1_to_t2";
        [SerializeField] private string _recipeName = "Hợp Nhất Chiến Binh T2";
        [TextArea(2, 3)]
        [SerializeField] private string _description = "Dung hợp lính để thăng cấp lên bậc cao hơn.";

        [Header("Input Ingredients (Thành phần nguyên liệu: 1, 2 hoặc 3 loại lính)")]
        [Tooltip("Cấu hình các loại lính thành phần: kéo trực tiếp SO lính hoặc nhập ID, quy định số lượng")]
        [SerializeField] private List<MergeIngredient> _ingredients = new List<MergeIngredient>();

        // Trường dự phòng tương thích ngược
        [SerializeField, HideInInspector] private string _inputMinionId = "";
        [SerializeField, HideInInspector] private MinionTier _inputTier = MinionTier.Tier1;
        [SerializeField, HideInInspector] private int _requiredCount = 3;

        [Header("Output Result (Lính Tạo Thành)")]
        [SerializeField] private MinionDataSO _outputMinion;

        [Header("Special Unlock Condition (Dành cho Tier 4 Đặc Biệt)")]
        [SerializeField] private bool _requiresSpecialUnlock = false;
        [Tooltip("Mã định danh điều kiện mở khóa (ví dụ: defeat_floor10_boss, ancient_relic_unlocked...)")]
        [SerializeField] private string _unlockConditionKey = "special_tier4_unlock";
        [SerializeField] private string _lockedHint = "Cần mở khóa điều kiện đặc biệt để dung hợp Tier 4.";

        public string RecipeId => _recipeId;
        public string RecipeName => _recipeName;
        public string Description => _description;

        public IReadOnlyList<MergeIngredient> Ingredients
        {
            get
            {
                if (_ingredients != null && _ingredients.Count > 0)
                {
                    return _ingredients;
                }
                // Fallback nếu công thức cũ chưa có danh sách Ingredients
                if (!string.IsNullOrEmpty(_inputMinionId))
                {
                    return new List<MergeIngredient> { new MergeIngredient(_inputMinionId, _requiredCount) };
                }
                return System.Array.Empty<MergeIngredient>();
            }
        }

        // Tương thích ngược: lấy thông tin của nguyên liệu đầu tiên
        public string InputMinionId
        {
            get
            {
                var ings = Ingredients;
                return ings.Count > 0 ? ings[0].MinionId : _inputMinionId;
            }
        }

        public MinionTier InputTier
        {
            get
            {
                var ings = Ingredients;
                if (ings.Count > 0 && ings[0].MinionData != null)
                {
                    return ings[0].MinionData.Tier;
                }
                return _inputTier;
            }
        }

        public int RequiredCount
        {
            get
            {
                var ings = Ingredients;
                if (ings.Count > 0)
                {
                    int total = 0;
                    foreach (var ing in ings) total += ing.Count;
                    return Mathf.Max(1, total);
                }
                return Mathf.Max(1, _requiredCount);
            }
        }

        public MinionDataSO OutputMinion => _outputMinion;
        public bool RequiresSpecialUnlock => _requiresSpecialUnlock;
        public string UnlockConditionKey => _unlockConditionKey;
        public string LockedHint => _lockedHint;
    }
}

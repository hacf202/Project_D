using System.Collections.Generic;
using UnityEngine;

namespace Project_D.Minions
{
    [CreateAssetMenu(fileName = "MinionDatabase", menuName = "Project_D/Minions/Minion Database")]
    public class MinionDatabaseSO : ScriptableObject
    {
        [Header("Minion Catalog")]
        [SerializeField] private List<MinionDataSO> _allMinions = new List<MinionDataSO>();

        [Header("Merge Recipes Catalog")]
        [SerializeField] private List<MergeRecipeSO> _allRecipes = new List<MergeRecipeSO>();

        public IReadOnlyList<MinionDataSO> AllMinions => _allMinions;
        public IReadOnlyList<MergeRecipeSO> AllRecipes => _allRecipes;

        public MinionDataSO GetMinion(string id)
        {
            if (_allMinions == null) return null;
            return _allMinions.Find(m => m != null && m.Id == id);
        }

        public MinionDataSO GetMinion(string id, MinionTier tier)
        {
            if (_allMinions == null) return null;
            var exact = _allMinions.Find(m => m != null && m.Id == id);
            if (exact != null) return exact;
            return _allMinions.Find(m => m != null && m.Tier == tier && (m.Id.Contains(id) || id.Contains(m.Id)));
        }

        public MergeRecipeSO GetRecipe(string inputId, MinionTier inputTier)
        {
            if (_allRecipes == null) return null;
            return _allRecipes.Find(r => r != null && r.InputMinionId == inputId && r.InputTier == inputTier);
        }

        public List<MergeRecipeSO> GetAvailableRecipesFor(string inputId, MinionTier inputTier)
        {
            var list = new List<MergeRecipeSO>();
            if (_allRecipes == null) return list;
            foreach (var r in _allRecipes)
            {
                if (r == null) continue;
                bool hasIngredient = false;
                foreach (var ing in r.Ingredients)
                {
                    if (ing != null && ing.MinionId == inputId)
                    {
                        hasIngredient = true;
                        break;
                    }
                }
                if (hasIngredient || (r.InputMinionId == inputId && r.InputTier == inputTier))
                {
                    list.Add(r);
                }
            }
            return list;
        }

#if UNITY_EDITOR
        [ContextMenu("Auto Populate All Minions & Recipes")]
        public void AutoPopulateDatabase()
        {
            _allMinions.Clear();
            _allRecipes.Clear();

            string[] minionGuids = UnityEditor.AssetDatabase.FindAssets("t:MinionDataSO");
            foreach (var guid in minionGuids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<MinionDataSO>(path);
                if (asset != null && !_allMinions.Contains(asset))
                {
                    _allMinions.Add(asset);
                }
            }

            string[] recipeGuids = UnityEditor.AssetDatabase.FindAssets("t:MergeRecipeSO");
            foreach (var guid in recipeGuids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<MergeRecipeSO>(path);
                if (asset != null && !_allRecipes.Contains(asset))
                {
                    _allRecipes.Add(asset);
                }
            }

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"<color=cyan>[MinionDatabase]</color> Đã nạp thành công {_allMinions.Count} Lính và {_allRecipes.Count} Công thức Merge!");
        }
#endif
    }
}

#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using Project_D.Minions;
using Project_D.Upgrades;

namespace Project_D.Minions.Editor
{
    public static class MinionSystemSetupUtility
    {
        private const string PREFAB_DIR = "Assets/_Project/Prefabs/Minions";
        private const string SO_DIR = "Assets/_Project/ScriptableObjects/Minions";
        private const string RECIPE_DIR = "Assets/_Project/ScriptableObjects/Minions/Recipes";
        private const string UPGRADE_DIR = "Assets/_Project/ScriptableObjects/Upgrades";
        private const string RES_DIR = "Assets/_Project/Resources";

        [InitializeOnLoadMethod]
        private static void AutoCheckAndGenerate()
        {
            EditorApplication.delayCall += () =>
            {
                string dbPath = $"{SO_DIR}/MinionDatabase.asset";
                if (!File.Exists(dbPath))
                {
                    GenerateAllSampleMinions(false);
                }
                AssignIconsToExistingMinions();
            };
        }

        [MenuItem("Project_D/Minions/Generate All Sample Minions, Recipes & Database", false, 1)]
        public static void GenerateFromMenu()
        {
            GenerateAllSampleMinions(true);
            AssignIconsToExistingMinions();
        }

        [MenuItem("Project_D/Minions/Fix and Assign Minion Icons", false, 2)]
        public static void FixAndAssignIcons()
        {
            AssignIconsToExistingMinions();
        }

        public static void AssignIconsToExistingMinions()
        {
            Sprite archerSprite = null;
            var archerAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Characters/Minions/sprOrcArcher3.png");
            if (archerAssets != null)
            {
                foreach (var a in archerAssets)
                {
                    if (a is Sprite s)
                    {
                        if (s.name == "sprOrcArcher3_3" || archerSprite == null)
                            archerSprite = s;
                    }
                }
            }

            Sprite warriorSprite = null;
            var warriorAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Characters/Minions/sprSkeleton3.png");
            if (warriorAssets != null)
            {
                foreach (var a in warriorAssets)
                {
                    if (a is Sprite s)
                    {
                        if (s.name == "sprSkeleton3_0" || warriorSprite == null)
                            warriorSprite = s;
                    }
                }
            }

            string[] guids = AssetDatabase.FindAssets("t:MinionDataSO", new[] { SO_DIR });
            bool changed = false;
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var minionData = AssetDatabase.LoadAssetAtPath<MinionDataSO>(path);
                if (minionData != null && minionData.Icon == null)
                {
                    var serialized = new SerializedObject(minionData);
                    var iconProp = serialized.FindProperty("_icon");
                    if (iconProp != null)
                    {
                        Sprite chosen = (minionData.Class == MinionClass.Archer) ? archerSprite : warriorSprite;
                        if (chosen != null)
                        {
                            iconProp.objectReferenceValue = chosen;
                            serialized.ApplyModifiedProperties();
                            EditorUtility.SetDirty(minionData);
                            changed = true;
                        }
                    }
                }
            }

            if (changed)
            {
                AssetDatabase.SaveAssets();
                Debug.Log("[MinionSystemSetupUtility] Auto-assigned missing icons for MinionDataSO assets.");
            }
        }

        public static void GenerateAllSampleMinions(bool showDialog = false)
        {
            EnsureDirectories();

            // 1. Tạo Prefab đạn bắn của Lính
            var projPrefab = CreateOrGetMinionProjectilePrefab();

            // 2. Tạo Prefab cho từng lớp lính
            var warriorPrefab = CreateOrGetMinionPrefab("Minion_Warrior", Color.cyan);
            var archerPrefab = CreateOrGetMinionPrefab("Minion_Archer", new Color(0.4f, 1f, 0.4f));

            // 3. Tạo ScriptableObjects cho nhánh Chiến Binh (Tier 1 -> 4)
            var wT1 = CreateMinionSO("minion_warrior", MinionTier.Tier1, "Chiến Binh Kiếm Gỗ", "Lính cận chiến cơ bản.",
                MinionClass.Warrior, 120, 15, 1.2f, 1.2f, 4.5f, 6.0f, AttackMethod.Melee, warriorPrefab, null, 10f);
            var wT2 = CreateMinionSO("minion_warrior", MinionTier.Tier2, "Chiến Binh Giáp Sắt", "Lính cận chiến trung cấp, giáp cứng cáp.",
                MinionClass.Warrior, 260, 35, 1.3f, 1.3f, 4.8f, 7.0f, AttackMethod.Melee, warriorPrefab, null, 10f);
            var wT3 = CreateMinionSO("minion_warrior", MinionTier.Tier3, "Hiệp Sĩ Hoàng Gia", "Đại hiệp sĩ dũng mãnh, sát thương uy lực.",
                MinionClass.Warrior, 550, 80, 1.5f, 1.4f, 5.0f, 8.0f, AttackMethod.Melee, warriorPrefab, null, 10f);
            var wT4 = CreateMinionSO("minion_warrior", MinionTier.Tier4_Special, "Thánh Thú Hộ Vệ Thần Thoại", "Chiến binh truyền thuyết tỏa hào quang dậm đất AoE.",
                MinionClass.Warrior, 1200, 180, 1.8f, 1.5f, 5.5f, 9.5f, AttackMethod.AoE, warriorPrefab, null, 10f);

            // 4. Tạo ScriptableObjects cho nhánh Cung Thủ (Tier 1 -> 4)
            var aT1 = CreateMinionSO("minion_archer", MinionTier.Tier1, "Cung Thủ Tập Sự", "Bắn tên tầm xa hỗ trợ liên tục.",
                MinionClass.Archer, 70, 12, 5.5f, 1.5f, 4.2f, 7.5f, AttackMethod.RangedProjectile, archerPrefab, projPrefab, 10f);
            var aT2 = CreateMinionSO("minion_archer", MinionTier.Tier2, "Cung Thủ Thiện Xạ", "Tầm bắn xa và tốc bắn cải thiện.",
                MinionClass.Archer, 160, 28, 6.5f, 1.8f, 4.5f, 8.5f, AttackMethod.RangedProjectile, archerPrefab, projPrefab, 10f);
            var aT3 = CreateMinionSO("minion_archer", MinionTier.Tier3, "Đặc Nhiệm Bão Tiễn", "Xạ thủ tinh nhuệ với những mũi tên chí mạng.",
                MinionClass.Archer, 360, 65, 7.5f, 2.2f, 4.8f, 9.5f, AttackMethod.RangedProjectile, archerPrefab, projPrefab, 10f);
            var aT4 = CreateMinionSO("minion_archer", MinionTier.Tier4_Special, "Thần Tiễn Thiên Giới", "Cung thủ thần thoại bắn xuyên phá mọi kẻ thù.",
                MinionClass.Archer, 850, 140, 9.0f, 2.8f, 5.2f, 11.0f, AttackMethod.RangedProjectile, archerPrefab, projPrefab, 10f);

            // 5. Tạo các Công Thức Hợp Nhất (Merge Recipes)
            // Nhánh Chiến Binh: 3 T1 -> 1 T2; 3 T2 -> 1 T3; 2 T3 -> 1 T4 (cần unlock)
            CreateRecipeSO("recipe_warrior_t1_to_t2", "Hợp Nhất Chiến Binh T2", "Gộp 3 Chiến Binh T1 thành 1 Chiến Binh Giáp Sắt T2",
                "minion_warrior", MinionTier.Tier1, 3, wT2, false, "");
            CreateRecipeSO("recipe_warrior_t2_to_t3", "Hợp Nhất Hiệp Sĩ T3", "Gộp 3 Chiến Binh T2 thành 1 Hiệp Sĩ Hoàng Gia T3",
                "minion_warrior", MinionTier.Tier2, 3, wT3, false, "");
            CreateRecipeSO("recipe_warrior_t3_to_t4", "Dung Hợp Thánh Thú T4 (Đặc Biệt)", "Cần 2 Hiệp Sĩ T3 và Mở Khóa Bí Thuật Chiến Binh",
                "minion_warrior", MinionTier.Tier3, 2, wT4, true, "unlock_warrior_t4", "Cần mở khóa Bí Thuật Chiến Binh để dung hợp Tier 4!");

            // Nhánh Cung Thủ: 3 T1 -> 1 T2; 3 T2 -> 1 T3; 2 T3 -> 1 T4 (cần unlock)
            CreateRecipeSO("recipe_archer_t1_to_t2", "Hợp Nhất Cung Thủ T2", "Gộp 3 Cung Thủ T1 thành 1 Thiện Xạ T2",
                "minion_archer", MinionTier.Tier1, 3, aT2, false, "");
            CreateRecipeSO("recipe_archer_t2_to_t3", "Hợp Nhất Bão Tiễn T3", "Gộp 3 Thiện Xạ T2 thành 1 Bão Tiễn T3",
                "minion_archer", MinionTier.Tier2, 3, aT3, false, "");
            CreateRecipeSO("recipe_archer_t3_to_t4", "Dung Hợp Thần Tiễn T4 (Đặc Biệt)", "Cần 2 Bão Tiễn T3 và Mở Khóa Bí Thuật Thần Tiễn",
                "minion_archer", MinionTier.Tier3, 2, aT4, true, "unlock_archer_t4", "Cần mở khóa Bí Thuật Thần Tiễn để dung hợp Tier 4!");

            // 6. Tạo MinionDatabase.asset và tự động Populate
            string dbPath = $"{SO_DIR}/MinionDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<MinionDatabaseSO>(dbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<MinionDatabaseSO>();
                AssetDatabase.CreateAsset(db, dbPath);
            }
            db.AutoPopulateDatabase();

            // Cũng tạo trong Resources để Runtime Resources.Load tìm thấy
            string resDbPath = $"{RES_DIR}/MinionDatabase.asset";
            var resDb = AssetDatabase.LoadAssetAtPath<MinionDatabaseSO>(resDbPath);
            if (resDb == null)
            {
                resDb = ScriptableObject.CreateInstance<MinionDatabaseSO>();
                AssetDatabase.CreateAsset(resDb, resDbPath);
            }
            resDb.AutoPopulateDatabase();

            // 7. Tạo Thẻ Bài Nâng Cấp tương ứng trong kho Upgrades
            CreateUpgradeCardSO("card_summon_warrior_t1", "Triệu Hồi Chiến Binh", "Triệu hồi 1 Chiến Binh Kiếm Gỗ (Tier 1) hỗ trợ chiến đấu.", wT1);
            CreateUpgradeCardSO("card_summon_archer_t1", "Triệu Hồi Cung Thủ", "Triệu hồi 1 Cung Thủ Tập Sự (Tier 1) bắn tỉa từ xa.", aT1);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green><b>[MinionSetup]</b></color> 🎉 Đã khởi tạo hoàn tất toàn bộ Prefab Lính, ScriptableObjects, Công thức Merge và Thẻ Bài Nâng Cấp!");
            if (showDialog && !Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Khởi Tạo Thành Công!",
                    "Đã tạo đầy đủ bộ dữ liệu mẫu:\n\n" +
                    "• 2 Prefab Lính (Warrior, Archer) & 1 Prefab Đạn Bắn\n" +
                    "• 8 Dữ Liệu Lính (Tier 1 -> Tier 4 cho Warrior & Archer)\n" +
                    "• 6 Công Thức Hợp Nhất (T1->T2, T2->T3, T3->T4 có khóa)\n" +
                    "• MinionDatabase.asset đồng bộ toàn bộ catalog\n" +
                    "• 2 Thẻ Bài Nâng Cấp triệu hồi lính vào UpgradeManager!", "Tuyệt Vời");
            }
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(PREFAB_DIR)) Directory.CreateDirectory(PREFAB_DIR);
            if (!Directory.Exists(SO_DIR)) Directory.CreateDirectory(SO_DIR);
            if (!Directory.Exists(RECIPE_DIR)) Directory.CreateDirectory(RECIPE_DIR);
            if (!Directory.Exists(UPGRADE_DIR)) Directory.CreateDirectory(UPGRADE_DIR);
            if (!Directory.Exists(RES_DIR)) Directory.CreateDirectory(RES_DIR);
        }

        private static GameObject CreateOrGetMinionProjectilePrefab()
        {
            string path = $"{PREFAB_DIR}/MinionProjectile.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject("MinionProjectile");
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.25f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetDefaultSquareSprite();
            sr.color = Color.yellow;
            sr.sortingOrder = 7;
            go.transform.localScale = new Vector3(0.4f, 0.15f, 1f);

            go.AddComponent<MinionProjectile>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateOrGetMinionPrefab(string name, Color color)
        {
            string path = $"{PREFAB_DIR}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject(name);
            int minionLayer = LayerMask.NameToLayer("Minion");
            if (minionLayer >= 0) go.layer = minionLayer;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            // 1. Solid Collider chặn tường
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.38f;
            col.isTrigger = false;

            // 2. Trigger Collider nhận diện quái và nhận đòn tiếp xúc
            var colHitbox = go.AddComponent<CircleCollider2D>();
            colHitbox.radius = 0.5f;
            colHitbox.isTrigger = true;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetDefaultSquareSprite();
            sr.color = color;
            sr.sortingOrder = 5;

            go.AddComponent<MinionController>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static MinionDataSO CreateMinionSO(
            string id, MinionTier tier, string name, string desc, MinionClass cls,
            int hp, int atk, float range, float atkSpeed, float moveSpeed, float aggro,
            AttackMethod method, GameObject prefab, GameObject projPrefab, float respawn)
        {
            string path = $"{SO_DIR}/Minion_{id}_T{(int)tier}.asset";
            var so = AssetDatabase.LoadAssetAtPath<MinionDataSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<MinionDataSO>();
                AssetDatabase.CreateAsset(so, path);
            }

            var serialized = new SerializedObject(so);
            serialized.FindProperty("_id").stringValue = id;
            serialized.FindProperty("_minionName").stringValue = name;
            serialized.FindProperty("_description").stringValue = desc;
            serialized.FindProperty("_class").enumValueIndex = (int)cls;
            serialized.FindProperty("_tier").enumValueIndex = (int)tier;
            serialized.FindProperty("_maxHealth").intValue = hp;
            serialized.FindProperty("_attackDamage").intValue = atk;
            serialized.FindProperty("_attackRange").floatValue = range;
            serialized.FindProperty("_attackSpeed").floatValue = atkSpeed;
            serialized.FindProperty("_moveSpeed").floatValue = moveSpeed;
            serialized.FindProperty("_aggroRange").floatValue = aggro;
            serialized.FindProperty("_attackMethod").enumValueIndex = (int)method;
            serialized.FindProperty("_prefab").objectReferenceValue = prefab;
            serialized.FindProperty("_projectilePrefab").objectReferenceValue = projPrefab;
            serialized.FindProperty("_respawnDuration").floatValue = respawn;
            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(so);
            return so;
        }

        private static MergeRecipeSO CreateRecipeSO(
            string recipeId, string name, string desc,
            string inputId, MinionTier inputTier, int reqCount,
            MinionDataSO outputMinion, bool specialUnlock, string unlockKey, string hint = "")
        {
            string path = $"{RECIPE_DIR}/{recipeId}.asset";
            var so = AssetDatabase.LoadAssetAtPath<MergeRecipeSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<MergeRecipeSO>();
                AssetDatabase.CreateAsset(so, path);
            }

            var serialized = new SerializedObject(so);
            serialized.FindProperty("_recipeId").stringValue = recipeId;
            serialized.FindProperty("_recipeName").stringValue = name;
            serialized.FindProperty("_description").stringValue = desc;
            serialized.FindProperty("_inputMinionId").stringValue = inputId;
            serialized.FindProperty("_inputTier").enumValueIndex = (int)inputTier;
            serialized.FindProperty("_requiredCount").intValue = reqCount;
            serialized.FindProperty("_outputMinion").objectReferenceValue = outputMinion;
            serialized.FindProperty("_requiresSpecialUnlock").boolValue = specialUnlock;
            serialized.FindProperty("_unlockConditionKey").stringValue = unlockKey;
            serialized.FindProperty("_lockedHint").stringValue = hint;
            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(so);
            return so;
        }

        private static void CreateUpgradeCardSO(string id, string title, string desc, MinionDataSO minionData)
        {
            string path = $"{UPGRADE_DIR}/{id}.asset";
            var so = AssetDatabase.LoadAssetAtPath<UpgradeDataSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<UpgradeDataSO>();
                AssetDatabase.CreateAsset(so, path);
            }

            var serialized = new SerializedObject(so);
            serialized.FindProperty("_upgradeId").stringValue = id;
            serialized.FindProperty("_title").stringValue = title;
            serialized.FindProperty("_description").stringValue = desc;
            serialized.FindProperty("_type").enumValueIndex = (int)UpgradeType.SummonMinion;
            serialized.FindProperty("_minionData").objectReferenceValue = minionData;
            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(so);
        }

        private static Sprite GetDefaultSquareSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd") 
                ?? AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        }
    }
}
#endif

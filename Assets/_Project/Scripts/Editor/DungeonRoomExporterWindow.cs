#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Project_D.Dungeon.Run;
using Project_D.Dungeon.Room;

namespace Project_D.EditorTools
{
    public class DungeonRoomExporterWindow : EditorWindow
    {
        private Grid _targetGrid;
        private string _roomName = "Room_Custom_01";
        private RunNodeType _roomType = RunNodeType.NormalBattle;
        private int _weight = 10;
        private string _description = "Phòng vẽ tự do bằng Tile Palette";

        [MenuItem("Project_D/Dungeon/Export Room Prefab Window", false, 2)]
        [MenuItem("Window/Dungeon/Room Exporter", false, 20)]
        public static void ShowWindow()
        {
            var win = GetWindow<DungeonRoomExporterWindow>("Xuất Bản Phòng Dungeon");
            win.minSize = new Vector2(420, 480);
            win.Show();
        }

        private void OnEnable()
        {
            FindDefaultGrid();
        }

        private void FindDefaultGrid()
        {
            if (_targetGrid == null)
            {
                var activeGo = Selection.activeGameObject;
                if (activeGo != null)
                {
                    _targetGrid = activeGo.GetComponentInParent<Grid>();
                }
                if (_targetGrid == null)
                {
                    _targetGrid = FindAnyObjectByType<Grid>();
                }
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            DrawHeaderCard();
            EditorGUILayout.Space(8);

            DrawTargetSelection();
            EditorGUILayout.Space(8);

            DrawConfigCard();
            EditorGUILayout.Space(8);

            DrawValidationCard();
            EditorGUILayout.Space(12);

            DrawExportButton();
        }

        private void DrawHeaderCard()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🏛️ XUẤT BẢN MAP TILEMAP THÀNH PHÒNG GAME", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Đóng gói toàn bộ Tilemap vừa vẽ thành Prefab và tự nạp vào Dungeon Run.", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawTargetSelection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("1. Chọn Grid Bản Đồ Cần Xuất Bản:", EditorStyles.boldLabel);
            _targetGrid = (Grid)EditorGUILayout.ObjectField("Grid Đối Tượng:", _targetGrid, typeof(Grid), true);

            if (_targetGrid == null)
            {
                EditorGUILayout.HelpBox("Chưa chọn Grid nào trong Scene! Hãy bấm nút bên dưới để tạo hoặc tìm Grid.", MessageType.Warning);
                if (GUILayout.Button("🔍 Tự Động Tìm Grid Trong Scene"))
                {
                    FindDefaultGrid();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawConfigCard()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("2. Cấu Hình Định Danh Phòng:", EditorStyles.boldLabel);

            _roomName = EditorGUILayout.TextField("Tên Phòng:", _roomName);
            _roomType = (RunNodeType)EditorGUILayout.EnumPopup("Loại Phòng Ải:", _roomType);
            _weight = EditorGUILayout.IntSlider("Tỷ Lệ Xuất Hiện (Weight):", _weight, 1, 50);
            _description = EditorGUILayout.TextField("Mô Tả:", _description);

            EditorGUILayout.EndVertical();
        }

        private void DrawValidationCard()
        {
            if (_targetGrid == null) return;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("3. Kiểm Tra Cấu Trúc Bản Đồ:", EditorStyles.boldLabel);

            // Kiểm tra Player Spawn
            Transform playerSpawn = null;
            foreach (Transform t in _targetGrid.GetComponentsInChildren<Transform>(true))
            {
                if (t != _targetGrid.transform && t.name.IndexOf("PlayerSpawn", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    playerSpawn = t;
                    break;
                }
            }

            if (playerSpawn != null)
            {
                GUI.color = new Color(0.3f, 1f, 0.4f);
                EditorGUILayout.LabelField($"  ✓ Player Spawn: Đã có tại tọa độ {playerSpawn.localPosition}");
            }
            else
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                EditorGUILayout.LabelField("  ✗ CẢNH BÁO: Chưa có Player Spawn (Hãy chọn cọ 🟢 trên Palette để chấm)!");
            }
            GUI.color = Color.white;

            // Kiểm tra Enemy Spawns
            int enemyCount = 0;
            foreach (Transform t in _targetGrid.GetComponentsInChildren<Transform>(true))
            {
                if (t != _targetGrid.transform && t != playerSpawn && t.name.IndexOf("EnemySpawn", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    enemyCount++;
                }
            }

            bool isCombat = _roomType == RunNodeType.NormalBattle || _roomType == RunNodeType.EliteBattle ||
                            _roomType == RunNodeType.MiniBoss || _roomType == RunNodeType.BossBattle;

            if (isCombat)
            {
                if (enemyCount > 0)
                {
                    GUI.color = new Color(0.3f, 1f, 0.4f);
                    EditorGUILayout.LabelField($"  ✓ Quái Spawn: Đã chấm {enemyCount} điểm chiến thuật.");
                }
                else
                {
                    GUI.color = new Color(1f, 0.6f, 0.2f);
                    EditorGUILayout.LabelField("  ⚠ CHÚ Ý: Chưa có quái spawn (Dùng cọ 🔴 trên Palette để chấm)!");
                }
            }
            else
            {
                GUI.color = Color.gray;
                EditorGUILayout.LabelField("  - Phòng Phi Chiến Đấu (Không bắt buộc quái spawn)");
            }
            GUI.color = Color.white;

            // Kiểm tra Portals
            var portals = _targetGrid.GetComponentsInChildren<RoomPortal>(true);
            int portalCount = portals != null ? portals.Length : 0;
            if (portalCount > 0)
            {
                GUI.color = new Color(0.3f, 1f, 0.4f);
                EditorGUILayout.LabelField($"  ✓ Cổng Dịch Chuyển: Có {portalCount} Cổng sẵn sàng.");
            }
            else
            {
                GUI.color = new Color(1f, 0.3f, 0.3f);
                EditorGUILayout.LabelField("  ✗ CẢNH BÁO: Chưa có Cổng (Dùng cọ 🔵 trên Palette để chấm)!");
            }
            GUI.color = Color.white;

            // Kiểm tra Tường Collider
            var colliders = _targetGrid.GetComponentsInChildren<Collider2D>();
            int nonTriggerCols = 0;
            foreach (var col in colliders)
            {
                if (!col.isTrigger) nonTriggerCols++;
            }

            if (nonTriggerCols > 0)
            {
                GUI.color = new Color(0.3f, 1f, 0.4f);
                EditorGUILayout.LabelField($"  ✓ Collider Tường: Đã có {nonTriggerCols} Collider cản đường.");
            }
            else
            {
                GUI.color = new Color(1f, 0.6f, 0.2f);
                EditorGUILayout.LabelField("  ⚠ Chú ý: Tilemap Tường cần có TilemapCollider2D để cản quái/player!");
            }
            GUI.color = Color.white;

            EditorGUILayout.EndVertical();
        }

        private void DrawExportButton()
        {
            if (_targetGrid == null) return;

            GUI.backgroundColor = new Color(0.2f, 0.9f, 0.4f);
            if (GUILayout.Button("💾 XUẤT BẢN THÀNH PREFAB & ĐĂNG KÝ DATABASE", GUILayout.Height(42)))
            {
                ExportGridToRoomPrefab();
            }
            GUI.backgroundColor = Color.white;
        }

        private void ExportGridToRoomPrefab()
        {
            if (_targetGrid == null)
            {
                EditorUtility.DisplayDialog("Lỗi", "Vui lòng chọn Grid bản đồ cần xuất bản!", "OK");
                return;
            }

            // Đảm bảo tên phòng hợp lệ
            string cleanName = string.IsNullOrWhiteSpace(_roomName) ? _targetGrid.gameObject.name : _roomName;
            cleanName = cleanName.Replace(" ", "_").Replace("[ActiveRoom]_", "").Replace("[CustomRoom]_", "");

            // 1. Đảm bảo Grid có component RoomLayout
            var layout = _targetGrid.GetComponent<RoomLayout>();
            if (layout == null)
            {
                layout = _targetGrid.gameObject.AddComponent<RoomLayout>();
            }

            // Đảm bảo Tilemap_Walls có Layer Obstacle và Collider
            foreach (var tm in _targetGrid.GetComponentsInChildren<Tilemap>(true))
            {
                if (tm.name.IndexOf("Wall", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    tm.gameObject.layer = DungeonTilePaletteTools.GetObstacleLayer();
                    if (tm.GetComponent<TilemapCollider2D>() == null)
                    {
                        tm.gameObject.AddComponent<TilemapCollider2D>();
                    }
                }
            }

            layout.SetIdentity(cleanName, _roomType, Vector2.zero);
            layout.AutoBindReferences();
            var size = layout.CalculateBoundsFromChildren();

            // Kiểm tra Player Spawn
            if (layout.PlayerSpawnPoint == null)
            {
                bool proceed = EditorUtility.DisplayDialog("Thiếu Player Spawn Point",
                    "Bản đồ này chưa có điểm xuất hiện của Player!\nBạn có muốn tiếp tục xuất bản không?", "Vẫn Xuất Bản", "Hủy");
                if (!proceed) return;
            }

            // 2. Thư mục xuất bản
            string prefabDir = "Assets/_Project/Prefabs/Dungeon/Rooms";
            string soDir = "Assets/_Project/ScriptableObjects/Dungeon/Rooms";
            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
            if (!Directory.Exists(soDir)) Directory.CreateDirectory(soDir);

            _targetGrid.gameObject.name = cleanName;
            string prefabPath = $"{prefabDir}/{cleanName}.prefab";

            bool success;
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(_targetGrid.gameObject, prefabPath, InteractionMode.UserAction, out success);

            if (!success || prefabAsset == null)
            {
                EditorUtility.DisplayDialog("Lỗi Xuất Bản", $"Không thể lưu Prefab tại:\n{prefabPath}", "OK");
                return;
            }

            // 3. Tạo / Cập nhật RoomVariationSO
            string soPath = $"{soDir}/{cleanName}_Data.asset";
            var variationSO = AssetDatabase.LoadAssetAtPath<RoomVariationSO>(soPath);
            if (variationSO == null)
            {
                variationSO = CreateInstance<RoomVariationSO>();
                AssetDatabase.CreateAsset(variationSO, soPath);
            }

            variationSO.Initialize(cleanName, _roomType, prefabAsset, _weight, _description);
            EditorUtility.SetDirty(variationSO);

            // 4. Đăng ký vào RoomDatabaseSO
            string dbPath = "Assets/_Project/ScriptableObjects/Dungeon/RoomDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<RoomDatabaseSO>(dbPath);
            if (db == null)
            {
                db = CreateInstance<RoomDatabaseSO>();
                AssetDatabase.CreateAsset(db, dbPath);
            }

            db.RegisterVariation(variationSO);
            EditorUtility.SetDirty(db);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green><b>[DUNGEON LEVEL DESIGN]</b></color> Xuất bản thành công phòng '{cleanName}' ({size.x}x{size.y}m) vào Prefab & Database!");

            EditorUtility.DisplayDialog("Xuất Bản Thành Công! 🎉",
                $"Phòng '{cleanName}' của bạn đã được đóng gói hoàn chỉnh:\n\n" +
                $"• Prefab: {prefabPath}\n" +
                $"• ScriptableObject: {soPath}\n" +
                $"• Kích thước tự đo: {size.x} x {size.y} mét\n" +
                $"• Đã tự động nạp vào RoomDatabase.asset!\n\n" +
                $"Bây giờ bạn có thể xóa Grid tạm này trong Scene và bấm Play để thưởng thức màn chơi mới!", "Tuyệt Vời!");

            EditorGUIUtility.PingObject(prefabAsset);
        }
    }
}
#endif

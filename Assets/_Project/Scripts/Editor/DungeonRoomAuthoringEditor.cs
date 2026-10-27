#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Project_D.Dungeon.Room;
using Project_D.Dungeon.Run;

namespace Project_D.EditorTools
{
    [CustomEditor(typeof(DungeonRoomAuthoring))]
    public class DungeonRoomAuthoringEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var authoring = (DungeonRoomAuthoring)target;

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("⚡ DUNGEON ROOM DESIGNER & EXPORTER", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Vẽ Sàn, Tường và chấm Player/Quái/Cổng trực tiếp từ Tile Palette. Sau đó chỉ cần bấm nút bên dưới để Tự Động Kết Nối & Xuất Bản!", MessageType.Info);
            EditorGUILayout.Space(6);

            // 1. Live Validation
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("📊 Kiểm Tra Các Thành Phần Trong Bản Đồ:", EditorStyles.boldLabel);

            var player = authoring.GetPlayerSpawn();
            if (player != null)
            {
                GUI.color = new Color(0.3f, 1f, 0.4f);
                EditorGUILayout.LabelField($"  ✓ Player Spawn: Đã có tại tọa độ ({player.position.x:F1}, {player.position.y:F1})");
            }
            else
            {
                GUI.color = new Color(1f, 0.35f, 0.35f);
                EditorGUILayout.LabelField("  ✗ Chưa có Player Spawn (Hãy chọn cọ 🟢 Player trên Palette vẽ lên sàn)!");
            }
            GUI.color = Color.white;

            int enemies = authoring.GetEnemySpawnCount();
            if (enemies > 0)
            {
                GUI.color = new Color(0.3f, 1f, 0.4f);
                EditorGUILayout.LabelField($"  ✓ Quái Spawn: Đã có {enemies} điểm chiến thuật.");
            }
            else
            {
                GUI.color = new Color(1f, 0.6f, 0.2f);
                EditorGUILayout.LabelField("  ⚠ Chưa có Quái (Hãy chọn cọ 🔴 Quái trên Palette vẽ lên sàn)!");
            }
            GUI.color = Color.white;

            int portals = authoring.GetPortalCount();
            if (portals > 0)
            {
                GUI.color = new Color(0.3f, 1f, 0.4f);
                EditorGUILayout.LabelField($"  ✓ Cổng Dịch Chuyển: Đã có {portals} cổng.");
            }
            else
            {
                GUI.color = new Color(1f, 0.35f, 0.35f);
                EditorGUILayout.LabelField("  ✗ Chưa có Cổng (Hãy chọn cọ 🔵 Cổng trên Palette vẽ lên sàn)!");
            }
            GUI.color = Color.white;

            int interactions = authoring.GetInteractionPointCount();
            if (interactions > 0)
            {
                GUI.color = new Color(1f, 0.85f, 0.2f);
                EditorGUILayout.LabelField($"  ✓ Điểm Tương Tác (Buff/Shop/Campfire): Đã có {interactions} điểm.");
            }
            else
            {
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                EditorGUILayout.LabelField("  ℹ Chưa có Điểm Tương Tác (Tùy chọn: Altar, Lửa trại, Rương Shop).");
            }
            GUI.color = Color.white;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6);

            // 1.5. Quick Paint Layer Switcher
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🖌️ Điều Hướng Vẽ Tilemap Nhanh:", EditorStyles.boldLabel);
            GUI.backgroundColor = new Color(0.8f, 0.95f, 1f);
            if (GUILayout.Button("🎨 Mở Cửa Sổ Tile Palette", GUILayout.Height(30)))
            {
                EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(2);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("🟢 Chọn Sàn (Floor)", GUILayout.Height(28)))
            {
                DungeonTilePaletteTools.FocusLayerInEditor(authoring.gameObject, "Tilemap_Floor");
            }
            GUI.backgroundColor = new Color(1f, 0.75f, 0.5f);
            if (GUILayout.Button("🧱 Chọn Tường (Walls)", GUILayout.Height(28)))
            {
                DungeonTilePaletteTools.FocusLayerInEditor(authoring.gameObject, "Tilemap_Walls");
            }
            GUI.backgroundColor = new Color(1f, 0.9f, 0.4f);
            if (GUILayout.Button("⭐ Chọn Entities", GUILayout.Height(28)))
            {
                DungeonTilePaletteTools.FocusLayerInEditor(authoring.gameObject, "Tilemap_Entities");
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6);

            // 2. Identity Config
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("⚙️ Cấu Hình Thông Tin Phòng:", EditorStyles.boldLabel);
            authoring.roomName = EditorGUILayout.TextField("Tên Phòng:", authoring.roomName);
            authoring.roomType = (RunNodeType)EditorGUILayout.EnumPopup("Loại Phòng Ải:", authoring.roomType);
            authoring.weight = EditorGUILayout.IntSlider("Tỷ Lệ Xuất Hiện (Weight):", authoring.weight, 1, 50);
            authoring.description = EditorGUILayout.TextField("Mô Tả:", authoring.description);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);

            // 3. One-Click Export Button
            GUI.backgroundColor = new Color(0.2f, 0.9f, 0.4f);
            if (GUILayout.Button("⚡ 1-CLICK: TỰ ĐỘNG KẾT NỐI & XUẤT BẢN THÀNH PHÒNG CHƠI", GUILayout.Height(46)))
            {
                ExecuteOneClickExport(authoring);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(4);
        }

        private void ExecuteOneClickExport(DungeonRoomAuthoring authoring)
        {
            var gridGo = authoring.gameObject;

            // Đảm bảo tên phòng hợp lệ
            string cleanName = string.IsNullOrWhiteSpace(authoring.roomName) ? gridGo.name : authoring.roomName;
            cleanName = cleanName.Replace(" ", "_").Replace("[ActiveRoom]_", "").Replace("[CustomRoom]_", "");

            // 1. Gắn / Cập nhật RoomLayout
            var layout = gridGo.GetComponent<RoomLayout>();
            if (layout == null)
            {
                layout = gridGo.AddComponent<RoomLayout>();
            }

            // Đảm bảo Tilemap_Walls có Obstacle Layer và Collider
            foreach (var tm in gridGo.GetComponentsInChildren<Tilemap>(true))
            {
                if (tm.name.IndexOf("Wall", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    tm.gameObject.layer = DungeonTilePaletteTools.GetObstacleLayer();
                    if (tm.GetComponent<TilemapCollider2D>() == null)
                    {
                        tm.gameObject.AddComponent<TilemapCollider2D>();
                    }
                }
            }

            layout.SetIdentity(cleanName, authoring.roomType, Vector2.zero);
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

            gridGo.name = cleanName;
            string prefabPath = $"{prefabDir}/{cleanName}.prefab";

            bool success;
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(gridGo, prefabPath, InteractionMode.UserAction, out success);

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
                variationSO = ScriptableObject.CreateInstance<RoomVariationSO>();
                AssetDatabase.CreateAsset(variationSO, soPath);
            }

            variationSO.Initialize(cleanName, authoring.roomType, prefabAsset, authoring.weight, authoring.description);
            EditorUtility.SetDirty(variationSO);

            // 4. Đăng ký vào RoomDatabaseSO
            string dbPath = "Assets/_Project/ScriptableObjects/Dungeon/RoomDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<RoomDatabaseSO>(dbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<RoomDatabaseSO>();
                AssetDatabase.CreateAsset(db, dbPath);
            }

            db.RegisterVariation(variationSO);
            EditorUtility.SetDirty(db);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green><b>[DUNGEON LEVEL DESIGN]</b></color> Đã tự động kết nối và xuất bản phòng '{cleanName}' ({size.x:F1}x{size.y:F1}m) vào Database!");

            EditorUtility.DisplayDialog("🎉 Xuất Bản Thành Công!",
                $"Phòng '{cleanName}' đã được TỰ ĐỘNG KẾT NỐI & NẠP VÀO GAME:\n\n" +
                $"• Tự động gắn và nối RoomLayout thành công!\n" +
                $"• Prefab: {prefabPath}\n" +
                $"• Kích thước phòng: {size.x:F1} x {size.y:F1} mét\n" +
                $"• Đã tự động đăng ký vào RoomDatabase.asset!\n\n" +
                $"Bây giờ bạn có thể XÓA đối tượng '{cleanName}' tạm này trên Scene và bấm nút PLAY ▶️ để tận hưởng phòng mới!", "Tuyệt Vời!");

            EditorGUIUtility.PingObject(prefabAsset);
        }
    }
}
#endif

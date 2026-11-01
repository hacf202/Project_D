#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Project_D.Dungeon.Run;
using Project_D.Dungeon.Room;

namespace Project_D.EditorTools
{
    [InitializeOnLoad]
    public static class RunMapSceneGraphEditor
    {
        // Settings
        public static bool ShowSceneGraph = false;
        public static Vector3 GraphOrigin = new Vector3(50f, 0f, 0f);
        public static float FloorSpacingY = 5.5f;
        public static float BranchSpacingX = 5.0f;

        // Trạng thái Node đang chọn trong Scene
        public static int SelectedNodeId = -1;

        // Trạng thái Phòng đang được mở ra Scene để vẽ
        private static GameObject _activeEditingInstance;
        private static GameObject _activeEditingPrefab;
        private static string _activeRoomName = "";

        static RunMapSceneGraphEditor()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            // 1. Nếu đang có phòng mở ra Scene để chỉnh sửa -> Hiện thanh công cụ lưu ở đỉnh Scene View
            if (_activeEditingInstance != null)
            {
                DrawActiveRoomEditorTopBar(sceneView);
            }

            // 2. Nếu tính năng hiển thị đồ thị trong Scene View đang bật
            if (ShowSceneGraph)
            {
                DrawSceneGraph(sceneView);
            }
        }

        // =========================================================================
        // 1. THANH CÔNG CỤ ĐỈNH KHI ĐANG VẼ PHÒNG TRONG SCENE
        // =========================================================================

        private static void DrawActiveRoomEditorTopBar(SceneView sceneView)
        {
            Handles.BeginGUI();

            float barWidth = 460f;
            float barHeight = 44f;
            Rect rect = new Rect((sceneView.position.width - barWidth) * 0.5f, 12, barWidth, barHeight);

            GUILayout.BeginArea(rect, GUI.skin.box);
            EditorGUILayout.BeginHorizontal();

            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(0.2f, 0.9f, 1f) },
                alignment = TextAnchor.MiddleLeft
            };

            EditorGUILayout.LabelField($"🏛️ ĐANG SỬA: {_activeRoomName}", labelStyle, GUILayout.Width(200), GUILayout.Height(32));

            GUI.backgroundColor = new Color(0.2f, 0.95f, 0.5f);
            if (GUILayout.Button("💾 Lưu Vào Prefab", GUILayout.Width(130), GUILayout.Height(30)))
            {
                SaveActiveEditingRoom();
            }

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("✕ Đóng", GUILayout.Width(75), GUILayout.Height(30)))
            {
                CloseActiveEditingRoom();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();

            Handles.EndGUI();
        }

        // =========================================================================
        // 2. VẼ ĐỒ THỊ 13 TẦNG TRỰC QUAN TRONG SCENE VIEW
        // =========================================================================

        private static void DrawSceneGraph(SceneView sceneView)
        {
            var mapConfig = GetActiveMapConfig();
            if (mapConfig == null || mapConfig.AllNodes == null || mapConfig.AllNodes.Count == 0) return;

            Event e = Event.current;
            int totalFloors = mapConfig.TotalFloors;

            // Tính toán vị trí không gian của từng Node
            var nodePositions = CalculateNodePositions(mapConfig);

            // 2.1 Vẽ đường nối mũi tên giữa các tầng (Linked List Connections)
            foreach (var node in mapConfig.AllNodes)
            {
                if (!nodePositions.ContainsKey(node.Id)) continue;
                Vector3 startPos = nodePositions[node.Id];

                foreach (int nextId in node.NextNodeIds)
                {
                    if (nodePositions.ContainsKey(nextId))
                    {
                        Vector3 endPos = nodePositions[nextId];
                        DrawSplineArrow(startPos, endPos, node.NodeColor);
                    }
                }
            }

            // 2.2 Vẽ các Node tròn và nhãn thông tin trong Scene View
            Handles.BeginGUI();
            GUIStyle nodeTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            GUIStyle subStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.85f, 0.95f, 1f) }
            };

            foreach (var node in mapConfig.AllNodes)
            {
                if (!nodePositions.ContainsKey(node.Id)) continue;
                Vector3 worldPos = nodePositions[node.Id];
                Vector2 guiPos = HandleUtility.WorldToGUIPoint(worldPos);

                bool isSelected = (node.Id == SelectedNodeId);

                // Kích thước card tròn trong GUI
                float size = isSelected ? 48f : 42f;
                Rect nodeRect = new Rect(guiPos.x - size * 0.5f, guiPos.y - size * 0.5f, size, size);

                // Xử lý sự kiện click vào Node trong Scene
                if (e.type == EventType.MouseDown && e.button == 0 && nodeRect.Contains(e.mousePosition))
                {
                    SelectedNodeId = node.Id;
                    e.Use();
                    Selection.activeObject = mapConfig;
                    sceneView.Repaint();
                }

                // Vẽ nền Node
                Color themeColor = node.NodeColor;
                GUI.color = themeColor;
                GUI.Box(nodeRect, GUIContent.none, "Button");
                GUI.color = Color.white;

                // Nếu đang chọn thì vẽ viền sáng
                if (isSelected)
                {
                    Handles.color = Color.yellow;
                    Handles.DrawWireDisc(worldPos, Vector3.forward, 1.4f);
                }

                // Nhãn bên trong
                string icon = GetNodeTypeIcon(node.NodeType);
                GUI.Label(new Rect(guiPos.x - 25, guiPos.y - 18, 50, 20), $"{icon}", nodeTitleStyle);
                GUI.Label(new Rect(guiPos.x - 35, guiPos.y - 2, 70, 16), $"#{node.Id} ({node.BranchName})", subStyle);
            }

            // 2.3 Bảng thông tin nhỏ góc màn hình cho Node đang chọn
            if (SelectedNodeId != -1)
            {
                var selectedNode = mapConfig.GetNodeById(SelectedNodeId);
                if (selectedNode != null && nodePositions.ContainsKey(selectedNode.Id))
                {
                    DrawSelectedNodeFloatingCard(selectedNode, mapConfig, sceneView);
                }
            }

            Handles.EndGUI();
        }

        private static void DrawSelectedNodeFloatingCard(RunNodeData node, RunMapConfigSO mapConfig, SceneView sceneView)
        {
            Rect cardRect = new Rect(14, sceneView.position.height - 180, 360, 150);
            GUILayout.BeginArea(cardRect, GUI.skin.window);

            EditorGUILayout.BeginHorizontal();
            GUI.color = node.NodeColor;
            EditorGUILayout.LabelField($"Node #{node.Id}: {node.DisplayName}", EditorStyles.boldLabel);
            GUI.color = Color.white;

            if (GUILayout.Button("✕", GUILayout.Width(20), GUILayout.Height(18)))
            {
                SelectedNodeId = -1;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"• Tầng: {node.FloorNumber}  |  Nhánh: {node.BranchName}  |  Loại: {node.NodeType}", EditorStyles.miniLabel);

            string prefabName = node.CustomRoomPrefab != null ? node.CustomRoomPrefab.name : "(Tự bốc từ Database)";
            EditorGUILayout.LabelField($"• Prefab: {prefabName}", EditorStyles.miniLabel);

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();

            // Nút mở phòng ra Scene để vẽ
            GUI.backgroundColor = new Color(0.3f, 0.85f, 1f);
            if (GUILayout.Button("👁️ Mở Phòng Vẽ Bằng Tilemap", GUILayout.Height(26)))
            {
                OpenRoomForNode(node, mapConfig);
            }

            // Nút thêm nhánh vào tầng này
            GUI.backgroundColor = new Color(0.4f, 1f, 0.6f);
            if (GUILayout.Button("+ Nhánh", GUILayout.Width(70), GUILayout.Height(26)))
            {
                Undo.RecordObject(mapConfig, $"Add Branch to Floor {node.FloorNumber}");
                mapConfig.AddBranchToFloor(node.FloorNumber, RunNodeType.NormalBattle);
                EditorUtility.SetDirty(mapConfig);
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        // =========================================================================
        // 3. TIỆN ÍCH TÍNH TOÁN TỌA ĐỘ VÀ ĐƯỜNG VẼ
        // =========================================================================

        private static Dictionary<int, Vector3> CalculateNodePositions(RunMapConfigSO mapConfig)
        {
            var dict = new Dictionary<int, Vector3>();
            var grouped = new Dictionary<int, List<RunNodeData>>();

            foreach (var node in mapConfig.AllNodes)
            {
                if (!grouped.ContainsKey(node.FloorNumber)) grouped[node.FloorNumber] = new List<RunNodeData>();
                grouped[node.FloorNumber].Add(node);
            }

            for (int f = 1; f <= mapConfig.TotalFloors; f++)
            {
                if (!grouped.ContainsKey(f)) continue;
                var list = grouped[f];
                int count = list.Count;
                float startX = -(count - 1) * 0.5f * BranchSpacingX;

                for (int i = 0; i < count; i++)
                {
                    float x = GraphOrigin.x + startX + (i * BranchSpacingX);
                    float y = GraphOrigin.y + (f - 1) * FloorSpacingY;
                    dict[list[i].Id] = new Vector3(x, y, 0f);
                }
            }

            return dict;
        }

        private static void DrawSplineArrow(Vector3 start, Vector3 end, Color col)
        {
            Handles.color = new Color(col.r, col.g, col.b, 0.7f);

            Vector3 startTangent = start + Vector3.up * (FloorSpacingY * 0.4f);
            Vector3 endTangent = end - Vector3.up * (FloorSpacingY * 0.4f);

            Handles.DrawBezier(start, end, startTangent, endTangent, Handles.color, null, 3f);

            // Đầu mũi tên ở đích đến
            Vector3 dir = (end - endTangent).normalized;
            Vector3 right = Quaternion.Euler(0, 0, 90) * dir;
            Vector3 arrowTip = end - dir * 0.8f;
            Vector3 p1 = arrowTip - dir * 0.6f + right * 0.4f;
            Vector3 p2 = arrowTip - dir * 0.6f - right * 0.4f;

            Handles.DrawAAConvexPolygon(arrowTip, p1, p2);
            Handles.color = Color.white;
        }

        // =========================================================================
        // 4. MỞ THẲNG VÀO PREFAB MODE ĐỂ VẼ TILEMAP (KHÔNG TẠO RA SCENE)
        // =========================================================================

        public static void OpenRoomForNode(RunNodeData node, RunMapConfigSO config = null)
        {
            if (node == null) return;

            // 1. Nếu Node đã có Prefab riêng được gán từ trước -> Mở thẳng Prefab Mode
            if (node.CustomRoomPrefab != null)
            {
                OpenRoomPrefabInPrefabMode(node.CustomRoomPrefab);
                return;
            }

            // 2. Tra cứu Prefab mẫu đại diện từ RoomDatabase
            GameObject templatePrefab = null;
            string dbPath = "Assets/_Project/ScriptableObjects/Dungeon/RoomDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<RoomDatabaseSO>(dbPath);
            if (db != null)
            {
                var variation = db.GetRandomRoomForType(node.NodeType);
                if (variation != null && variation.RoomPrefab != null)
                {
                    templatePrefab = variation.RoomPrefab;
                }
            }
            if (templatePrefab == null)
            {
                templatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Dungeon/Rooms/Start_Room.prefab");
            }

            if (templatePrefab == null)
            {
                EditorUtility.DisplayDialog("Chưa Tìm Thấy Prefab",
                    $"Chưa có Prefab mẫu nào tương ứng với loại '{node.NodeType}'!\nHãy bấm nút '⚡ Tự Động Sinh & Đồng Bộ Phòng' trước.", "OK");
                return;
            }

            // 3. Hiển thị bảng hỏi lựa chọn (Phương Án 2)
            int choice = EditorUtility.DisplayDialogComplex(
                "Tùy Chọn Chỉnh Sửa Phòng",
                $"Phòng [#{node.Id}] ({node.DisplayName}) hiện CHƯA có Prefab riêng.\n(Đang mượn Prefab mẫu chung: '{templatePrefab.name}')\n\n" +
                "• 'Tạo Prefab Riêng & Vẽ' (Khuyên dùng):\n" +
                "   Tự động nhân bản thành Prefab riêng cho phòng này để vẽ tự do, KHÔNG ảnh hưởng các phòng khác.\n\n" +
                "• 'Sửa Prefab Mẫu Toàn Cục':\n" +
                "   Chỉnh sửa trực tiếp file mẫu gốc trong Database (CẢNH BÁO: Tất cả các phòng khác cùng loại trên toàn game sẽ bị thay đổi theo).",
                "Tạo Prefab Riêng & Vẽ",   // choice 0 (OK)
                "Hủy",                    // choice 1 (Cancel)
                "Sửa Prefab Mẫu Toàn Cục" // choice 2 (Alt)
            );

            if (choice == 0) // Tạo Prefab Riêng & Vẽ
            {
                var newCustomPrefab = CreateCustomRoomPrefabForNode(node, templatePrefab, config);
                if (newCustomPrefab != null)
                {
                    Debug.Log($"<color=green><b>[CUSTOM PREFAB]</b></color> Đã tạo Prefab riêng '{newCustomPrefab.name}' cho Phòng [#{node.Id}] và gán vào cấu hình bản đồ!");
                    OpenRoomPrefabInPrefabMode(newCustomPrefab);
                }
            }
            else if (choice == 2) // Sửa Prefab Mẫu Toàn Cục
            {
                Debug.LogWarning($"<color=yellow><b>[TEMPLATE MODE]</b></color> Bạn đang sửa trực tiếp Prefab mẫu toàn cục '{templatePrefab.name}'. Các phòng khác dùng chung mẫu này sẽ bị thay đổi theo!");
                OpenRoomPrefabInPrefabMode(templatePrefab);
            }
            // choice == 1 (Hủy) -> không làm gì cả
        }

        public static GameObject CreateCustomRoomPrefabForNode(RunNodeData node, GameObject basePrefab, RunMapConfigSO config = null)
        {
            if (node == null || basePrefab == null) return null;

            string folder = "Assets/_Project/Prefabs/Dungeon/Rooms/Custom";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Dungeon/Rooms"))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Dungeon"))
                    {
                        AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Dungeon");
                    }
                    AssetDatabase.CreateFolder("Assets/_Project/Prefabs/Dungeon", "Rooms");
                }
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs/Dungeon/Rooms", "Custom");
            }

            string branchSafe = string.IsNullOrEmpty(node.BranchName) ? $"Node{node.Id}" : node.BranchName.Replace("/", "_").Replace(" ", "_");
            string newPath = $"{folder}/Room_F{node.FloorNumber}_{branchSafe}.prefab";
            newPath = AssetDatabase.GenerateUniqueAssetPath(newPath);

            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(basePrefab), newPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GameObject uniquePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(newPath);
            if (uniquePrefab != null)
            {
                if (config != null)
                {
                    Undo.RecordObject(config, $"Assign Custom Prefab to Node {node.Id}");
                }
                node.SetCustomRoomPrefab(uniquePrefab);

                if (config != null)
                {
                    EditorUtility.SetDirty(config);
                    AssetDatabase.SaveAssets();
                }
                else
                {
                    var defaultConfig = AssetDatabase.LoadAssetAtPath<RunMapConfigSO>("Assets/_Project/ScriptableObjects/Dungeon/RunMap_13Floors.asset");
                    if (defaultConfig != null)
                    {
                        EditorUtility.SetDirty(defaultConfig);
                        AssetDatabase.SaveAssets();
                    }
                }
            }
            return uniquePrefab;
        }

        public static void OpenRoomPrefabInPrefabMode(GameObject prefab)
        {
            if (prefab == null) return;

            string assetPath = AssetDatabase.GetAssetPath(prefab);
            if (!string.IsNullOrEmpty(assetPath))
            {
                // Mở trực tiếp Prefab trong chế độ Prefab Mode độc lập của Unity
                UnityEditor.SceneManagement.PrefabStageUtility.OpenPrefab(assetPath);

                // Tự động mở cửa sổ Tile Palette để sẵn sàng vẽ
                EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");

                Debug.Log($"<color=green><b>[PREFAB MODE]</b></color> Đã mở trực tiếp Prefab '{prefab.name}' ({assetPath}) trong Prefab Mode để vẽ Tilemap!");
            }
        }

        public static void SaveActiveEditingRoom()
        {
            if (_activeEditingInstance == null || _activeEditingPrefab == null) return;

            string path = AssetDatabase.GetAssetPath(_activeEditingPrefab);
            if (!string.IsNullOrEmpty(path))
            {
                // Tự động kết nối references
                var layout = _activeEditingInstance.GetComponent<RoomLayout>();
                if (layout != null)
                {
                    layout.AutoBindReferences();
                    layout.CalculateBoundsFromChildren();
                }

                PrefabUtility.SaveAsPrefabAssetAndConnect(_activeEditingInstance, path, InteractionMode.UserAction);
                Debug.Log($"<color=green><b>[SCENE EDITOR]</b></color> Đã lưu toàn bộ thay đổi Tilemap/Spawns vào Prefab: {path} thành công!");

                EditorUtility.DisplayDialog("Lưu Thành Công! 🎉",
                    $"Đã đóng gói và cập nhật thành công các chỉnh sửa vừa vẽ vào:\n{path}", "OK");

                CloseActiveEditingRoom();
            }
        }

        public static void CloseActiveEditingRoom()
        {
            if (_activeEditingInstance != null)
            {
                UnityEngine.Object.DestroyImmediate(_activeEditingInstance);
                _activeEditingInstance = null;
                _activeEditingPrefab = null;
                _activeRoomName = "";
            }
        }

        public static void FocusOnGraph(SceneView sceneView = null)
        {
            var sv = sceneView ?? SceneView.lastActiveSceneView;
            if (sv != null)
            {
                sv.Frame(new Bounds(new Vector3(GraphOrigin.x, 35f, 0f), new Vector3(30f, 75f, 1f)), false);
            }
        }

        private static RunMapConfigSO GetActiveMapConfig()
        {
            // Tìm trong RunManager hiện tại hoặc asset
            var rm = UnityEngine.Object.FindAnyObjectByType<RunManager>();
            if (rm != null && rm.MapConfig != null) return rm.MapConfig;

            return AssetDatabase.LoadAssetAtPath<RunMapConfigSO>("Assets/_Project/ScriptableObjects/Dungeon/RunMap_13Floors.asset");
        }

        private static string GetNodeTypeIcon(RunNodeType type)
        {
            switch (type)
            {
                case RunNodeType.NormalBattle: return "⚔️";
                case RunNodeType.StarterReward: return "🎁";
                case RunNodeType.EliteBattle: return "💀";
                case RunNodeType.MiniBoss: return "👹";
                case RunNodeType.BigReward: return "⭐";
                case RunNodeType.RestSite: return "🏕️";
                case RunNodeType.Shop: return "💰";
                case RunNodeType.BuffReward: return "📜";
                case RunNodeType.BossBattle: return "👑";
                default: return "🚪";
            }
        }
    }
}
#endif

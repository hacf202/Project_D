#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Project_D.Dungeon.Run;

namespace Project_D.EditorTools
{
    [CustomEditor(typeof(RunMapConfigSO))]
    public class RunMapConfigSOEditor : Editor
    {
        private RunMapConfigSO _mapConfig;
        private SerializedProperty _totalFloorsProp;
        private SerializedProperty _allNodesProp;

        private int _selectedNodeId = 1;
        private bool _showAllCardsMode = false;
        private bool _showValidationBox = false;
        private string _validationReport = "";
        private bool _validationSuccess = true;

        private void OnEnable()
        {
            _mapConfig = (RunMapConfigSO)target;
            _totalFloorsProp = serializedObject.FindProperty("_totalFloors");
            _allNodesProp = serializedObject.FindProperty("_allNodes");

            // Đảm bảo node đang chọn hợp lệ
            if (_mapConfig.GetNodeById(_selectedNodeId) == null && _mapConfig.AllNodes.Count > 0)
            {
                _selectedNodeId = _mapConfig.AllNodes[0].Id;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Nút mở Cửa Sổ Node Graph Editor Kéo Thả Tự Do
            GUI.backgroundColor = new Color(0.2f, 0.85f, 1f);
            if (GUILayout.Button("🚀 MỞ CỬA SỔ NODE GRAPH KÉO THẢ TỰ DO (DUNGEON EDITOR)", GUILayout.Height(32)))
            {
                DungeonGraphEditorWindow.OpenWithConfig(_mapConfig, _selectedNodeId);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(4);

            DrawCompactHeader();
            EditorGUILayout.Space(6);

            DrawInteractiveFlowMap();
            EditorGUILayout.Space(8);

            if (_showAllCardsMode)
            {
                DrawAllCardsMode();
            }
            else
            {
                DrawFocusedNodeDetailCard();
            }

            EditorGUILayout.Space(10);
            DrawBottomToolbar();

            serializedObject.ApplyModifiedProperties();
        }

        // =========================================================================
        // 1. BANNER & THỐNG KÊ GỌN GÀNG (COMPACT HEADER)
        // =========================================================================

        private void DrawCompactHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.25f, 0.85f, 1f) }
            };
            EditorGUILayout.LabelField("🗺️ BẢN ĐỒ DUNGEON RUN", titleStyle, GUILayout.Width(175));

            // Nút tự động nối toàn bộ bản đồ
            GUI.backgroundColor = new Color(0.35f, 0.85f, 1f);
            if (GUILayout.Button("⚡ Nối Cổng Toàn Bản Đồ", EditorStyles.miniButton, GUILayout.Height(20)))
            {
                Undo.RecordObject(_mapConfig, "Auto Link All Floors");
                _mapConfig.AutoLinkAllFloors();
                EditorUtility.SetDirty(_mapConfig);
                serializedObject.Update();
            }

            // Nút kiểm tra
            GUI.backgroundColor = new Color(0.35f, 0.95f, 0.6f);
            if (GUILayout.Button("🔍 Kiểm Tra", EditorStyles.miniButton, GUILayout.Width(75), GUILayout.Height(20)))
            {
                ValidateGraphIntegrity();
            }

            // Nút menu tiện ích
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("⚙️ Tiện Ích ▼", EditorStyles.miniButton, GUILayout.Width(75), GUILayout.Height(20)))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("🧹 Dọn Dẹp Cổng Lỗi"), false, () =>
                {
                    Undo.RecordObject(_mapConfig, "Clean Dangling Connections");
                    int removed = _mapConfig.CleanDanglingConnections();
                    EditorUtility.SetDirty(_mapConfig);
                    serializedObject.Update();
                    EditorUtility.DisplayDialog("Dọn Cổng Lỗi", $"Đã quét và xóa thành công {removed} cổng trỏ sai!", "OK");
                });
                menu.AddItem(new GUIContent("🔄 Đặt Lại 13 Tầng Chuẩn (22 Nút)"), false, () =>
                {
                    if (EditorUtility.DisplayDialog("Xác nhận Khởi Tạo Lại",
                        "Bạn có chắc muốn đặt lại đồ thị chuẩn 13 Tầng (22 Nút) của game không?", "Đặt Lại", "Hủy"))
                    {
                        Undo.RecordObject(_mapConfig, "Reset Run Map 13 Floors");
                        _mapConfig.BuildDefault13FloorRunMap();
                        EditorUtility.SetDirty(_mapConfig);
                        _selectedNodeId = 1;
                        serializedObject.Update();
                    }
                });
                menu.ShowAsContext();
            }

            EditorGUILayout.EndHorizontal();

            // Thống kê 1 dòng ngắn gọn
            int totalNodes = _allNodesProp.arraySize;
            EditorGUILayout.BeginHorizontal();
            GUI.color = new Color(0.8f, 0.9f, 1f);
            EditorGUILayout.LabelField($"• Tổng số: <b>{_totalFloorsProp.intValue} Tầng</b> | <b>{totalNodes} Phòng</b>", GetRichLabelStyle());
            GUI.color = Color.white;

            // Toggle chuyển chế độ xem
            string modeText = _showAllCardsMode ? "📋 Chế độ: Cuộn" : "🎯 Chế độ: Tinh Gọn";
            if (GUILayout.Button(modeText, EditorStyles.miniButton, GUILayout.Width(130)))
            {
                _showAllCardsMode = !_showAllCardsMode;
            }
            EditorGUILayout.EndHorizontal();

            // Dòng thao tác nhanh Thêm / Bớt Tầng & Đặt Lại Mặc Định
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.85f, 0.95f, 1f);
            if (GUILayout.Button($"➕ Thêm Tầng (Tầng {_totalFloorsProp.intValue + 1})", EditorStyles.miniButton))
            {
                Undo.RecordObject(_mapConfig, "Add Floor At End");
                _mapConfig.AddFloorAtEnd();
                _mapConfig.AutoLayoutNodes();
                EditorUtility.SetDirty(_mapConfig);
                AssetDatabase.SaveAssets();
                serializedObject.Update();
            }

            using (new EditorGUI.DisabledScope(_totalFloorsProp.intValue <= 1))
            {
                GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
                if (GUILayout.Button($"➖ Bớt Tầng Cuối", EditorStyles.miniButton, GUILayout.Width(105)))
                {
                    if (EditorUtility.DisplayDialog("Xóa Tầng Cuối", $"Bạn có chắc muốn xóa Tầng {_totalFloorsProp.intValue} không?", "Xóa", "Hủy"))
                    {
                        Undo.RecordObject(_mapConfig, "Delete Last Floor");
                        _mapConfig.DeleteFloor(_totalFloorsProp.intValue);
                        _mapConfig.AutoLayoutNodes();
                        EditorUtility.SetDirty(_mapConfig);
                        AssetDatabase.SaveAssets();
                        serializedObject.Update();
                    }
                }
            }

            GUI.backgroundColor = new Color(1f, 0.45f, 0.4f);
            if (GUILayout.Button("🔄 Đặt Lại 13 Tầng Mặc Định", EditorStyles.miniButton, GUILayout.Width(165)))
            {
                if (EditorUtility.DisplayDialog("Đặt Lại Bản Đồ Mặc Định", "Bạn có chắc muốn đặt lại toàn bộ bản đồ về cấu trúc 13 Tầng chuẩn mặc định (22 Nút) không?", "Đặt Lại", "Hủy"))
                {
                    Undo.RecordObject(_mapConfig, "Reset Run Map 13 Floors");
                    _mapConfig.BuildDefault13FloorRunMap();
                    _mapConfig.AutoLayoutNodes();
                    EditorUtility.SetDirty(_mapConfig);
                    AssetDatabase.SaveAssets();
                    _selectedNodeId = 1;
                    serializedObject.Update();
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            if (_showValidationBox)
            {
                EditorGUILayout.Space(2);
                MessageType msgType = _validationSuccess ? MessageType.Info : MessageType.Warning;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(_validationReport, msgType);
                if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(22)))
                {
                    _showValidationBox = false;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        // =========================================================================
        // 2. SƠ ĐỒ CÂY TƯƠNG TÁC CHÍNH (INTERACTIVE FLOW MAP)
        // =========================================================================

        private void DrawInteractiveFlowMap()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🌐 SƠ ĐỒ CÂY HÀNH TRÌNH (Click để chọn phòng):", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            var nodesByFloor = GetNodesGroupedByFloor();

            for (int f = 1; f <= _totalFloorsProp.intValue; f++)
            {
                if (!nodesByFloor.ContainsKey(f) || nodesByFloor[f].Count == 0) continue;

                var floorList = nodesByFloor[f];
                EditorGUILayout.BeginHorizontal();

                // Nhãn Tầng gọn gàng
                GUI.color = GetFloorHeaderColor(f);
                EditorGUILayout.LabelField($"T.{f:00}:", EditorStyles.boldLabel, GUILayout.Width(42));
                GUI.color = Color.white;

                // Các Node trên tầng đó (Giữ 100% màu sắc cố định theo loại phòng, không đổi màu lung tung!)
                foreach (var (index, nodeProp) in floorList)
                {
                    int id = nodeProp.FindPropertyRelative("_id").intValue;
                    string branch = nodeProp.FindPropertyRelative("_branchName").stringValue;
                    var type = (RunNodeType)nodeProp.FindPropertyRelative("_nodeType").enumValueIndex;
                    var prefabProp = nodeProp.FindPropertyRelative("_customRoomPrefab");
                    bool hasCustomPrefab = prefabProp != null && prefabProp.objectReferenceValue != null;

                    bool isSelected = (id == _selectedNodeId);

                    string star = hasCustomPrefab ? "★" : "";
                    string icon = GetNodeTypeIcon(type);

                    // CHỈ PHÒNG ĐANG CHỌN MỚI HIGHLIGHT TRẮNG SÁNG
                    // CÁC PHÒNG KHÁC GIỮ NGUYÊN MÀU ĐẶC TRƯNG, KHÔNG ĐỔI MÀU LUNG TUNG!
                    string label;
                    Color btnColor;

                    if (isSelected)
                    {
                        label = $"▶ [#{id}] {icon} {branch}{star} ◀";
                        btnColor = Color.white; // Nổi bật phòng đang chọn
                    }
                    else
                    {
                        label = $"[#{id}] {icon} {branch}{star}";
                        btnColor = GetNodeColor(type); // Giữ nguyên màu chuẩn của loại phòng
                    }

                    GUI.backgroundColor = btnColor;
                    if (GUILayout.Button(label, "Button", GUILayout.Height(22)))
                    {
                        _selectedNodeId = id;
                        GUI.FocusControl(null);
                    }
                    GUI.backgroundColor = Color.white;
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        // =========================================================================
        // 3. BẢNG CHI TIẾT NÚT ĐANG CHỌN (FOCUSED DETAIL INSPECTOR)
        // =========================================================================

        private void DrawFocusedNodeDetailCard()
        {
            var selectedNode = _mapConfig.GetNodeById(_selectedNodeId);
            if (selectedNode == null)
            {
                EditorGUILayout.HelpBox("Click vào một phòng trên Sơ Đồ Cây ở trên để bắt đầu chỉnh sửa.", MessageType.Info);
                return;
            }

            // Tìm SerializedProperty tương ứng
            SerializedProperty selectedProp = null;
            int elementIndex = -1;
            for (int i = 0; i < _allNodesProp.arraySize; i++)
            {
                var n = _allNodesProp.GetArrayElementAtIndex(i);
                if (n.FindPropertyRelative("_id").intValue == _selectedNodeId)
                {
                    selectedProp = n;
                    elementIndex = i;
                    break;
                }
            }

            if (selectedProp == null) return;

            var idProp = selectedProp.FindPropertyRelative("_id");
            var floorProp = selectedProp.FindPropertyRelative("_floorNumber");
            var branchProp = selectedProp.FindPropertyRelative("_branchName");
            var typeProp = selectedProp.FindPropertyRelative("_nodeType");
            var displayProp = selectedProp.FindPropertyRelative("_displayName");
            var colorProp = selectedProp.FindPropertyRelative("_nodeColor");
            var nextIdsProp = selectedProp.FindPropertyRelative("_nextNodeIds");
            var customPrefabProp = selectedProp.FindPropertyRelative("_customRoomPrefab");
            var posProp = selectedProp.FindPropertyRelative("_editorPosition");

            var nodeType = (RunNodeType)typeProp.enumValueIndex;
            string typeIcon = GetNodeTypeIcon(nodeType);
            Color themeCol = GetNodeColor(nodeType);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 1. TIÊU ĐỀ THẺ GỌN GÀNG
            EditorGUILayout.BeginHorizontal();
            GUI.color = themeCol;
            EditorGUILayout.LabelField($"⚙️ PHÒNG [#{idProp.intValue}] - TẦNG {floorProp.intValue}: {displayProp.stringValue}", EditorStyles.boldLabel);
            GUI.color = Color.white;

            using (new EditorGUI.DisabledScope(_allNodesProp.arraySize <= 1))
            {
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("✕ Xóa Phòng", EditorStyles.miniButton, GUILayout.Width(85), GUILayout.Height(18)))
                {
                    if (EditorUtility.DisplayDialog("Xóa Phòng", $"Bạn có chắc muốn xóa Node #{idProp.intValue} không?", "Xóa", "Hủy"))
                    {
                        _allNodesProp.DeleteArrayElementAtIndex(elementIndex);
                        serializedObject.ApplyModifiedProperties();
                        if (_mapConfig.AllNodes.Count > 0) _selectedNodeId = _mapConfig.AllNodes[0].Id;
                        return;
                    }
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            // 2. THÔNG TIN CƠ BẢN (1 DÒNG GỌN)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Loại:", GUILayout.Width(35));
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(typeProp, GUIContent.none, GUILayout.Width(130));
            if (EditorGUI.EndChangeCheck())
            {
                var newType = (RunNodeType)typeProp.enumValueIndex;
                colorProp.colorValue = GetNodeColor(newType);
                displayProp.stringValue = $"Tầng {floorProp.intValue}: {GetNodeDefaultTitle(newType)}";
            }

            EditorGUILayout.LabelField("Nhánh:", GUILayout.Width(50));
            EditorGUILayout.PropertyField(branchProp, GUIContent.none, GUILayout.Width(55));

            EditorGUILayout.LabelField("Tên:", GUILayout.Width(35));
            EditorGUILayout.PropertyField(displayProp, GUIContent.none);
            EditorGUILayout.EndHorizontal();

            // 3. PREFAB PHÒNG
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Prefab Riêng:", GUILayout.Width(80));
            EditorGUILayout.PropertyField(customPrefabProp, GUIContent.none);

            GUI.backgroundColor = new Color(0.35f, 0.85f, 1f);
            if (GUILayout.Button("🖌️ Vẽ Tilemap", EditorStyles.miniButton, GUILayout.Width(100), GUILayout.Height(18)))
            {
                var nodeData = _mapConfig.GetNodeById(idProp.intValue);
                RunMapSceneGraphEditor.OpenRoomForNode(nodeData, _mapConfig);
                GUIUtility.ExitGUI();
                return;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // 3b. TỌA ĐỘ TỰ DO 2D (FREE POSITION)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Vị trí 2D:", GUILayout.Width(80));
            if (posProp != null)
            {
                EditorGUILayout.PropertyField(posProp, GUIContent.none);
            }
            if (GUILayout.Button("🔍 Mở Graph", EditorStyles.miniButton, GUILayout.Width(100), GUILayout.Height(18)))
            {
                DungeonGraphEditorWindow.OpenWithConfig(_mapConfig, idProp.intValue);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // 4. HỆ THỐNG CỔNG DỊCH CHUYỂN (TỐI GIẢN - HUMAN FRIENDLY)
            DrawSimplePortalSection(idProp.intValue, floorProp.intValue, nextIdsProp);

            EditorGUILayout.Space(4);

            // 5. THAO TÁC TẦNG (GỌN GÀNG)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Tầng này:", EditorStyles.miniBoldLabel, GUILayout.Width(60));

            GUI.backgroundColor = new Color(0.4f, 0.9f, 1f);
            if (GUILayout.Button($"+ Thêm Nhánh", EditorStyles.miniButton))
            {
                Undo.RecordObject(_mapConfig, $"Add Branch to Floor {floorProp.intValue}");
                var created = _mapConfig.AddBranchToFloor(floorProp.intValue, RunNodeType.NormalBattle);
                if (created != null) _selectedNodeId = created.Id;
                EditorUtility.SetDirty(_mapConfig);
                serializedObject.Update();
                return;
            }

            GUI.backgroundColor = new Color(0.5f, 1f, 0.6f);
            if (GUILayout.Button($"+ Chèn Tầng Sau", EditorStyles.miniButton))
            {
                Undo.RecordObject(_mapConfig, $"Insert Floor After {floorProp.intValue}");
                _mapConfig.InsertFloorAfter(floorProp.intValue);
                EditorUtility.SetDirty(_mapConfig);
                serializedObject.Update();
                return;
            }

            GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
            if (GUILayout.Button($"🗑️ Xóa Tầng {floorProp.intValue}", EditorStyles.miniButton, GUILayout.Width(85)))
            {
                if (EditorUtility.DisplayDialog("Xóa Tầng",
                    $"Bạn có chắc muốn xóa Tầng {floorProp.intValue} không?\n(Mạch liên kết sẽ tự động nối tắt qua tầng sau)", "Xóa", "Hủy"))
                {
                    Undo.RecordObject(_mapConfig, $"Delete Floor {floorProp.intValue}");
                    _mapConfig.DeleteFloor(floorProp.intValue);
                    EditorUtility.SetDirty(_mapConfig);
                    if (_mapConfig.AllNodes.Count > 0) _selectedNodeId = _mapConfig.AllNodes[0].Id;
                    serializedObject.Update();
                    return;
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawSimplePortalSection(int currentId, int currentFloor, SerializedProperty nextIdsProp)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            bool isFinalFloor = (currentFloor >= _totalFloorsProp.intValue);

            // A. CỔNG DẪN VÀO (INCOMING) - RẤT GỌN GÀNG
            if (currentFloor > 1)
            {
                var incoming = _mapConfig.GetIncomingNodes(currentId);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("⬅ Cổng dẫn vào:", EditorStyles.miniBoldLabel, GUILayout.Width(100));
                if (incoming.Count > 0)
                {
                    foreach (var inc in incoming)
                    {
                        if (GUILayout.Button($"[#{inc.Id}] {inc.BranchName}", EditorStyles.miniButton, GUILayout.Height(18)))
                        {
                            _selectedNodeId = inc.Id;
                        }
                    }
                }
                else
                {
                    GUI.color = new Color(1f, 0.5f, 0.2f);
                    EditorGUILayout.LabelField("⚠️ Chưa có phòng tầng trước nào nối vào phòng này!", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }

            // B. CỔNG SANG TẦNG TIẾP THEO (OUTGOING) - BẢNG TOGGLE ĐƠN GIẢN
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🚪 Cổng Sang Tầng Sau (Tối đa 3 Cổng):", EditorStyles.boldLabel);

            if (!isFinalFloor)
            {
                // Nút tự động nối riêng tầng này
                GUI.backgroundColor = new Color(0.4f, 0.9f, 1f);
                if (GUILayout.Button("⚡ Nối Tự Động", EditorStyles.miniButton, GUILayout.Width(90), GUILayout.Height(18)))
                {
                    Undo.RecordObject(_mapConfig, $"Auto Link Floor {currentFloor}");
                    _mapConfig.AutoLinkFloor(currentFloor);
                    EditorUtility.SetDirty(_mapConfig);
                    serializedObject.Update();
                    return;
                }

                GUI.backgroundColor = new Color(0.9f, 0.95f, 1f);
                if (GUILayout.Button("+ Nối Khác...", EditorStyles.miniButton, GUILayout.Width(80), GUILayout.Height(18)))
                {
                    ShowAnyNodeSelectionMenu(currentId);
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            if (isFinalFloor)
            {
                GUI.color = new Color(1f, 0.85f, 0.2f);
                EditorGUILayout.LabelField("👑 Đây là Phòng Boss Cuối - Hoàn Thành Run Game!", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }
            else
            {
                int nextFloor = currentFloor + 1;
                var candidates = _mapConfig.GetNodesForFloor(nextFloor);

                if (candidates.Count == 0)
                {
                    EditorGUILayout.LabelField($"   (Chưa có phòng nào ở Tầng {nextFloor})", EditorStyles.miniLabel);
                }
                else
                {
                    // Lấy danh sách ID đã nối hiện tại
                    var connectedList = new List<int>();
                    for (int i = 0; i < nextIdsProp.arraySize; i++)
                    {
                        connectedList.Add(nextIdsProp.GetArrayElementAtIndex(i).intValue);
                    }

                    // Hiển thị dạng Toggle Checklist siêu gọn gàng, thân thiện
                    foreach (var cand in candidates)
                    {
                        bool isConnected = connectedList.Contains(cand.Id);
                        int slotIdx = connectedList.IndexOf(cand.Id);
                        string portalBadge = isConnected ? $" ({RunMapConfigSO.GetPhysicalPortalName(slotIdx, connectedList.Count)})" : "";

                        EditorGUILayout.BeginHorizontal();

                        EditorGUI.BeginChangeCheck();
                        bool canToggleOn = isConnected || connectedList.Count < RunMapConfigSO.MaxPortalsPerRoom;
                        using (new EditorGUI.DisabledScope(!canToggleOn))
                        {
                            string typeIcon = GetNodeTypeIcon(cand.NodeType);
                            string toggleLabel = $"{typeIcon} {cand.DisplayName} [{cand.BranchName}]{portalBadge}";
                            bool newConnected = EditorGUILayout.ToggleLeft(toggleLabel, isConnected, EditorStyles.label);

                            if (EditorGUI.EndChangeCheck())
                            {
                                if (newConnected && !isConnected)
                                {
                                    Undo.RecordObject(_mapConfig, $"Connect Node {currentId} to {cand.Id}");
                                    _mapConfig.ConnectNodes(currentId, cand.Id);
                                    EditorUtility.SetDirty(_mapConfig);
                                    serializedObject.Update();
                                    return;
                                }
                                else if (!newConnected && isConnected)
                                {
                                    Undo.RecordObject(_mapConfig, $"Disconnect Node {currentId} to {cand.Id}");
                                    _mapConfig.DisconnectNodes(currentId, cand.Id);
                                    EditorUtility.SetDirty(_mapConfig);
                                    serializedObject.Update();
                                    return;
                                }
                            }
                        }

                        if (!isConnected && connectedList.Count >= RunMapConfigSO.MaxPortalsPerRoom)
                        {
                            GUI.color = Color.gray;
                            EditorGUILayout.LabelField($"(Đã đủ {RunMapConfigSO.MaxPortalsPerRoom} cổng)", EditorStyles.miniLabel, GUILayout.Width(80));
                            GUI.color = Color.white;
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    if (connectedList.Count == 0)
                    {
                        GUI.color = new Color(1f, 0.4f, 0.4f);
                        EditorGUILayout.LabelField("⚠️ Chưa tick chọn cổng nào! Người chơi sẽ bị kẹt lại phòng này.", EditorStyles.miniLabel);
                        GUI.color = Color.white;
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void ShowAnyNodeSelectionMenu(int fromId)
        {
            var fromNode = _mapConfig.GetNodeById(fromId);
            if (fromNode == null) return;

            if (fromNode.NextNodeIds != null && fromNode.NextNodeIds.Count >= RunMapConfigSO.MaxPortalsPerRoom)
            {
                EditorUtility.DisplayDialog("Đã Đạt Giới Hạn", $"Phòng [#{fromNode.Id}] đã nối tối đa {RunMapConfigSO.MaxPortalsPerRoom} phòng tiếp theo!", "OK");
                return;
            }

            GenericMenu menu = new GenericMenu();

            for (int f = 1; f <= _mapConfig.TotalFloors; f++)
            {
                var floorNodes = _mapConfig.GetNodesForFloor(f);
                foreach (var node in floorNodes)
                {
                    if (node.Id == fromId) continue;

                    int targetId = node.Id;
                    string label = $"Tầng {f:00}/[#{node.Id}] {node.DisplayName} ({node.BranchName})";

                    if (fromNode.NextNodeIds != null && fromNode.NextNodeIds.Contains(targetId))
                    {
                        menu.AddDisabledItem(new GUIContent(label + " [Đã nối]"));
                    }
                    else if (!_mapConfig.CanConnect(fromId, targetId, out string reason))
                    {
                        menu.AddDisabledItem(new GUIContent(label + $" [{reason}]"));
                    }
                    else
                    {
                        menu.AddItem(new GUIContent(label), false, () =>
                        {
                            Undo.RecordObject(_mapConfig, $"Connect Node {fromId} to {targetId}");
                            _mapConfig.ConnectNodes(fromId, targetId);
                            EditorUtility.SetDirty(_mapConfig);
                            serializedObject.Update();
                        });
                    }
                }
            }

            menu.ShowAsContext();
        }

        // =========================================================================
        // 4. CHẾ ĐỘ XEM TẤT CẢ (FALLBACK NẾU MUỐN CUỘN TOÀN BỘ)
        // =========================================================================

        private void DrawAllCardsMode()
        {
            EditorGUILayout.HelpBox("Đang ở Chế Độ Xem Đầy Đủ: Toàn bộ các phòng được liệt kê tuần tự bên dưới.", MessageType.None);
            for (int i = 0; i < _allNodesProp.arraySize; i++)
            {
                var nodeProp = _allNodesProp.GetArrayElementAtIndex(i);
                int id = nodeProp.FindPropertyRelative("_id").intValue;

                if (GUILayout.Button($"Chọn Node #{id} làm Node chính", EditorStyles.miniButton))
                {
                    _selectedNodeId = id;
                    _showAllCardsMode = false;
                }
            }
        }

        // =========================================================================
        // 5. BOTTOM TOOLBAR
        // =========================================================================

        private void DrawBottomToolbar()
        {
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.85f, 0.95f, 1f);
            string addFloorLabel = $"➕ Thêm Tầng Mới Ở Cuối (Tầng {_totalFloorsProp.intValue + 1})";
            if (GUILayout.Button(addFloorLabel, GUILayout.Height(30)))
            {
                Undo.RecordObject(_mapConfig, "Add Floor At End");
                _mapConfig.AddFloorAtEnd();
                EditorUtility.SetDirty(_mapConfig);
                serializedObject.Update();
            }

            GUI.backgroundColor = new Color(0.3f, 0.95f, 0.55f);
            if (GUILayout.Button("⚡ Tự Động Sinh & Đồng Bộ 24 Biến Thể Phòng", GUILayout.Height(30)))
            {
                DungeonRoomGenerator.GenerateAllRooms(true);
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        // =========================================================================
        // 6. GRAPH INTEGRITY VALIDATOR
        // =========================================================================

        private void ValidateGraphIntegrity()
        {
            var allIds = new HashSet<int>();
            int total = _allNodesProp.arraySize;
            List<string> issues = new List<string>();
            List<string> warnings = new List<string>();

            for (int i = 0; i < total; i++)
            {
                var n = _allNodesProp.GetArrayElementAtIndex(i);
                int id = n.FindPropertyRelative("_id").intValue;
                if (!allIds.Add(id))
                {
                    issues.Add($"• Trùng lặp ID #{id}!");
                }
            }

            for (int i = 0; i < total; i++)
            {
                var n = _allNodesProp.GetArrayElementAtIndex(i);
                int id = n.FindPropertyRelative("_id").intValue;
                int floor = n.FindPropertyRelative("_floorNumber").intValue;
                var nexts = n.FindPropertyRelative("_nextNodeIds");

                // 1. Kiểm tra ngõ cụt
                if (floor < _totalFloorsProp.intValue && nexts.arraySize == 0)
                {
                    issues.Add($"• [Ngõ Cụt] Node #{id} (Tầng {floor}) chưa có Cổng nối tiếp sang tầng sau!");
                }

                // 2. Kiểm tra vi phạm ràng buộc tối đa 3 phòng
                if (nexts.arraySize > RunMapConfigSO.MaxPortalsPerRoom)
                {
                    issues.Add($"• [Vi Phạm Giới Hạn Cổng] Node #{id} (Tầng {floor}) đang nối tới {nexts.arraySize} phòng (Vượt quá ràng buộc tối đa {RunMapConfigSO.MaxPortalsPerRoom} phòng)!");
                }

                // 3. Kiểm tra cổng dẫn vào (Unreachable dead node)
                if (floor > 1)
                {
                    var incoming = _mapConfig.GetIncomingNodes(id);
                    if (incoming.Count == 0)
                    {
                        issues.Add($"• [Không thể đến] Node #{id} (Tầng {floor}) chưa có phòng nào ở tầng trước dẫn cổng vào!");
                    }
                }

                // 4. Kiểm tra cổng trỏ sai (Dangling IDs / Self Loop)
                for (int j = 0; j < nexts.arraySize; j++)
                {
                    int targetId = nexts.GetArrayElementAtIndex(j).intValue;
                    if (!allIds.Contains(targetId))
                    {
                        issues.Add($"• [Cổng lỗi] Node #{id} trỏ đến Node #{targetId} không tồn tại trong bản đồ!");
                    }
                    else if (targetId == id)
                    {
                        issues.Add($"• [Vòng lặp] Node #{id} tự trỏ cổng vào chính nó!");
                    }
                }
            }

            _showValidationBox = true;
            if (issues.Count == 0 && warnings.Count == 0)
            {
                _validationSuccess = true;
                _validationReport = $"🎉 ĐỒ THỊ HOÀN HẢO! Toàn bộ {total} Nút liên thông 100%, không có ngõ cụt, không có phòng chết và số lượng cổng vật lý hoàn toàn hợp lệ.";
            }
            else
            {
                _validationSuccess = (issues.Count == 0);
                string report = "";
                if (issues.Count > 0)
                {
                    report += "❌ CÁC LỖI CẦN SỬA:\n" + string.Join("\n", issues);
                }
                if (warnings.Count > 0)
                {
                    if (report.Length > 0) report += "\n\n";
                    report += "⚠️ CẢNH BÁO CẦN LƯU Ý:\n" + string.Join("\n", warnings);
                }
                _validationReport = report;
            }
        }

        // =========================================================================
        // TIỆN ÍCH HỖ TRỢ
        // =========================================================================

        private Dictionary<int, List<(int index, SerializedProperty nodeProp)>> GetNodesGroupedByFloor()
        {
            var dict = new Dictionary<int, List<(int index, SerializedProperty nodeProp)>>();
            for (int i = 0; i < _allNodesProp.arraySize; i++)
            {
                var nodeProp = _allNodesProp.GetArrayElementAtIndex(i);
                int floor = nodeProp.FindPropertyRelative("_floorNumber").intValue;
                if (!dict.ContainsKey(floor)) dict[floor] = new List<(int, SerializedProperty)>();
                dict[floor].Add((i, nodeProp));
            }
            return dict;
        }

        private Color GetFloorHeaderColor(int floor)
        {
            if (floor == 13) return new Color(1f, 0.4f, 0.3f);
            if (floor == 7) return new Color(0.9f, 0.4f, 0.9f);
            if (floor == 12) return new Color(0.4f, 0.9f, 0.5f);
            return new Color(0.85f, 0.92f, 1f);
        }

        private Color GetNodeColor(RunNodeType type)
        {
            return RunMapConfigSO.GetDefaultColor(type);
        }

        private string GetNodeTypeIcon(RunNodeType type)
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

        private string GetNodeDefaultTitle(RunNodeType type)
        {
            return RunMapConfigSO.GetDefaultTitle(type);
        }

        private GUIStyle GetRichLabelStyle()
        {
            return new GUIStyle(EditorStyles.label) { richText = true };
        }
    }
}
#endif

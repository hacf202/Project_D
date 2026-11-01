#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Project_D.Dungeon.Run;
using Project_D.Dungeon.Room;

namespace Project_D.EditorTools
{
    public class DungeonGraphEditorWindow : EditorWindow
    {
        [SerializeField] private RunMapConfigSO _mapConfig;

        // Tọa độ & Trạng thái Canvas
        private Vector2 _panOffset = new Vector2(80, 100);
        private float _zoom = 0.85f;
        private const float MinZoom = 0.25f;
        private const float MaxZoom = 1.6f;
        private bool _needsInitialCenter = true;

        // Kích thước Node Card
        private const float NodeWidth = 165f;
        private const float NodeHeight = 65f;

        // Trạng thái Tương tác
        private int _selectedNodeId = -1;
        private int _draggingNodeId = -1;
        private Vector2 _dragOffset;
        private bool _isPanning = false;

        // Trạng thái Kéo dây nối cổng (Wire Connection)
        private int _connectingFromNodeId = -1;

        // Hằng số Kích thước Giao diện
        private const float ToolbarHeight = 44f;
        private const float MinInspectorWidth = 380f;
        private const float MaxInspectorWidth = 700f;
        private const float MinInspectorHeight = 420f;
        private const float MaxInspectorHeight = 900f;

        // Trạng thái Floating Inspector
        private bool _showFloatingInspector = true;
        private Rect _floatingInspectorRect = new Rect(20, 54, 450, 580);
        private Vector2 _inspectorScroll;
        private bool _isDraggingInspector = false;
        private bool _isResizingInspector = false;
        private bool _inspectorPositionInitialized = false;

        // Trạng thái Validation Report
        private bool _showValidationBox = false;
        private string _validationReport = "";
        private bool _validationSuccess = true;

        [MenuItem("Tools/Project D/Dungeon Map Graph Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<DungeonGraphEditorWindow>("Dungeon Map");
            window.minSize = new Vector2(800, 500);
            window.Show();
        }

        public static void OpenWithConfig(RunMapConfigSO config, int focusNodeId = -1)
        {
            var window = GetWindow<DungeonGraphEditorWindow>("Dungeon Map");
            window._mapConfig = config;
            if (focusNodeId > 0)
            {
                window._selectedNodeId = focusNodeId;
            }
            window.minSize = new Vector2(800, 500);
            window.Show();
        }

        private void OnEnable()
        {
            _needsInitialCenter = true;
            if (_mapConfig == null)
            {
                _mapConfig = AssetDatabase.LoadAssetAtPath<RunMapConfigSO>("Assets/_Project/ScriptableObjects/Dungeon/RunMap_13Floors.asset");
            }

            if (_mapConfig != null)
            {
                // Kiểm tra nếu các node đang bị xếp theo chiều dọc (X bằng nhau) -> Tự động chuyển sang chiều ngang
                CheckAndApplyHorizontalLayout();
            }
        }

        private void CheckAndApplyHorizontalLayout()
        {
            if (_mapConfig == null || _mapConfig.AllNodes.Count == 0) return;

            bool isVerticalOrZero = false;
            if (_mapConfig.AllNodes.Count >= 2)
            {
                var n1 = _mapConfig.AllNodes[0];
                var nLast = _mapConfig.AllNodes[_mapConfig.AllNodes.Count - 1];
                // Nếu khoảng cách X giữa Node 1 và Node cuối quá nhỏ so với số tầng -> Đang bị xếp dọc
                if (Mathf.Abs(nLast.EditorPosition.x - n1.EditorPosition.x) < 200f || n1.EditorPosition == Vector2.zero)
                {
                    isVerticalOrZero = true;
                }
            }

            if (isVerticalOrZero)
            {
                _mapConfig.AutoLayoutNodes();
                EditorUtility.SetDirty(_mapConfig);
                AssetDatabase.SaveAssets();
            }
        }

        private void OnGUI()
        {
            if (_mapConfig == null)
            {
                DrawEmptyConfigState();
                return;
            }

            if (!_inspectorPositionInitialized && position.width > 500)
            {
                float targetWidth = Mathf.Min(450f, position.width - 40f);
                float targetHeight = Mathf.Min(580f, position.height - ToolbarHeight - 20f);
                _floatingInspectorRect = new Rect(position.width - targetWidth - 20, ToolbarHeight + 10, targetWidth, targetHeight);
                _inspectorPositionInitialized = true;
            }

            if (_needsInitialCenter && position.width > 200 && position.height > 200)
            {
                CenterCanvasOnNodes();
                _needsInitialCenter = false;
            }

            // 1. Xử lý sự kiện chuột (Pan, Zoom, Drag, Context Menu)
            HandleCanvasEvents();

            // 2. Vẽ lưới nền Canvas (Dark Blueprint Grid)
            DrawCanvasGrid(25f * _zoom, new Color(0.15f, 0.15f, 0.16f, 0.6f));
            DrawCanvasGrid(125f * _zoom, new Color(0.22f, 0.22f, 0.24f, 0.9f));

            // 3. Vẽ các cột mốc tầng theo chiều ngang (Floor Column Markers: Trái ➔ Phải)
            DrawFloorColumnMarkers();

            // 4. Vẽ các đường dây liên kết Cổng (Bezier Wires: Trái ➔ Phải)
            DrawConnectionWires();

            // 5. Vẽ dây đang kéo từ cổng nếu có
            DrawActiveConnectingWire();

            // 6. Vẽ các Node Phòng (Free Nodes)
            DrawNodes();

            // 7. Thanh công cụ ở đỉnh cửa sổ (Top Toolbar)
            DrawTopToolbar();

            // 8. Bảng thuộc tính nổi của Node đang chọn (Floating Inspector)
            if (_showFloatingInspector && _selectedNodeId > 0)
            {
                DrawFloatingNodeInspector();
            }

            // 9. Khung báo cáo kiểm tra nếu được bật
            if (_showValidationBox)
            {
                DrawValidationBoxOverlay();
            }

            // Yêu cầu vẽ lại liên tục khi đang tương tác
            if (_isPanning || _draggingNodeId != -1 || _connectingFromNodeId != -1)
            {
                Repaint();
            }
        }

        // =========================================================================
        // 1. THANH CÔNG CỤ ĐỈNH (TOP TOOLBAR)
        // =========================================================================

        private void DrawTopToolbar()
        {
            Rect barRect = new Rect(0, 0, position.width, ToolbarHeight);

            // Nền tối thanh lịch, độ tương phản cao với đường viền dưới
            EditorGUI.DrawRect(barRect, new Color(0.12f, 0.13f, 0.15f, 1f));
            Handles.BeginGUI();
            Handles.color = new Color(0.25f, 0.28f, 0.33f, 1f);
            Handles.DrawLine(new Vector3(0, ToolbarHeight, 0), new Vector3(position.width, ToolbarHeight, 0));
            Handles.color = Color.white;
            Handles.EndGUI();

            GUILayout.BeginArea(barRect);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);

            // 1. Tiêu đề Logo & Chọn Asset Map
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.3f, 0.9f, 1f) }
            };
            EditorGUILayout.LabelField("🗺️ DUNGEON MAP", titleStyle, GUILayout.Width(125), GUILayout.Height(ToolbarHeight));

            _mapConfig = (RunMapConfigSO)EditorGUILayout.ObjectField(_mapConfig, typeof(RunMapConfigSO), false, GUILayout.Width(160), GUILayout.Height(24));

            DrawToolbarDivider();

            // 2. Quản lý Thêm / Bớt Tầng Trực Tiếp
            GUIStyle floorBadgeStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.85f, 0.95f, 1f) }
            };
            EditorGUILayout.LabelField($"Tầng: {_mapConfig.TotalFloors:00}", floorBadgeStyle, GUILayout.Width(58), GUILayout.Height(26));

            GUI.backgroundColor = new Color(0.35f, 0.9f, 0.55f);
            if (GUILayout.Button("➕", GUILayout.Width(28), GUILayout.Height(26)))
            {
                Undo.RecordObject(_mapConfig, "Add Floor At End");
                _mapConfig.AddFloorAtEnd();
                _mapConfig.AutoLayoutNodes();
                EditorUtility.SetDirty(_mapConfig);
                AssetDatabase.SaveAssets();
                ShowNotification(new GUIContent($"Đã thêm Tầng {_mapConfig.TotalFloors}!"));
            }

            using (new EditorGUI.DisabledScope(_mapConfig.TotalFloors <= 1))
            {
                GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
                if (GUILayout.Button("➖", GUILayout.Width(28), GUILayout.Height(26)))
                {
                    if (EditorUtility.DisplayDialog("Xóa Tầng Cuối", $"Bạn có chắc muốn xóa Tầng {_mapConfig.TotalFloors} khỏi bản đồ không?", "Xóa", "Hủy"))
                    {
                        Undo.RecordObject(_mapConfig, "Delete Last Floor");
                        _mapConfig.DeleteFloor(_mapConfig.TotalFloors);
                        _mapConfig.AutoLayoutNodes();
                        EditorUtility.SetDirty(_mapConfig);
                        AssetDatabase.SaveAssets();
                        ShowNotification(new GUIContent($"Đã bớt tầng cuối, còn lại {_mapConfig.TotalFloors} tầng!"));
                    }
                }
            }

            GUILayout.Space(4);

            // Nút Đặt Lại 13 Tầng Mặc Định
            GUI.backgroundColor = new Color(1f, 0.42f, 0.42f);
            if (GUILayout.Button("🔄 Đặt Lại 13 Tầng Mặc Định", GUILayout.Width(170), GUILayout.Height(26)))
            {
                if (EditorUtility.DisplayDialog("Đặt Lại Bản Đồ Mặc Định",
                    "Bạn có chắc muốn đặt lại toàn bộ bản đồ về cấu trúc 13 Tầng chuẩn mặc định (22 Phòng liên thông) không?\nMọi phòng và nhánh tùy biến thêm bớt sẽ được hoàn nguyên.", "Đặt Lại Mặc Định", "Hủy"))
                {
                    Undo.RecordObject(_mapConfig, "Reset Run Map 13 Floors");
                    _mapConfig.BuildDefault13FloorRunMap();
                    _mapConfig.AutoLayoutNodes();
                    EditorUtility.SetDirty(_mapConfig);
                    AssetDatabase.SaveAssets();
                    _selectedNodeId = 1;
                    CenterCanvasOnNodes();
                    ShowNotification(new GUIContent("Đã đặt lại 13 Tầng mặc định thành công!"));
                }
            }

            DrawToolbarDivider();

            // 3. Căn Ngang & Căn Giữa Màn Hình
            GUI.backgroundColor = new Color(0.85f, 0.92f, 1f);
            if (GUILayout.Button("⚡ Căn Ngang", GUILayout.Width(88), GUILayout.Height(26)))
            {
                Undo.RecordObject(_mapConfig, "Auto Layout Horizontal");
                _mapConfig.AutoLayoutNodes();
                CenterCanvasOnNodes();
                EditorUtility.SetDirty(_mapConfig);
                AssetDatabase.SaveAssets();
            }

            if (GUILayout.Button("🔍 Căn Giữa", GUILayout.Width(82), GUILayout.Height(26)))
            {
                CenterCanvasOnNodes();
            }

            DrawToolbarDivider();

            // 4. Thêm Phòng Mới & Tự Động Nối Cổng
            GUI.backgroundColor = new Color(0.3f, 0.95f, 0.55f);
            if (GUILayout.Button("➕ Thêm Phòng", GUILayout.Width(102), GUILayout.Height(26)))
            {
                Vector2 spawnPos = ScreenToCanvasSpace(new Vector2(position.width * 0.4f, position.height * 0.45f));
                Undo.RecordObject(_mapConfig, "Create Free Node");
                var created = _mapConfig.CreateFreeNode(spawnPos, RunNodeType.NormalBattle);
                _selectedNodeId = created.Id;
                EditorUtility.SetDirty(_mapConfig);
            }

            GUI.backgroundColor = new Color(0.35f, 0.85f, 1f);
            if (GUILayout.Button("⚡ Tự Nối Cổng", GUILayout.Width(96), GUILayout.Height(26)))
            {
                Undo.RecordObject(_mapConfig, "Auto Link All Floors");
                _mapConfig.AutoLinkAllFloors();
                EditorUtility.SetDirty(_mapConfig);
            }

            // 5. Kiểm Tra Toàn Vẹn & Tiện Ích
            GUI.backgroundColor = new Color(1f, 0.88f, 0.35f);
            if (GUILayout.Button("🛡️ Kiểm Tra", GUILayout.Width(88), GUILayout.Height(26)))
            {
                RunIntegrityValidation();
            }

            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("⚙️ Tiện Ích ▾", GUILayout.Width(88), GUILayout.Height(26)))
            {
                ShowMapOptionsMenu();
            }

            GUILayout.FlexibleSpace();

            // 6. Khu vực bên phải: Zoom & Toggle Bảng Sửa Node
            GUIStyle zoomStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };
            if (GUILayout.Button($"🔎 {Mathf.RoundToInt(_zoom * 100)}%", zoomStyle, GUILayout.Width(58), GUILayout.Height(26)))
            {
                _zoom = 1f;
                Repaint();
            }

            GUILayout.Space(6);

            GUI.backgroundColor = _showFloatingInspector ? new Color(0.3f, 0.95f, 0.6f) : new Color(0.65f, 0.7f, 0.75f);
            string inspectorToggleLabel = _showFloatingInspector ? "👁️ Bảng Sửa Node [BẬT]" : "👁️ Bảng Sửa Node [TẮT]";
            _showFloatingInspector = GUILayout.Toggle(_showFloatingInspector, inspectorToggleLabel, "Button", GUILayout.Width(160), GUILayout.Height(26));
            GUI.backgroundColor = Color.white;

            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawToolbarDivider()
        {
            GUILayout.Space(6);
            Rect r = GUILayoutUtility.GetRect(1, 24, GUILayout.Width(1), GUILayout.Height(24));
            EditorGUI.DrawRect(r, new Color(0.32f, 0.35f, 0.40f, 0.7f));
            GUILayout.Space(6);
        }

        private void ShowMapOptionsMenu()
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("🔄 Đặt Lại 13 Tầng Mặc Định"), false, () =>
            {
                if (EditorUtility.DisplayDialog("Đặt Lại Bản Đồ", "Bạn có chắc muốn đặt lại đồ thị chuẩn 13 Tầng (22 Nút) không?", "Đặt Lại", "Hủy"))
                {
                    Undo.RecordObject(_mapConfig, "Reset Run Map 13 Floors");
                    _mapConfig.BuildDefault13FloorRunMap();
                    _mapConfig.AutoLayoutNodes();
                    EditorUtility.SetDirty(_mapConfig);
                    AssetDatabase.SaveAssets();
                    _selectedNodeId = 1;
                    CenterCanvasOnNodes();
                }
            });

            menu.AddItem(new GUIContent("📦 Tự Động Sinh Đầy Đủ 24 Biến Thể Prefab Phòng"), false, () =>
            {
                DungeonRoomGenerator.GenerateAllRooms(true);
            });

            menu.AddItem(new GUIContent("🧹 Dọn Dẹp Cổng Lỗi"), false, () =>
            {
                Undo.RecordObject(_mapConfig, "Clean Dangling Connections");
                int removed = _mapConfig.CleanDanglingConnections();
                EditorUtility.SetDirty(_mapConfig);
                EditorUtility.DisplayDialog("Dọn Cổng Lỗi", $"Đã quét và xóa thành công {removed} cổng lỗi!", "OK");
            });

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("➕ Thêm Tầng Mới Ở Cuối Bản Đồ"), false, () =>
            {
                Undo.RecordObject(_mapConfig, "Add Floor At End");
                _mapConfig.AddFloorAtEnd();
                _mapConfig.AutoLayoutNodes();
                EditorUtility.SetDirty(_mapConfig);
                AssetDatabase.SaveAssets();
            });

            menu.AddItem(new GUIContent("🗑️ Xóa Tầng Cuối Cùng"), false, () =>
            {
                if (_mapConfig.TotalFloors > 1)
                {
                    if (EditorUtility.DisplayDialog("Xóa Tầng Cuối", $"Bạn có chắc muốn xóa Tầng {_mapConfig.TotalFloors} không?", "Xóa", "Hủy"))
                    {
                        Undo.RecordObject(_mapConfig, "Delete Last Floor");
                        _mapConfig.DeleteFloor(_mapConfig.TotalFloors);
                        _mapConfig.AutoLayoutNodes();
                        EditorUtility.SetDirty(_mapConfig);
                        AssetDatabase.SaveAssets();
                    }
                }
            });

            menu.ShowAsContext();
        }

        // =========================================================================
        // 2. VẼ LƯỚI & CỘT MỐC TẦNG THEO CHIỀU NGANG (TRÁI ➔ PHẢI)
        // =========================================================================

        private void DrawCanvasGrid(float gridSpacing, Color gridColor)
        {
            int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
            int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);

            Handles.BeginGUI();
            Handles.color = gridColor;

            Vector2 offset = new Vector2(_panOffset.x % gridSpacing, _panOffset.y % gridSpacing);

            for (int i = 0; i <= widthDivs; i++)
            {
                Handles.DrawLine(new Vector3(gridSpacing * i + offset.x, ToolbarHeight, 0), new Vector3(gridSpacing * i + offset.x, position.height, 0));
            }

            for (int j = 0; j <= heightDivs; j++)
            {
                float y = gridSpacing * j + offset.y;
                if (y >= ToolbarHeight)
                {
                    Handles.DrawLine(new Vector3(0, y, 0), new Vector3(position.width, y, 0));
                }
            }

            Handles.color = Color.white;
            Handles.EndGUI();
        }

        // Vẽ các vạch cột phân chia tầng theo trục hoành (Left to Right)
        private void DrawFloorColumnMarkers()
        {
            if (_mapConfig == null) return;

            Handles.BeginGUI();
            GUIStyle colHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = Mathf.RoundToInt(11 * _zoom),
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.6f, 0.75f, 0.9f, 0.45f) }
            };

            float spacingX = 240f;
            float startX = 80f;

            for (int f = 1; f <= _mapConfig.TotalFloors; f++)
            {
                float canvasX = startX + (f - 1) * spacingX;
                Vector2 screenTop = CanvasToScreenSpace(new Vector2(canvasX + NodeWidth * 0.5f, 30f));
                Vector2 screenBottom = CanvasToScreenSpace(new Vector2(canvasX + NodeWidth * 0.5f, 850f));

                if (screenTop.x < -100 || screenTop.x > position.width + 100) continue;

                // Vạch mờ phân chia tầng
                Handles.color = new Color(0.3f, 0.5f, 0.7f, 0.15f);
                Handles.DrawLine(new Vector3(screenTop.x, ToolbarHeight, 0), new Vector3(screenTop.x, position.height, 0));

                // Tiêu đề Tầng trên đỉnh
                Rect headerRect = new Rect(screenTop.x - 60 * _zoom, ToolbarHeight + 6, 120 * _zoom, 22 * _zoom);
                string floorTitle = (f == _mapConfig.TotalFloors) ? $"TẦNG {f:00} (BOSS)" : $"TẦNG {f:00}";
                GUI.Label(headerRect, floorTitle, colHeaderStyle);
            }

            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private Vector2 CanvasToScreenSpace(Vector2 canvasPos)
        {
            return canvasPos * _zoom + _panOffset;
        }

        private Vector2 ScreenToCanvasSpace(Vector2 screenPos)
        {
            return (screenPos - _panOffset) / _zoom;
        }

        // =========================================================================
        // 3. VẼ DÂY LIÊN KẾT CỔNG (BEZIER WIRES: TRÁI ➔ PHẢI)
        // =========================================================================

        private void DrawConnectionWires()
        {
            Handles.BeginGUI();

            foreach (var node in _mapConfig.AllNodes)
            {
                if (node.NextNodeIds == null || node.NextNodeIds.Count == 0) continue;

                // Cổng ra nằm ở mép phải của Node nguồn (Output Socket)
                Vector2 startPos = CanvasToScreenSpace(node.EditorPosition + new Vector2(NodeWidth, NodeHeight * 0.5f));

                for (int i = 0; i < node.NextNodeIds.Count; i++)
                {
                    int targetId = node.NextNodeIds[i];
                    var targetNode = _mapConfig.GetNodeById(targetId);
                    if (targetNode == null) continue;

                    // Cổng vào nằm ở mép trái của Node đích (Input Socket)
                    Vector2 endPos = CanvasToScreenSpace(targetNode.EditorPosition + new Vector2(0f, NodeHeight * 0.5f));

                    // Tính tiếp tuyến ngang tạo đường cong uốn lượn mượt mà từ Trái sang Phải
                    float dx = Mathf.Max(60f, Mathf.Abs(endPos.x - startPos.x) * 0.55f);
                    Vector2 startTangent = startPos + Vector2.right * dx;
                    Vector2 endTangent = endPos - Vector2.right * dx;

                    bool isWireSelected = (node.Id == _selectedNodeId || targetId == _selectedNodeId);
                    Color wireCol = isWireSelected ? new Color(0.35f, 1f, 0.7f, 0.95f) : new Color(0.45f, 0.75f, 0.95f, 0.65f);
                    float wireWidth = isWireSelected ? 3.5f : 2.2f;

                    Handles.DrawBezier(startPos, endPos, startTangent, endTangent, wireCol, null, wireWidth);

                    // Mũi tên chỉ hướng cổng vào
                    DrawArrowHead(endPos, (endPos - endTangent).normalized, wireCol, 9f * _zoom);
                }
            }

            Handles.EndGUI();
        }

        private void DrawActiveConnectingWire()
        {
            if (_connectingFromNodeId <= 0) return;

            var fromNode = _mapConfig.GetNodeById(_connectingFromNodeId);
            if (fromNode == null) return;

            Vector2 startPos = CanvasToScreenSpace(fromNode.EditorPosition + new Vector2(NodeWidth, NodeHeight * 0.5f));
            Vector2 mousePos = Event.current.mousePosition;

            float dx = Mathf.Max(50f, Mathf.Abs(mousePos.x - startPos.x) * 0.5f);
            Vector2 startTangent = startPos + Vector2.right * dx;
            Vector2 endTangent = mousePos - Vector2.right * dx;

            Handles.BeginGUI();
            Handles.DrawBezier(startPos, mousePos, startTangent, endTangent, Color.yellow, null, 3f);
            Handles.EndGUI();
        }

        private void DrawArrowHead(Vector2 tip, Vector2 dir, Color color, float size)
        {
            Vector2 normal = new Vector2(-dir.y, dir.x);
            Vector2 p1 = tip - dir * size + normal * (size * 0.55f);
            Vector2 p2 = tip - dir * size - normal * (size * 0.55f);

            Handles.color = color;
            Handles.DrawAAConvexPolygon(tip, p1, p2);
            Handles.color = Color.white;
        }

        // =========================================================================
        // 4. VẼ CÁC NODE PHÒNG (FREEFORM NODE CARDS)
        // =========================================================================

        private void DrawNodes()
        {
            foreach (var node in _mapConfig.AllNodes)
            {
                Rect screenRect = new Rect(
                    CanvasToScreenSpace(node.EditorPosition),
                    new Vector2(NodeWidth * _zoom, NodeHeight * _zoom)
                );

                // Culling màn hình
                if (screenRect.yMax < ToolbarHeight || screenRect.yMin > position.height || screenRect.xMax < 0 || screenRect.xMin > position.width)
                {
                    continue;
                }

                DrawSingleNode(node, screenRect);
            }
        }

        private void DrawSingleNode(RunNodeData node, Rect rect)
        {
            bool isSelected = (node.Id == _selectedNodeId);

            // Viền vàng nổi bật khi được chọn
            if (isSelected)
            {
                Rect glowRect = new Rect(rect.x - 3, rect.y - 3, rect.width + 6, rect.height + 6);
                EditorGUI.DrawRect(glowRect, new Color(1f, 0.85f, 0.2f, 0.9f));
            }

            // Nền thẻ node
            EditorGUI.DrawRect(rect, new Color(0.18f, 0.2f, 0.23f, 0.96f));

            // Header Bar (Màu theo loại phòng)
            float headerH = 22f * _zoom;
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, headerH);
            Color typeColor = RunMapConfigSO.GetDefaultColor(node.NodeType);
            EditorGUI.DrawRect(headerRect, typeColor);

            // Tiêu đề Header
            GUIStyle headerTextStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = Mathf.RoundToInt(11 * _zoom),
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };
            string icon = GetNodeTypeIcon(node.NodeType);
            string headerText = $"{icon} [#{node.Id}] {node.BranchName}";
            GUI.Label(new Rect(headerRect.x + 6 * _zoom, headerRect.y, headerRect.width - 12 * _zoom, headerRect.height), headerText, headerTextStyle);

            // Nội dung Body
            GUIStyle bodyStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = Mathf.RoundToInt(10 * _zoom),
                normal = { textColor = new Color(0.9f, 0.92f, 0.95f) }
            };
            string star = (node.CustomRoomPrefab != null) ? " ★" : "";
            string bodyText = $"{node.DisplayName}{star}";
            GUI.Label(new Rect(rect.x + 6 * _zoom, rect.y + headerH + 3 * _zoom, rect.width - 12 * _zoom, 18 * _zoom), bodyText, bodyStyle);

            // Cổng Input (Trái) & Output (Phải)
            float socketSize = 10f * _zoom;
            Rect inSocket = new Rect(rect.x - socketSize * 0.5f, rect.y + (rect.height - socketSize) * 0.5f, socketSize, socketSize);
            Rect outSocket = new Rect(rect.xMax - socketSize * 0.5f, rect.y + (rect.height - socketSize) * 0.5f, socketSize, socketSize);

            EditorGUI.DrawRect(inSocket, new Color(0.3f, 0.85f, 1f));

            // Hiển thị màu sắc và trạng thái của Output Socket dựa trên số lượng cổng nối (Tối đa 3)
            int outgoingCount = node.NextNodeIds?.Count ?? 0;
            Color outSocketCol = outgoingCount >= RunMapConfigSO.MaxPortalsPerRoom
                ? new Color(1f, 0.55f, 0.2f)   // Đầy 3 cổng -> Màu cam cảnh báo
                : new Color(0.35f, 1f, 0.6f);  // Còn slot -> Màu xanh lá sẵn sàng

            EditorGUI.DrawRect(outSocket, outSocketCol);

            // Badge nhỏ đếm số cổng (0/3, 1/3, 2/3, 3/3) ở góc phải
            GUIStyle portalBadgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = Mathf.RoundToInt(9 * _zoom),
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = outgoingCount >= RunMapConfigSO.MaxPortalsPerRoom ? new Color(1f, 0.6f, 0.3f) : new Color(0.7f, 0.85f, 0.95f) }
            };
            GUI.Label(new Rect(rect.xMax - 45 * _zoom, rect.yMax - 16 * _zoom, 40 * _zoom, 14 * _zoom), $"{outgoingCount}/{RunMapConfigSO.MaxPortalsPerRoom}", portalBadgeStyle);
        }

        // =========================================================================
        // 5. XỬ LÝ SỰ KIỆN CHUỘT & TƯƠNG TÁC (CANVAS EVENTS)
        // =========================================================================

        private void HandleCanvasEvents()
        {
            Event e = Event.current;

            // Bỏ qua nếu chuột đang nằm trong Floating Inspector hoặc đang kéo/resize Inspector
            if (_showFloatingInspector && _selectedNodeId > 0 && (_floatingInspectorRect.Contains(e.mousePosition) || _isDraggingInspector || _isResizingInspector))
            {
                return;
            }

            // Bỏ qua nếu chuột nằm trong Top Toolbar
            if (e.mousePosition.y < ToolbarHeight)
            {
                return;
            }

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0) // Chuột trái
                    {
                        var clickedNode = GetNodeAtPosition(e.mousePosition);

                        if (clickedNode != null)
                        {
                            Rect nodeRect = GetNodeScreenRect(clickedNode);
                            float socketSize = 14f * _zoom;
                            Rect outSocket = new Rect(nodeRect.xMax - socketSize, nodeRect.y + (nodeRect.height - socketSize) * 0.5f, socketSize * 1.5f, socketSize);

                            // Bấm vào Socket Output để kéo dây nối cổng
                            if (outSocket.Contains(e.mousePosition))
                            {
                                int currentCount = clickedNode.NextNodeIds?.Count ?? 0;
                                if (currentCount >= RunMapConfigSO.MaxPortalsPerRoom)
                                {
                                    ShowNotification(new GUIContent($"Phòng [#{clickedNode.Id}] đã nối tối đa {RunMapConfigSO.MaxPortalsPerRoom} phòng!"));
                                }
                                else
                                {
                                    _connectingFromNodeId = clickedNode.Id;
                                }
                            }
                            else
                            {
                                _selectedNodeId = clickedNode.Id;
                                _draggingNodeId = clickedNode.Id;
                                _dragOffset = ScreenToCanvasSpace(e.mousePosition) - clickedNode.EditorPosition;
                            }
                            e.Use();
                        }
                        else
                        {
                            _selectedNodeId = -1;
                            GUI.FocusControl(null);
                        }
                    }
                    else if (e.button == 2 || (e.button == 1 && e.alt)) // Chuột giữa hoặc Alt+Phải để Pan
                    {
                        _isPanning = true;
                        e.Use();
                    }
                    else if (e.button == 1) // Chuột phải -> Context Menu
                    {
                        var clickedNode = GetNodeAtPosition(e.mousePosition);
                        if (clickedNode != null)
                        {
                            ShowNodeContextMenu(clickedNode);
                        }
                        else
                        {
                            ShowCanvasContextMenu(e.mousePosition);
                        }
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (_isPanning || e.button == 2)
                    {
                        _panOffset += e.delta;
                        e.Use();
                    }
                    else if (_draggingNodeId > 0)
                    {
                        var node = _mapConfig.GetNodeById(_draggingNodeId);
                        if (node != null)
                        {
                            Vector2 targetCanvasPos = ScreenToCanvasSpace(e.mousePosition) - _dragOffset;
                            node.SetEditorPosition(targetCanvasPos);
                        }
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (_connectingFromNodeId > 0)
                    {
                        var targetNode = GetNodeAtPosition(e.mousePosition);
                        if (targetNode != null && targetNode.Id != _connectingFromNodeId)
                        {
                            if (_mapConfig.CanConnect(_connectingFromNodeId, targetNode.Id, out string reason))
                            {
                                Undo.RecordObject(_mapConfig, $"Connect Node {_connectingFromNodeId} to {targetNode.Id}");
                                _mapConfig.ConnectNodes(_connectingFromNodeId, targetNode.Id);
                                EditorUtility.SetDirty(_mapConfig);
                            }
                            else if (!string.IsNullOrEmpty(reason))
                            {
                                EditorUtility.DisplayDialog("Ràng Buộc Cổng Nối",
                                    $"{reason}\n\n(Lưu ý: Một phòng chỉ được phép nối tối đa đến {RunMapConfigSO.MaxPortalsPerRoom} phòng tiếp theo tương ứng 3 cổng vật lý: Trái, Giữa, Phải).",
                                    "OK");
                            }
                        }
                        _connectingFromNodeId = -1;
                        e.Use();
                    }

                    if (_draggingNodeId > 0)
                    {
                        Undo.RecordObject(_mapConfig, "Move Node Position");
                        EditorUtility.SetDirty(_mapConfig);
                        _draggingNodeId = -1;
                        e.Use();
                    }

                    _isPanning = false;
                    break;

                case EventType.ScrollWheel:
                    float oldZoom = _zoom;
                    _zoom = Mathf.Clamp(_zoom - e.delta.y * 0.05f, MinZoom, MaxZoom);

                    Vector2 mousePos = e.mousePosition;
                    _panOffset = mousePos - (mousePos - _panOffset) * (_zoom / oldZoom);

                    e.Use();
                    break;
            }
        }

        private RunNodeData GetNodeAtPosition(Vector2 screenPos)
        {
            for (int i = _mapConfig.AllNodes.Count - 1; i >= 0; i--)
            {
                var node = _mapConfig.AllNodes[i];
                Rect rect = GetNodeScreenRect(node);
                if (rect.Contains(screenPos))
                {
                    return node;
                }
            }
            return null;
        }

        private Rect GetNodeScreenRect(RunNodeData node)
        {
            return new Rect(
                CanvasToScreenSpace(node.EditorPosition),
                new Vector2(NodeWidth * _zoom, NodeHeight * _zoom)
            );
        }

        // =========================================================================
        // 6. MENU CHUỘT PHẢI (CONTEXT MENUS)
        // =========================================================================

        private void ShowCanvasContextMenu(Vector2 screenPos)
        {
            GenericMenu menu = new GenericMenu();
            Vector2 spawnCanvasPos = ScreenToCanvasSpace(screenPos);

            menu.AddItem(new GUIContent("➕ Tạo Phòng/⚔️ Quái Thường (Normal Battle)"), false, () =>
            {
                CreateNewNodeAt(spawnCanvasPos, RunNodeType.NormalBattle);
            });
            menu.AddItem(new GUIContent("➕ Tạo Phòng/💀 Tinh Anh (Elite Battle)"), false, () =>
            {
                CreateNewNodeAt(spawnCanvasPos, RunNodeType.EliteBattle);
            });
            menu.AddItem(new GUIContent("➕ Tạo Phòng/💰 Cửa Hàng (Shop)"), false, () =>
            {
                CreateNewNodeAt(spawnCanvasPos, RunNodeType.Shop);
            });
            menu.AddItem(new GUIContent("➕ Tạo Phòng/🏕️ Lửa Trại Hồi Máu (Rest Site)"), false, () =>
            {
                CreateNewNodeAt(spawnCanvasPos, RunNodeType.RestSite);
            });
            menu.AddItem(new GUIContent("➕ Tạo Phòng/📜 Nhận Thẻ Buff (Buff Reward)"), false, () =>
            {
                CreateNewNodeAt(spawnCanvasPos, RunNodeType.BuffReward);
            });
            menu.AddItem(new GUIContent("➕ Tạo Phòng/👹 MINI BOSS"), false, () =>
            {
                CreateNewNodeAt(spawnCanvasPos, RunNodeType.MiniBoss);
            });
            menu.AddItem(new GUIContent("➕ Tạo Phòng/👑 TRÙM CUỐI (Boss Battle)"), false, () =>
            {
                CreateNewNodeAt(spawnCanvasPos, RunNodeType.BossBattle);
            });

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("⚡ Căn Ngang Sơ Đồ (Trái ➔ Phải)"), false, () =>
            {
                Undo.RecordObject(_mapConfig, "Auto Layout Horizontal");
                _mapConfig.AutoLayoutNodes();
                CenterCanvasOnNodes();
                EditorUtility.SetDirty(_mapConfig);
            });
            menu.AddItem(new GUIContent("🔍 Căn Giữa Màn Hình (Focus All)"), false, CenterCanvasOnNodes);

            menu.ShowAsContext();
        }

        private void ShowNodeContextMenu(RunNodeData node)
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent($"⚙️ Chọn Node [#{node.Id}]"), false, () =>
            {
                _selectedNodeId = node.Id;
            });

            menu.AddItem(new GUIContent("🖌️ Mở Vẽ Tilemap Trong Prefab"), false, () =>
            {
                RunMapSceneGraphEditor.OpenRoomForNode(node, _mapConfig);
            });

            menu.AddItem(new GUIContent("📋 Nhân Bản Node"), false, () =>
            {
                DuplicateNode(node);
            });

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("❌ Gỡ Toàn Bộ Cổng Nối"), false, () =>
            {
                Undo.RecordObject(_mapConfig, $"Clear Connections Node {node.Id}");
                node.NextNodeIds.Clear();
                EditorUtility.SetDirty(_mapConfig);
            });

            menu.AddItem(new GUIContent("🗑️ Xóa Phòng Này"), false, () =>
            {
                if (EditorUtility.DisplayDialog("Xóa Phòng", $"Bạn có chắc muốn xóa Node #{node.Id} ({node.DisplayName}) không?", "Xóa", "Hủy"))
                {
                    Undo.RecordObject(_mapConfig, $"Delete Node {node.Id}");
                    _mapConfig.DeleteNode(node.Id);
                    if (_selectedNodeId == node.Id) _selectedNodeId = -1;
                    EditorUtility.SetDirty(_mapConfig);
                }
            });

            menu.ShowAsContext();
        }

        private void CreateNewNodeAt(Vector2 canvasPos, RunNodeType type)
        {
            Undo.RecordObject(_mapConfig, "Create Free Node");
            var newNode = _mapConfig.CreateFreeNode(canvasPos, type);
            _selectedNodeId = newNode.Id;
            EditorUtility.SetDirty(_mapConfig);
        }

        private void DuplicateNode(RunNodeData source)
        {
            Undo.RecordObject(_mapConfig, "Duplicate Node");
            Vector2 newPos = source.EditorPosition + new Vector2(40, 40);
            var newNode = _mapConfig.CreateFreeNode(newPos, source.NodeType, source.DisplayName + " (Copy)");
            newNode.SetFloorNumber(source.FloorNumber);
            newNode.SetCustomRoomPrefab(source.CustomRoomPrefab);
            _selectedNodeId = newNode.Id;
            EditorUtility.SetDirty(_mapConfig);
        }

        private void CenterCanvasOnNodes()
        {
            if (_mapConfig.AllNodes.Count == 0)
            {
                _panOffset = new Vector2(position.width * 0.5f, position.height * 0.5f);
                _zoom = 0.85f;
                return;
            }

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            foreach (var n in _mapConfig.AllNodes)
            {
                if (n.EditorPosition.x < min.x) min.x = n.EditorPosition.x;
                if (n.EditorPosition.y < min.y) min.y = n.EditorPosition.y;
                if (n.EditorPosition.x + NodeWidth > max.x) max.x = n.EditorPosition.x + NodeWidth;
                if (n.EditorPosition.y + NodeHeight > max.y) max.y = n.EditorPosition.y + NodeHeight;
            }

            Vector2 center = (min + max) * 0.5f;
            float contentW = max.x - min.x + 200f;
            float contentH = max.y - min.y + 200f;

            float fitZoomX = position.width / contentW;
            float fitZoomY = (position.height - 40f) / contentH;
            _zoom = Mathf.Clamp(Mathf.Min(fitZoomX, fitZoomY), 0.25f, 1.2f);

            _panOffset = new Vector2(position.width * 0.5f, (position.height + ToolbarHeight) * 0.5f) - center * _zoom;
        }

        // =========================================================================
        // 7. BẢNG THUỘC TÍNH NỔI TOÀN DIỆN (FULL FLOATING INSPECTOR)
        // =========================================================================

        private void DrawInspectorSectionHeader(string title, Color barColor)
        {
            EditorGUILayout.Space(6);
            Rect r = GUILayoutUtility.GetRect(100, 20);
            EditorGUI.DrawRect(new Rect(r.x, r.y, 4, 18), barColor);
            GUIStyle s = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.9f, 0.92f, 0.95f) }
            };
            GUI.Label(new Rect(r.x + 8, r.y, r.width - 8, 18), title, s);
            EditorGUILayout.Space(2);
        }

        private void DrawFloatingNodeInspector()
        {
            var node = _mapConfig.GetNodeById(_selectedNodeId);
            if (node == null) return;

            Event e = Event.current;

            // Vùng kéo di chuyển cả bảng
            Rect titleBarScreenRect = new Rect(_floatingInspectorRect.x, _floatingInspectorRect.y, _floatingInspectorRect.width - 65, 32);

            // Góc dưới phải để kéo co giãn kích thước (Resize Handle)
            Rect resizeHandleRect = new Rect(_floatingInspectorRect.xMax - 20, _floatingInspectorRect.yMax - 20, 20, 20);
            EditorGUIUtility.AddCursorRect(resizeHandleRect, MouseCursor.ResizeUpLeft);

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (resizeHandleRect.Contains(e.mousePosition))
                {
                    _isResizingInspector = true;
                    e.Use();
                }
                else if (titleBarScreenRect.Contains(e.mousePosition))
                {
                    _isDraggingInspector = true;
                    e.Use();
                }
            }
            else if (e.type == EventType.MouseDrag)
            {
                if (_isResizingInspector)
                {
                    _floatingInspectorRect.width = Mathf.Clamp(_floatingInspectorRect.width + e.delta.x, MinInspectorWidth, MaxInspectorWidth);
                    _floatingInspectorRect.height = Mathf.Clamp(_floatingInspectorRect.height + e.delta.y, MinInspectorHeight, MaxInspectorHeight);
                    e.Use();
                    Repaint();
                }
                else if (_isDraggingInspector)
                {
                    _floatingInspectorRect.position += e.delta;
                    _floatingInspectorRect.x = Mathf.Clamp(_floatingInspectorRect.x, 10, position.width - _floatingInspectorRect.width - 10);
                    _floatingInspectorRect.y = Mathf.Clamp(_floatingInspectorRect.y, ToolbarHeight + 6, position.height - _floatingInspectorRect.height - 10);
                    e.Use();
                    Repaint();
                }
            }
            else if (e.type == EventType.MouseUp)
            {
                if (_isDraggingInspector || _isResizingInspector)
                {
                    _isDraggingInspector = false;
                    _isResizingInspector = false;
                    e.Use();
                }
            }

            _floatingInspectorRect.x = Mathf.Clamp(_floatingInspectorRect.x, 10, Mathf.Max(10, position.width - _floatingInspectorRect.width - 10));
            _floatingInspectorRect.y = Mathf.Clamp(_floatingInspectorRect.y, ToolbarHeight + 6, Mathf.Max(ToolbarHeight + 6, position.height - _floatingInspectorRect.height - 10));

            // Vẽ khung nền Card với viền ngoài sắc nét
            Handles.BeginGUI();
            Handles.DrawSolidRectangleWithOutline(_floatingInspectorRect,
                new Color(0.13f, 0.14f, 0.16f, 0.98f),
                new Color(0.30f, 0.35f, 0.42f, 1f));
            Handles.EndGUI();

            GUILayout.BeginArea(_floatingInspectorRect);
            EditorGUILayout.BeginVertical();
            GUILayout.Space(6);

            // Tiêu đề (có thể kéo thả)
            EditorGUILayout.BeginHorizontal();
            Color themeCol = RunMapConfigSO.GetDefaultColor(node.NodeType);
            GUIStyle headStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = themeCol }
            };
            EditorGUILayout.LabelField($"⚙️ BẢNG SỬA PHÒNG [#{node.Id}] - {node.BranchName}", headStyle);

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("🗑️", EditorStyles.miniButton, GUILayout.Width(28), GUILayout.Height(22)))
            {
                if (EditorUtility.DisplayDialog("Xóa Phòng", $"Xóa Node #{node.Id} ({node.DisplayName})?", "Xóa", "Hủy"))
                {
                    Undo.RecordObject(_mapConfig, $"Delete Node {node.Id}");
                    _mapConfig.DeleteNode(node.Id);
                    _selectedNodeId = -1;
                    EditorUtility.SetDirty(_mapConfig);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    GUILayout.EndArea();
                    return;
                }
            }

            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(26), GUILayout.Height(22)))
            {
                _showFloatingInspector = false;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("   (Giữ chuột ở thanh trên để di chuyển, kéo góc dưới phải để phóng to)", EditorStyles.miniLabel);
            EditorGUILayout.Space(2);

            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);

            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100f;

            // ==================== 1. THÔNG TIN CƠ BẢN ====================
            DrawInspectorSectionHeader("1. THÔNG TIN CƠ BẢN", new Color(0.3f, 0.85f, 1f));

            EditorGUI.BeginChangeCheck();
            var newType = (RunNodeType)EditorGUILayout.EnumPopup("Loại Phòng:", node.NodeType, GUILayout.Height(20));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_mapConfig, "Change Node Type");
                node.SetNodeType(newType);
                node.SetNodeColor(RunMapConfigSO.GetDefaultColor(newType));
                node.SetDisplayName($"Tầng {node.FloorNumber}: {RunMapConfigSO.GetDefaultTitle(newType)}");
                EditorUtility.SetDirty(_mapConfig);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 45f;
            EditorGUI.BeginChangeCheck();
            int newFloor = EditorGUILayout.IntField("Tầng:", node.FloorNumber);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_mapConfig, "Edit Floor");
                node.SetFloorNumber(Mathf.Max(1, newFloor));
                EditorUtility.SetDirty(_mapConfig);
            }

            GUILayout.Space(12);

            EditorGUIUtility.labelWidth = 55f;
            EditorGUI.BeginChangeCheck();
            string newBranch = EditorGUILayout.TextField("Nhánh:", node.BranchName);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_mapConfig, "Edit Branch");
                node.SetBranchName(newBranch);
                EditorUtility.SetDirty(_mapConfig);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = 100f;

            EditorGUI.BeginChangeCheck();
            string newName = EditorGUILayout.TextField("Tên Hiển Thị:", node.DisplayName);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_mapConfig, "Edit Node Name");
                node.SetDisplayName(newName);
                EditorUtility.SetDirty(_mapConfig);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Vị trí 2D:", GUILayout.Width(100));
            EditorGUIUtility.labelWidth = 20f;
            EditorGUI.BeginChangeCheck();
            float newX = EditorGUILayout.FloatField("X", node.EditorPosition.x);
            GUILayout.Space(12);
            float newY = EditorGUILayout.FloatField("Y", node.EditorPosition.y);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_mapConfig, "Edit Node Position");
                node.SetEditorPosition(new Vector2(newX, newY));
                EditorUtility.SetDirty(_mapConfig);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = 100f;

            // ==================== 2. PREFAB & VẼ TILEMAP ====================
            DrawInspectorSectionHeader("2. PREFAB & VẼ TILEMAP (PREFAB MODE)", new Color(0.2f, 0.9f, 0.55f));

            EditorGUI.BeginChangeCheck();
            var newPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab Riêng:", node.CustomRoomPrefab, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_mapConfig, "Change Room Prefab");
                node.SetCustomRoomPrefab(newPrefab);
                EditorUtility.SetDirty(_mapConfig);
            }

            if (node.CustomRoomPrefab != null)
            {
                EditorGUILayout.HelpBox($"✓ Đang dùng Prefab riêng: '{node.CustomRoomPrefab.name}'", MessageType.Info);

                GUI.backgroundColor = new Color(0.2f, 0.9f, 0.55f);
                if (GUILayout.Button($"🖌️ MỞ VẼ TILEMAP TRONG PREFAB MODE ({node.CustomRoomPrefab.name})", GUILayout.Height(30)))
                {
                    RunMapSceneGraphEditor.OpenRoomForNode(node, _mapConfig);
                }
                GUI.backgroundColor = Color.white;

                GUI.backgroundColor = new Color(0.9f, 0.85f, 0.85f);
                if (GUILayout.Button("↩ Gỡ Prefab Riêng (Dùng lại Mẫu Chung Database)", GUILayout.Height(22)))
                {
                    if (EditorUtility.DisplayDialog("Gỡ Prefab Riêng", "Bạn có muốn gỡ bỏ Prefab riêng này để quay lại dùng Prefab mẫu chung từ Database không?", "Gỡ Bỏ", "Hủy"))
                    {
                        Undo.RecordObject(_mapConfig, $"Remove Custom Prefab from Node {node.Id}");
                        node.SetCustomRoomPrefab(null);
                        EditorUtility.SetDirty(_mapConfig);
                    }
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                EditorGUILayout.HelpBox("• Node chưa gán Prefab riêng. Bấm 'Vẽ Tilemap' để tạo bản sao riêng biệt hoặc sửa mẫu chung.", MessageType.None);

                GUI.backgroundColor = new Color(0.2f, 0.9f, 0.55f);
                if (GUILayout.Button("🖌️ MỞ VẼ TILEMAP (Hỏi Tạo Prefab Riêng & Vẽ)", GUILayout.Height(30)))
                {
                    RunMapSceneGraphEditor.OpenRoomForNode(node, _mapConfig);
                }
                GUI.backgroundColor = Color.white;

                GUI.backgroundColor = new Color(0.35f, 0.82f, 1f);
                if (GUILayout.Button("➕ Tạo Ngay Prefab Riêng Độc Lập Cho Nút Này", GUILayout.Height(24)))
                {
                    var created = CreateAndAssignUniquePrefab(node);
                    if (created != null)
                    {
                        RunMapSceneGraphEditor.OpenRoomPrefabInPrefabMode(created);
                    }
                }
                GUI.backgroundColor = Color.white;
            }

            // ==================== 3. THAO TÁC TẦNG & NHÁNH ====================
            DrawInspectorSectionHeader("3. QUẢN LÝ NHÁNH & TẦNG", new Color(1f, 0.85f, 0.35f));

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.4f, 0.85f, 1f);
            if (GUILayout.Button("➕ Thêm Nhánh Cùng Tầng", GUILayout.Height(26)))
            {
                Undo.RecordObject(_mapConfig, $"Add Branch to Floor {node.FloorNumber}");
                var created = _mapConfig.AddBranchToFloor(node.FloorNumber, RunNodeType.NormalBattle);
                if (created != null)
                {
                    _selectedNodeId = created.Id;
                    _mapConfig.AutoLayoutNodes();
                }
                EditorUtility.SetDirty(_mapConfig);
            }

            GUI.backgroundColor = new Color(0.9f, 0.95f, 1f);
            if (GUILayout.Button("📋 Nhân Bản Node", GUILayout.Width(130), GUILayout.Height(26)))
            {
                DuplicateNode(node);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.85f, 0.95f, 1f);
            if (GUILayout.Button($"➕ Chèn Tầng Sau Tầng {node.FloorNumber}", GUILayout.Height(26)))
            {
                Undo.RecordObject(_mapConfig, $"Insert Floor After {node.FloorNumber}");
                _mapConfig.InsertFloorAfter(node.FloorNumber);
                _mapConfig.AutoLayoutNodes();
                EditorUtility.SetDirty(_mapConfig);
                AssetDatabase.SaveAssets();
            }

            using (new EditorGUI.DisabledScope(_mapConfig.TotalFloors <= 1))
            {
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button($"🗑️ Xóa Tầng {node.FloorNumber}", GUILayout.Width(120), GUILayout.Height(26)))
                {
                    if (EditorUtility.DisplayDialog("Xóa Tầng", $"Bạn có chắc muốn xóa toàn bộ Tầng {node.FloorNumber}?\nCác cổng nối từ tầng trước sẽ tự động bắc cầu sang tầng sau.", "Xóa", "Hủy"))
                    {
                        Undo.RecordObject(_mapConfig, $"Delete Floor {node.FloorNumber}");
                        _mapConfig.DeleteFloor(node.FloorNumber);
                        _mapConfig.AutoLayoutNodes();
                        _selectedNodeId = -1;
                        EditorUtility.SetDirty(_mapConfig);
                        AssetDatabase.SaveAssets();
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndScrollView();
                        EditorGUILayout.EndVertical();
                        GUILayout.EndArea();
                        return;
                    }
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // ==================== 4. CỔNG SANG TẦNG TIẾP THEO ====================
            DrawInspectorSectionHeader("4. CỔNG DẪN TIẾP (PORTAL NETWORK)", new Color(0.95f, 0.5f, 0.25f));
            DrawFloatingPortalsChecklist(node);

            // ==================== 5. CỔNG DẪN VÀO ====================
            DrawInspectorSectionHeader("5. CỔNG DẪN VÀO (INCOMING)", new Color(0.6f, 0.7f, 0.85f));
            DrawFloatingIncomingList(node);

            EditorGUIUtility.labelWidth = originalLabelWidth;
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
            GUILayout.EndArea();

            // Vẽ ký hiệu tay nắm co giãn ở góc dưới phải (Resize Gripper Grip)
            Handles.BeginGUI();
            Handles.color = new Color(0.6f, 0.65f, 0.75f, 0.7f);
            Handles.DrawLine(new Vector3(_floatingInspectorRect.xMax - 4, _floatingInspectorRect.yMax - 14, 0), new Vector3(_floatingInspectorRect.xMax - 14, _floatingInspectorRect.yMax - 4, 0));
            Handles.DrawLine(new Vector3(_floatingInspectorRect.xMax - 4, _floatingInspectorRect.yMax - 9, 0), new Vector3(_floatingInspectorRect.xMax - 9, _floatingInspectorRect.yMax - 4, 0));
            Handles.DrawLine(new Vector3(_floatingInspectorRect.xMax - 4, _floatingInspectorRect.yMax - 4, 0), new Vector3(_floatingInspectorRect.xMax - 4, _floatingInspectorRect.yMax - 4, 0));
            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private GameObject CreateAndAssignUniquePrefab(RunNodeData node)
        {
            GameObject basePrefab = null;
            string dbPath = "Assets/_Project/ScriptableObjects/Dungeon/RoomDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<RoomDatabaseSO>(dbPath);
            if (db != null)
            {
                var variation = db.GetRandomRoomForType(node.NodeType);
                if (variation != null && variation.RoomPrefab != null)
                {
                    basePrefab = variation.RoomPrefab;
                }
            }
            if (basePrefab == null)
            {
                basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Dungeon/Rooms/Start_Room.prefab");
            }

            if (basePrefab == null)
            {
                EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Prefab mẫu để nhân bản!", "OK");
                return null;
            }

            string folder = "Assets/_Project/Prefabs/Dungeon/Rooms/Custom";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Dungeon/Rooms"))
                {
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
                Undo.RecordObject(_mapConfig, $"Assign Unique Prefab to Node {node.Id}");
                node.SetCustomRoomPrefab(uniquePrefab);
                EditorUtility.SetDirty(_mapConfig);
            }
            return uniquePrefab;
        }

        private void DrawFloatingPortalsChecklist(RunNodeData node)
        {
            int outgoingCount = node.NextNodeIds?.Count ?? 0;
            EditorGUILayout.LabelField($"🚪 Cổng Dẫn Tiếp ({outgoingCount}/{RunMapConfigSO.MaxPortalsPerRoom} Cổng):", EditorStyles.miniBoldLabel);

            if (outgoingCount >= RunMapConfigSO.MaxPortalsPerRoom)
            {
                GUI.color = new Color(1f, 0.7f, 0.3f);
                EditorGUILayout.LabelField("⚠️ Đã đạt giới hạn tối đa 3 phòng", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            int nextFloor = node.FloorNumber + 1;
            var candidates = _mapConfig.GetNodesForFloor(nextFloor);

            if (candidates.Count > 0)
            {
                foreach (var cand in candidates)
                {
                    bool isConnected = node.NextNodeIds != null && node.NextNodeIds.Contains(cand.Id);
                    int slotIdx = isConnected ? node.NextNodeIds.IndexOf(cand.Id) : -1;
                    string portalBadge = isConnected ? $" ({RunMapConfigSO.GetPhysicalPortalName(slotIdx, node.NextNodeIds.Count)})" : "";

                    EditorGUI.BeginChangeCheck();
                    bool canToggleOn = isConnected || (node.NextNodeIds != null && node.NextNodeIds.Count < RunMapConfigSO.MaxPortalsPerRoom);

                    using (new EditorGUI.DisabledScope(!canToggleOn))
                    {
                        string icon = GetNodeTypeIcon(cand.NodeType);
                        string toggleLabel = $"{icon} [#{cand.Id}] {cand.DisplayName}{portalBadge}";
                        bool newConnected = EditorGUILayout.ToggleLeft(toggleLabel, isConnected, EditorStyles.label);

                        if (EditorGUI.EndChangeCheck())
                        {
                            if (newConnected && !isConnected)
                            {
                                Undo.RecordObject(_mapConfig, $"Connect Node {node.Id} to {cand.Id}");
                                _mapConfig.ConnectNodes(node.Id, cand.Id);
                                EditorUtility.SetDirty(_mapConfig);
                            }
                            else if (!newConnected && isConnected)
                            {
                                Undo.RecordObject(_mapConfig, $"Disconnect Node {node.Id} to {cand.Id}");
                                _mapConfig.DisconnectNodes(node.Id, cand.Id);
                                EditorUtility.SetDirty(_mapConfig);
                            }
                        }
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField($"   (Chưa có phòng ở Tầng {nextFloor})", EditorStyles.miniLabel);
            }

            // Nút nối sang phòng bất kỳ khác tầng
            using (new EditorGUI.DisabledScope(outgoingCount >= RunMapConfigSO.MaxPortalsPerRoom))
            {
                GUI.backgroundColor = new Color(0.85f, 0.95f, 1f);
                string btnLabel = outgoingCount >= RunMapConfigSO.MaxPortalsPerRoom ? "+ Đã Đủ 3 Cổng (Tối Đa)" : "+ Nối Sang Phòng Bất Kỳ...";
                if (GUILayout.Button(btnLabel, EditorStyles.miniButton))
                {
                    ShowAnyNodeConnectMenu(node);
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space(2);
        }

        private void DrawFloatingIncomingList(RunNodeData node)
        {
            var incoming = _mapConfig.GetIncomingNodes(node.Id);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("⬅ Cổng vào từ:", EditorStyles.miniBoldLabel, GUILayout.Width(95));
            if (incoming.Count > 0)
            {
                foreach (var inc in incoming)
                {
                    if (GUILayout.Button($"[#{inc.Id}]", EditorStyles.miniButton, GUILayout.Width(45)))
                    {
                        _selectedNodeId = inc.Id;
                    }
                }
            }
            else if (node.FloorNumber > 1)
            {
                GUI.color = new Color(1f, 0.5f, 0.2f);
                EditorGUILayout.LabelField("⚠️ Chưa có cổng vào", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(0.3f, 1f, 0.5f);
                EditorGUILayout.LabelField("🚩 Điểm bắt đầu (Root)", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ShowAnyNodeConnectMenu(RunNodeData fromNode)
        {
            if (fromNode.NextNodeIds != null && fromNode.NextNodeIds.Count >= RunMapConfigSO.MaxPortalsPerRoom)
            {
                EditorUtility.DisplayDialog("Đã Đủ Giới Hạn", $"Phòng [#{fromNode.Id}] đã nối tối đa {RunMapConfigSO.MaxPortalsPerRoom} phòng. Không thể nối thêm phòng mới!", "OK");
                return;
            }

            GenericMenu menu = new GenericMenu();
            foreach (var n in _mapConfig.AllNodes)
            {
                if (n.Id == fromNode.Id) continue;
                int targetId = n.Id;
                string label = $"Tầng {n.FloorNumber:00}/[#{n.Id}] {n.DisplayName} ({n.BranchName})";

                if (fromNode.NextNodeIds != null && fromNode.NextNodeIds.Contains(targetId))
                {
                    menu.AddDisabledItem(new GUIContent(label + " [Đã nối]"));
                }
                else if (!_mapConfig.CanConnect(fromNode.Id, targetId, out string reason))
                {
                    menu.AddDisabledItem(new GUIContent(label + $" [{reason}]"));
                }
                else
                {
                    menu.AddItem(new GUIContent(label), false, () =>
                    {
                        Undo.RecordObject(_mapConfig, $"Connect Node {fromNode.Id} to {targetId}");
                        _mapConfig.ConnectNodes(fromNode.Id, targetId);
                        EditorUtility.SetDirty(_mapConfig);
                    });
                }
            }
            menu.ShowAsContext();
        }

        // =========================================================================
        // 8. KIỂM TRA TOÀN VẸN ĐỒ THỊ
        // =========================================================================

        private void RunIntegrityValidation()
        {
            var allIds = new HashSet<int>();
            int total = _mapConfig.AllNodes.Count;
            List<string> issues = new List<string>();
            List<string> warnings = new List<string>();

            for (int i = 0; i < total; i++)
            {
                var n = _mapConfig.AllNodes[i];
                if (!allIds.Add(n.Id))
                {
                    issues.Add($"• Trùng lặp ID #{n.Id}!");
                }
            }

            for (int i = 0; i < total; i++)
            {
                var n = _mapConfig.AllNodes[i];
                if (n.FloorNumber < _mapConfig.TotalFloors && (n.NextNodeIds == null || n.NextNodeIds.Count == 0))
                {
                    issues.Add($"• [Ngõ Cụt] Node #{n.Id} (Tầng {n.FloorNumber}) chưa có Cổng nối tiếp!");
                }

                // Kiểm tra ràng buộc nghiêm ngặt: 1 phòng chỉ được nối tối đa 3 phòng
                if (n.NextNodeIds != null && n.NextNodeIds.Count > RunMapConfigSO.MaxPortalsPerRoom)
                {
                    issues.Add($"• [Vi Phạm Giới Hạn Cổng] Node #{n.Id} (Tầng {n.FloorNumber}) đang nối tới {n.NextNodeIds.Count} phòng (Vượt quá ràng buộc tối đa {RunMapConfigSO.MaxPortalsPerRoom} phòng)!");
                }

                if (n.FloorNumber > 1)
                {
                    var inc = _mapConfig.GetIncomingNodes(n.Id);
                    if (inc.Count == 0)
                    {
                        issues.Add($"• [Không thể đến] Node #{n.Id} (Tầng {n.FloorNumber}) chưa có cổng từ tầng trước dẫn vào!");
                    }
                }
            }

            _showValidationBox = true;
            if (issues.Count == 0 && warnings.Count == 0)
            {
                _validationSuccess = true;
                _validationReport = $"🎉 ĐỒ THỊ HOÀN HẢO! Toàn bộ {total} Nút liên thông 100%, không có ngõ cụt và không có phòng chết.";
            }
            else
            {
                _validationSuccess = (issues.Count == 0);
                string report = "";
                if (issues.Count > 0) report += "❌ LỖI CẦN SỬA:\n" + string.Join("\n", issues);
                if (warnings.Count > 0)
                {
                    if (report.Length > 0) report += "\n\n";
                    report += "⚠️ LƯU Ý:\n" + string.Join("\n", warnings);
                }
                _validationReport = report;
            }
        }

        private void DrawValidationBoxOverlay()
        {
            Rect boxRect = new Rect(position.width * 0.5f - 240, 45, 480, 160);
            GUILayout.BeginArea(boxRect, GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🔍 BÁO CÁO TOÀN VẸN BẢN ĐỒ", EditorStyles.boldLabel);
            if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
            {
                _showValidationBox = false;
            }
            EditorGUILayout.EndHorizontal();

            MessageType msgType = _validationSuccess ? MessageType.Info : MessageType.Warning;
            EditorGUILayout.HelpBox(_validationReport, msgType);

            GUILayout.EndArea();
        }

        private void DrawEmptyConfigState()
        {
            GUILayout.BeginArea(new Rect(position.width * 0.25f, position.height * 0.35f, position.width * 0.5f, 160), GUI.skin.box);
            EditorGUILayout.LabelField("⚠️ Chưa có RunMapConfigSO nào được chọn!", EditorStyles.boldLabel);
            EditorGUILayout.Space(8);

            _mapConfig = (RunMapConfigSO)EditorGUILayout.ObjectField("Chọn Cấu Hình:", _mapConfig, typeof(RunMapConfigSO), false);

            if (GUILayout.Button("Tự Động Nạp RunMap_13Floors.asset", GUILayout.Height(30)))
            {
                _mapConfig = AssetDatabase.LoadAssetAtPath<RunMapConfigSO>("Assets/_Project/ScriptableObjects/Dungeon/RunMap_13Floors.asset");
            }
            GUILayout.EndArea();
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
    }
}
#endif
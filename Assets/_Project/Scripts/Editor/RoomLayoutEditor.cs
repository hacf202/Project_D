#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.Tilemaps;
using Project_D.Dungeon.Room;
using Project_D.Dungeon.Run;
using Project_D.Dungeon.Interactions;

namespace Project_D.EditorTools
{
    public enum PlacementTool
    {
        None,
        PlaceFloorTile,         // 🟫 Chấm gạch sàn (kéo chuột để lát sàn)
        PlaceObstacleBlock,     // 🧱 Chấm khối tường/vật cản vuông 1x1
        PlaceObstaclePillar,    // ⚪ Chấm trụ đá tròn
        SetPlayerSpawn,         // 🟢 Chấm vị trí Player xuất hiện
        AddEnemySpawn,          // 🔴 Chấm điểm spawn quái chiến thuật
        AddPortal,              // 🔵 Chấm cổng dịch chuyển tầng sau
        PlaceBuffAltar,         // 🔮 Chấm bệ ngọc ban phước (Buff)
        PlaceRestCampfire,      // 🔥 Chấm đống lửa trại (Hồi máu)
        PlaceShopChest,         // 🪙 Chấm rương báu / cửa hàng (Shop)
        PlaceCustomEntity,      // ⭐ Chấm Prefab tùy chọn bất kỳ (bẫy, thùng, quái, đuốc...)
        Eraser                  // ❌ Cục tẩy xóa vật thể click trúng
    }

    [CustomEditor(typeof(RoomLayout))]
    public class RoomLayoutEditor : Editor
    {
        private RoomLayout _layout;

        // Tool settings
        private static PlacementTool _currentTool = PlacementTool.None;
        private static float _gridSnap = 1.0f; // 0 = None, 0.5, 1.0, 2.0
        private static bool _showSceneToolbar = true;

        // Configurable Asset Palette
        private static Sprite _customFloorSprite;
        private static Color _floorColor = new Color(0.12f, 0.13f, 0.16f, 1f);
        private static Sprite _customWallSprite;
        private static Color _wallColor = new Color(0.28f, 0.32f, 0.42f, 1f);
        private static GameObject _customEntityPrefab;

        private void OnEnable()
        {
            _layout = (RoomLayout)target;
        }

        // =========================================================================
        // SCENE VIEW INTERACTIVE PAINTER & PLACEMENT ENGINE
        // =========================================================================

        private void OnSceneGUI()
        {
            // Đã lược bỏ bảng bút nổi trên Scene View theo yêu cầu để người dùng tập trung 100% vẽ bằng Tile Palette.
        }

        private void DrawSceneViewFloatingToolbar()
        {
            Handles.BeginGUI();

            GUILayout.BeginArea(new Rect(12, 12, 250, _showSceneToolbar ? 440 : 36), "🎨 BỘ BÚT CHẤM ĐIỂM SCENE", GUI.skin.window);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(_showSceneToolbar ? "Chọn bút và click vào Scene:" : "Bút vẽ Scene (Đang ẩn)", EditorStyles.miniBoldLabel);
            if (GUILayout.Button(_showSceneToolbar ? "▲" : "▼", GUILayout.Width(24), GUILayout.Height(18)))
            {
                _showSceneToolbar = !_showSceneToolbar;
            }
            EditorGUILayout.EndHorizontal();

            if (_showSceneToolbar)
            {
                EditorGUILayout.Space(2);

                // Tool Buttons
                DrawToolButton("🖐️ Tắt Bút (Chọn bình thường)", PlacementTool.None, Color.white);
                DrawToolButton("🟫 Chấm Sàn Đất/Gạch (Kéo rê để lát)", PlacementTool.PlaceFloorTile, new Color(0.6f, 0.5f, 0.4f));
                DrawToolButton("🧱 Chấm Tường/Đá Vuông (Kéo để vẽ)", PlacementTool.PlaceObstacleBlock, new Color(0.7f, 0.8f, 0.9f));
                DrawToolButton("⚪ Chấm Trụ Đá Tròn", PlacementTool.PlaceObstaclePillar, new Color(0.7f, 0.8f, 0.9f));
                DrawToolButton("🟢 Chấm Vị Trí Player", PlacementTool.SetPlayerSpawn, new Color(0.4f, 1f, 0.4f));
                DrawToolButton("🔴 Chấm Điểm Quái Spawn", PlacementTool.AddEnemySpawn, new Color(1f, 0.45f, 0.45f));
                DrawToolButton("🔵 Chấm Cổng Dịch Chuyển", PlacementTool.AddPortal, new Color(0.3f, 0.9f, 1f));
                DrawToolButton("🔮 Chấm Bệ Thờ Buff (Altar)", PlacementTool.PlaceBuffAltar, new Color(1f, 0.85f, 0.3f));
                DrawToolButton("🔥 Chấm Lửa Trại (Campfire)", PlacementTool.PlaceRestCampfire, new Color(1f, 0.6f, 0.2f));
                DrawToolButton("🪙 Chấm Rương Mua Đồ (Shop)", PlacementTool.PlaceShopChest, new Color(1f, 0.9f, 0.4f));
                DrawToolButton("⭐ Chấm Prefab Tự Chọn (Entity)", PlacementTool.PlaceCustomEntity, new Color(0.9f, 0.5f, 1f));
                DrawToolButton("❌ Cục Tẩy (Xóa vật thể click trúng)", PlacementTool.Eraser, new Color(1f, 0.3f, 0.3f));

                EditorGUILayout.Space(4);

                // Grid Snapping
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Snap:", GUILayout.Width(40));
                if (GUILayout.Toggle(_gridSnap == 0f, "Tự do", "Button", GUILayout.Width(45))) _gridSnap = 0f;
                if (GUILayout.Toggle(_gridSnap == 0.5f, "0.5m", "Button", GUILayout.Width(45))) _gridSnap = 0.5f;
                if (GUILayout.Toggle(_gridSnap == 1.0f, "1.0m", "Button", GUILayout.Width(45))) _gridSnap = 1.0f;
                if (GUILayout.Toggle(_gridSnap == 2.0f, "2.0m", "Button", GUILayout.Width(45))) _gridSnap = 2.0f;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(2);
                EditorGUILayout.HelpBox("Mẹo: Click Chuột Trái để chấm đặt.\nClick Chuột Phải để hủy chọn bút.", MessageType.None);
            }

            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private void DrawToolButton(string label, PlacementTool tool, Color activeCol)
        {
            bool isSelected = _currentTool == tool;
            if (isSelected) GUI.backgroundColor = activeCol;

            if (GUILayout.Button(label, GUILayout.Height(21)))
            {
                _currentTool = isSelected ? PlacementTool.None : tool;
            }

            GUI.backgroundColor = Color.white;
        }

        private void DrawGhostPreview(Vector3 pos)
        {
            switch (_currentTool)
            {
                case PlacementTool.PlaceFloorTile:
                    Handles.color = new Color(0.8f, 0.6f, 0.4f, 0.6f);
                    Handles.DrawWireCube(pos, Vector3.one);
                    break;
                case PlacementTool.PlaceObstacleBlock:
                    Handles.color = new Color(0.3f, 0.8f, 1f, 0.8f);
                    Handles.DrawWireCube(pos, Vector3.one);
                    break;
                case PlacementTool.PlaceObstaclePillar:
                    Handles.color = new Color(0.3f, 0.8f, 1f, 0.8f);
                    Handles.DrawWireDisc(pos, Vector3.forward, 0.6f);
                    break;
                case PlacementTool.SetPlayerSpawn:
                    Handles.color = Color.green;
                    Handles.DrawWireDisc(pos, Vector3.forward, 0.8f);
                    Handles.DrawLine(pos + Vector3.left * 1.2f, pos + Vector3.right * 1.2f);
                    Handles.DrawLine(pos + Vector3.down * 1.2f, pos + Vector3.up * 1.2f);
                    Handles.Label(pos + Vector3.up * 0.9f, "[Player Start]");
                    break;
                case PlacementTool.AddEnemySpawn:
                    Handles.color = Color.red;
                    Handles.DrawWireDisc(pos, Vector3.forward, 0.6f);
                    Handles.Label(pos + Vector3.up * 0.7f, "[+ Quái Spawn]");
                    break;
                case PlacementTool.AddPortal:
                    Handles.color = Color.cyan;
                    Handles.DrawWireDisc(pos, Vector3.forward, 1.2f);
                    Handles.Label(pos + Vector3.up * 1.4f, "[+ Cổng Dịch Chuyển]");
                    break;
                case PlacementTool.PlaceBuffAltar:
                case PlacementTool.PlaceRestCampfire:
                case PlacementTool.PlaceShopChest:
                    Handles.color = Color.yellow;
                    Handles.DrawWireCube(pos, Vector3.one * 1.5f);
                    Handles.Label(pos + Vector3.up * 1.1f, $"[+ {_currentTool}]");
                    break;
                case PlacementTool.PlaceCustomEntity:
                    Handles.color = new Color(0.8f, 0.4f, 1f, 0.9f);
                    Handles.DrawWireCube(pos, Vector3.one * 1.2f);
                    string name = _customEntityPrefab != null ? _customEntityPrefab.name : "Kéo Prefab vào Inspector";
                    Handles.Label(pos + Vector3.up * 1.1f, $"[+ {name}]");
                    break;
                case PlacementTool.Eraser:
                    Handles.color = Color.red;
                    Handles.DrawWireDisc(pos, Vector3.forward, 0.8f);
                    Handles.DrawLine(pos - new Vector3(0.5f, 0.5f, 0), pos + new Vector3(0.5f, 0.5f, 0));
                    Handles.DrawLine(pos - new Vector3(0.5f, -0.5f, 0), pos + new Vector3(0.5f, -0.5f, 0));
                    break;
            }
        }

        private void ApplyPlacement(Vector3 pos)
        {
            switch (_currentTool)
            {
                case PlacementTool.PlaceFloorTile:
                    CreateFloorTileAt(pos);
                    break;
                case PlacementTool.PlaceObstacleBlock:
                    CreateObstacleAt(pos, true);
                    break;
                case PlacementTool.PlaceObstaclePillar:
                    CreateObstacleAt(pos, false);
                    break;
                case PlacementTool.SetPlayerSpawn:
                    SetPlayerSpawnAt(pos);
                    break;
                case PlacementTool.AddEnemySpawn:
                    AddEnemySpawnAt(pos);
                    break;
                case PlacementTool.AddPortal:
                    AddPortalAt(pos);
                    break;
                case PlacementTool.PlaceBuffAltar:
                    CreateInteractionAt(pos, InteractionType.BuffReward);
                    break;
                case PlacementTool.PlaceRestCampfire:
                    CreateInteractionAt(pos, InteractionType.RestSite);
                    break;
                case PlacementTool.PlaceShopChest:
                    CreateInteractionAt(pos, InteractionType.Shop);
                    break;
                case PlacementTool.PlaceCustomEntity:
                    CreateCustomEntityAt(pos);
                    break;
                case PlacementTool.Eraser:
                    EraseAt(pos);
                    break;
            }

            _layout.AutoBindReferences();
            EditorUtility.SetDirty(_layout);
        }

        private void CreateFloorTileAt(Vector3 pos)
        {
            Transform parent = _layout.FloorRoot;
            if (parent == null)
            {
                var fGo = new GameObject("Floor");
                fGo.transform.SetParent(_layout.transform, false);
                parent = fGo.transform;
            }

            foreach (Transform child in parent)
            {
                if (Vector3.Distance(child.position, pos) < 0.2f) return;
            }

            int count = parent.childCount + 1;
            var tileGO = new GameObject($"Floor_Tile_{count}");
            tileGO.transform.SetParent(parent, false);
            tileGO.transform.position = pos;

            var sr = tileGO.AddComponent<SpriteRenderer>();
            sr.sprite = _customFloorSprite != null ? _customFloorSprite : GetSquareSprite();
            sr.color = _floorColor;
            sr.sortingOrder = -10; // Luôn nằm dưới cùng của phòng
            tileGO.transform.localScale = Vector3.one;

            Undo.RegisterCreatedObjectUndo(tileGO, "Place Floor Tile");
        }

        private void CreateObstacleAt(Vector3 pos, bool isBox)
        {
            Transform parent = _layout.ObstaclesRoot;
            if (parent == null)
            {
                var oGo = new GameObject("Obstacles");
                oGo.transform.SetParent(_layout.transform, false);
                parent = oGo.transform;
            }

            foreach (Transform child in parent)
            {
                if (Vector3.Distance(child.position, pos) < 0.2f) return;
            }

            int count = parent.childCount + 1;
            string prefix = isBox ? "Obstacle_Block" : "Obstacle_Pillar";
            var obsGO = new GameObject($"{prefix}_{count}");
            obsGO.transform.SetParent(parent, false);
            obsGO.transform.position = pos;
            obsGO.layer = GetObstacleLayer();

            var sr = obsGO.AddComponent<SpriteRenderer>();
            sr.sprite = _customWallSprite != null ? _customWallSprite : GetSquareSprite();
            sr.color = _wallColor;
            sr.sortingOrder = 1;

            if (isBox)
            {
                var col = obsGO.AddComponent<BoxCollider2D>();
                col.size = Vector2.one;
                obsGO.transform.localScale = Vector3.one;
            }
            else
            {
                var col = obsGO.AddComponent<CircleCollider2D>();
                col.radius = 0.5f;
                obsGO.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            }

            Undo.RegisterCreatedObjectUndo(obsGO, "Place Obstacle Dot");
        }

        private void CreateCustomEntityAt(Vector3 pos)
        {
            if (_customEntityPrefab == null)
            {
                Debug.LogWarning("<color=orange>[ROOM LEVEL DESIGN]</color> Hãy kéo một Prefab vào ô 'Prefab Tự Chọn (Entity)' trên Inspector trước khi chấm!");
                return;
            }

            Transform parent = _layout.transform.Find("Entities");
            if (parent == null)
            {
                var eGo = new GameObject("Entities");
                eGo.transform.SetParent(_layout.transform, false);
                parent = eGo.transform;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(_customEntityPrefab, parent);
            if (instance == null)
            {
                instance = Instantiate(_customEntityPrefab, parent);
            }
            instance.transform.position = pos;
            Undo.RegisterCreatedObjectUndo(instance, $"Place {_customEntityPrefab.name}");
        }

        private void SetPlayerSpawnAt(Vector3 pos)
        {
            Transform spawn = _layout.PlayerSpawnPoint;
            if (spawn == null || spawn == _layout.transform)
            {
                var spawnRoot = _layout.transform.Find("SpawnPoints");
                if (spawnRoot == null)
                {
                    var sGo = new GameObject("SpawnPoints");
                    sGo.transform.SetParent(_layout.transform, false);
                    spawnRoot = sGo.transform;
                }
                var pGo = new GameObject("PlayerSpawnPoint");
                pGo.transform.SetParent(spawnRoot, false);
                pGo.transform.position = pos;
                Undo.RegisterCreatedObjectUndo(pGo, "Set Player Spawn");
            }
            else
            {
                Undo.RecordObject(spawn, "Move Player Spawn");
                spawn.position = pos;
            }
        }

        private void AddEnemySpawnAt(Vector3 pos)
        {
            Transform parent = _layout.transform.Find("SpawnPoints/EnemySpawns") ?? _layout.transform.Find("EnemySpawns");
            if (parent == null)
            {
                var spawnRoot = _layout.transform.Find("SpawnPoints");
                if (spawnRoot == null)
                {
                    var sGo = new GameObject("SpawnPoints");
                    sGo.transform.SetParent(_layout.transform, false);
                    spawnRoot = sGo.transform;
                }
                var esGo = new GameObject("EnemySpawns");
                esGo.transform.SetParent(spawnRoot, false);
                parent = esGo.transform;
            }

            int count = parent.childCount + 1;
            var pt = new GameObject($"EnemySpawn_{count}");
            pt.transform.SetParent(parent, false);
            pt.transform.position = pos;

            Undo.RegisterCreatedObjectUndo(pt, "Add Enemy Spawn Point");
        }

        private void AddPortalAt(Vector3 pos)
        {
            Transform parent = _layout.transform.Find("Portals");
            if (parent == null)
            {
                var pGo = new GameObject("Portals");
                pGo.transform.SetParent(_layout.transform, false);
                parent = pGo.transform;
            }

            int count = parent.childCount + 1;
            var portalGO = CreatePortalGameObject($"Portal_{count}", parent, Vector3.zero);
            portalGO.transform.position = pos;

            Undo.RegisterCreatedObjectUndo(portalGO, "Add Portal");
        }

        private void CreateInteractionAt(Vector3 pos, InteractionType type)
        {
            Transform parent = _layout.transform.Find("Interactions");
            if (parent == null)
            {
                var iGo = new GameObject("Interactions");
                iGo.transform.SetParent(_layout.transform, false);
                parent = iGo.transform;
            }

            string name = type switch
            {
                InteractionType.BuffReward => "Buff_Altar",
                InteractionType.Shop => "Shop_Chest",
                _ => "RestSite_Campfire"
            };

            var pointGO = new GameObject($"{name}_{parent.childCount + 1}");
            pointGO.transform.SetParent(parent, false);
            pointGO.transform.position = pos;

            var col = pointGO.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1.2f;

            var sr = pointGO.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;

            var comp = pointGO.AddComponent<InteractionPoint>();
            var so = new SerializedObject(comp);
            so.FindProperty("_type").enumValueIndex = (int)type;

            switch (type)
            {
                case InteractionType.BuffReward:
                    sr.sprite = GetOrbPedestalSprite();
                    so.FindProperty("_promptText").stringValue = "Chạm để Nhận Buff";
                    break;
                case InteractionType.Shop:
                    sr.sprite = GetChestSprite();
                    so.FindProperty("_promptText").stringValue = "Chạm để Mua Sắm";
                    break;
                case InteractionType.RestSite:
                    sr.sprite = GetBrazierSprite();
                    so.FindProperty("_promptText").stringValue = "Chạm để Nghỉ Ngơi";
                    break;
            }
            so.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(pointGO, $"Place {type}");
        }

        private void EraseAt(Vector3 pos)
        {
            var hits = Physics2D.OverlapCircleAll(pos, 0.8f);
            foreach (var hit in hits)
            {
                if (hit != null && hit.transform.IsChildOf(_layout.transform) && hit.transform != _layout.transform)
                {
                    Undo.DestroyObjectImmediate(hit.gameObject);
                    return;
                }
            }

            foreach (Transform child in _layout.GetComponentsInChildren<Transform>(true))
            {
                if (child != _layout.transform && Vector3.Distance(child.position, pos) < 0.8f)
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                    return;
                }
            }
        }

        // =========================================================================
        // INSPECTOR INTERFACE
        // =========================================================================

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeaderCard();
            EditorGUILayout.Space(8);

            DrawValidationCard();
            EditorGUILayout.Space(8);

            DrawTilemapIntegrationSection();
            EditorGUILayout.Space(8);

            DrawAssetPaletteSection();
            EditorGUILayout.Space(8);

            DrawLevelDesignToolbar();
            EditorGUILayout.Space(10);

            DrawDefaultFields();
            EditorGUILayout.Space(12);

            DrawExportSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeaderCard()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🏛️ BẢNG ĐIỀU KHIỂN THIẾT KẾ PHÒNG (LEVEL DESIGN)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("• Tích hợp Tilemap (Rule Tile) & Bút vẽ Scene View chấm điểm tự do.", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawTilemapIntegrationSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🗺️ TÍCH HỢP TILEMAP VÀO PHÒNG (RULE TILE / SÀN / TƯỜNG):", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "TẤT CẢ TILEMAP NẰM TRONG PHÒNG SẼ ĐƯỢC TỰ ĐỘNG ĐÓNG GÓI VÀO PREFAB KHI XUẤT BẢN!\n" +
                "• Bấm 'Vẽ TƯỜNG': Sẽ tự chọn Layer Obstacle và mở Palette để bạn quét Rule Tile.\n" +
                "• Bấm 'Gom Grid Ngoài Scene': Nếu bạn lỡ vẽ Tilemap ở ngoài Scene, nút này sẽ gom ngay vào phòng!",
                MessageType.Info);

            var wallTilemap = _layout.transform.Find("Grid/Tilemap_Walls") ?? _layout.transform.Find("Tilemap_Walls");
            var floorTilemap = _layout.transform.Find("Grid/Tilemap_Floor") ?? _layout.transform.Find("Tilemap_Floor");

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.3f, 0.85f, 1f);
            if (GUILayout.Button("🖌️ Bắt Đầu Vẽ TƯỜNG (Rule Tile)", GUILayout.Height(32)))
            {
                if (wallTilemap == null) EnsureTilemapStructure();
                wallTilemap = _layout.transform.Find("Grid/Tilemap_Walls") ?? _layout.transform.Find("Tilemap_Walls");
                if (wallTilemap != null)
                {
                    Selection.activeGameObject = wallTilemap.gameObject;
                    EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
                }
            }

            GUI.backgroundColor = new Color(0.9f, 0.7f, 0.4f);
            if (GUILayout.Button("🖌️ Bắt Đầu Vẽ SÀN (Tile Palette)", GUILayout.Height(32)))
            {
                if (floorTilemap == null) EnsureTilemapStructure();
                floorTilemap = _layout.transform.Find("Grid/Tilemap_Floor") ?? _layout.transform.Find("Tilemap_Floor");
                if (floorTilemap != null)
                {
                    Selection.activeGameObject = floorTilemap.gameObject;
                    EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);
            GUI.backgroundColor = new Color(0.85f, 0.95f, 0.4f);
            if (GUILayout.Button("🔗 Gom Toàn Bộ Grid / Tilemap Vừa Vẽ Ngoài Scene Vào Phòng Này", GUILayout.Height(28)))
            {
                AdoptExternalTilemaps();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        private void EnsureTilemapStructure()
        {
            SetupTilemapUnderRoom(_layout.transform);
            _layout.AutoBindReferences();
            EditorUtility.SetDirty(_layout);
        }

        public static void SetupTilemapUnderRoom(Transform roomTransform)
        {
            var gridTr = roomTransform.Find("Grid");
            if (gridTr == null)
            {
                var gGo = new GameObject("Grid");
                gGo.transform.SetParent(roomTransform, false);
                var g = gGo.AddComponent<Grid>();
                g.cellSize = new Vector3(1f, 1f, 0f);
                gridTr = gGo.transform;
            }

            var floorTm = gridTr.Find("Tilemap_Floor");
            if (floorTm == null)
            {
                var fGo = new GameObject("Tilemap_Floor");
                fGo.transform.SetParent(gridTr, false);
                fGo.AddComponent<UnityEngine.Tilemaps.Tilemap>();
                var r = fGo.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
                r.sortingOrder = -10;
            }

            var wallTm = gridTr.Find("Tilemap_Walls");
            if (wallTm == null)
            {
                var wGo = new GameObject("Tilemap_Walls");
                wGo.transform.SetParent(gridTr, false);
                wGo.layer = GetObstacleLayer();
                wGo.AddComponent<UnityEngine.Tilemaps.Tilemap>();
                var r = wGo.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
                r.sortingOrder = 1;
                wGo.AddComponent<TilemapCollider2D>();
            }
        }

        private void AdoptExternalTilemaps()
        {
            var grids = FindObjectsByType<Grid>();
            int count = 0;
            foreach (var g in grids)
            {
                if (g != null && !g.transform.IsChildOf(_layout.transform) && g.transform != _layout.transform)
                {
                    Undo.SetTransformParent(g.transform, _layout.transform, "Adopt Grid into Room");
                    g.transform.localPosition = Vector3.zero;
                    count++;

                    var allTilemaps = g.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(true);
                    foreach (var tm in allTilemaps)
                    {
                        bool isWall = tm.name.IndexOf("Wall", StringComparison.OrdinalIgnoreCase) >= 0
                                      || tm.GetComponent<TilemapCollider2D>() != null
                                      || (allTilemaps.Length == 1 && tm.name.IndexOf("Floor", StringComparison.OrdinalIgnoreCase) < 0);

                        if (isWall)
                        {
                            tm.gameObject.layer = GetObstacleLayer();
                            if (tm.name == "Tilemap") tm.name = "Tilemap_Walls";
                            if (tm.GetComponent<TilemapCollider2D>() == null)
                            {
                                tm.gameObject.AddComponent<TilemapCollider2D>();
                            }
                            var tr = tm.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();
                            if (tr != null && tr.sortingOrder == 0) tr.sortingOrder = 1;
                        }
                        else
                        {
                            if (tm.name == "Tilemap") tm.name = "Tilemap_Floor";
                            var tr = tm.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();
                            if (tr != null && tr.sortingOrder == 0) tr.sortingOrder = -10;
                        }
                    }
                }
            }

            _layout.AutoBindReferences();
            _layout.CalculateBoundsFromChildren();
            EditorUtility.SetDirty(_layout);

            if (count > 0)
            {
                EditorUtility.DisplayDialog("Gom Tilemap Thành Công 🎉",
                    $"Đã tự động gom {count} Grid/Tilemap trong Scene vào phòng '{_layout.gameObject.name}'!\n" +
                    $"• Đã tự động gán Layer 'Obstacle' và TilemapCollider2D cho tường.\n" +
                    $"• Đã tự động cập nhật kích thước phòng.\n\n" +
                    $"Bây giờ khi bạn bấm 'Xuất Bản Thành Prefab', toàn bộ Tilemap này sẽ được lưu chung vào Prefab của phòng!", "Tuyệt Vời");
            }
            else
            {
                EnsureTilemapStructure();
                EditorUtility.DisplayDialog("Đã Tạo Sẵn Grid Trong Phòng",
                    "Đã tạo sẵn cấu trúc Grid/Tilemap_Walls & Tilemap_Floor ngay bên trong phòng!\n" +
                    "Bạn chỉ cần chọn cọ để vẽ trực tiếp vào phòng.", "OK");
            }
        }

        private void DrawValidationCard()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Kiểm Tra Cấu Trúc Phòng Hiện Tại:", EditorStyles.boldLabel);

            // 1. Player Spawn Check
            if (_layout.PlayerSpawnPoint != null && _layout.PlayerSpawnPoint != _layout.transform)
            {
                GUI.color = new Color(0.3f, 1f, 0.4f);
                EditorGUILayout.LabelField("  ✓ Player Spawn: Đã có tại " + _layout.PlayerSpawnPoint.localPosition);
            }
            else
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                EditorGUILayout.LabelField("  ✗ CẢNH BÁO: Chưa có PlayerSpawnPoint (Dùng bút 🟢 chấm vào Scene)!");
            }
            GUI.color = Color.white;

            // 2. Enemy Spawns Check
            int enemyCount = _layout.EnemySpawnPoints != null ? _layout.EnemySpawnPoints.Length : 0;
            if (IsCombatRoom(_layout.SupportedType))
            {
                if (enemyCount > 0)
                {
                    GUI.color = new Color(0.3f, 1f, 0.4f);
                    EditorGUILayout.LabelField($"  ✓ Quái Spawn: Đã chấm {enemyCount} điểm chiến thuật.");
                }
                else
                {
                    GUI.color = new Color(1f, 0.6f, 0.2f);
                    EditorGUILayout.LabelField("  ⚠ CHÚ Ý: Chưa có điểm quái spawn (Dùng bút 🔴 chấm vào Scene)!");
                }
            }
            else
            {
                GUI.color = Color.gray;
                EditorGUILayout.LabelField("  - Phòng Phi Chiến Đấu (Không cần quái spawn)");
            }
            GUI.color = Color.white;

            // 3. Portals Check
            int portalCount = _layout.Portals != null ? _layout.Portals.Length : 0;
            if (portalCount > 0)
            {
                GUI.color = new Color(0.3f, 1f, 0.4f);
                EditorGUILayout.LabelField($"  ✓ Cổng Dịch Chuyển: Có {portalCount} Cổng sẵn sàng.");
            }
            else
            {
                GUI.color = new Color(1f, 0.3f, 0.3f);
                EditorGUILayout.LabelField("  ✗ CẢNH BÁO: Chưa có Cổng dịch chuyển (Dùng bút 🔵 chấm vào Scene)!");
            }
            GUI.color = Color.white;

            // 4. Kích thước tự tính
            EditorGUILayout.LabelField($"  📏 Kích Thước Bounding Tự Đo: {_layout.RoomSize.x} x {_layout.RoomSize.y} m", EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }

        private void DrawAssetPaletteSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🎨 PALETTE TÀI NGUYÊN (KÉO THẢ ASSET CỦA BẠN VÀO ĐÂY)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Bạn có thể kéo bất kỳ Sprite/Prefab mới nào vào các ô dưới đây để bút vẽ sử dụng tài nguyên của bạn!", MessageType.Info);

            _customFloorSprite = (Sprite)EditorGUILayout.ObjectField("Sprite Sàn Nhà:", _customFloorSprite, typeof(Sprite), false);
            _floorColor = EditorGUILayout.ColorField("Màu Sàn:", _floorColor);

            EditorGUILayout.Space(2);
            _customWallSprite = (Sprite)EditorGUILayout.ObjectField("Sprite Tường/Đá:", _customWallSprite, typeof(Sprite), false);
            _wallColor = EditorGUILayout.ColorField("Màu Tường:", _wallColor);

            EditorGUILayout.Space(2);
            _customEntityPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab Tự Chọn (Bẫy/Thùng/Entity):", _customEntityPrefab, typeof(GameObject), false);
            EditorGUILayout.EndVertical();
        }

        private void DrawLevelDesignToolbar()
        {
            EditorGUILayout.LabelField("Thao Tác Nhanh:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🎯 Quét & Liên Kết Toàn Bộ", GUILayout.Height(28)))
            {
                Undo.RecordObject(_layout, "Auto Bind Room Layout");
                _layout.AutoBindReferences();
                EditorUtility.SetDirty(_layout);
            }

            if (GUILayout.Button("📐 Tính Kích Thước Bounding", GUILayout.Height(28)))
            {
                Undo.RecordObject(_layout, "Calculate Room Bounds");
                _layout.CalculateBoundsFromChildren();
                EditorUtility.SetDirty(_layout);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDefaultFields()
        {
            EditorGUILayout.LabelField("Thông Tin Định Danh & Cấu Hình:", EditorStyles.boldLabel);
            DrawDefaultInspector();
        }

        private void DrawExportSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("ĐÓNG GÓI VÀ ĐĂNG KÝ VÀO GAME:", EditorStyles.boldLabel);

            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.4f);
            if (GUILayout.Button("💾 XUẤT BẢN THÀNH PREFAB & ĐĂNG KÝ VÀO DATABASE", GUILayout.Height(40)))
            {
                ExportCurrentRoomToPrefabAndDatabase();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        private void ExportCurrentRoomToPrefabAndDatabase()
        {
            _layout.AutoBindReferences();
            _layout.CalculateBoundsFromChildren();

            if (_layout.PlayerSpawnPoint == null)
            {
                EditorUtility.DisplayDialog("Thiếu Player Spawn Point",
                    "Phòng này chưa có điểm xuất hiện của Player!\nHãy dùng bút 🟢 'Chấm Vị Trí Player' trên Scene View để đặt.", "OK");
                return;
            }

            if (_layout.Portals == null || _layout.Portals.Length == 0)
            {
                EditorUtility.DisplayDialog("Thiếu Cổng Dịch Chuyển",
                    "Phòng này chưa có Cổng dịch chuyển nào!\nHãy dùng bút 🔵 'Chấm Cổng Dịch Chuyển' trên Scene View để đặt cửa sang tầng sau.", "OK");
                return;
            }

            string cleanName = string.IsNullOrWhiteSpace(_layout.RoomName) ? _layout.gameObject.name : _layout.RoomName;
            cleanName = cleanName.Replace(" ", "_").Replace("[ActiveRoom]_", "").Replace("[CustomRoom]_", "");

            string prefabDir = "Assets/_Project/Prefabs/Dungeon/Rooms";
            string soDir = "Assets/_Project/ScriptableObjects/Dungeon/Rooms";

            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
            if (!Directory.Exists(soDir)) Directory.CreateDirectory(soDir);

            _layout.gameObject.name = cleanName;

            string prefabPath = $"{prefabDir}/{cleanName}.prefab";
            bool success;
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(_layout.gameObject, prefabPath, InteractionMode.UserAction, out success);

            if (!success || prefabAsset == null)
            {
                EditorUtility.DisplayDialog("Lỗi Xuất Bản", $"Không thể lưu Prefab tại đường dẫn:\n{prefabPath}", "OK");
                return;
            }

            string soPath = $"{soDir}/{cleanName}_Data.asset";
            var variationSO = AssetDatabase.LoadAssetAtPath<RoomVariationSO>(soPath);
            if (variationSO == null)
            {
                variationSO = CreateInstance<RoomVariationSO>();
                AssetDatabase.CreateAsset(variationSO, soPath);
            }

            variationSO.Initialize(cleanName, _layout.SupportedType, prefabAsset, 10, $"Phòng thiết kế tự do '{cleanName}'");
            EditorUtility.SetDirty(variationSO);

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

            Debug.Log($"<color=green><b>[ROOM LEVEL DESIGN]</b></color> Đã xuất bản thành công phòng '{cleanName}' vào Prefab & đăng ký Database!");

            EditorUtility.DisplayDialog("Xuất Bản Thành Công! 🎉",
                $"Phòng tự do '{cleanName}' của bạn đã được xuất bản hoàn chỉnh:\n\n" +
                $"• Prefab: {prefabPath}\n" +
                $"• Data SO: {soPath}\n" +
                $"• Đã tự động nạp vào RoomDatabase.asset!\n\n" +
                $"Bây giờ bạn có thể xóa GameObject tạm này trong Scene và bấm Play để thử nghiệm phòng mới!", "Tuyệt Vời!");

            EditorGUIUtility.PingObject(prefabAsset);
        }

        // =========================================================================
        // TẠO GRID ĐỂ BẮT ĐẦU VẼ BẰNG TILE PALETTE (THAY THẾ CHO CUSTOMROOM CŨ)
        // =========================================================================

        public static void CreateFreeformRoomCanvas()
        {
            Project_D.EditorTools.DungeonTilePaletteTools.CreateRoomGridCanvas();
        }

        public static void CreateCustomRoomCanvas()
        {
            Project_D.EditorTools.DungeonTilePaletteTools.CreateRoomGridCanvas();
        }

        // =========================================================================
        // HELPERS
        // =========================================================================

        public static GameObject CreatePortalGameObject(string name, Transform parent, Vector3 localPos)
        {
            var portalGO = new GameObject(name);
            portalGO.transform.SetParent(parent, false);
            portalGO.transform.localPosition = localPos;

            var col = portalGO.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.8f;

            var sr = portalGO.AddComponent<SpriteRenderer>();
            sr.sprite = GetPortalSprite();
            sr.sortingOrder = 5;

            var symbolGO = new GameObject("Symbol_Text");
            symbolGO.transform.SetParent(portalGO.transform, false);
            symbolGO.transform.localPosition = Vector3.zero;
            var symbolTmp = symbolGO.AddComponent<TextMeshPro>();
            symbolTmp.text = "GATE";
            symbolTmp.fontSize = 4;
            symbolTmp.alignment = TextAlignmentOptions.Center;
            symbolTmp.sortingOrder = 6;
            symbolGO.GetComponent<RectTransform>().sizeDelta = new Vector2(2f, 2f);

            var labelGO = new GameObject("Label_Text");
            labelGO.transform.SetParent(portalGO.transform, false);
            labelGO.transform.localPosition = new Vector3(0, -1.3f, 0);
            var labelTmp = labelGO.AddComponent<TextMeshPro>();
            labelTmp.text = "CỔNG DỊCH CHUYỂN";
            labelTmp.fontSize = 3;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.sortingOrder = 6;
            labelGO.GetComponent<RectTransform>().sizeDelta = new Vector2(4f, 1.5f);

            var portalComp = portalGO.AddComponent<RoomPortal>();

            var so = new SerializedObject(portalComp);
            so.FindProperty("_portalRenderer").objectReferenceValue = sr;
            so.FindProperty("_symbolText").objectReferenceValue = symbolTmp;
            so.FindProperty("_labelText").objectReferenceValue = labelTmp;
            so.ApplyModifiedProperties();

            return portalGO;
        }

        public static int GetObstacleLayer()
        {
            int layer = LayerMask.NameToLayer("Obstacle ");
            if (layer == -1) layer = LayerMask.NameToLayer("Obstacle");
            if (layer == -1) layer = 7;
            return layer;
        }

        public static Sprite GetSquareSprite()
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath("311925a002f4447b3a28927169b83ea6"));
            if (s != null) return s;

            var guids = AssetDatabase.FindAssets("Square t:Sprite");
            if (guids.Length > 0)
            {
                return AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            return null;
        }

        public static Sprite GetPortalSprite()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Props/portal-purple_32px_scroller_outline_lit.png");
            foreach (var a in assets)
            {
                if (a is Sprite sp) return sp;
            }
            return GetSquareSprite();
        }

        public static Sprite GetBrazierSprite()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Props/brazier-orange_32px_scroller_no-outline_lit.png");
            foreach (var a in assets)
            {
                if (a is Sprite sp) return sp;
            }
            return GetSquareSprite();
        }

        public static Sprite GetOrbPedestalSprite()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Props/orb-pedestal-purple_48px_scroller_outline_lit.png");
            foreach (var a in assets)
            {
                if (a is Sprite sp) return sp;
            }
            return GetSquareSprite();
        }

        public static Sprite GetChestSprite()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Props/coins-chests-etc-2-0-noborders.png");
            foreach (var a in assets)
            {
                if (a is Sprite sp) return sp;
            }
            return GetSquareSprite();
        }

        private static bool IsCombatRoom(RunNodeType type)
        {
            return type == RunNodeType.NormalBattle || type == RunNodeType.EliteBattle ||
                   type == RunNodeType.MiniBoss || type == RunNodeType.BossBattle;
        }
    }
}
#endif

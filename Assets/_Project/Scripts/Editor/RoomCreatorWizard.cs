#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;
using Project_D.Dungeon.Run;
using Project_D.Dungeon.Room;
using Project_D.Dungeon.Interactions;

namespace Project_D.EditorTools
{
    public enum RoomSizePreset
    {
        Small_20x20,
        Medium_30x30,
        Large_40x40,
        Boss_45x45,
        Custom
    }

    public enum RoomLayoutStyle
    {
        OpenArena,              // 4 tường bao quanh, sàn mở thoáng
        FourCornerPillars,      // 4 trụ góc che chắn đạn/tầm nhìn chiến thuật
        CenterCrossChokepoints, // 4 đoạn tường chữ thập tạo hành lang hẹp & khu vực giao tranh
        RingOfBoulders,         // Vòng 8 trụ đá/vật cản tròn quanh tâm đấu trường
        RestSiteCamp,           // Phòng nghỉ ngơi có đống lửa trung tâm, bàn đá
        ShopOutpost,            // Cửa hàng có quầy gỗ ngăn cách và rương báu
        BuffAltarSanctum        // Điện thờ có hàng cột dẫn lối và bệ ngọc ban phước
    }

    public class RoomCreatorWizard : EditorWindow
    {
        // Obsolete menu items removed to keep Project_D menu clean and focused on Tile Palette workflow
        public static void SetupRoomManagerInScene()
        {
            var roomMgr = UnityEngine.Object.FindAnyObjectByType<RoomManager>();
            if (roomMgr == null)
            {
                var rc = UnityEngine.Object.FindAnyObjectByType<RoomController>();
                GameObject targetGO;
                if (rc != null)
                {
                    targetGO = rc.gameObject;
                }
                else
                {
                    targetGO = new GameObject("[RoomManager]");
                    Undo.RegisterCreatedObjectUndo(targetGO, "Create RoomManager");
                }

                roomMgr = Undo.AddComponent<RoomManager>(targetGO);
            }

            // Tự động tìm hoặc tạo RoomDatabase.asset
            string dbPath = "Assets/_Project/ScriptableObjects/Dungeon/RoomDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<RoomDatabaseSO>(dbPath);
            if (db == null)
            {
                string dir = "Assets/_Project/ScriptableObjects/Dungeon";
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                db = ScriptableObject.CreateInstance<RoomDatabaseSO>();
                AssetDatabase.CreateAsset(db, dbPath);
                AssetDatabase.SaveAssets();
            }

            var so = new SerializedObject(roomMgr);
            var dbProp = so.FindProperty("_roomDatabase");
            if (dbProp != null)
            {
                dbProp.objectReferenceValue = db;
                so.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(roomMgr);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(roomMgr.gameObject.scene);

            Selection.activeGameObject = roomMgr.gameObject;
            EditorGUIUtility.PingObject(roomMgr.gameObject);

            Debug.Log("<color=green><b>[RoomCreatorWizard]</b></color> Đã thiết lập thành công RoomManager và gắn kết RoomDatabase trong Scene!");
            EditorUtility.DisplayDialog("Thiết Lập Thành Công",
                "Đã gắn RoomManager vào Scene và liên kết với RoomDatabase.asset thành công!", "OK");
        }

        // Room Metadata
        [SerializeField] private string _roomName = "Room_Normal_FourPillars";
        [SerializeField] private RunNodeType _supportedType = RunNodeType.NormalBattle;
        [SerializeField] private int _selectionWeight = 10;
        [SerializeField] private string _description = "Phòng chiến đấu với 4 trụ đá che chắn.";

        // Geometry
        [SerializeField] private RoomSizePreset _sizePreset = RoomSizePreset.Medium_30x30;
        [SerializeField] private float _roomWidth = 30f;
        [SerializeField] private float _roomHeight = 30f;
        [SerializeField] private float _wallThickness = 1.5f;

        // Architectural Style
        [SerializeField] private RoomLayoutStyle _layoutStyle = RoomLayoutStyle.FourCornerPillars;

        // Spawns & Portals
        [SerializeField] private int _enemySpawnCount = 8;
        [SerializeField] private int _portalCount = 3;

        // Visual Colors
        [SerializeField] private Color _floorColor = new Color(0.11f, 0.12f, 0.15f, 1f);
        [SerializeField] private Color _wallColor = new Color(0.24f, 0.28f, 0.36f, 1f);
        [SerializeField] private Color _obstacleColor = new Color(0.38f, 0.44f, 0.54f, 1f);

        private Vector2 _scrollPos;

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawHeader();

            EditorGUILayout.Space(8);
            DrawIdentitySection();

            EditorGUILayout.Space(8);
            DrawGeometrySection();

            EditorGUILayout.Space(8);
            DrawStyleSection();

            EditorGUILayout.Space(8);
            DrawSpawnsAndPortalsSection();

            EditorGUILayout.Space(8);
            DrawVisualColorsSection();

            EditorGUILayout.Space(14);
            DrawActionButtons();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("BỘ CÔNG CỤ TẠO PHÒNG HẦM NGỤC (ROOM CREATOR)", style);
            EditorGUILayout.HelpBox("Tạo và tự động xuất bản Prefab phòng, cấu hình RoomLayout, RoomVariationSO và đăng ký vào RoomDatabaseSO chỉ với 1 click.", MessageType.Info);
        }

        private void DrawIdentitySection()
        {
            EditorGUILayout.LabelField("1. ĐỊNH DANH PHÒNG (ROOM IDENTITY)", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            _roomName = EditorGUILayout.TextField("Tên Phòng (Prefab Name):", _roomName);

            var prevType = _supportedType;
            _supportedType = (RunNodeType)EditorGUILayout.EnumPopup("Loại Node Hỗ Trợ:", _supportedType);
            if (prevType != _supportedType)
            {
                OnNodeTypeChanged();
            }

            _selectionWeight = EditorGUILayout.IntSlider("Trọng Số Ngẫu Nhiên (1-100):", _selectionWeight, 1, 100);
            _description = EditorGUILayout.TextField("Mô Tả Chiến Thuật:", _description);

            EditorGUI.indentLevel--;
        }

        private void OnNodeTypeChanged()
        {
            switch (_supportedType)
            {
                case RunNodeType.NormalBattle:
                    _layoutStyle = RoomLayoutStyle.FourCornerPillars;
                    _enemySpawnCount = 8;
                    _sizePreset = RoomSizePreset.Medium_30x30;
                    _roomWidth = 30f; _roomHeight = 30f;
                    _description = "Phòng quái thường với 4 cột đá che chắn.";
                    break;
                case RunNodeType.EliteBattle:
                    _layoutStyle = RoomLayoutStyle.CenterCrossChokepoints;
                    _enemySpawnCount = 10;
                    _sizePreset = RoomSizePreset.Medium_30x30;
                    _roomWidth = 34f; _roomHeight = 34f;
                    _description = "Đấu trường Elite với các chướng ngại vật chữ thập chia cắt làn đạn.";
                    break;
                case RunNodeType.BossBattle:
                    _layoutStyle = RoomLayoutStyle.RingOfBoulders;
                    _enemySpawnCount = 12;
                    _sizePreset = RoomSizePreset.Boss_45x45;
                    _roomWidth = 44f; _roomHeight = 44f;
                    _description = "Đại đấu trường Boss rộng lớn với vòng đá giác đấu.";
                    break;
                case RunNodeType.RestSite:
                    _layoutStyle = RoomLayoutStyle.RestSiteCamp;
                    _enemySpawnCount = 0;
                    _sizePreset = RoomSizePreset.Small_20x20;
                    _roomWidth = 20f; _roomHeight = 20f;
                    _description = "Khu lửa trại yên bình phục hồi sinh lực.";
                    break;
                case RunNodeType.Shop:
                    _layoutStyle = RoomLayoutStyle.ShopOutpost;
                    _enemySpawnCount = 0;
                    _sizePreset = RoomSizePreset.Small_20x20;
                    _roomWidth = 22f; _roomHeight = 22f;
                    _description = "Quán thương nhân bí ẩn bán vật phẩm và nâng cấp.";
                    break;
                case RunNodeType.BuffReward:
                    _layoutStyle = RoomLayoutStyle.BuffAltarSanctum;
                    _enemySpawnCount = 0;
                    _sizePreset = RoomSizePreset.Small_20x20;
                    _roomWidth = 24f; _roomHeight = 24f;
                    _description = "Điện thờ cổ xưa ban tặng phước lành kỹ năng.";
                    break;
            }
        }

        private void DrawGeometrySection()
        {
            EditorGUILayout.LabelField("2. KÍCH THƯỚC PHÒNG (GEOMETRY)", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            var prevPreset = _sizePreset;
            _sizePreset = (RoomSizePreset)EditorGUILayout.EnumPopup("Kích Thước Mẫu:", _sizePreset);

            if (prevPreset != _sizePreset)
            {
                switch (_sizePreset)
                {
                    case RoomSizePreset.Small_20x20:
                        _roomWidth = 20f; _roomHeight = 20f; break;
                    case RoomSizePreset.Medium_30x30:
                        _roomWidth = 30f; _roomHeight = 30f; break;
                    case RoomSizePreset.Large_40x40:
                        _roomWidth = 40f; _roomHeight = 40f; break;
                    case RoomSizePreset.Boss_45x45:
                        _roomWidth = 45f; _roomHeight = 45f; break;
                }
            }

            using (new EditorGUI.DisabledScope(_sizePreset != RoomSizePreset.Custom))
            {
                _roomWidth = EditorGUILayout.FloatField("Chiều Rộng (Width):", _roomWidth);
                _roomHeight = EditorGUILayout.FloatField("Chiều Dài (Height):", _roomHeight);
            }

            _wallThickness = EditorGUILayout.Slider("Độ Dày Tường Bao:", _wallThickness, 0.8f, 3f);
            EditorGUI.indentLevel--;
        }

        private void DrawStyleSection()
        {
            EditorGUILayout.LabelField("3. BỐ CỤC KIẾN TRÚC & VẬT CẢN (LAYOUT STYLE)", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            _layoutStyle = (RoomLayoutStyle)EditorGUILayout.EnumPopup("Kiểu Bố Cục:", _layoutStyle);
            EditorGUI.indentLevel--;
        }

        private void DrawSpawnsAndPortalsSection()
        {
            EditorGUILayout.LabelField("4. ĐIỂM XUẤT HIỆN & CỔNG DỊCH CHUYỂN", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            if (_supportedType == RunNodeType.NormalBattle || _supportedType == RunNodeType.EliteBattle || _supportedType == RunNodeType.BossBattle)
            {
                _enemySpawnCount = EditorGUILayout.IntSlider("Số Điểm Quái Spawn:", _enemySpawnCount, 4, 16);
            }
            else
            {
                EditorGUILayout.HelpBox("Phòng Phi Chiến Đấu: Không cần điểm xuất hiện của Quái.", MessageType.None);
            }

            _portalCount = EditorGUILayout.IntSlider("Số Cổng Dịch Chuyển:", _portalCount, 1, 3);
            EditorGUI.indentLevel--;
        }

        private void DrawVisualColorsSection()
        {
            EditorGUILayout.LabelField("5. MÀU SẮC & GIAO DIỆN (PALETTE)", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            _floorColor = EditorGUILayout.ColorField("Màu Sàn Đấu:", _floorColor);
            _wallColor = EditorGUILayout.ColorField("Màu Tường Bao:", _wallColor);
            _obstacleColor = EditorGUILayout.ColorField("Màu Chướng Ngại Vật:", _obstacleColor);
            EditorGUI.indentLevel--;
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.LabelField("6. THỰC THI (ACTIONS)", EditorStyles.boldLabel);

            GUI.backgroundColor = new Color(0.2f, 0.9f, 0.7f);
            if (GUILayout.Button("🎨 TẠO GRID ĐỂ BẮT ĐẦU VẼ BẰNG TILE PALETTE (NEW GRID)", GUILayout.Height(34)))
            {
                Project_D.EditorTools.DungeonTilePaletteTools.CreateRoomGridCanvas();
            }

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
            if (GUILayout.Button("Tạo Phòng Mẫu Vào Scene Để Tinh Chỉnh (Scene Spawn)", GUILayout.Height(30)))
            {
                SpawnRoomInScene();
            }

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(0.4f, 1f, 0.5f);
            if (GUILayout.Button("Xuất Bản Prefab + RoomVariationSO + Đăng Ký Database", GUILayout.Height(36)))
            {
                ExportPrefabAndScriptableObject();
            }

            EditorGUILayout.Space(10);
            GUI.backgroundColor = new Color(1f, 0.75f, 0.3f);
            if (GUILayout.Button("⚡ TỰ ĐỘNG SINH TOÀN BỘ 7 PHÒNG MẪU CHUẨN (BATCH GENERATE ALL)", GUILayout.Height(40)))
            {
                BatchGenerateAllStandardRooms();
            }

            GUI.backgroundColor = Color.white;
        }

        // =========================================================================
        // LOGIC TẠO CÂY HIERARCHY CỦA PHÒNG
        // =========================================================================

        private GameObject BuildRoomHierarchy(string roomName, RunNodeType nodeType, Vector2 size, float wallThickness,
            RoomLayoutStyle style, int enemyCount, int portalCount, Color floorCol, Color wallCol, Color obstacleCol)
        {
            int obstacleLayer = GetObstacleLayer();
            Sprite squareSprite = GetSquareSprite();

            // 1. Root GameObject
            var root = new GameObject(roomName);
            var layout = root.AddComponent<RoomLayout>();
            layout.SetIdentity(roomName, nodeType, size);

            // 2. Floor
            var floorGO = new GameObject("Floor");
            floorGO.transform.SetParent(root.transform, false);
            var floorSR = floorGO.AddComponent<SpriteRenderer>();
            floorSR.sprite = squareSprite;
            floorSR.color = floorCol;
            floorSR.sortingOrder = -10;
            floorGO.transform.localScale = new Vector3(size.x, size.y, 1f);

            // 3. Boundaries (Outer Walls with Layer Obstacle)
            var boundariesRoot = new GameObject("Boundaries");
            boundariesRoot.transform.SetParent(root.transform, false);

            CreateWall("Wall_Top", boundariesRoot.transform, new Vector3(0, size.y * 0.5f + wallThickness * 0.5f, 0),
                new Vector2(size.x + wallThickness * 2, wallThickness), obstacleLayer, squareSprite, wallCol);

            CreateWall("Wall_Bottom", boundariesRoot.transform, new Vector3(0, -size.y * 0.5f - wallThickness * 0.5f, 0),
                new Vector2(size.x + wallThickness * 2, wallThickness), obstacleLayer, squareSprite, wallCol);

            CreateWall("Wall_Left", boundariesRoot.transform, new Vector3(-size.x * 0.5f - wallThickness * 0.5f, 0, 0),
                new Vector2(wallThickness, size.y), obstacleLayer, squareSprite, wallCol);

            CreateWall("Wall_Right", boundariesRoot.transform, new Vector3(size.x * 0.5f + wallThickness * 0.5f, 0, 0),
                new Vector2(wallThickness, size.y), obstacleLayer, squareSprite, wallCol);

            // 4. Obstacles
            var obstaclesRoot = new GameObject("Obstacles");
            obstaclesRoot.transform.SetParent(root.transform, false);
            BuildObstacles(obstaclesRoot.transform, style, size, obstacleLayer, squareSprite, obstacleCol);

            // 5. Spawn Points
            var spawnRoot = new GameObject("SpawnPoints");
            spawnRoot.transform.SetParent(root.transform, false);

            var playerSpawn = new GameObject("PlayerSpawnPoint");
            playerSpawn.transform.SetParent(spawnRoot.transform, false);
            float playerY = (nodeType == RunNodeType.NormalBattle || nodeType == RunNodeType.EliteBattle || nodeType == RunNodeType.BossBattle)
                ? -size.y * 0.35f : -size.y * 0.25f;
            playerSpawn.transform.localPosition = new Vector3(0, playerY, 0);

            var enemySpawnsRoot = new GameObject("EnemySpawns");
            enemySpawnsRoot.transform.SetParent(spawnRoot.transform, false);

            if (nodeType == RunNodeType.NormalBattle || nodeType == RunNodeType.EliteBattle || nodeType == RunNodeType.BossBattle)
            {
                float spawnRadius = Mathf.Min(size.x, size.y) * 0.38f;
                for (int i = 0; i < enemyCount; i++)
                {
                    float angleDeg = i * (360f / enemyCount);
                    float rad = angleDeg * Mathf.Deg2Rad;
                    Vector3 pos = new Vector3(Mathf.Cos(rad) * spawnRadius, Mathf.Sin(rad) * spawnRadius, 0);

                    var pt = new GameObject($"EnemySpawn_{i + 1:D2}");
                    pt.transform.SetParent(enemySpawnsRoot.transform, false);
                    pt.transform.localPosition = pos;
                }
            }

            // 6. Portals
            var portalsRoot = new GameObject("Portals");
            portalsRoot.transform.SetParent(root.transform, false);
            BuildPortals(portalsRoot.transform, portalCount, size);

            // 7. Non-Combat Interaction Points
            var interactionsRoot = new GameObject("InteractionPoints");
            interactionsRoot.transform.SetParent(root.transform, false);
            BuildInteractionPoint(interactionsRoot.transform, nodeType, size);

            // Auto-Bind references
            layout.AutoBindReferences();

            return root;
        }

        private void CreateWall(string name, Transform parent, Vector3 localPos, Vector2 size, int layer, Sprite sprite, Color col)
        {
            var wall = new GameObject(name);
            wall.layer = layer;
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = localPos;
            wall.transform.localScale = new Vector3(size.x, size.y, 1f);

            var collider = wall.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;

            var sr = wall.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = col;
            sr.sortingOrder = 0;
        }

        private void BuildObstacles(Transform parent, RoomLayoutStyle style, Vector2 size, int layer, Sprite sprite, Color col)
        {
            switch (style)
            {
                case RoomLayoutStyle.OpenArena:
                    // Sàn mở không chướng ngại vật
                    break;

                case RoomLayoutStyle.FourCornerPillars:
                    float offsetX = size.x * 0.25f;
                    float offsetY = size.y * 0.25f;
                    Vector2 pillarSize = new Vector2(3f, 3f);
                    CreateWall("Pillar_TL", parent, new Vector3(-offsetX, offsetY, 0), pillarSize, layer, sprite, col);
                    CreateWall("Pillar_TR", parent, new Vector3(offsetX, offsetY, 0), pillarSize, layer, sprite, col);
                    CreateWall("Pillar_BL", parent, new Vector3(-offsetX, -offsetY, 0), pillarSize, layer, sprite, col);
                    CreateWall("Pillar_BR", parent, new Vector3(offsetX, -offsetY, 0), pillarSize, layer, sprite, col);
                    break;

                case RoomLayoutStyle.CenterCrossChokepoints:
                    float dist = size.x * 0.22f;
                    CreateWall("Cross_Top", parent, new Vector3(0, dist, 0), new Vector2(2f, size.y * 0.22f), layer, sprite, col);
                    CreateWall("Cross_Bottom", parent, new Vector3(0, -dist, 0), new Vector2(2f, size.y * 0.22f), layer, sprite, col);
                    CreateWall("Cross_Left", parent, new Vector3(-dist, 0, 0), new Vector2(size.x * 0.22f, 2f), layer, sprite, col);
                    CreateWall("Cross_Right", parent, new Vector3(dist, 0, 0), new Vector2(size.x * 0.22f, 2f), layer, sprite, col);
                    break;

                case RoomLayoutStyle.RingOfBoulders:
                    int count = 8;
                    float radius = Mathf.Min(size.x, size.y) * 0.26f;
                    for (int i = 0; i < count; i++)
                    {
                        float rad = (i * (360f / count)) * Mathf.Deg2Rad;
                        Vector3 pos = new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0);
                        CreateWall($"Boulder_{i + 1}", parent, pos, new Vector2(2.2f, 2.2f), layer, sprite, col);
                    }
                    break;

                case RoomLayoutStyle.RestSiteCamp:
                    // Campfire base decor and side stones
                    CreateWall("Bench_Left", parent, new Vector3(-3.5f, 0, 0), new Vector2(1.2f, 3f), layer, sprite, col);
                    CreateWall("Bench_Right", parent, new Vector3(3.5f, 0, 0), new Vector2(1.2f, 3f), layer, sprite, col);
                    break;

                case RoomLayoutStyle.ShopOutpost:
                    // Quầy gỗ ngăn cách
                    CreateWall("Counter_Left", parent, new Vector3(-size.x * 0.2f, 1.2f, 0), new Vector2(size.x * 0.35f, 1.2f), layer, sprite, col);
                    CreateWall("Counter_Right", parent, new Vector3(size.x * 0.2f, 1.2f, 0), new Vector2(size.x * 0.35f, 1.2f), layer, sprite, col);
                    CreateWall("Crate_1", parent, new Vector3(-size.x * 0.38f, -2f, 0), new Vector2(2f, 2f), layer, sprite, col);
                    CreateWall("Crate_2", parent, new Vector3(size.x * 0.38f, -2f, 0), new Vector2(2f, 2f), layer, sprite, col);
                    break;

                case RoomLayoutStyle.BuffAltarSanctum:
                    // Hàng cột dẫn lối
                    float colX = 4f;
                    CreateWall("Pillar_L1", parent, new Vector3(-colX, 2f, 0), new Vector2(1.6f, 1.6f), layer, sprite, col);
                    CreateWall("Pillar_L2", parent, new Vector3(-colX, -1f, 0), new Vector2(1.6f, 1.6f), layer, sprite, col);
                    CreateWall("Pillar_R1", parent, new Vector3(colX, 2f, 0), new Vector2(1.6f, 1.6f), layer, sprite, col);
                    CreateWall("Pillar_R2", parent, new Vector3(colX, -1f, 0), new Vector2(1.6f, 1.6f), layer, sprite, col);
                    break;
            }
        }

        private void BuildPortals(Transform parent, int count, Vector2 size)
        {
            Sprite portalSprite = GetPortalSprite();
            float portalY = size.y * 0.5f - 2.5f;

            float[] xOffsets;
            if (count == 1) xOffsets = new float[] { 0f };
            else if (count == 2) xOffsets = new float[] { -size.x * 0.2f, size.x * 0.2f };
            else xOffsets = new float[] { -size.x * 0.28f, 0f, size.x * 0.28f };

            string[] names = count switch
            {
                1 => new[] { "Portal_Center" },
                2 => new[] { "Portal_Left", "Portal_Right" },
                _ => new[] { "Portal_Left", "Portal_Center", "Portal_Right" }
            };

            for (int i = 0; i < count; i++)
            {
                var portalGO = new GameObject(names[i]);
                portalGO.transform.SetParent(parent, false);
                portalGO.transform.localPosition = new Vector3(xOffsets[i], portalY, 0);

                var col = portalGO.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.8f;

                var sr = portalGO.AddComponent<SpriteRenderer>();
                sr.sprite = portalSprite;
                sr.sortingOrder = 5;

                // Symbol Text
                var symbolGO = new GameObject("Symbol_Text");
                symbolGO.transform.SetParent(portalGO.transform, false);
                symbolGO.transform.localPosition = Vector3.zero;
                var symbolTmp = symbolGO.AddComponent<TextMeshPro>();
                symbolTmp.text = "GATE";
                symbolTmp.fontSize = 4;
                symbolTmp.alignment = TextAlignmentOptions.Center;
                symbolTmp.sortingOrder = 6;
                var symbolRect = symbolGO.GetComponent<RectTransform>();
                symbolRect.sizeDelta = new Vector2(2f, 2f);

                // Label Text
                var labelGO = new GameObject("Label_Text");
                labelGO.transform.SetParent(portalGO.transform, false);
                labelGO.transform.localPosition = new Vector3(0, -1.3f, 0);
                var labelTmp = labelGO.AddComponent<TextMeshPro>();
                labelTmp.text = "CỔNG DỊCH CHUYỂN";
                labelTmp.fontSize = 3;
                labelTmp.alignment = TextAlignmentOptions.Center;
                labelTmp.sortingOrder = 6;
                var labelRect = labelGO.GetComponent<RectTransform>();
                labelRect.sizeDelta = new Vector2(4f, 1.5f);

                // RoomPortal Component
                var portalComp = portalGO.AddComponent<RoomPortal>();

                // Serialized Fields Binding
                var so = new SerializedObject(portalComp);
                so.FindProperty("_portalRenderer").objectReferenceValue = sr;
                so.FindProperty("_symbolText").objectReferenceValue = symbolTmp;
                so.FindProperty("_labelText").objectReferenceValue = labelTmp;
                so.ApplyModifiedProperties();
            }
        }

        private void BuildInteractionPoint(Transform parent, RunNodeType nodeType, Vector2 size)
        {
            if (nodeType == RunNodeType.NormalBattle || nodeType == RunNodeType.EliteBattle || nodeType == RunNodeType.BossBattle)
            {
                return; // Không cần điểm tương tác cho phòng quái
            }

            var pointGO = new GameObject();
            var col = pointGO.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1.5f;

            var sr = pointGO.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;

            var interactComp = pointGO.AddComponent<InteractionPoint>();
            var so = new SerializedObject(interactComp);

            switch (nodeType)
            {
                case RunNodeType.RestSite:
                    pointGO.name = "RestSite_Altar";
                    pointGO.transform.SetParent(parent, false);
                    pointGO.transform.localPosition = new Vector3(0, 0, 0);
                    sr.sprite = GetBrazierSprite();
                    so.FindProperty("_type").enumValueIndex = (int)InteractionType.RestSite;
                    so.FindProperty("_promptText").stringValue = "Chạm để Nghỉ Ngơi";
                    break;

                case RunNodeType.Shop:
                    pointGO.name = "Shop_Counter";
                    pointGO.transform.SetParent(parent, false);
                    pointGO.transform.localPosition = new Vector3(0, 2.2f, 0);
                    sr.sprite = GetChestSprite();
                    so.FindProperty("_type").enumValueIndex = (int)InteractionType.Shop;
                    so.FindProperty("_promptText").stringValue = "Chạm để Mua Sắm";
                    break;

                case RunNodeType.BuffReward:
                    pointGO.name = "Buff_Altar";
                    pointGO.transform.SetParent(parent, false);
                    pointGO.transform.localPosition = new Vector3(0, 1.5f, 0);
                    sr.sprite = GetOrbPedestalSprite();
                    so.FindProperty("_type").enumValueIndex = (int)InteractionType.BuffReward;
                    so.FindProperty("_promptText").stringValue = "Chạm để Nhận Buff";
                    break;
            }

            so.ApplyModifiedProperties();
        }

        // =========================================================================
        // TIỆN ÍCH TẢI SPRITE & LAYER AN TOÀN
        // =========================================================================

        private int GetObstacleLayer()
        {
            int layer = LayerMask.NameToLayer("Obstacle ");
            if (layer == -1) layer = LayerMask.NameToLayer("Obstacle");
            if (layer == -1) layer = 7;
            return layer;
        }

        private Sprite GetSquareSprite()
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

        private Sprite GetPortalSprite()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Props/portal-purple_32px_scroller_outline_lit.png");
            foreach (var a in assets)
            {
                if (a is Sprite sp) return sp;
            }
            return GetSquareSprite();
        }

        private Sprite GetBrazierSprite()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Props/brazier-orange_32px_scroller_no-outline_lit.png");
            foreach (var a in assets)
            {
                if (a is Sprite sp) return sp;
            }
            return GetSquareSprite();
        }

        private Sprite GetOrbPedestalSprite()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Props/orb-pedestal-purple_48px_scroller_outline_lit.png");
            foreach (var a in assets)
            {
                if (a is Sprite sp) return sp;
            }
            return GetSquareSprite();
        }

        private Sprite GetChestSprite()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Props/coins-chests-etc-2-0-noborders.png");
            foreach (var a in assets)
            {
                if (a is Sprite sp) return sp;
            }
            return GetSquareSprite();
        }

        // =========================================================================
        // HÀNH ĐỘNG SINH & XUẤT BẢN
        // =========================================================================

        private void SpawnRoomInScene()
        {
            var go = BuildRoomHierarchy(_roomName, _supportedType, new Vector2(_roomWidth, _roomHeight),
                _wallThickness, _layoutStyle, _enemySpawnCount, _portalCount, _floorColor, _wallColor, _obstacleColor);

            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Create Room Layout");
            Debug.Log($"<color=green><b>[RoomCreatorWizard]</b></color> Đã tạo phòng '{_roomName}' thành công trong Scene!");
        }

        public static (GameObject prefab, RoomVariationSO variation) ExportRoom(string roomName, RunNodeType nodeType,
            Vector2 size, float wallThickness, RoomLayoutStyle style, int enemyCount, int portalCount,
            int weight, string desc, Color floorCol, Color wallCol, Color obstacleCol)
        {
            string prefabDir = "Assets/_Project/Prefabs/Dungeon/Rooms";
            string soDir = "Assets/_Project/ScriptableObjects/Dungeon/Rooms";

            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
            if (!Directory.Exists(soDir)) Directory.CreateDirectory(soDir);

            // 1. Tạo GameObject tạm thời trong Scene
            var wizard = CreateInstance<RoomCreatorWizard>();
            var tempGO = wizard.BuildRoomHierarchy(roomName, nodeType, size, wallThickness, style, enemyCount, portalCount, floorCol, wallCol, obstacleCol);
            DestroyImmediate(wizard);

            // 2. Lưu thành Prefab
            string prefabPath = $"{prefabDir}/{roomName}.prefab";
            var prefabAsset = PrefabUtility.SaveAsPrefabAsset(tempGO, prefabPath);
            DestroyImmediate(tempGO);

            // 3. Tạo ScriptableObject RoomVariationSO
            string soPath = $"{soDir}/{roomName}_Data.asset";
            var variationSO = AssetDatabase.LoadAssetAtPath<RoomVariationSO>(soPath);
            if (variationSO == null)
            {
                variationSO = ScriptableObject.CreateInstance<RoomVariationSO>();
                AssetDatabase.CreateAsset(variationSO, soPath);
            }
            variationSO.Initialize(roomName, nodeType, prefabAsset, weight, desc);
            EditorUtility.SetDirty(variationSO);

            // 4. Đăng ký vào RoomDatabase
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

            return (prefabAsset, variationSO);
        }

        private void ExportPrefabAndScriptableObject()
        {
            var (prefab, variation) = ExportRoom(_roomName, _supportedType, new Vector2(_roomWidth, _roomHeight),
                _wallThickness, _layoutStyle, _enemySpawnCount, _portalCount, _selectionWeight, _description,
                _floorColor, _wallColor, _obstacleColor);

            EditorGUIUtility.PingObject(variation);
            Selection.activeObject = variation;

            EditorUtility.DisplayDialog("Xuất Bản Phòng Hoàn Tất!",
                $"Phòng '{_roomName}' đã được tạo thành công:\n" +
                $"- Prefab: Assets/_Project/Prefabs/Dungeon/Rooms/{_roomName}.prefab\n" +
                $"- Data SO: Assets/_Project/ScriptableObjects/Dungeon/Rooms/{_roomName}_Data.asset\n" +
                $"- Đã tự động cập nhật vào: RoomDatabase.asset", "Đồng ý");
        }

        public static void BatchGenerateAllStandardRooms()
        {
            bool confirm = EditorUtility.DisplayDialog("Xác nhận Tự Động Sinh 7 Phòng Mẫu",
                "Hệ thống sẽ tự động tạo trọn bộ 7 biến thể phòng chuẩn mực cho 13 tầng Dungeon:\n\n" +
                "1. Room_Normal_OpenField (Quái thường - Sân mở)\n" +
                "2. Room_Normal_FourPillars (Quái thường - 4 Cột che chắn)\n" +
                "3. Room_Elite_Chokepoints (Quái Elite - Chữ thập chia làn)\n" +
                "4. Room_RestSite_Campfire (Lửa trại hồi máu)\n" +
                "5. Room_Shop_MerchantOutpost (Cửa hàng mua sắm)\n" +
                "6. Room_BuffReward_AltarSanctum (Điện thờ ban phước)\n" +
                "7. Room_Boss_Colosseum (Đại đấu trường Boss)\n\n" +
                "Bạn có muốn tiếp tục?", "Sinh ngay", "Hủy");

            if (!confirm) return;

            EditorUtility.DisplayProgressBar("Đang sinh phòng...", "Tạo trọn bộ 7 phòng Dungeon...", 0.1f);

            Color floorCombat = new Color(0.12f, 0.13f, 0.17f, 1f);
            Color wallCombat = new Color(0.25f, 0.30f, 0.40f, 1f);
            Color obsCombat = new Color(0.38f, 0.44f, 0.54f, 1f);

            Color floorRest = new Color(0.18f, 0.15f, 0.12f, 1f);
            Color wallRest = new Color(0.40f, 0.32f, 0.25f, 1f);
            Color obsRest = new Color(0.55f, 0.45f, 0.35f, 1f);

            Color floorShop = new Color(0.14f, 0.16f, 0.13f, 1f);
            Color wallShop = new Color(0.30f, 0.38f, 0.28f, 1f);
            Color obsShop = new Color(0.45f, 0.52f, 0.40f, 1f);

            Color floorBuff = new Color(0.15f, 0.12f, 0.18f, 1f);
            Color wallBuff = new Color(0.35f, 0.28f, 0.42f, 1f);
            Color obsBuff = new Color(0.50f, 0.40f, 0.60f, 1f);

            // 1. Normal - Open Field
            ExportRoom("Room_Normal_OpenField", RunNodeType.NormalBattle, new Vector2(30f, 30f), 1.5f,
                RoomLayoutStyle.OpenArena, 8, 3, 10, "Đấu trường quái thường thoáng đãng, dễ di chuyển.",
                floorCombat, wallCombat, obsCombat);

            // 2. Normal - Four Pillars
            ExportRoom("Room_Normal_FourPillars", RunNodeType.NormalBattle, new Vector2(30f, 30f), 1.5f,
                RoomLayoutStyle.FourCornerPillars, 8, 3, 10, "Đấu trường quái thường có 4 trụ đá che chắn hỏa lực.",
                floorCombat, wallCombat, obsCombat);

            // 3. Elite - Chokepoints
            ExportRoom("Room_Elite_Chokepoints", RunNodeType.EliteBattle, new Vector2(34f, 34f), 1.5f,
                RoomLayoutStyle.CenterCrossChokepoints, 10, 3, 15, "Đấu trường Elite với các chướng ngại chữ thập tạo hành lang hẹp.",
                floorCombat, wallCombat, obsCombat);

            // 4. RestSite - Campfire
            ExportRoom("Room_RestSite_Campfire", RunNodeType.RestSite, new Vector2(20f, 20f), 1.5f,
                RoomLayoutStyle.RestSiteCamp, 0, 3, 10, "Khu vực nghỉ ngơi phục hồi sinh lực với đống lửa trung tâm.",
                floorRest, wallRest, obsRest);

            // 5. Shop - Merchant Outpost
            ExportRoom("Room_Shop_MerchantOutpost", RunNodeType.Shop, new Vector2(22f, 22f), 1.5f,
                RoomLayoutStyle.ShopOutpost, 0, 3, 10, "Quầy hàng của thương nhân bí ẩn với nhiều trang bị.",
                floorShop, wallShop, obsShop);

            // 6. Buff Reward - Altar Sanctum
            ExportRoom("Room_BuffReward_AltarSanctum", RunNodeType.BuffReward, new Vector2(24f, 24f), 1.5f,
                RoomLayoutStyle.BuffAltarSanctum, 0, 3, 10, "Điện thờ cổ xưa linh thiêng ban tặng phước lành kỹ năng.",
                floorBuff, wallBuff, obsBuff);

            // 7. Boss - Colosseum
            ExportRoom("Room_Boss_Colosseum", RunNodeType.BossBattle, new Vector2(44f, 44f), 2f,
                RoomLayoutStyle.RingOfBoulders, 12, 1, 20, "Đại đấu trường Boss rộng lớn với vòng đá giác đấu.",
                floorCombat, wallCombat, obsCombat);

            EditorUtility.ClearProgressBar();

            string dbPath = "Assets/_Project/ScriptableObjects/Dungeon/RoomDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<RoomDatabaseSO>(dbPath);
            if (db != null)
            {
                db.AutoFindAllVariationsInProject();
            }

            EditorUtility.DisplayDialog("Thành Công Rực Rỡ!",
                "Đã tự động tạo và cấu hình hoàn tất trọn bộ 7 phòng mẫu chuẩn!\n" +
                "- 7 Prefabs trong Assets/_Project/Prefabs/Dungeon/Rooms/\n" +
                "- 7 ScriptableObjects trong Assets/_Project/ScriptableObjects/Dungeon/Rooms/\n" +
                "- Đã kết nối tất cả vào RoomDatabase.asset", "Tuyệt Vời!");
        }
    }
}
#endif

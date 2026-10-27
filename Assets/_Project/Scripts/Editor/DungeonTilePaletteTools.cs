#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Project_D.Dungeon.Run;
using Project_D.Dungeon.Room;
using Project_D.Dungeon.Interactions;
using Project_D.Dungeon;

namespace Project_D.EditorTools
{
    public static class DungeonTilePaletteTools
    {
        private const string PREFAB_DIR = "Assets/_Project/Prefabs/Dungeon/LevelDesign";
        private const string TILES_BASE_DIR = "Assets/_Project/Tiles";
        private const string TILES_GAMEPLAY_DIR = "Assets/_Project/Tiles/LevelDesign";
        private const string TILES_FLOOR_DIR = "Assets/_Project/Tiles/Floors";
        private const string TILES_WALL_DIR = "Assets/_Project/Tiles/Walls";

        public const string PRIMARY_PALETTE_PATH = "Assets/_Project/Tiles/DungeonPalette.prefab";
        public const string SECONDARY_PALETTE_PATH = "Assets/_Project/Art/Environment/DungeonPalette.prefab";

        private const string FLOOR_ATLAS_PATH = "Assets/_Project/Art/Environment/atlas_floor-16x16.png";
        private const string WALL_ATLAS_PATH = "Assets/_Project/Art/Environment/atlas_walls_low-16x16.png";
        private const string WALL_RULE_TILE_PATH = "Assets/_Project/Tiles/Wall_RuleTile.asset";



        public static void SetupCompleteDungeonPalette(bool showDialog = false)
        {
            try
            {
                EnsureDirectories();

                // 1. Tạo & Cập nhật các Prefab Gameplay
                var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/PlayerSpawnPoint.prefab") ?? CreateOrUpdatePlayerSpawnPrefab();
                var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/EnemySpawnPoint.prefab") ?? CreateOrUpdateEnemySpawnPrefab();
                var portalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/RoomPortal.prefab") ?? CreateOrUpdatePortalPrefab();
                var altarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/Buff_Altar.prefab") ?? CreateOrUpdateInteractionPrefab("Buff_Altar", InteractionType.BuffReward, GetOrbPedestalSprite(), "Chạm để Nhận Buff");
                var campfirePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/RestSite_Campfire.prefab") ?? CreateOrUpdateInteractionPrefab("RestSite_Campfire", InteractionType.RestSite, GetBrazierSprite(), "Chạm để Nghỉ Ngơi");
                var chestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/Shop_Chest.prefab") ?? CreateOrUpdateInteractionPrefab("Shop_Chest", InteractionType.Shop, GetChestSprite(), "Chạm để Mua Sắm");

                // 2. Tạo các Gameplay Tile Assets (Khi vẽ sẽ tự instantiate Prefab)
                var playerTile = CreateTileAsset(TILES_GAMEPLAY_DIR, "Tile_PlayerSpawn", playerPrefab, GetPlayerSprite(), new Color(0.4f, 1f, 0.4f), Tile.ColliderType.None);
                var enemyTile = CreateTileAsset(TILES_GAMEPLAY_DIR, "Tile_EnemySpawn", enemyPrefab, GetEnemySprite(), new Color(1f, 0.45f, 0.45f), Tile.ColliderType.None);
                var portalTile = CreateTileAsset(TILES_GAMEPLAY_DIR, "Tile_Portal", portalPrefab, GetPortalSprite(), new Color(0.3f, 0.9f, 1f), Tile.ColliderType.None);
                var altarTile = CreateTileAsset(TILES_GAMEPLAY_DIR, "Tile_BuffAltar", altarPrefab, GetOrbPedestalSprite(), new Color(1f, 0.85f, 0.3f), Tile.ColliderType.None);
                var campfireTile = CreateTileAsset(TILES_GAMEPLAY_DIR, "Tile_Campfire", campfirePrefab, GetBrazierSprite(), new Color(1f, 0.6f, 0.2f), Tile.ColliderType.None);
                var chestTile = CreateTileAsset(TILES_GAMEPLAY_DIR, "Tile_ShopChest", chestPrefab, GetChestSprite(), new Color(1f, 0.9f, 0.4f), Tile.ColliderType.None);

                // 3. Tạo các Tile Sàn từ atlas_floor-16x16.png
                var floorTiles = GenerateTilesFromAtlas(FLOOR_ATLAS_PATH, TILES_FLOOR_DIR, Tile.ColliderType.None);

                // 4. Tạo các Tile Tường & Vật Cản từ atlas_walls_low-16x16.png
                var wallTiles = GenerateTilesFromAtlas(WALL_ATLAS_PATH, TILES_WALL_DIR, Tile.ColliderType.Grid);

                // Rule Tile Tường thông minh tự động kết nối viền
                TileBase wallRuleTile = AssetDatabase.LoadAssetAtPath<TileBase>(WALL_RULE_TILE_PATH);

                // 5. Khởi tạo và Lưu Palette Prefab
                BuildAndSavePalettePrefab(
                    gameplayTiles: new[] { playerTile, enemyTile, portalTile, altarTile, campfireTile, chestTile },
                    wallRuleTile: wallRuleTile,
                    wallTiles: wallTiles,
                    floorTiles: floorTiles
                );

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("<color=green><b>[DUNGEON LEVEL DESIGN]</b></color> Thiết lập thành công Tile Palette toàn diện (Sàn, Tường, Vật cản & Điểm tương tác)!");

                if (showDialog)
                {
                    EditorUtility.DisplayDialog("Thiết Lập Palette Toàn Diện Thành Công! 🎉",
                        "Đã tạo đầy đủ các Cọ trên Tile Palette 'DungeonPalette':\n\n" +
                        "1. 🟢 Gameplay & Điểm Tương Tác (Hàng đỉnh):\n" +
                        "   • Player Spawn, Quái Spawn, Cổng Dịch Chuyển (Portal)\n" +
                        "   • Bệ Thờ Buff, Lửa Trại Hồi Máu, Rương Shop\n\n" +
                        "2. 🧱 Tường & Rule Tile (Hàng giữa):\n" +
                        "   • Cọ Thông Minh Wall_RuleTile (Tự nối góc & cạnh)\n" +
                        "   • Các khối tường tĩnh & cột chướng ngại vật (Có Collider)\n\n" +
                        "3. 🟫 Hệ Thống Sàn Đa Dạng (Khu vực dưới):\n" +
                        "   • Đầy đủ sàn đá sạch, sàn nứt, sàn rêu, sàn họa tiết\n\n" +
                        "Bảng Tile Palette hiện đang sẵn sàng để bạn vẽ!", "Bắt Đầu Vẽ Ngay");
                }

                EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DungeonTilePaletteTools] Lỗi khi tạo Tile Palette: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(PREFAB_DIR)) Directory.CreateDirectory(PREFAB_DIR);
            if (!Directory.Exists(TILES_BASE_DIR)) Directory.CreateDirectory(TILES_BASE_DIR);
            if (!Directory.Exists(TILES_GAMEPLAY_DIR)) Directory.CreateDirectory(TILES_GAMEPLAY_DIR);
            if (!Directory.Exists(TILES_FLOOR_DIR)) Directory.CreateDirectory(TILES_FLOOR_DIR);
            if (!Directory.Exists(TILES_WALL_DIR)) Directory.CreateDirectory(TILES_WALL_DIR);
            string artEnvDir = Path.GetDirectoryName(SECONDARY_PALETTE_PATH);
            if (!string.IsNullOrEmpty(artEnvDir) && !Directory.Exists(artEnvDir)) Directory.CreateDirectory(artEnvDir);
        }

        private static List<Tile> GenerateTilesFromAtlas(string atlasPath, string outputDir, Tile.ColliderType colliderType)
        {
            var result = new List<Tile>();
            var sprites = LoadAllSpritesFromPath(atlasPath);
            if (sprites == null || sprites.Count == 0) return result;

            foreach (var sp in sprites)
            {
                string tileName = $"Tile_{sp.name}";
                string assetPath = $"{outputDir}/{tileName}.asset";

                var tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = sp;
                    tile.color = Color.white;
                    tile.colliderType = colliderType;
                    AssetDatabase.CreateAsset(tile, assetPath);
                }
                else
                {
                    tile.sprite = sp;
                    tile.colliderType = colliderType;
                    EditorUtility.SetDirty(tile);
                }
                result.Add(tile);
            }
            return result;
        }

        private static List<Sprite> LoadAllSpritesFromPath(string path)
        {
            var list = new List<Sprite>();
            var objects = AssetDatabase.LoadAllAssetsAtPath(path);
            if (objects == null || objects.Length == 0)
            {
                // Fallback tìm theo tên file
                string filename = Path.GetFileNameWithoutExtension(path);
                var guids = AssetDatabase.FindAssets($"{filename} t:Texture2D");
                if (guids.Length > 0)
                {
                    string resolvedPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    objects = AssetDatabase.LoadAllAssetsAtPath(resolvedPath);
                }
            }

            if (objects != null)
            {
                foreach (var obj in objects)
                {
                    if (obj is Sprite sp) list.Add(sp);
                }
            }
            return list;
        }

        private static void BuildAndSavePalettePrefab(Tile[] gameplayTiles, TileBase wallRuleTile, List<Tile> wallTiles, List<Tile> floorTiles)
        {
            // Tạo GameObject tạm thời trong bộ nhớ để làm cấu trúc Palette
            var paletteGo = new GameObject("DungeonPalette");
            var grid = paletteGo.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);
            grid.cellLayout = GridLayout.CellLayout.Rectangle;
            grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;

            var layerGo = new GameObject("Palette_Layer");
            layerGo.transform.SetParent(paletteGo.transform, false);
            var tilemap = layerGo.AddComponent<Tilemap>();
            var tr = layerGo.AddComponent<TilemapRenderer>();
            tr.sortingOrder = 0;

            // =========================================================================
            // SẮP XẾP BẢNG TILE PALETTE THEO HÀNG TRỰC QUAN & KHOA HỌC
            // =========================================================================

            // 1. HÀNG ĐỈNH (Y = 10): GAMEPLAY ENTITIES & ĐIỂM TƯƠNG TÁC
            // Đặt cách nhau 2 ô (X = 0, 2, 4, 6, 8, 10) để click chọn chuẩn xác, không bị nhầm lẫn
            int entX = 0;
            const int entY = 10;
            foreach (var gt in gameplayTiles)
            {
                if (gt != null)
                {
                    tilemap.SetTile(new Vector3Int(entX, entY, 0), gt);
                    entX += 2;
                }
            }

            // 2. HÀNG TƯỜNG (Y = 7): RULE TILE (CỌ THÔNG MINH) & CÁC KHỐI TƯỜNG TĨNH
            const int wallRowY = 7;
            if (wallRuleTile != null)
            {
                tilemap.SetTile(new Vector3Int(0, wallRowY, 0), wallRuleTile);
            }

            int staticWallX = 2;
            int countWallPlaced = 0;
            if (wallTiles != null)
            {
                foreach (var wt in wallTiles)
                {
                    tilemap.SetTile(new Vector3Int(staticWallX, wallRowY, 0), wt);
                    staticWallX++;
                    countWallPlaced++;
                    if (countWallPlaced >= 10) break; // Lấy 10 mẫu tường đầu tiên cho hàng 1
                }
            }

            // 3. HÀNG VẬT CẢN / BIẾN THỂ TƯỜNG (Y = 5)
            const int obstacleRowY = 5;
            if (wallTiles != null && wallTiles.Count > 10)
            {
                int obsX = 0;
                for (int i = 10; i < Mathf.Min(wallTiles.Count, 22); i++)
                {
                    tilemap.SetTile(new Vector3Int(obsX, obstacleRowY, 0), wallTiles[i]);
                    obsX++;
                }
            }

            // 4. KHU VỰC SÀN (Y = 3, 2, 1, 0, -1, -2): LƯỚI SÀN ĐA DẠNG
            if (floorTiles != null && floorTiles.Count > 0)
            {
                int floorCols = 10;
                int startFloorY = 3;
                int currentFloorX = 0;
                int currentFloorY = startFloorY;

                foreach (var ft in floorTiles)
                {
                    tilemap.SetTile(new Vector3Int(currentFloorX, currentFloorY, 0), ft);
                    currentFloorX++;
                    if (currentFloorX >= floorCols)
                    {
                        currentFloorX = 0;
                        currentFloorY--;
                    }
                }
            }

            // Lưu Prefab vào cả 2 vị trí chuẩn
            PrefabUtility.SaveAsPrefabAsset(paletteGo, PRIMARY_PALETTE_PATH);
            PrefabUtility.SaveAsPrefabAsset(paletteGo, SECONDARY_PALETTE_PATH);

            UnityEngine.Object.DestroyImmediate(paletteGo);
        }

        [MenuItem("Project_D/Dungeon/Create New Room Grid Canvas", false, 1)]
        [MenuItem("GameObject/2D Object/Dungeon Room Grid", false, 10)]
        public static GameObject CreateRoomGridCanvas()
        {
            var gridGo = new GameObject("DungeonRoom_Grid");
            gridGo.transform.position = Vector3.zero;
            var grid = gridGo.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            // 1. Tilemap Floor
            var floorGo = new GameObject("Tilemap_Floor");
            floorGo.transform.SetParent(gridGo.transform, false);
            floorGo.AddComponent<Tilemap>();
            var floorRenderer = floorGo.AddComponent<TilemapRenderer>();
            floorRenderer.sortingOrder = -10;

            // 2. Tilemap Walls
            var wallGo = new GameObject("Tilemap_Walls");
            wallGo.transform.SetParent(gridGo.transform, false);
            wallGo.layer = GetObstacleLayer();
            wallGo.AddComponent<Tilemap>();
            var wallRenderer = wallGo.AddComponent<TilemapRenderer>();
            wallRenderer.sortingOrder = 1;
            wallGo.AddComponent<TilemapCollider2D>();

            // 3. Tilemap Entities (Chứa Player, Quái, Cổng, Điểm tương tác)
            var entGo = new GameObject("Tilemap_Entities");
            entGo.transform.SetParent(gridGo.transform, false);
            entGo.AddComponent<Tilemap>();
            var entRenderer = entGo.AddComponent<TilemapRenderer>();
            entRenderer.sortingOrder = 5;

            // 4. Gắn DungeonRoomAuthoring để có 1-Click Export ngay trong Inspector
            gridGo.AddComponent<DungeonRoomAuthoring>();

            Undo.RegisterCreatedObjectUndo(gridGo, "Create Dungeon Room Grid");
            Selection.activeGameObject = gridGo;

            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }

            EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
            Debug.Log("<color=green><b>[DUNGEON LEVEL DESIGN]</b></color> Đã tạo Dungeon Room Grid sạch sẽ. Hãy chọn cọ từ Palette và vẽ sàn, tường, quái, tương tác!");
            return gridGo;
        }

        public static void FocusLayerInEditor(GameObject root, string childName)
        {
            if (root == null) return;
            var child = root.transform.Find(childName);
            if (child != null)
            {
                Selection.activeGameObject = child.gameObject;
                EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
            }
        }

        // =========================================================================
        // PREFAB & TILE CREATION LOGIC
        // =========================================================================

        private static GameObject CreateOrUpdatePlayerSpawnPrefab()
        {
            string path = $"{PREFAB_DIR}/PlayerSpawnPoint.prefab";
            var go = new GameObject("PlayerSpawnPoint");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetPlayerSprite();
            sr.color = new Color(0.4f, 1f, 0.4f);
            sr.sortingOrder = 10;
            go.AddComponent<EditorMarker>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateOrUpdateEnemySpawnPrefab()
        {
            string path = $"{PREFAB_DIR}/EnemySpawnPoint.prefab";
            var go = new GameObject("EnemySpawnPoint");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetEnemySprite();
            sr.color = new Color(1f, 0.45f, 0.45f);
            sr.sortingOrder = 10;
            go.AddComponent<EditorMarker>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateOrUpdatePortalPrefab()
        {
            string path = $"{PREFAB_DIR}/RoomPortal.prefab";
            var go = new GameObject("RoomPortal");
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1.2f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetPortalSprite();
            sr.sortingOrder = 2;

            var symbolGO = new GameObject("Symbol_Text");
            symbolGO.transform.SetParent(go.transform, false);
            symbolGO.transform.localPosition = new Vector3(0, 0.2f, 0);
            var symbolTmp = symbolGO.AddComponent<TextMeshPro>();
            symbolTmp.text = "GATE";
            symbolTmp.fontSize = 4;
            symbolTmp.alignment = TextAlignmentOptions.Center;
            symbolTmp.sortingOrder = 6;
            symbolGO.GetComponent<RectTransform>().sizeDelta = new Vector2(2f, 2f);

            var labelGO = new GameObject("Label_Text");
            labelGO.transform.SetParent(go.transform, false);
            labelGO.transform.localPosition = new Vector3(0, -1.3f, 0);
            var labelTmp = labelGO.AddComponent<TextMeshPro>();
            labelTmp.text = "CỔNG DỊCH CHUYỂN";
            labelTmp.fontSize = 3;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.sortingOrder = 6;
            labelGO.GetComponent<RectTransform>().sizeDelta = new Vector2(4f, 1.5f);

            var portalComp = go.AddComponent<RoomPortal>();
            var so = new SerializedObject(portalComp);
            so.FindProperty("_portalRenderer").objectReferenceValue = sr;
            so.FindProperty("_symbolText").objectReferenceValue = symbolTmp;
            so.FindProperty("_labelText").objectReferenceValue = labelTmp;
            so.ApplyModifiedProperties();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateOrUpdateInteractionPrefab(string name, InteractionType type, Sprite sprite, string prompt)
        {
            string path = $"{PREFAB_DIR}/{name}.prefab";
            var go = new GameObject(name);
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1.2f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 3;

            var comp = go.AddComponent<InteractionPoint>();
            var so = new SerializedObject(comp);
            so.FindProperty("_type").enumValueIndex = (int)type;
            so.FindProperty("_promptText").stringValue = prompt;
            so.ApplyModifiedProperties();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static Tile CreateTileAsset(string folder, string tileName, GameObject prefab, Sprite sprite, Color color, Tile.ColliderType colliderType)
        {
            string path = $"{folder}/{tileName}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }

            tile.sprite = sprite;
            tile.color = color;
            tile.gameObject = prefab;
            tile.colliderType = colliderType;

            EditorUtility.SetDirty(tile);
            return tile;
        }

        public static int GetObstacleLayer()
        {
            int layer = LayerMask.NameToLayer("Obstacle ");
            if (layer == -1) layer = LayerMask.NameToLayer("Obstacle");
            if (layer == -1) layer = 7;
            return layer;
        }

        public static Sprite GetPlayerSprite()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Characters/Player/PlayerRun.png");
            foreach (var a in assets)
            {
                if (a is Sprite sp && sp.name == "PlayerRun_0") return sp;
            }
            foreach (var a in assets)
            {
                if (a is Sprite sp) return sp;
            }
            return FindSpriteFallback("PlayerRun");
        }

        public static Sprite GetEnemySprite()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Characters/Enemies/Slime_Small_Orange.png");
            foreach (var a in assets)
            {
                if (a is Sprite sp && sp.name == "Slime_Small_Orange_0") return sp;
            }
            foreach (var a in assets)
            {
                if (a is Sprite sp) return sp;
            }
            return FindSpriteFallback("Slime_Small_Orange");
        }

        public static Sprite GetPortalSprite()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Props/portal-purple_32px_scroller_outline_lit.png");
            foreach (var a in assets)
            {
                if (a is Sprite sp) return sp;
            }
            return FindSpriteFallback("portal-purple_32px_scroller_outline_lit");
        }

        public static Sprite GetBrazierSprite()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Props/brazier-orange_32px_scroller_no-outline_lit.png");
            foreach (var a in assets)
            {
                if (a is Sprite sp) return sp;
            }
            return FindSpriteFallback("brazier-orange_32px_scroller_no-outline_lit");
        }

        public static Sprite GetOrbPedestalSprite()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Props/orb-pedestal-purple_48px_scroller_outline_lit.png");
            foreach (var a in assets)
            {
                if (a is Sprite sp) return sp;
            }
            return FindSpriteFallback("orb-pedestal-purple_48px_scroller_outline_lit");
        }

        public static Sprite GetChestSprite()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Props/coins-chests-etc-2-0-noborders.png");
            foreach (var a in assets)
            {
                if (a is Sprite sp) return sp;
            }
            return FindSpriteFallback("coins-chests-etc-2-0-noborders");
        }

        private static Sprite FindSpriteFallback(string filter)
        {
            var guids = AssetDatabase.FindAssets($"{filter} t:Texture2D");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var loaded = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var a in loaded)
                {
                    if (a is Sprite sp) return sp;
                }
            }
            return GetSquareSprite();
        }

        public static Sprite GetSquareSprite()
        {
            var guids = AssetDatabase.FindAssets("Square t:Sprite");
            if (guids.Length > 0)
            {
                return AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            return null;
        }
    }
}
#endif

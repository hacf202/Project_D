#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Project_D.Dungeon.Run;
using Project_D.Dungeon.Room;
using Project_D.Dungeon.Interactions;

namespace Project_D.EditorTools
{
    public static class DungeonRoomGenerator
    {
        private const string PREFAB_DIR = "Assets/_Project/Prefabs/Dungeon/Rooms";
        private const string SO_DIR = "Assets/_Project/ScriptableObjects/Dungeon/Rooms";
        private const string DB_PATH = "Assets/_Project/ScriptableObjects/Dungeon/RoomDatabase.asset";

        private const string LEVEL_DESIGN_DIR = "Assets/_Project/Prefabs/Dungeon/LevelDesign";
        private const string FLOOR_TILE_PATH = "Assets/_Project/Art/Tiles/Floors/atlas_floor-16x16_0.asset";
        private const string FLOOR_TILE_ALT_PATH = "Assets/_Project/Art/Tiles/Floors/atlas_floor-16x16_1.asset";
        private const string WALL_RULE_TILE_PATH = "Assets/_Project/Art/Tiles/Walls/Wall_RuleTile.asset";

        [InitializeOnLoadMethod]
        private static void OnEditorLoad()
        {
            // Tự động kiểm tra và sinh bộ phòng nếu chưa có
            EditorApplication.delayCall += () =>
            {
                string triggerFile = $"{PREFAB_DIR}/Room_Boss_InfernoCore.prefab";
                if (!File.Exists(triggerFile))
                {
                    Debug.Log("<color=cyan><b>[DungeonRoomGenerator]</b></color> Phát hiện chưa đủ 24 biến thể phòng đa dạng, tiến hành tự động khởi tạo...");
                    GenerateAllRooms(false);
                }
            };
        }

        [MenuItem("Project_D/Dungeon/Generate All Diverse Room Prefabs", false, 1)]
        public static void GenerateAllRoomsMenu()
        {
            GenerateAllRooms(true);
        }

        public static void GenerateAllRooms(bool showDialog = false)
        {
            try
            {
                if (!Directory.Exists(PREFAB_DIR)) Directory.CreateDirectory(PREFAB_DIR);
                if (!Directory.Exists(SO_DIR)) Directory.CreateDirectory(SO_DIR);

                // 1. Tải các Tile và Prefab gameplay cơ bản
                var floorTile = AssetDatabase.LoadAssetAtPath<TileBase>(FLOOR_TILE_PATH);
                var floorTileAlt = AssetDatabase.LoadAssetAtPath<TileBase>(FLOOR_TILE_ALT_PATH) ?? floorTile;
                var wallRuleTile = AssetDatabase.LoadAssetAtPath<TileBase>(WALL_RULE_TILE_PATH);

                if (floorTile == null || wallRuleTile == null)
                {
                    Debug.LogError("[DungeonRoomGenerator] Không tìm thấy Floor Tile hoặc Wall Rule Tile! Hãy đảm bảo thư mục Art/Tiles đã được tạo.");
                    return;
                }

                var playerSpawnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{LEVEL_DESIGN_DIR}/PlayerSpawnPoint.prefab");
                var enemySpawnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{LEVEL_DESIGN_DIR}/EnemySpawnPoint.prefab");
                var portalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{LEVEL_DESIGN_DIR}/RoomPortal.prefab");
                var buffAltarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{LEVEL_DESIGN_DIR}/Buff_Altar.prefab");
                var campfirePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{LEVEL_DESIGN_DIR}/RestSite_Campfire.prefab");
                var shopChestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{LEVEL_DESIGN_DIR}/Shop_Chest.prefab");

                int generatedCount = 0;

                // =========================================================================
                // 1. CÁC PHÒNG QUÁI THƯỜNG (NORMAL BATTLE)
                // =========================================================================

                // 1.1 Phòng 4 Trụ Đá Che Chắn (Four Pillars)
                CreateRoomPrefab("Room_Normal_FourPillars", RunNodeType.NormalBattle, 32, 32, 10,
                    "Đấu trường quái thường với 4 cột đá che chắn hỏa lực và làn đạn.",
                    floorTile, floorTileAlt, wallRuleTile, playerSpawnPrefab, enemySpawnPrefab, portalPrefab, null,
                    setupWalls: (walls) =>
                    {
                        // 4 trụ góc (3x3)
                        FillWallRect(walls, wallRuleTile, -8, -6, 6, 8);
                        FillWallRect(walls, wallRuleTile, 6, 8, 6, 8);
                        FillWallRect(walls, wallRuleTile, -8, -6, -8, -6);
                        FillWallRect(walls, wallRuleTile, 6, 8, -8, -6);
                    },
                    playerPos: new Vector3(0, -11, 0),
                    enemyPositions: new[]
                    {
                        new Vector3(-10, 10, 0), new Vector3(10, 10, 0),
                        new Vector3(-10, -4, 0), new Vector3(10, -4, 0),
                        new Vector3(-7, 1, 0),  new Vector3(7, 1, 0),
                        new Vector3(0, 8, 0),   new Vector3(0, -3, 0)
                    },
                    portalPositions: new[]
                    {
                        new Vector3(-7, 12, 0), new Vector3(0, 12, 0), new Vector3(7, 12, 0)
                    });
                generatedCount++;

                // 1.2 Phòng Chữ Thập Hành Lang Hẹp (Cross Chokepoints)
                CreateRoomPrefab("Room_Normal_CrossChamber", RunNodeType.NormalBattle, 34, 34, 10,
                    "Đấu trường chữ thập chia cắt làn đạn, tạo các ngõ hẹp chiến thuật.",
                    floorTile, floorTileAlt, wallRuleTile, playerSpawnPrefab, enemySpawnPrefab, portalPrefab, null,
                    setupWalls: (walls) =>
                    {
                        // 4 đoạn tường chữ thập để ngỏ tâm
                        FillWallRect(walls, wallRuleTile, -1, 1, 4, 10);
                        FillWallRect(walls, wallRuleTile, -1, 1, -10, -4);
                        FillWallRect(walls, wallRuleTile, -10, -4, -1, 1);
                        FillWallRect(walls, wallRuleTile, 4, 10, -1, 1);
                    },
                    playerPos: new Vector3(0, -12, 0),
                    enemyPositions: new[]
                    {
                        new Vector3(-9, 9, 0),   new Vector3(9, 9, 0),
                        new Vector3(-9, -9, 0),  new Vector3(9, -9, 0),
                        new Vector3(-11, 0, 0),  new Vector3(11, 0, 0),
                        new Vector3(0, 11, 0),   new Vector3(-5, 5, 0)
                    },
                    portalPositions: new[]
                    {
                        new Vector3(-8, 13, 0), new Vector3(0, 13, 0), new Vector3(8, 13, 0)
                    });
                generatedCount++;

                // 1.3 Phòng 2 Làn Song Song (Dual Corridor)
                CreateRoomPrefab("Room_Normal_DualCorridor", RunNodeType.NormalBattle, 36, 30, 10,
                    "Phòng chiến đấu chia làm 2 làn di chuyển với chướng ngại vật trung tâm.",
                    floorTile, floorTileAlt, wallRuleTile, playerSpawnPrefab, enemySpawnPrefab, portalPrefab, null,
                    setupWalls: (walls) =>
                    {
                        // Khối tường chia trung tâm
                        FillWallRect(walls, wallRuleTile, -2, 2, -6, 6);
                        // Khối cản 2 cánh
                        FillWallRect(walls, wallRuleTile, -11, -9, 2, 6);
                        FillWallRect(walls, wallRuleTile, 9, 11, -6, -2);
                    },
                    playerPos: new Vector3(0, -10, 0),
                    enemyPositions: new[]
                    {
                        new Vector3(-10, 9, 0),  new Vector3(10, 9, 0),
                        new Vector3(-10, -3, 0), new Vector3(10, 3, 0),
                        new Vector3(-6, 4, 0),   new Vector3(6, -4, 0),
                        new Vector3(-6, -8, 0),  new Vector3(6, 8, 0),
                        new Vector3(-12, 0, 0),  new Vector3(12, 0, 0)
                    },
                    portalPositions: new[]
                    {
                        new Vector3(-9, 11, 0), new Vector3(0, 11, 0), new Vector3(9, 11, 0)
                    });
                generatedCount++;

                // =========================================================================
                // 2. PHÒNG THƯỞNG KHỞI ĐẦU (STARTER REWARD - TẦNG 2)
                // =========================================================================
                CreateRoomPrefab("Room_Starter_Sanctuary", RunNodeType.StarterReward, 24, 24, 20,
                    "Thánh điện sơ khởi thanh tịnh ban tặng kỹ năng mở đầu và mở ra 2 ngã rẽ hành trình.",
                    floorTile, floorTileAlt, wallRuleTile, playerSpawnPrefab, null, portalPrefab, buffAltarPrefab,
                    setupWalls: (walls) =>
                    {
                        // 4 trụ góc nhỏ
                        FillWallRect(walls, wallRuleTile, -7, -6, 5, 6);
                        FillWallRect(walls, wallRuleTile, 6, 7, 5, 6);
                        FillWallRect(walls, wallRuleTile, -7, -6, -6, -5);
                        FillWallRect(walls, wallRuleTile, 6, 7, -6, -5);
                    },
                    playerPos: new Vector3(0, -7, 0),
                    enemyPositions: null,
                    portalPositions: new[]
                    {
                        new Vector3(-5, 8, 0), new Vector3(5, 8, 0) // 2 cổng dẫn vào Nhánh 3A và 3B
                    },
                    interactionPos: new Vector3(0, 1.5f, 0));
                generatedCount++;

                // =========================================================================
                // 3. PHÒNG QUÁI TINH ANH (ELITE BATTLE)
                // =========================================================================

                // 3.1 Đấu Trường Giác Đấu Elite (Colosseum)
                CreateRoomPrefab("Room_Elite_Colosseum", RunNodeType.EliteBattle, 38, 38, 15,
                    "Đấu trường Elite hình tròn với vòng cột đá giác đấu bao quanh tâm.",
                    floorTile, floorTileAlt, wallRuleTile, playerSpawnPrefab, enemySpawnPrefab, portalPrefab, null,
                    setupWalls: (walls) =>
                    {
                        // Vòng 8 trụ đá giác đấu
                        int count = 8;
                        float r = 9f;
                        for (int i = 0; i < count; i++)
                        {
                            float rad = i * (Mathf.PI * 2f / count);
                            int cx = Mathf.RoundToInt(Mathf.Cos(rad) * r);
                            int cy = Mathf.RoundToInt(Mathf.Sin(rad) * r);
                            FillWallRect(walls, wallRuleTile, cx - 1, cx, cy - 1, cy);
                        }
                    },
                    playerPos: new Vector3(0, -14, 0),
                    enemyPositions: new[]
                    {
                        new Vector3(0, 2, 0),     // Vị trí Boss Elite
                        new Vector3(-6, 6, 0),   new Vector3(6, 6, 0),
                        new Vector3(-6, -4, 0),  new Vector3(6, -4, 0),
                        new Vector3(-12, 4, 0),  new Vector3(12, 4, 0),
                        new Vector3(-12, -6, 0), new Vector3(12, -6, 0),
                        new Vector3(0, 10, 0),   new Vector3(0, -7, 0)
                    },
                    portalPositions: new[]
                    {
                        new Vector3(-10, 15, 0), new Vector3(0, 15, 0), new Vector3(10, 15, 0)
                    });
                generatedCount++;

                // 3.2 Mê Cung Góc Khuất Elite (Labyrinth)
                CreateRoomPrefab("Room_Elite_Labyrinth", RunNodeType.EliteBattle, 36, 36, 15,
                    "Phòng Elite với các khối tường chữ L tạo góc khuất phục kích nguy hiểm.",
                    floorTile, floorTileAlt, wallRuleTile, playerSpawnPrefab, enemySpawnPrefab, portalPrefab, null,
                    setupWalls: (walls) =>
                    {
                        // 4 góc tường chữ L
                        FillWallRect(walls, wallRuleTile, -8, -4, 6, 8);
                        FillWallRect(walls, wallRuleTile, -8, -6, 4, 6);

                        FillWallRect(walls, wallRuleTile, 4, 8, 6, 8);
                        FillWallRect(walls, wallRuleTile, 6, 8, 4, 6);

                        FillWallRect(walls, wallRuleTile, -8, -4, -8, -6);
                        FillWallRect(walls, wallRuleTile, -8, -6, -6, -4);

                        FillWallRect(walls, wallRuleTile, 4, 8, -8, -6);
                        FillWallRect(walls, wallRuleTile, 6, 8, -6, -4);
                    },
                    playerPos: new Vector3(0, -13, 0),
                    enemyPositions: new[]
                    {
                        new Vector3(0, 1, 0),
                        new Vector3(-11, 10, 0), new Vector3(11, 10, 0),
                        new Vector3(-11, -8, 0), new Vector3(11, -8, 0),
                        new Vector3(-5, 0, 0),   new Vector3(5, 0, 0),
                        new Vector3(0, 8, 0),    new Vector3(0, -6, 0),
                        new Vector3(-12, 1, 0),  new Vector3(12, 1, 0)
                    },
                    portalPositions: new[]
                    {
                        new Vector3(-9, 14, 0), new Vector3(0, 14, 0), new Vector3(9, 14, 0)
                    });
                generatedCount++;

                // =========================================================================
                // 4. ĐẠI ĐẤU TRƯỜNG MINI BOSS (TẦNG 7)
                // =========================================================================
                CreateRoomPrefab("Room_MiniBoss_Arena", RunNodeType.MiniBoss, 40, 40, 20,
                    "Đại đấu trường Mini Boss Tầng 7 với 4 trụ thành trì kiên cố bảo vệ khu vực trung tâm.",
                    floorTile, floorTileAlt, wallRuleTile, playerSpawnPrefab, enemySpawnPrefab, portalPrefab, null,
                    setupWalls: (walls) =>
                    {
                        // 4 đại trụ góc (4x4)
                        FillWallRect(walls, wallRuleTile, -12, -9, 9, 12);
                        FillWallRect(walls, wallRuleTile, 9, 12, 9, 12);
                        FillWallRect(walls, wallRuleTile, -12, -9, -12, -9);
                        FillWallRect(walls, wallRuleTile, 9, 12, -12, -9);
                        // 2 bờ tường chắn cánh
                        FillWallRect(walls, wallRuleTile, -14, -13, -2, 2);
                        FillWallRect(walls, wallRuleTile, 13, 14, -2, 2);
                    },
                    playerPos: new Vector3(0, -15, 0),
                    enemyPositions: new[]
                    {
                        new Vector3(0, 3, 0),    // Mini Boss Spawn Point
                        new Vector3(-7, 8, 0),   new Vector3(7, 8, 0),
                        new Vector3(-7, -4, 0),  new Vector3(7, -4, 0),
                        new Vector3(-13, 5, 0),  new Vector3(13, 5, 0),
                        new Vector3(-13, -7, 0), new Vector3(13, -7, 0)
                    },
                    portalPositions: new[]
                    {
                        new Vector3(-10, 16, 0), new Vector3(0, 16, 0), new Vector3(10, 16, 0)
                    });
                generatedCount++;

                // =========================================================================
                // 5. KHO BÁU ĐẠI THƯỞNG SAU MINI BOSS (BIG REWARD - TẦNG 8)
                // =========================================================================
                CreateRoomPrefab("Room_BigReward_Vault", RunNodeType.BigReward, 26, 26, 20,
                    "Kho báu hoàng gia linh thiêng ban thưởng lớn sau chiến thắng Mini Boss.",
                    floorTile, floorTileAlt, wallRuleTile, playerSpawnPrefab, null, portalPrefab, buffAltarPrefab,
                    setupWalls: (walls) =>
                    {
                        // Hàng cột hoàng gia 2 bên
                        FillWallRect(walls, wallRuleTile, -7, -6, 2, 4);
                        FillWallRect(walls, wallRuleTile, -7, -6, -4, -2);
                        FillWallRect(walls, wallRuleTile, 6, 7, 2, 4);
                        FillWallRect(walls, wallRuleTile, 6, 7, -4, -2);
                    },
                    playerPos: new Vector3(0, -8, 0),
                    enemyPositions: null,
                    portalPositions: new[]
                    {
                        new Vector3(-6, 9, 0), new Vector3(0, 9, 0), new Vector3(6, 9, 0)
                    },
                    interactionPos: new Vector3(0, 2f, 0));
                generatedCount++;

                // =========================================================================
                // 6. KHU LỬA TRẠI HỒI MÁU (REST SITE)
                // =========================================================================
                CreateRoomPrefab("Room_Rest_Campfire", RunNodeType.RestSite, 24, 24, 15,
                    "Khu vực lửa trại yên tĩnh giúp hồi phục sinh lực và chuẩn bị cho các thử thách phía trước.",
                    floorTile, floorTileAlt, wallRuleTile, playerSpawnPrefab, null, portalPrefab, campfirePrefab,
                    setupWalls: (walls) =>
                    {
                        // 2 tảng đá tự nhiên hai bên đống lửa làm ghế nghỉ
                        FillWallRect(walls, wallRuleTile, -5, -4, -1, 1);
                        FillWallRect(walls, wallRuleTile, 4, 5, -1, 1);
                    },
                    playerPos: new Vector3(0, -7, 0),
                    enemyPositions: null,
                    portalPositions: new[]
                    {
                        new Vector3(-6, 8, 0), new Vector3(0, 8, 0), new Vector3(6, 8, 0)
                    },
                    interactionPos: new Vector3(0, 0, 0));
                generatedCount++;

                // =========================================================================
                // 7. CỬA HÀNG THƯƠNG NHÂN (SHOP)
                // =========================================================================
                CreateRoomPrefab("Room_Shop_Bazaar", RunNodeType.Shop, 26, 24, 15,
                    "Thương xá bí ẩn với quầy hàng của thương nhân hầm ngục cung cấp trang bị và vật phẩm.",
                    floorTile, floorTileAlt, wallRuleTile, playerSpawnPrefab, null, portalPrefab, shopChestPrefab,
                    setupWalls: (walls) =>
                    {
                        // Quầy đá ngăn cách
                        FillWallRect(walls, wallRuleTile, -8, -2, 2, 3);
                        FillWallRect(walls, wallRuleTile, 2, 8, 2, 3);
                        // Thùng hàng góc
                        FillWallRect(walls, wallRuleTile, -8, -7, -4, -3);
                        FillWallRect(walls, wallRuleTile, 7, 8, -4, -3);
                    },
                    playerPos: new Vector3(0, -7, 0),
                    enemyPositions: null,
                    portalPositions: new[]
                    {
                        new Vector3(-6, 8, 0), new Vector3(0, 8, 0), new Vector3(6, 8, 0)
                    },
                    interactionPos: new Vector3(0, 4.2f, 0));
                generatedCount++;

                // =========================================================================
                // 8. ĐIỆN THỜ BAN PHƯỚC BUFF (BUFF REWARD)
                // =========================================================================
                CreateRoomPrefab("Room_Buff_Sanctuary", RunNodeType.BuffReward, 26, 26, 15,
                    "Thánh điện linh thiêng với hàng cột dẫn lối tới bệ thờ ban phước kỹ năng.",
                    floorTile, floorTileAlt, wallRuleTile, playerSpawnPrefab, null, portalPrefab, buffAltarPrefab,
                    setupWalls: (walls) =>
                    {
                        // Hàng cột dẫn đường
                        FillWallRect(walls, wallRuleTile, -5, -4, 4, 5);
                        FillWallRect(walls, wallRuleTile, -5, -4, 0, 1);
                        FillWallRect(walls, wallRuleTile, -5, -4, -4, -3);

                        FillWallRect(walls, wallRuleTile, 4, 5, 4, 5);
                        FillWallRect(walls, wallRuleTile, 4, 5, 0, 1);
                        FillWallRect(walls, wallRuleTile, 4, 5, -4, -3);
                    },
                    playerPos: new Vector3(0, -8, 0),
                    enemyPositions: null,
                    portalPositions: new[]
                    {
                        new Vector3(-6, 9, 0), new Vector3(0, 9, 0), new Vector3(6, 9, 0)
                    },
                    interactionPos: new Vector3(0, 4.5f, 0));
                generatedCount++;

                // =========================================================================
                // 9. ĐẠI ĐẤU TRƯỜNG NGAI VÀNG TRÙM CUỐI (BOSS BATTLE - TẦNG 13)
                // =========================================================================
                CreateRoomPrefab("Room_Boss_Throne", RunNodeType.BossBattle, 46, 46, 30,
                    "Phòng ngai vàng Trùm Cuối Tầng 13 - Trận chiến định mệnh quyết định số phận Run.",
                    floorTile, floorTileAlt, wallRuleTile, playerSpawnPrefab, enemySpawnPrefab, portalPrefab, null,
                    setupWalls: (walls) =>
                    {
                        // 6 đại trụ ngai vàng hoàng gia (4x4)
                        FillWallRect(walls, wallRuleTile, -14, -11, 10, 13);
                        FillWallRect(walls, wallRuleTile, 11, 14, 10, 13);
                        FillWallRect(walls, wallRuleTile, -14, -11, -4, -1);
                        FillWallRect(walls, wallRuleTile, 11, 14, -4, -1);
                        FillWallRect(walls, wallRuleTile, -9, -7, -14, -12);
                        FillWallRect(walls, wallRuleTile, 7, 9, -14, -12);
                    },
                    playerPos: new Vector3(0, -17, 0),
                    enemyPositions: new[]
                    {
                        new Vector3(0, 5, 0),    // Final Boss Spawn Point
                        new Vector3(-9, 12, 0),  new Vector3(9, 12, 0),
                        new Vector3(-9, -2, 0),  new Vector3(9, -2, 0),
                        new Vector3(-15, 6, 0),  new Vector3(15, 6, 0),
                        new Vector3(-15, -8, 0), new Vector3(15, -8, 0),
                        new Vector3(-5, 0, 0),   new Vector3(5, 0, 0),
                        new Vector3(0, 14, 0),   new Vector3(0, -9, 0)
                    },
                    portalPositions: new[]
                    {
                        new Vector3(0, 18, 0) // Cổng chiến thắng kết thúc Run
                    });
                generatedCount++;

                // =========================================================================
                // BỘ PHÒNG ĐA DẠNG MỞ RỘNG (12 BIẾN THỂ MỚI BỔ SUNG)
                // =========================================================================

                // 1.4 Phòng Vòng Xuyến Lõi Đá (Ring of Fire)
                CreateRoomPrefab("Room_Normal_RingOfFire", RunNodeType.NormalBattle, 34, 34, 12,
                    "Đấu trường hình xuyến với khối tường lõi trung tâm che chắn làn đạn, thích hợp chiến thuật kiting vòng tròn.",
                    floorTile, floorTileAlt, wallRuleTile, playerSpawnPrefab, enemySpawnPrefab, portalPrefab, null,
                    setupWalls: (walls) =>
                    {
                        FillWallRect(walls, wallRuleTile, -3, 3, -3, 3);
                        FillWallRect(walls, wallRuleTile, -12, -10, 8, 10);
                        FillWallRect(walls, wallRuleTile, 10, 12, 8, 10);
                        FillWallRect(walls, wallRuleTile, -12, -10, -10, -8);
                        FillWallRect(walls, wallRuleTile, 10, 12, -10, -8);
                    },
                    playerPos: new Vector3(0, -12, 0),
                    enemyPositions: new[]
                    {
                        new Vector3(-7, 7, 0), new Vector3(7, 7, 0),
                        new Vector3(-7, -7, 0), new Vector3(7, -7, 0),
                        new Vector3(-9, 0, 0), new Vector3(9, 0, 0),
                        new Vector3(0, 8, 0), new Vector3(0, -6, 0)
                    },
                    portalPositions: new[]
                    {
                        new Vector3(-8, 13, 0), new Vector3(0, 13, 0), new Vector3(8, 13, 0)
                    });
                generatedCount++;

                // 1.5 Phòng Ốc Đảo Đá (Island Chambers)
                CreateRoomPrefab("Room_Normal_IslandChambers", RunNodeType.NormalBattle, 36, 32, 10,
                    "Phòng chiến đấu với 3 ốc đảo đá so le tạo nên các lối rẽ quanh co và điểm ẩn nấp chiến thuật.",
                    floorTile, floorTileAlt, wallRuleTile, playerSpawnPrefab, enemySpawnPrefab, portalPrefab, null,
                    setupWalls: (walls) =>
                    {
                        FillWallRect(walls, wallRuleTile, -10, -4, 2, 4);
                        FillWallRect(walls, wallRuleTile, 4, 10, 2, 4);
                        FillWallRect(walls, wallRuleTile, -4, 4, -4, -2);
                    },
                    playerPos: new Vector3(0, -11, 0),
                    enemyPositions: new[]
                    {
                        new Vector3(-8, 9, 0), new Vector3(8, 9, 0),
                        new Vector3(-8, -6, 0), new Vector3(8, -6, 0),
                        new Vector3(-12, 2, 0), new Vector3(12, 2, 0),
                        new Vector3(0, 7, 0), new Vector3(0, -2, 0)
                    },
                    portalPositions: new[]
                    {
                        new Vector3(-8, 12, 0), new Vector3(0, 12, 0), new Vector3(8, 12, 0)
                    });
                generatedCount++;

                // 1.6 Phòng Chữ Z Ngoằn Ngoèo (ZigZag Run)
                CreateRoomPrefab("Room_Normal_ZigZagRun", RunNodeType.NormalBattle, 32, 36, 10,
                    "Phòng chiến đấu hành lang chữ Z buộc người chơi linh hoạt né đòn dọc các khúc cua hẹp.",
                    floorTile, floorTileAlt, wallRuleTile, playerSpawnPrefab, enemySpawnPrefab, portalPrefab, null,
                    setupWalls: (walls) =>
                    {
                        FillWallRect(walls, wallRuleTile, -11, 4, 3, 5);
                        FillWallRect(walls, wallRuleTile, -4, 11, -5, -3);
                    },
                    playerPos: new Vector3(0, -13, 0),
                    enemyPositions: new[]
                    {
                        new Vector3(-8, -9, 0), new Vector3(8, -9, 0),
                        new Vector3(-8, 0, 0), new Vector3(8, 0, 0),
                        new Vector3(-8, 9, 0), new Vector3(8, 9, 0),
                        new Vector3(0, -1, 0), new Vector3(0, 11, 0)
                    },
                    portalPositions: new[]
                    {
                        new Vector3(-7, 14, 0), new Vector3(0, 14, 0), new Vector3(7, 14, 0)
                    });
                generatedCount++;

                // 1.7 Phòng Hốc Phục Kích (Ambush Corners)
                CreateRoomPrefab("Room_Normal_AmbushCorners", RunNodeType.NormalBattle, 36, 36, 10,
                    "Đấu trường với 4 hốc tường phục kích nhô ra ở 4 góc, trung tâm sàn đấu mở rộng.",
                    floorTile, floorTileAlt, wallRuleTile, playerSpawnPrefab, enemySpawnPrefab, portalPrefab, null,
                    setupWalls: (walls) =>
                    {
                        FillWallRect(walls, wallRuleTile, -10, -7, 6, 10);
                        FillWallRect(walls, wallRuleTile, 7, 10, 6, 10);
                        FillWallRect(walls, wallRuleTile, -10, -7, -10, -6);
                        FillWallRect(walls, wallRuleTile, 7, 10, -10, -6);
                    },
                    playerPos: new Vector3(0, -12, 0),
                    enemyPositions: new[]
                    {
                        new Vector3(-12, 8, 0), new Vector3(12, 8, 0),
                        new Vector3(-12, -8, 0), new Vector3(12, -8, 0),
                        new Vector3(-6, 0, 0), new Vector3(6, 0, 0),
                        new Vector3(0, 9, 0), new Vector3(0, -3, 0)
                    },
                    portalPositions: new[]
                    {
                        new Vector3(-8, 14, 0), new Vector3(0, 14, 0), new Vector3(8, 14, 0)
                    });
                generatedCount++;

                // 3.3 Thánh Điện Bát Giác Elite (Octagon Sanctum)
                CreateRoomPrefab("Room_Elite_OctagonSanctum", RunNodeType.EliteBattle, 38, 38, 15,
                    "Đấu trường bát giác thiêng liêng với góc tường vát chéo và các gờ đá chắn ngang che chở bục chỉ huy quái tinh anh.",
                    floorTile, floorTileAlt, wallRuleTile, playerSpawnPrefab, enemySpawnPrefab, portalPrefab, null,
                    setupWalls: (walls) =>
                    {
                        FillWallRect(walls, wallRuleTile, -13, -9, 9, 13);
                        FillWallRect(walls, wallRuleTile, 9, 13, 9, 13);
                        FillWallRect(walls, wallRuleTile, -13, -9, -13, -9);
                        FillWallRect(walls, wallRuleTile, 9, 13, -13, -9);
                        FillWallRect(walls, wallRuleTile, -6, -4, -1, 3);
                        FillWallRect(walls, wallRuleTile, 4, 6, -1, 3);
                    },
                    playerPos: new Vector3(0, -14, 0),
                    enemyPositions: new[]
                    {
                        new Vector3(0, 3, 0), // Boss Elite
                        new Vector3(-8, 8, 0), new Vector3(8, 8, 0),
                        new Vector3(-8, -4, 0), new Vector3(8, -4, 0),
                        new Vector3(-12, 0, 0), new Vector3(12, 0, 0),
                        new Vector3(0, 11, 0), new Vector3(0, -7, 0)
                    },
                    portalPositions: new[]
                    {
                        new Vector3(-9, 15, 0), new Vector3(0, 15, 0), new Vector3(9, 15, 0)
                    });
                generatedCount++;

                // 3.4 Phòng Song Đấu Trụ Thành Trì (Twin Corridors)
                CreateRoomPrefab("Room_Elite_TwinCorridors", RunNodeType.EliteBattle, 36, 34, 15,
                    "Phòng tinh anh với 2 đại trụ phân chia đấu trường làm 2 cánh đối xứng.",
                    floorTile, floorTileAlt, wallRuleTile, playerSpawnPrefab, enemySpawnPrefab, portalPrefab, null,
                    setupWalls: (walls) =>
                    {
                        FillWallRect(walls, wallRuleTile, -3, -2, -7, 7);
                        FillWallRect(walls, wallRuleTile, 2, 3, -7, 7);
                    },
                    playerPos: new Vector3(0, -12, 0),
                    enemyPositions: new[]
                    {
                        new Vector3(0, 2, 0), // Boss Elite
                        new Vector3(-8, 7, 0), new Vector3(8, 7, 0),
                        new Vector3(-8, -3, 0), new Vector3(8, -3, 0),
                        new Vector3(-11, 4, 0), new Vector3(11, 4, 0),
                        new Vector3(-11, -5, 0), new Vector3(11, -5, 0)
                    },
                    portalPositions: new[]
                    {
                        new Vector3(-8, 13, 0), new Vector3(0, 13, 0), new Vector3(8, 13, 0)
                    });
                generatedCount++;

                // 6.2 Vườn Thiền Nghỉ Chân (Zen Garden Rest)
                CreateRoomPrefab("Room_Rest_ZenGarden", RunNodeType.RestSite, 26, 26, 15,
                    "Khu vườn thiền thanh bình với các tảng đá tự nhiên bao bọc quanh đống lửa trại ấm cúng.",
                    floorTile, floorTileAlt, wallRuleTile, playerSpawnPrefab, null, portalPrefab, campfirePrefab,
                    setupWalls: (walls) =>
                    {
                        FillWallRect(walls, wallRuleTile, -6, -5, 3, 5);
                        FillWallRect(walls, wallRuleTile, 5, 6, 3, 5);
                        FillWallRect(walls, wallRuleTile, -6, -5, -5, -3);
                        FillWallRect(walls, wallRuleTile, 5, 6, -5, -3);
                    },
                    playerPos: new Vector3(0, -7, 0),
                    enemyPositions: null,
                    portalPositions: new[]
                    {
                        new Vector3(-6, 9, 0), new Vector3(0, 9, 0), new Vector3(6, 9, 0)
                    },
                    interactionPos: new Vector3(0, 0, 0));
                generatedCount++;

                // 6.3 Hầm Mộ An Nghỉ Cổ Kính (Crypt Shrine Rest)
                CreateRoomPrefab("Room_Rest_CryptShrine", RunNodeType.RestSite, 24, 26, 15,
                    "Thánh địa lăng mộ cổ xưa yên bình với 4 bia đá linh thiêng bảo hộ ngọn lửa phục hồi sinh lực.",
                    floorTile, floorTileAlt, wallRuleTile, playerSpawnPrefab, null, portalPrefab, campfirePrefab,
                    setupWalls: (walls) =>
                    {
                        FillWallRect(walls, wallRuleTile, -5, -4, 1, 3);
                        FillWallRect(walls, wallRuleTile, 4, 5, 1, 3);
                        FillWallRect(walls, wallRuleTile, -5, -4, -3, -1);
                        FillWallRect(walls, wallRuleTile, 4, 5, -3, -1);
                    },
                    playerPos: new Vector3(0, -8, 0),
                    enemyPositions: null,
                    portalPositions: new[]
                    {
                        new Vector3(-6, 9, 0), new Vector3(0, 9, 0), new Vector3(6, 9, 0)
                    },
                    interactionPos: new Vector3(0, 0, 0));
                generatedCount++;

                // 7.2 Chợ Đen Hầm Ngục (Black Market Shop)
                CreateRoomPrefab("Room_Shop_BlackMarket", RunNodeType.Shop, 28, 26, 15,
                    "Thương hội chợ đen bí mật nằm sâu dưới lòng đất với các quầy đá kiên cố cất giữ bảo vật.",
                    floorTile, floorTileAlt, wallRuleTile, playerSpawnPrefab, null, portalPrefab, shopChestPrefab,
                    setupWalls: (walls) =>
                    {
                        FillWallRect(walls, wallRuleTile, -9, -3, 3, 4);
                        FillWallRect(walls, wallRuleTile, 3, 9, 3, 4);
                        FillWallRect(walls, wallRuleTile, -9, -7, -2, 0);
                        FillWallRect(walls, wallRuleTile, 7, 9, -2, 0);
                    },
                    playerPos: new Vector3(0, -8, 0),
                    enemyPositions: null,
                    portalPositions: new[]
                    {
                        new Vector3(-7, 9, 0), new Vector3(0, 9, 0), new Vector3(7, 9, 0)
                    },
                    interactionPos: new Vector3(0, 4.5f, 0));
                generatedCount++;

                // 8.2 Tế Đàn Thiên Thể (Celestial Altar Buff)
                CreateRoomPrefab("Room_Buff_CelestialAltar", RunNodeType.BuffReward, 28, 28, 15,
                    "Đàn tế thiên thể huyền bí với 4 tháp đá dẫn truyền năng lượng tinh tú ban phước lành.",
                    floorTile, floorTileAlt, wallRuleTile, playerSpawnPrefab, null, portalPrefab, buffAltarPrefab,
                    setupWalls: (walls) =>
                    {
                        FillWallRect(walls, wallRuleTile, -6, -5, 5, 6);
                        FillWallRect(walls, wallRuleTile, 5, 6, 5, 6);
                        FillWallRect(walls, wallRuleTile, -6, -5, -4, -3);
                        FillWallRect(walls, wallRuleTile, 5, 6, -4, -3);
                    },
                    playerPos: new Vector3(0, -8, 0),
                    enemyPositions: null,
                    portalPositions: new[]
                    {
                        new Vector3(-7, 10, 0), new Vector3(0, 10, 0), new Vector3(7, 10, 0)
                    },
                    interactionPos: new Vector3(0, 4.5f, 0));
                generatedCount++;

                // 4.2 Hầm Mộ Mini Boss Tử Thần (Catacomb Depths)
                CreateRoomPrefab("Room_MiniBoss_CatacombDepths", RunNodeType.MiniBoss, 42, 42, 20,
                    "Đại hầm mộ tử thần sâu thẳm với 6 cột đá đồ sộ che chắn các đợt càn quét kinh hoàng của Mini Boss.",
                    floorTile, floorTileAlt, wallRuleTile, playerSpawnPrefab, enemySpawnPrefab, portalPrefab, null,
                    setupWalls: (walls) =>
                    {
                        FillWallRect(walls, wallRuleTile, -13, -11, 8, 10);
                        FillWallRect(walls, wallRuleTile, 11, 13, 8, 10);
                        FillWallRect(walls, wallRuleTile, -13, -11, -3, -1);
                        FillWallRect(walls, wallRuleTile, 11, 13, -3, -1);
                        FillWallRect(walls, wallRuleTile, -13, -11, -12, -10);
                        FillWallRect(walls, wallRuleTile, 11, 13, -12, -10);
                    },
                    playerPos: new Vector3(0, -16, 0),
                    enemyPositions: new[]
                    {
                        new Vector3(0, 4, 0), // Mini Boss
                        new Vector3(-7, 9, 0), new Vector3(7, 9, 0),
                        new Vector3(-7, -3, 0), new Vector3(7, -3, 0),
                        new Vector3(-14, 4, 0), new Vector3(14, 4, 0),
                        new Vector3(-14, -7, 0), new Vector3(14, -7, 0)
                    },
                    portalPositions: new[]
                    {
                        new Vector3(-11, 17, 0), new Vector3(0, 17, 0), new Vector3(11, 17, 0)
                    });
                generatedCount++;

                // 9.2 Lõi Địa Ngục Trùm Cuối (Boss Inferno Core)
                CreateRoomPrefab("Room_Boss_InfernoCore", RunNodeType.BossBattle, 48, 48, 30,
                    "Lõi địa ngục rực lửa với 8 đại trụ dung nham, sàn đấu rộng lớn thênh thang cho trận thư hùng Trùm Cuối đỉnh cao.",
                    floorTile, floorTileAlt, wallRuleTile, playerSpawnPrefab, enemySpawnPrefab, portalPrefab, null,
                    setupWalls: (walls) =>
                    {
                        FillWallRect(walls, wallRuleTile, -15, -12, 11, 14);
                        FillWallRect(walls, wallRuleTile, 12, 15, 11, 14);
                        FillWallRect(walls, wallRuleTile, -15, -12, -3, 0);
                        FillWallRect(walls, wallRuleTile, 12, 15, -3, 0);
                        FillWallRect(walls, wallRuleTile, -15, -12, -14, -11);
                        FillWallRect(walls, wallRuleTile, 12, 15, -14, -11);
                        FillWallRect(walls, wallRuleTile, -6, -4, -17, -15);
                        FillWallRect(walls, wallRuleTile, 4, 6, -17, -15);
                    },
                    playerPos: new Vector3(0, -18, 0),
                    enemyPositions: new[]
                    {
                        new Vector3(0, 6, 0), // Final Boss
                        new Vector3(-9, 13, 0), new Vector3(9, 13, 0),
                        new Vector3(-9, -2, 0), new Vector3(9, -2, 0),
                        new Vector3(-16, 6, 0), new Vector3(16, 6, 0),
                        new Vector3(-16, -7, 0), new Vector3(16, -7, 0),
                        new Vector3(0, 15, 0), new Vector3(0, -8, 0)
                    },
                    portalPositions: new[]
                    {
                        new Vector3(0, 19, 0)
                    });
                generatedCount++;

                // =========================================================================
                // 10. ĐỒNG BỘ VÀO ROOM DATABASE
                // =========================================================================
                var db = AssetDatabase.LoadAssetAtPath<RoomDatabaseSO>(DB_PATH);
                if (db == null)
                {
                    db = ScriptableObject.CreateInstance<RoomDatabaseSO>();
                    AssetDatabase.CreateAsset(db, DB_PATH);
                }
                db.AutoFindAllVariationsInProject();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"<color=green><b>[DungeonRoomGenerator]</b></color> Đã tạo và cấu hình thành công {generatedCount} phòng mới cho Dungeon Run!");

                if (showDialog)
                {
                    EditorUtility.DisplayDialog("Khởi Tạo Phòng Thành Công! 🎉",
                        $"Hệ thống đã tự động tạo {generatedCount} Prefab phòng đa dạng chuẩn mực:\n\n" +
                        $"• 7 Phòng Quái Thường (FourPillars, CrossChamber, DualCorridor, RingOfFire, IslandChambers, ZigZagRun, AmbushCorners)\n" +
                        $"• 4 Phòng Tinh Anh Elite (Colosseum, Labyrinth, OctagonSanctum, TwinCorridors)\n" +
                        $"• 2 Phòng Mini Boss (MiniBoss Arena, Catacomb Depths)\n" +
                        $"• 2 Phòng Đại Chiến Trùm Cuối (Boss Throne Room, Inferno Core)\n" +
                        $"• 3 Phòng Lửa Trại Hồi Máu (Campfire, Zen Garden, Crypt Shrine)\n" +
                        $"• 2 Phòng Cửa Hàng (Bazaar, Black Market)\n" +
                        $"• 2 Phòng Điện Thờ Buff (Buff Sanctuary, Celestial Altar)\n" +
                        $"• 1 Phòng Thưởng Khởi Đầu (Starter Sanctuary)\n" +
                        $"• 1 Phòng Kho Báu Đại Thưởng (BigReward Vault)\n\n" +
                        $"Toàn bộ 24 phòng đã được đồng bộ vào RoomDatabase.asset!", "Tuyệt Vời");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DungeonRoomGenerator] Lỗi khi tạo phòng: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void CreateRoomPrefab(
            string roomName, RunNodeType roomType, int width, int height, int weight, string description,
            TileBase floorTile, TileBase floorTileAlt, TileBase wallRuleTile,
            GameObject playerPrefab, GameObject enemyPrefab, GameObject portalPrefab, GameObject interactionPrefab,
            Action<Tilemap> setupWalls, Vector3 playerPos, Vector3[] enemyPositions, Vector3[] portalPositions,
            Vector3? interactionPos = null)
        {
            var roomGO = new GameObject(roomName);
            roomGO.AddComponent<Grid>();

            var authoring = roomGO.AddComponent<DungeonRoomAuthoring>();
            authoring.roomName = roomName;
            authoring.roomType = roomType;
            authoring.weight = weight;
            authoring.description = description;

            var layout = roomGO.AddComponent<RoomLayout>();

            // 1. Tilemap Sàn (Floor)
            var floorGO = new GameObject("Tilemap_Floor");
            floorGO.transform.SetParent(roomGO.transform, false);
            var floorTm = floorGO.AddComponent<Tilemap>();
            var floorRend = floorGO.AddComponent<TilemapRenderer>();
            floorRend.sortingOrder = -10;

            int halfW = width / 2;
            int halfH = height / 2;

            for (int x = -halfW; x <= halfW; x++)
            {
                for (int y = -halfH; y <= halfH; y++)
                {
                    TileBase t = ((x + y) % 2 == 0) ? floorTile : floorTileAlt;
                    floorTm.SetTile(new Vector3Int(x, y, 0), t);
                }
            }

            // 2. Tilemap Tường & Vật Cản (Walls with Obstacle layer & Collider)
            var wallsGO = new GameObject("Tilemap_Walls");
            wallsGO.transform.SetParent(roomGO.transform, false);
            wallsGO.layer = DungeonTilePaletteTools.GetObstacleLayer();
            var wallsTm = wallsGO.AddComponent<Tilemap>();
            var wallsRend = wallsGO.AddComponent<TilemapRenderer>();
            wallsRend.sortingOrder = 0;
            wallsGO.AddComponent<TilemapCollider2D>();

            // Viền tường bao quanh phòng (2 lớp gạch cho chắc chắn)
            for (int x = -halfW; x <= halfW; x++)
            {
                wallsTm.SetTile(new Vector3Int(x, halfH, 0), wallRuleTile);
                wallsTm.SetTile(new Vector3Int(x, halfH - 1, 0), wallRuleTile);
                wallsTm.SetTile(new Vector3Int(x, -halfH, 0), wallRuleTile);
                wallsTm.SetTile(new Vector3Int(x, -halfH + 1, 0), wallRuleTile);
            }
            for (int y = -halfH; y <= halfH; y++)
            {
                wallsTm.SetTile(new Vector3Int(-halfW, y, 0), wallRuleTile);
                wallsTm.SetTile(new Vector3Int(-halfW + 1, y, 0), wallRuleTile);
                wallsTm.SetTile(new Vector3Int(halfW, y, 0), wallRuleTile);
                wallsTm.SetTile(new Vector3Int(halfW - 1, y, 0), wallRuleTile);
            }

            // Xây tường/chướng ngại vật bên trong theo bố cục chiến thuật riêng
            setupWalls?.Invoke(wallsTm);

            wallsTm.RefreshAllTiles();
            wallsTm.CompressBounds();

            // 3. Hệ Thống Điểm Xuất Hiện (SpawnPoints)
            var spawnPointsRoot = new GameObject("SpawnPoints").transform;
            spawnPointsRoot.SetParent(roomGO.transform, false);

            // 3.1 Player Spawn Point
            if (playerPrefab != null)
            {
                var pInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, spawnPointsRoot);
                PrefabUtility.UnpackPrefabInstance(pInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                pInstance.name = "PlayerSpawnPoint";
                pInstance.transform.localPosition = playerPos;
            }

            // 3.2 Enemy Spawn Points
            var enemyRoot = new GameObject("EnemySpawns").transform;
            enemyRoot.SetParent(spawnPointsRoot, false);
            if (enemyPrefab != null && enemyPositions != null && enemyPositions.Length > 0)
            {
                for (int i = 0; i < enemyPositions.Length; i++)
                {
                    var eInstance = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab, enemyRoot);
                    PrefabUtility.UnpackPrefabInstance(eInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    eInstance.name = $"EnemySpawn_{i + 1:D2}";
                    eInstance.transform.localPosition = enemyPositions[i];
                }
            }

            // 3.3 Portals
            var portalsRoot = new GameObject("Portals").transform;
            portalsRoot.SetParent(spawnPointsRoot, false);
            if (portalPrefab != null && portalPositions != null && portalPositions.Length > 0)
            {
                for (int i = 0; i < portalPositions.Length; i++)
                {
                    var ptInstance = (GameObject)PrefabUtility.InstantiatePrefab(portalPrefab, portalsRoot);
                    PrefabUtility.UnpackPrefabInstance(ptInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    ptInstance.name = $"RoomPortal_{i + 1:D2}";
                    ptInstance.transform.localPosition = portalPositions[i];
                    var sr = ptInstance.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.enabled = true;
                        sr.sortingOrder = 5;
                    }
                }
            }

            // 3.4 Interaction Points (Altar, Campfire, Shop Chest)
            var interactionsRoot = new GameObject("Interactions").transform;
            interactionsRoot.SetParent(spawnPointsRoot, false);
            if (interactionPrefab != null && interactionPos.HasValue)
            {
                var itInstance = (GameObject)PrefabUtility.InstantiatePrefab(interactionPrefab, interactionsRoot);
                PrefabUtility.UnpackPrefabInstance(itInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                itInstance.name = interactionPrefab.name;
                itInstance.transform.localPosition = interactionPos.Value;
                var sr = itInstance.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.enabled = true;
                    sr.sortingOrder = 5;
                }
            }

            // 4. Tự động kết nối và căn chỉnh RoomLayout
            layout.SetIdentity(roomName, roomType, new Vector2(width, height));
            layout.AutoBindReferences();
            layout.CalculateBoundsFromChildren();

            // 5. Lưu thành Prefab
            string prefabPath = $"{PREFAB_DIR}/{roomName}.prefab";
            var prefabAsset = PrefabUtility.SaveAsPrefabAsset(roomGO, prefabPath);
            UnityEngine.Object.DestroyImmediate(roomGO);

            // 6. Tạo RoomVariationSO
            string soPath = $"{SO_DIR}/{roomName}_Data.asset";
            var variationSO = AssetDatabase.LoadAssetAtPath<RoomVariationSO>(soPath);
            if (variationSO == null)
            {
                variationSO = ScriptableObject.CreateInstance<RoomVariationSO>();
                AssetDatabase.CreateAsset(variationSO, soPath);
            }
            variationSO.Initialize(roomName, roomType, prefabAsset, weight, description);
            EditorUtility.SetDirty(variationSO);
        }

        private static void FillWallRect(Tilemap tm, TileBase tile, int xMin, int xMax, int yMin, int yMax)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                for (int y = yMin; y <= yMax; y++)
                {
                    tm.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }
    }
}
#endif

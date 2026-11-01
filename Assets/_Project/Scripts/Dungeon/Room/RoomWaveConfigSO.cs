using System.Collections.Generic;
using UnityEngine;
using Project_D.Enemies;

namespace Project_D.Dungeon.Room
{
    [CreateAssetMenu(fileName = "RoomWaveConfig", menuName = "Project_D/Dungeon/Room Wave Config")]
    public class RoomWaveConfigSO : ScriptableObject
    {
        [Header("Room Wave Settings")]
        [SerializeField] private List<WaveConfig> _waves = new List<WaveConfig>();
        [SerializeField] private float _delayBetweenWaves = 1.5f;

        public IReadOnlyList<WaveConfig> Waves => _waves;
        public float DelayBetweenWaves => _delayBetweenWaves;
        public int TotalWaves => _waves.Count;

        // Tự động tạo cấu hình mẫu 3 Wave cho phòng quái thường với độ đa dạng cao
        [ContextMenu("Create Default 3-Wave Normal Battle")]
        public void CreateDefault3WaveNormal(EnemyDataSO defaultEnemy = null)
        {
            _waves.Clear();
            _delayBetweenWaves = 1.5f;

            var types = defaultEnemy != null ? new[] { defaultEnemy } : null;

            _waves.Add(new WaveConfig("Đợt 1 / 3: Tiền Trạm", 8, 0.4f, types));
            _waves.Add(new WaveConfig("Đợt 2 / 3: Tăng Cường", 12, 0.35f, types));
            _waves.Add(new WaveConfig("Đợt 3 / 3: Tổng Lực", 16, 0.3f, types));
        }

        public void PopulateNormalBattleWaves(EnemyDataSO swarmer, EnemyDataSO zombie, EnemyDataSO cultist, EnemyDataSO bomber, EnemyDataSO golem)
        {
            _waves.Clear();
            _delayBetweenWaves = 1.5f;

            var wave1Types = new List<EnemyDataSO>();
            if (swarmer != null) wave1Types.Add(swarmer);
            if (zombie != null) wave1Types.Add(zombie);

            var wave2Types = new List<EnemyDataSO>();
            if (zombie != null) wave2Types.Add(zombie);
            if (cultist != null) wave2Types.Add(cultist);
            if (bomber != null) wave2Types.Add(bomber);

            var wave3Types = new List<EnemyDataSO>();
            if (golem != null) wave3Types.Add(golem);
            if (cultist != null) wave3Types.Add(cultist);
            if (bomber != null) wave3Types.Add(bomber);
            if (swarmer != null) wave3Types.Add(swarmer);

            _waves.Add(new WaveConfig("Đợt 1 / 3: Tiền Trạm (Bầy Xác Sống)", 8, 0.35f, wave1Types.ToArray()));
            _waves.Add(new WaveConfig("Đợt 2 / 3: Hỏa Lực & Bom Cảm Tử", 12, 0.3f, wave2Types.ToArray()));
            _waves.Add(new WaveConfig("Đợt 3 / 3: Tổng Lực Thiết Giáp", 16, 0.25f, wave3Types.ToArray()));
        }

        // Tự động tạo cấu hình mẫu 4 Wave cho phòng Elite
        [ContextMenu("Create Default 4-Wave Elite Battle")]
        public void CreateDefault4WaveElite(EnemyDataSO defaultEnemy = null)
        {
            _waves.Clear();
            _delayBetweenWaves = 1.5f;

            var types = defaultEnemy != null ? new[] { defaultEnemy } : null;

            _waves.Add(new WaveConfig("Đợt 1 / 4: Dò Đường", 8, 0.35f, types));
            _waves.Add(new WaveConfig("Đợt 2 / 4: Quái Đông", 14, 0.3f, types));
            _waves.Add(new WaveConfig("Đợt 3 / 4: Vây Hãm", 18, 0.25f, types));
            _waves.Add(new WaveConfig("Đợt 4 / 4: Tinh Anh Trỗi Dậy", 22, 0.2f, types));
        }

        public void PopulateEliteBattleWaves(EnemyDataSO golem, EnemyDataSO cultist, EnemyDataSO bomber, EnemyDataSO swarmer)
        {
            _waves.Clear();
            _delayBetweenWaves = 1.5f;

            var w1 = new List<EnemyDataSO>();
            if (golem != null) w1.Add(golem);
            if (swarmer != null) w1.Add(swarmer);

            var w2 = new List<EnemyDataSO>();
            if (cultist != null) w2.Add(cultist);
            if (bomber != null) w2.Add(bomber);

            var w3 = new List<EnemyDataSO>();
            if (golem != null) w3.Add(golem);
            if (cultist != null) w3.Add(cultist);
            if (bomber != null) w3.Add(bomber);

            var w4 = new List<EnemyDataSO>();
            if (golem != null) w4.Add(golem);
            if (cultist != null) w4.Add(cultist);

            _waves.Add(new WaveConfig("Đợt 1 / 4: Thiết Giáp Dò Đường", 10, 0.3f, w1.ToArray()));
            _waves.Add(new WaveConfig("Đợt 2 / 4: Giáo Phái Hỏa Lực", 14, 0.25f, w2.ToArray()));
            _waves.Add(new WaveConfig("Đợt 3 / 4: Trận Địa Phục Kích", 18, 0.22f, w3.ToArray()));
            _waves.Add(new WaveConfig("Đợt 4 / 4: Tinh Anh Hủy Diệt", 22, 0.2f, w4.ToArray()));
        }

        public void PopulateMiniBossWaves(EnemyDataSO miniBoss, EnemyDataSO[] minions)
        {
            _waves.Clear();
            _delayBetweenWaves = 1.8f;

            _waves.Add(new WaveConfig("Đợt 1 / 3: Vệ Binh Tiên Phong", 10, 0.3f, minions));
            _waves.Add(new WaveConfig("Đợt 2 / 3: Bão Quái Hộ Giá", 14, 0.25f, minions));

            // Đợt 3: MiniBoss xuất hiện cùng 6 tiểu quái
            var bossWaveTypes = new List<EnemyDataSO>();
            if (miniBoss != null) bossWaveTypes.Add(miniBoss);
            if (minions != null) bossWaveTypes.AddRange(minions);

            _waves.Add(new WaveConfig("Đợt 3 / 3: TIỂU MA VƯƠNG XUẤT HIỆN!", 8, 0.4f, bossWaveTypes.ToArray()));
        }

        public void PopulateBossWaves(EnemyDataSO boss, EnemyDataSO[] minions)
        {
            _waves.Clear();
            _delayBetweenWaves = 2.0f;

            _waves.Add(new WaveConfig("Đợt 1 / 3: Đội Cận Vệ Hoàng Gia", 12, 0.3f, minions));
            _waves.Add(new WaveConfig("Đợt 2 / 3: Cuồng Nộ Huyết Tế", 16, 0.25f, minions));

            // Đợt 3: Boss tối thượng xuất kích cùng tùy tùng
            var bossWaveTypes = new List<EnemyDataSO>();
            if (boss != null) bossWaveTypes.Add(boss);
            if (minions != null) bossWaveTypes.AddRange(minions);

            _waves.Add(new WaveConfig("Đợt 3 / 3: ĐẠI CHIẾN MA VƯƠNG TỐI THƯỢNG!", 10, 0.4f, bossWaveTypes.ToArray()));
        }

        private void OnValidate()
        {
            if (_waves == null || _waves.Count == 0)
            {
                CreateDefault3WaveNormal();
            }
        }
    }
}

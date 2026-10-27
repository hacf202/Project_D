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

        // Tự động tạo cấu hình mẫu 3 Wave cho phòng quái thường
        [ContextMenu("Create Default 3-Wave Normal Battle")]
        public void CreateDefault3WaveNormal(EnemyDataSO defaultEnemy = null)
        {
            _waves.Clear();
            _delayBetweenWaves = 1.5f;

            var types = defaultEnemy != null ? new[] { defaultEnemy } : null;

            _waves.Add(new WaveConfig("Đợt 1 / 3: Tiền Trạm", 6, 0.4f, types));
            _waves.Add(new WaveConfig("Đợt 2 / 3: Tăng Cường", 10, 0.35f, types));
            _waves.Add(new WaveConfig("Đợt 3 / 3: Tổng Lực", 15, 0.3f, types));
        }

        // Tự động tạo cấu hình mẫu 4 Wave cho phòng Elite / Mini Boss
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

        private void OnValidate()
        {
            if (_waves == null || _waves.Count == 0)
            {
                CreateDefault3WaveNormal();
            }
        }
    }
}

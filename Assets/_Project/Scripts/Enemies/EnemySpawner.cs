using System.Collections.Generic;
using UnityEngine;
using Project_D.Core;
using Project_D.Player;

namespace Project_D.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Enemy Configurations")]
        [SerializeField] private EnemyDataSO[] _enemyTypes;
        [SerializeField] private int _prewarmCountPerType = 20;

        [Header("Spawn Rhythm")]
        [SerializeField] private float _spawnInterval = 1.5f;
        [SerializeField] private int _enemiesPerWave = 2;
        [SerializeField] private bool _autoStartSpawning = true;

        [Header("Spawn Distance (Camera Edge Buffer)")]
        [SerializeField] private float _minSpawnRadius = 12f; // Ngoài rìa màn hình camera
        [SerializeField] private float _maxSpawnRadius = 14f; // Nằm an toàn bên trong lưới 30x30

        private Transform _playerTransform;
        private float _spawnTimer;
        private bool _isSpawning;

        private void Awake()
        {
            // Nếu Scene có RoomController điều phối phòng theo Wave thì Spawner vô tận này tự động vô hiệu hóa
            if (Object.FindAnyObjectByType<Project_D.Dungeon.Room.RoomController>() != null)
            {
                _isSpawning = false;
                enabled = false;
            }
        }

        private void Start()
        {
            if (Project_D.Dungeon.Room.RoomController.Instance != null || Object.FindAnyObjectByType<Project_D.Dungeon.Room.RoomController>() != null)
            {
                _isSpawning = false;
                enabled = false;
                return;
            }

            FindPlayer();
            PrewarmPools();

            if (_autoStartSpawning)
            {
                _isSpawning = true;
            }
        }

        private void FindPlayer()
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                _playerTransform = player.transform;
            }
        }

        private void PrewarmPools()
        {
            if (PoolManager.Instance == null || _enemyTypes == null) return;

            var prewarmedGems = new HashSet<GameObject>();

            foreach (var enemyData in _enemyTypes)
            {
                if (enemyData == null) continue;

                if (enemyData.Prefab != null)
                {
                    PoolManager.Instance.Prewarm(enemyData.Prefab, _prewarmCountPerType);
                }

                if (enemyData.ExpGemPrefab != null && prewarmedGems.Add(enemyData.ExpGemPrefab))
                {
                    PoolManager.Instance.Prewarm(enemyData.ExpGemPrefab, 50);
                }
            }
        }

        private void Update()
        {
            if (!_isSpawning || _playerTransform == null || _enemyTypes == null || _enemyTypes.Length == 0)
                return;

            _spawnTimer += Time.deltaTime;

            if (_spawnTimer >= _spawnInterval)
            {
                _spawnTimer = 0f;
                SpawnWave();
            }
        }

        private void SpawnWave()
        {
            for (int i = 0; i < _enemiesPerWave; i++)
            {
                // Chọn ngẫu nhiên 1 loại quái trong danh sách
                EnemyDataSO randomEnemyData = _enemyTypes[Random.Range(0, _enemyTypes.Length)];
                if (randomEnemyData == null || randomEnemyData.Prefab == null) continue;

                // Tính toán vị trí sinh ngẫu nhiên ở vành đai ngoài màn hình
                Vector3 spawnPos = CalculateSpawnPosition();

                // Lấy quái từ PoolManager
                GameObject enemyObj = PoolManager.Instance.Get(randomEnemyData.Prefab, spawnPos, Quaternion.identity);

                if (enemyObj != null)
                {
                    if (enemyObj.TryGetComponent<EnemyController>(out var controller))
                    {
                        controller.Initialize(randomEnemyData, randomEnemyData.Prefab);
                    }
                }
            }
        }

        private Vector3 CalculateSpawnPosition()
        {
            // Góc ngẫu nhiên từ 0 đến 360 độ (tính bằng radian)
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(_minSpawnRadius, _maxSpawnRadius);

            float x = Mathf.Cos(angle) * distance;
            float y = Mathf.Sin(angle) * distance;

            return _playerTransform.position + new Vector3(x, y, 0f);
        }

        public void StartSpawning() => _isSpawning = true;
        public void StopSpawning() => _isSpawning = false;
    }
}

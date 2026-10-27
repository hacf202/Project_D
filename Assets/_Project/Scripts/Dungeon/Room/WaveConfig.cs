using System;
using System.Collections.Generic;
using UnityEngine;
using Project_D.Enemies;

namespace Project_D.Dungeon.Room
{
    [Serializable]
    public class WaveConfig
    {
        [SerializeField] private string _waveName = "Đợt 1";
        [SerializeField] private int _enemyCount = 8;
        [SerializeField] private float _spawnInterval = 0.5f;
        [SerializeField] private EnemyDataSO[] _enemyTypes;

        public string WaveName => _waveName;
        public int EnemyCount => _enemyCount;
        public float SpawnInterval => _spawnInterval;
        public EnemyDataSO[] EnemyTypes => _enemyTypes;

        public WaveConfig(string name, int count, float interval, EnemyDataSO[] types = null)
        {
            _waveName = name;
            _enemyCount = count;
            _spawnInterval = interval;
            _enemyTypes = types;
        }
    }
}

using UnityEngine;
using Project_D.Core;
using Project_D.Player;
using Project_D.Minions;

namespace Project_D.Enemies
{
    public class EnemyProjectile : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _speed = 6f;
        [SerializeField] private float _lifeTime = 3.5f;

        private GameObject _prefabOrigin;
        private float _currentLifeTime;
        private int _damage = 8;
        private bool _isDespawned;

        public int Damage => _damage;

        public void Initialize(GameObject prefabOrigin, int damage, float speed = 6f)
        {
            _prefabOrigin = prefabOrigin;
            _damage = damage;
            if (speed > 0f) _speed = speed;
            _isDespawned = false;
            _currentLifeTime = 0f;
        }

        private void OnEnable()
        {
            _currentLifeTime = 0f;
            _isDespawned = false;
        }

        private void Update()
        {
            if (_isDespawned) return;

            // Đạn tự động bay thẳng theo hướng mặt (Vector3.right theo trục xoay)
            transform.Translate(Vector3.right * (_speed * Time.deltaTime));

            _currentLifeTime += Time.deltaTime;
            if (_currentLifeTime >= _lifeTime)
            {
                Despawn();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isDespawned) return;

            // Gây sát thương lên người chơi
            if (other.TryGetComponent<PlayerHealth>(out var playerHealth) || (playerHealth = other.GetComponentInParent<PlayerHealth>()) != null)
            {
                if (playerHealth.IsAlive && !playerHealth.IsInvulnerable)
                {
                    _isDespawned = true;
                    playerHealth.TakeDamage(_damage);
                    Despawn();
                }
            }
            // Gây sát thương lên lính triệu hồi
            else if (other.TryGetComponent<MinionController>(out var minion) || (minion = other.GetComponentInParent<MinionController>()) != null)
            {
                if (minion.IsAlive)
                {
                    _isDespawned = true;
                    minion.TakeDamage(_damage);
                    Despawn();
                }
            }
            // Va chạm vào tường hoặc chướng ngại vật
            else if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
            {
                _isDespawned = true;
                Despawn();
            }
        }

        private void Despawn()
        {
            _isDespawned = true;

            if (PoolManager.Instance != null && _prefabOrigin != null)
            {
                PoolManager.Instance.ReturnToPool(_prefabOrigin, gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}

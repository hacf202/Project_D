using UnityEngine;
using Project_D.Core;
using Project_D.Enemies;

namespace Project_D.Minions
{
    public class MinionProjectile : MonoBehaviour
    {
        [Header("Projectile Physics")]
        [SerializeField] private float _speed = 12f;
        [SerializeField] private float _lifeTime = 2.5f;

        private GameObject _originPrefab;
        private int _damage = 10;
        private float _knockbackForce = 1f;
        private float _currentLifeTime = 0f;
        private bool _isDespawned = false;

        public void Initialize(GameObject originPrefab, int damage, float knockbackForce = 1f)
        {
            _originPrefab = originPrefab;
            _damage = damage;
            _knockbackForce = knockbackForce;
            _isDespawned = false;
        }

        private void OnEnable()
        {
            _currentLifeTime = 0f;
            _isDespawned = false;
        }

        private void Update()
        {
            if (_isDespawned) return;

            // Đạn bay về phía trước theo trục X (Vector3.right)
            transform.Translate(Vector3.right * (_speed * Time.deltaTime));

            _currentLifeTime += Time.deltaTime;
            if (_currentLifeTime >= _lifeTime)
            {
                Despawn();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Chặn đứng va chạm trùng trong cùng một physics frame (ví dụ: quái vật có Dual Colliders)
            if (_isDespawned) return;

            // Chỉ tương tác và gây sát thương lên Quái vật (Layer / Tag Enemy)
            if (other.CompareTag("Enemy") || other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                _isDespawned = true;

                if (other.TryGetComponent<EnemyController>(out var enemy))
                {
                    enemy.TakeDamage(_damage, transform.right, _knockbackForce);
                }

                Despawn();
            }
        }

        private void Despawn()
        {
            _isDespawned = true;

            if (PoolManager.Instance != null && _originPrefab != null)
            {
                PoolManager.Instance.ReturnToPool(_originPrefab, gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}

using UnityEngine;
using Project_D.Core;

namespace Project_D.Combat
{
    public class Projectile : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _speed = 12f;
        [SerializeField] private float _lifeTime = 2f;

        private GameObject _prefabOrigin;
        private float _currentLifeTime;
        private int _damage = 10;
        private float _knockbackForce = 0f;

        public int Damage => _damage;
        public float KnockbackForce => _knockbackForce;

        // Khởi tạo đạn với sát thương và lực đẩy lùi (tùy chọn) do Vũ khí (WeaponDataSO) truyền vào
        public void Initialize(GameObject prefabOrigin, int damage, float knockbackForce = 0f)
        {
            _prefabOrigin = prefabOrigin;
            _damage = damage;
            _knockbackForce = knockbackForce;
        }

        private void OnEnable()
        {
            _currentLifeTime = 0f;
        }

        private void Update()
        {
            // Đạn tự động bay thẳng theo hướng mũi nhọn (trục X - Vector3.right)
            transform.Translate(Vector3.right * (_speed * Time.deltaTime));

            _currentLifeTime += Time.deltaTime;
            if (_currentLifeTime >= _lifeTime)
            {
                Despawn();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Enemy"))
            {
                // Nếu quái có EnemyController thì trừ máu quái và truyền thêm hướng bay + lực giật lùi
                if (other.TryGetComponent<Project_D.Enemies.EnemyController>(out var enemy))
                {
                    enemy.TakeDamage(_damage, transform.right, _knockbackForce);
                }

                Despawn();
            }
        }

        private void Despawn()
        {
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

using UnityEngine;
using Project_D.Core;

namespace Project_D.UI
{
    public class DamageTextManager : MonoBehaviour
    {
        public static DamageTextManager Instance { get; private set; }

        [Header("Damage Text Configuration")]
        [SerializeField] private GameObject _damageTextPrefab;
        [SerializeField] private int _prewarmCount = 30;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (_damageTextPrefab == null)
            {
#if UNITY_EDITOR
                _damageTextPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/DamageText.prefab");
#endif
            }

            if (_damageTextPrefab != null && PoolManager.Instance != null)
            {
                PoolManager.Instance.Prewarm(_damageTextPrefab, _prewarmCount);
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            if (_damageTextPrefab == null)
            {
                _damageTextPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/DamageText.prefab");
            }
        }
#endif

        // Bắn ra số sát thương tại tọa độ quái vật
        public void Spawn(int damage, Vector3 worldPosition, bool isCrit = false)
        {
            if (_damageTextPrefab == null || PoolManager.Instance == null) return;

            // Độ lệch ngẫu nhiên nhẹ ở tâm quái để các con số không đè khít lên nhau
            Vector3 offset = new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0.2f, 0.45f), 0f);
            Vector3 spawnPosition = worldPosition + offset;

            GameObject obj = PoolManager.Instance.Get(_damageTextPrefab, spawnPosition, Quaternion.identity);
            if (obj != null && obj.TryGetComponent<DamageNumber>(out var damageNumber))
            {
                damageNumber.Initialize(damage, _damageTextPrefab, isCrit);
            }
        }
    }
}

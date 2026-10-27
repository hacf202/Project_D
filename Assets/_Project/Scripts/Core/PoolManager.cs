using System.Collections.Generic;
using UnityEngine;

namespace Project_D.Core
{
    public class PoolManager : MonoBehaviour
    {
        // Singleton để bất kỳ script nào (Weapon, Spawner) cũng có thể gọi PoolManager.Instance
        public static PoolManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        // Từ điển lưu trữ: Mỗi Prefab sẽ có một Queue chứa các vật thể đang rảnh rỗi (inactive)
        private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new Dictionary<GameObject, Queue<GameObject>>();
        // Tập hợp bảo vệ: Kiểm tra O(1) để ngăn chặn tuyệt đối lỗi nạp trùng một GameObject vào Pool
        private readonly Dictionary<GameObject, HashSet<GameObject>> _poolSets = new Dictionary<GameObject, HashSet<GameObject>>();

        // Hàm tạo sẵn một số lượng vật thể ngay từ đầu game để tránh lag
        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null) return;

            EnsurePoolContainers(prefab);

            for (int i = 0; i < count; i++)
            {
                GameObject obj = Instantiate(prefab, transform);
                obj.SetActive(false); // Tạo ra là tắt ngay, cất vào kho
                _pools[prefab].Enqueue(obj);
                _poolSets[prefab].Add(obj);
            }
        }

        private void EnsurePoolContainers(GameObject prefab)
        {
            if (!_pools.ContainsKey(prefab))
            {
                _pools[prefab] = new Queue<GameObject>();
            }
            if (!_poolSets.ContainsKey(prefab))
            {
                _poolSets[prefab] = new HashSet<GameObject>();
            }
        }

        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            EnsurePoolContainers(prefab);

            GameObject obj = null;

            // Nếu trong kho có hàng đang ngủ: Lấy ra dùng (đồng thời dọn sạch các đối tượng bị hủy ngoài ý muốn)
            while (_pools[prefab].Count > 0)
            {
                obj = _pools[prefab].Dequeue();
                _poolSets[prefab].Remove(obj);

                if (obj != null) break;
            }

            // Nếu kho hết hàng hoặc toàn bộ rỗng: Đẻ thêm 1 cái mới
            if (obj == null)
            {
                obj = Instantiate(prefab, transform);
            }

            // Đặt vị trí, hướng xoay và bật đối tượng lên
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);

            return obj;
        }

        // Bí danh thuận tiện (Alias) cho hàm Get
        public GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return Get(prefab, position, rotation);
        }

        public void ReturnToPool(GameObject prefab, GameObject instance)
        {
            if (instance == null || prefab == null) return;

            EnsurePoolContainers(prefab);

            // BẢO VỆ CHỐNG SOFTLOCK: Nếu đối tượng này ĐÃ NẰM TRONG KHO POOL rồi, bỏ qua ngay!
            if (_poolSets[prefab].Contains(instance))
            {
                Debug.LogWarning($"<color=yellow>[PoolManager Guard]</color> Đối tượng '{instance.name}' đã nằm trong Pool của '{prefab.name}'! Chặn đứng thao tác nạp trùng.");
                return;
            }

            // Tắt đối tượng đi
            instance.SetActive(false);

            // Cho đối tượng làm con của PoolManager để Hierarchy trong Unity gọn gàng
            instance.transform.SetParent(transform);

            // Nhét lại vào đuôi hàng đợi và ghi nhận vào tập hợp bảo vệ
            _pools[prefab].Enqueue(instance);
            _poolSets[prefab].Add(instance);
        }
    }
}

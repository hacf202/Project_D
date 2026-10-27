using System;
using System.Collections.Generic;
using UnityEngine;
using Project_D.Player;
using Project_D.Dungeon.Run;
using Project_D.Enemies.FlowField;

namespace Project_D.Dungeon.Room
{
    public class RoomManager : MonoBehaviour
    {
        public static RoomManager Instance { get; private set; }

        [Header("Room Database")]
        [SerializeField] private RoomDatabaseSO _roomDatabase;
        [SerializeField] private Transform _roomContainer;

        private RoomVariationSO _currentVariation;
        private GameObject _currentRoomInstance;
        private RoomLayout _currentRoomLayout;
        private Transform _playerTransform;

        public RoomLayout CurrentRoomLayout => _currentRoomLayout;
        public RoomVariationSO CurrentVariation => _currentVariation;
        public RoomDatabaseSO Database => _roomDatabase;

        public static event Action<RoomLayout> OnRoomLayoutLoaded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            EnsureDatabase();

            if (_roomContainer == null)
            {
                var existingContainer = GameObject.Find("CurrentRoom_Container");
                if (existingContainer != null)
                {
                    _roomContainer = existingContainer.transform;
                }
                else
                {
                    var container = new GameObject("CurrentRoom_Container");
                    container.transform.position = Vector3.zero;
                    if (transform.parent != null)
                    {
                        container.transform.SetParent(transform.parent);
                    }
                    _roomContainer = container.transform;
                }
            }

            FindPlayer();
        }

        public void EnsureDatabase()
        {
            if (_roomDatabase != null) return;

#if UNITY_EDITOR
            string dbPath = "Assets/_Project/ScriptableObjects/Dungeon/RoomDatabase.asset";
            _roomDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<RoomDatabaseSO>(dbPath);
#endif
            if (_roomDatabase == null)
            {
                _roomDatabase = Resources.Load<RoomDatabaseSO>("Dungeon/RoomDatabase");
            }
        }

        private void FindPlayer()
        {
            if (_playerTransform == null)
            {
                var player = FindAnyObjectByType<PlayerController>();
                if (player != null)
                {
                    _playerTransform = player.transform;
                }
            }
        }

        // Tải và kích hoạt một biến thể phòng tương ứng với RunNodeType
        public RoomLayout LoadRoomForNode(RunNodeData nodeData)
        {
            if (nodeData == null) return null;

            FindPlayer();
            EnsureDatabase();

            // 1. Dọn dẹp phòng cũ nếu có
            CleanUpCurrentRoom();

            // 2. Tra cứu trong RoomDatabaseSO để bốc 1 biến thể phòng ngẫu nhiên
            RoomVariationSO selectedVariation = null;
            if (_roomDatabase != null)
            {
                selectedVariation = _roomDatabase.GetRandomRoomForType(nodeData.NodeType, _currentVariation);
            }

            // 3. Nếu tìm thấy biến thể có Prefab: Instantiate phòng mới
            if (selectedVariation != null && selectedVariation.RoomPrefab != null)
            {
                _currentVariation = selectedVariation;
                _currentRoomInstance = Instantiate(selectedVariation.RoomPrefab, Vector3.zero, Quaternion.identity, _roomContainer);
                _currentRoomInstance.name = $"[ActiveRoom]_{selectedVariation.RoomName}";

                _currentRoomLayout = _currentRoomInstance.GetComponent<RoomLayout>();
                if (_currentRoomLayout == null)
                {
                    _currentRoomLayout = _currentRoomInstance.AddComponent<RoomLayout>();
                    _currentRoomLayout.AutoBindReferences();
                }

                Debug.Log($"<color=green><b>[ROOM MANAGER]</b></color> Đã nạp thành công phòng: '{_currentVariation.RoomName}' cho {nodeData.DisplayName}!");
            }
            else
            {
                // Fallback nếu chưa tạo Prefab phòng cho loại này: Sử dụng phòng hiện hữu trong Scene nếu có
                _currentRoomLayout = FindAnyObjectByType<RoomLayout>();
                Debug.LogWarning($"[ROOM MANAGER] Không tìm thấy Prefab biến thể cho '{nodeData.NodeType}' trong Database! Sử dụng phòng mặc định trong Scene.");
            }

            // 4. Đặt người chơi vào điểm xuất hiện PlayerSpawnPoint của phòng
            if (_playerTransform != null && _currentRoomLayout != null)
            {
                _playerTransform.position = _currentRoomLayout.PlayerSpawnPoint.position;
            }
            else if (_playerTransform != null)
            {
                _playerTransform.position = Vector3.zero;
            }

            // 5. Cập nhật FlowFieldManager để quái nhận diện bản đồ tường & vật cản của phòng mới
            if (FlowFieldManager.Instance != null)
            {
                FlowFieldManager.Instance.UpdateFlowField();
            }

            OnRoomLayoutLoaded?.Invoke(_currentRoomLayout);
            return _currentRoomLayout;
        }

        public void CleanUpCurrentRoom()
        {
            if (_currentRoomInstance != null)
            {
                Destroy(_currentRoomInstance);
                _currentRoomInstance = null;
                _currentRoomLayout = null;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project_D.Dungeon.Run
{
    public class RunManager : MonoBehaviour
    {
        public static RunManager Instance { get; private set; }

        [Header("Run Configuration")]
        [SerializeField] private RunMapConfigSO _mapConfig;

        [Header("Run State (Runtime)")]
        [SerializeField] private int _currentFloor = 1;
        [SerializeField] private int _currentSouls = 0;
        private RunNodeData _currentNode;
        private bool _isRunActive = false;

        // Observer Pattern Events
        public static event Action<int, RunNodeData> OnFloorChanged; // currentFloor, currentNode
        public static event Action<int> OnSoulsChanged;               // currentSouls
        public static event Action<RunNodeData> OnRoomEntered;       // nodeData
        public static event Action OnRunCompleted;                   // Victory

        public int CurrentFloor => _currentFloor;
        public int CurrentSouls => _currentSouls;
        public RunNodeData CurrentNode => _currentNode;
        public RunMapConfigSO MapConfig => _mapConfig;
        public bool IsRunActive => _isRunActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            // Tự động tìm hoặc sinh RunMapConfigSO nếu chưa gán
            if (_mapConfig == null)
            {
                _mapConfig = ScriptableObject.CreateInstance<RunMapConfigSO>();
                _mapConfig.BuildDefault13FloorRunMap();
            }
        }

        private void Start()
        {
            StartNewRun();
        }

        // Bắt đầu một lượt chơi (Run) mới từ Tầng 1
        public void StartNewRun()
        {
            _isRunActive = true;
            _currentSouls = 0;

            if (_mapConfig != null)
            {
                _currentNode = _mapConfig.GetRootNode();
                _currentFloor = _currentNode != null ? _currentNode.FloorNumber : 1;
            }
            else
            {
                _currentFloor = 1;
            }

            Debug.Log($"<color=green><b>[RUN STARTED]</b></color> Bắt đầu lượt chơi mới tại Tầng {_currentFloor} ({_currentNode?.DisplayName})");

            OnSoulsChanged?.Invoke(_currentSouls);
            OnFloorChanged?.Invoke(_currentFloor, _currentNode);

            if (_currentNode != null)
            {
                OnRoomEntered?.Invoke(_currentNode);
            }
        }

        // Người chơi bước vào cổng dịch chuyển để sang Node tiếp theo
        public void AdvanceToNode(int nextNodeId)
        {
            if (_mapConfig == null) return;

            var targetNode = _mapConfig.GetNodeById(nextNodeId);
            if (targetNode == null)
            {
                Debug.LogError($"[RUN MANAGER] Không tìm thấy Node ID {nextNodeId} trong bản đồ!");
                return;
            }

            _currentNode = targetNode;
            _currentFloor = _currentNode.FloorNumber;

            Debug.Log($"<color=cyan><b>[ADVANCE FLOOR]</b></color> Người chơi đã tiến vào: Tầng {_currentFloor} - {_currentNode.DisplayName} (Nhánh: {_currentNode.BranchName})");

            OnFloorChanged?.Invoke(_currentFloor, _currentNode);
            OnRoomEntered?.Invoke(_currentNode);
        }

        // Cộng thẳng Souls khi tiêu diệt quái (Zero Pickup Delay)
        public void AddSouls(int amount)
        {
            if (amount <= 0) return;

            _currentSouls += amount;
            Debug.Log($"<color=yellow>[SOULS +{amount}]</color> Hiện tại: {_currentSouls} Souls");
            OnSoulsChanged?.Invoke(_currentSouls);
        }

        // Tiêu Souls khi mua đồ ở Shop hoặc hồi đầy máu ở RestSite
        public bool SpendSouls(int amount)
        {
            if (amount <= 0) return true;

            if (_currentSouls >= amount)
            {
                _currentSouls -= amount;
                Debug.Log($"<color=orange>[SOULS SPENT -{amount}]</color> Còn lại: {_currentSouls} Souls");
                OnSoulsChanged?.Invoke(_currentSouls);
                return true;
            }

            Debug.LogWarning($"<color=red>[SOULS INSUFFICIENT]</color> Không đủ Souls! Cần {amount}, chỉ có {_currentSouls}");
            return false;
        }

        // Gọi khi hạ gục Trùm Cuối Tầng 13
        public void CompleteRun()
        {
            _isRunActive = false;
            Debug.Log("<color=gold><b>🏆 [RUN COMPLETED - VICTORY!]</b></color> Chúc mừng! Bạn đã hoàn thành xuất sắc 13 Tầng Hầm Ngục!");
            OnRunCompleted?.Invoke();
        }

        // Lấy danh sách các Node nhánh rẽ kế tiếp của phòng hiện tại
        public List<RunNodeData> GetCurrentNextNodes()
        {
            if (_mapConfig == null || _currentNode == null) return new List<RunNodeData>();
            return _mapConfig.GetNextNodes(_currentNode);
        }
    }
}

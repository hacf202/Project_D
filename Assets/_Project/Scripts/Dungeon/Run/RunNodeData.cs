using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project_D.Dungeon.Run
{
    [Serializable]
    public class RunNodeData
    {
        [SerializeField] private int _id;
        [SerializeField] private int _floorNumber;
        [SerializeField] private string _branchName;
        [SerializeField] private RunNodeType _nodeType;
        [SerializeField] private string _displayName;
        [SerializeField] private Color _nodeColor = Color.white;
        [SerializeField] private List<int> _nextNodeIds = new List<int>();
        [Tooltip("Gán trực tiếp Prefab phòng bạn tự thiết kế cho Node này. Nếu để trống sẽ bốc ngẫu nhiên từ RoomDatabase.")]
        [SerializeField] private GameObject _customRoomPrefab;
        [Tooltip("Tọa độ tự do của Node trên Canvas thiết kế 2D để người dùng kéo thả tùy ý.")]
        [SerializeField] private Vector2 _editorPosition = Vector2.zero;

        public int Id => _id;
        public int FloorNumber => _floorNumber;
        public string BranchName => _branchName;
        public RunNodeType NodeType => _nodeType;
        public string DisplayName => _displayName;
        public Color NodeColor => _nodeColor;
        public List<int> NextNodeIds => _nextNodeIds;
        public GameObject CustomRoomPrefab => _customRoomPrefab;
        public Vector2 EditorPosition => _editorPosition;

        /// <summary>
        /// Kiểm tra phòng này có còn slot để nối thêm phòng kế tiếp hay không (Tối đa 3 phòng).
        /// </summary>
        public bool CanAddNextNode => _nextNodeIds == null || _nextNodeIds.Count < RunMapConfigSO.MaxPortalsPerRoom;

        public void SetFloorNumber(int floor) => _floorNumber = floor;
        public void SetBranchName(string branch) => _branchName = branch;
        public void SetDisplayName(string name) => _displayName = name;
        public void SetNodeType(RunNodeType type) => _nodeType = type;
        public void SetNodeColor(Color color) => _nodeColor = color;
        public void SetCustomRoomPrefab(GameObject prefab) => _customRoomPrefab = prefab;
        public void SetEditorPosition(Vector2 pos) => _editorPosition = pos;

        public RunNodeData(int id, int floorNumber, string branchName, RunNodeType nodeType, string displayName, Color color, params int[] nextNodeIds)
        {
            _id = id;
            _floorNumber = floorNumber;
            _branchName = branchName;
            _nodeType = nodeType;
            _displayName = displayName;
            _nodeColor = color;
            _editorPosition = Vector2.zero;
            _nextNodeIds = new List<int>(nextNodeIds);
        }

        public RunNodeData(int id, int floorNumber, string branchName, RunNodeType nodeType, string displayName, Color color, Vector2 editorPosition, params int[] nextNodeIds)
        {
            _id = id;
            _floorNumber = floorNumber;
            _branchName = branchName;
            _nodeType = nodeType;
            _displayName = displayName;
            _nodeColor = color;
            _editorPosition = editorPosition;
            _nextNodeIds = new List<int>(nextNodeIds);
        }

        // Kiểm tra xem phòng này có phải phòng chiến đấu hay không
        public bool IsCombatRoom => _nodeType == RunNodeType.NormalBattle ||
                                   _nodeType == RunNodeType.EliteBattle ||
                                   _nodeType == RunNodeType.MiniBoss ||
                                   _nodeType == RunNodeType.BossBattle;
    }
}

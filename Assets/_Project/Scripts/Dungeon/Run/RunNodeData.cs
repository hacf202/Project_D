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

        public int Id => _id;
        public int FloorNumber => _floorNumber;
        public string BranchName => _branchName;
        public RunNodeType NodeType => _nodeType;
        public string DisplayName => _displayName;
        public Color NodeColor => _nodeColor;
        public List<int> NextNodeIds => _nextNodeIds;

        public RunNodeData(int id, int floorNumber, string branchName, RunNodeType nodeType, string displayName, Color color, params int[] nextNodeIds)
        {
            _id = id;
            _floorNumber = floorNumber;
            _branchName = branchName;
            _nodeType = nodeType;
            _displayName = displayName;
            _nodeColor = color;
            _nextNodeIds = new List<int>(nextNodeIds);
        }

        // Kiểm tra xem phòng này có phải phòng chiến đấu hay không
        public bool IsCombatRoom => _nodeType == RunNodeType.NormalBattle ||
                                   _nodeType == RunNodeType.EliteBattle ||
                                   _nodeType == RunNodeType.MiniBoss ||
                                   _nodeType == RunNodeType.BossBattle;
    }
}

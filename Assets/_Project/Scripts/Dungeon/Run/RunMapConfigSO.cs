using System.Collections.Generic;
using UnityEngine;

namespace Project_D.Dungeon.Run
{
    [CreateAssetMenu(fileName = "RunMap_13Floors", menuName = "Project_D/Dungeon/Run Map Config")]
    public class RunMapConfigSO : ScriptableObject
    {
        [Header("Run Configuration")]
        [SerializeField] private int _totalFloors = 13;
        [SerializeField] private List<RunNodeData> _allNodes = new List<RunNodeData>();

        public int TotalFloors => _totalFloors;
        public IReadOnlyList<RunNodeData> AllNodes => _allNodes;

        public RunNodeData GetNodeById(int id)
        {
            return _allNodes.Find(n => n.Id == id);
        }

        public RunNodeData GetRootNode()
        {
            return GetNodeById(1);
        }

        public List<RunNodeData> GetNodesForFloor(int floorNumber)
        {
            return _allNodes.FindAll(n => n.FloorNumber == floorNumber);
        }

        public List<RunNodeData> GetNextNodes(RunNodeData currentNode)
        {
            var result = new List<RunNodeData>();
            if (currentNode == null) return result;

            foreach (int nextId in currentNode.NextNodeIds)
            {
                var nextNode = GetNodeById(nextId);
                if (nextNode != null)
                {
                    result.Add(nextNode);
                }
            }
            return result;
        }

        // Tự động khởi tạo chuẩn 22 Nút theo đúng cấu trúc 13 Tầng của người dùng
        [ContextMenu("Build Default 13-Floor Run Map")]
        public void BuildDefault13FloorRunMap()
        {
            _allNodes.Clear();
            _totalFloors = 13;

            // Định nghĩa màu đại diện
            Color battleColor = new Color(0.9f, 0.3f, 0.3f);     // Đỏ kiếm
            Color eliteColor = new Color(0.9f, 0.15f, 0.5f);    // Hồng tím đậm
            Color miniBossColor = new Color(0.85f, 0.2f, 0.9f);  // Tím quỷ
            Color bossColor = new Color(1f, 0.2f, 0.1f);        // Đỏ lửa trùm
            Color restColor = new Color(0.2f, 0.85f, 0.35f);    // Xanh lá cây
            Color shopColor = new Color(1f, 0.85f, 0.2f);       // Vàng kim
            Color rewardColor = new Color(0.3f, 0.75f, 1f);     // Xanh lam ngọc

            // Tầng 1: Quái thường
            _allNodes.Add(new RunNodeData(1, 1, "1", RunNodeType.NormalBattle, "Tầng 1: Quái Thường", battleColor, 2));

            // Tầng 2: Thưởng khởi đầu
            _allNodes.Add(new RunNodeData(2, 2, "2", RunNodeType.StarterReward, "Tầng 2: Thưởng Khởi Đầu", rewardColor, 3, 4));

            // Tầng 3: Phân 2 nhánh (Quái thường)
            _allNodes.Add(new RunNodeData(3, 3, "3A", RunNodeType.NormalBattle, "Tầng 3A: Quái Thường", battleColor, 5, 6));
            _allNodes.Add(new RunNodeData(4, 3, "3B", RunNodeType.NormalBattle, "Tầng 3B: Quái Thường", battleColor, 6, 7));

            // Tầng 4: Phân 3 nhánh (Cửa hàng, Buff, Hồi máu)
            _allNodes.Add(new RunNodeData(5, 4, "4-Shop", RunNodeType.Shop, "Tầng 4: Cửa Hàng", shopColor, 8));
            _allNodes.Add(new RunNodeData(6, 4, "4-Buff", RunNodeType.BuffReward, "Tầng 4: Nhận Buff", rewardColor, 8, 9));
            _allNodes.Add(new RunNodeData(7, 4, "4-Rest", RunNodeType.RestSite, "Tầng 4: Lửa Trại Hồi Máu", restColor, 9));

            // Tầng 5: Phân 2 nhánh (Quái thường hoặc Elite)
            _allNodes.Add(new RunNodeData(8, 5, "5A", RunNodeType.NormalBattle, "Tầng 5A: Quái Thường", battleColor, 10, 11));
            _allNodes.Add(new RunNodeData(9, 5, "5B-Elite", RunNodeType.EliteBattle, "Tầng 5B: Tinh Anh Elite", eliteColor, 10, 11));

            // Tầng 6: Phân 2 nhánh (Hồi máu hoặc Cửa hàng)
            _allNodes.Add(new RunNodeData(10, 6, "6-Rest", RunNodeType.RestSite, "Tầng 6: Lửa Trại Hồi Máu", restColor, 12));
            _allNodes.Add(new RunNodeData(11, 6, "6-Shop", RunNodeType.Shop, "Tầng 6: Cửa Hàng", shopColor, 12));

            // Tầng 7: Hội tụ đánh Mini Boss
            _allNodes.Add(new RunNodeData(12, 7, "7-MiniBoss", RunNodeType.MiniBoss, "Tầng 7: MINI BOSS", miniBossColor, 13));

            // Tầng 8: Nhận buff thưởng lớn sau Mini Boss
            _allNodes.Add(new RunNodeData(13, 8, "8-BigBuff", RunNodeType.BigReward, "Tầng 8: Buff Lớn Sau Mini Boss", rewardColor, 14, 15));

            // Tầng 9: Phân 2 nhánh (Quái thường hoặc Elite)
            _allNodes.Add(new RunNodeData(14, 9, "9A", RunNodeType.NormalBattle, "Tầng 9A: Quái Thường", battleColor, 16, 17));
            _allNodes.Add(new RunNodeData(15, 9, "9B-Elite", RunNodeType.EliteBattle, "Tầng 9B: Tinh Anh Elite", eliteColor, 16, 17));

            // Tầng 10: Phân 2 nhánh (Nhận buff hoặc Hồi máu)
            _allNodes.Add(new RunNodeData(16, 10, "10-Buff", RunNodeType.BuffReward, "Tầng 10: Nhận Buff", rewardColor, 18, 19));
            _allNodes.Add(new RunNodeData(17, 10, "10-Rest", RunNodeType.RestSite, "Tầng 10: Lửa Trại Hồi Máu", restColor, 19, 20));

            // Tầng 11: Phân 3 nhánh (Quái thường hoặc Elite)
            _allNodes.Add(new RunNodeData(18, 11, "11A", RunNodeType.NormalBattle, "Tầng 11A: Quái Thường", battleColor, 21));
            _allNodes.Add(new RunNodeData(19, 11, "11B-Elite", RunNodeType.EliteBattle, "Tầng 11B: Tinh Anh Elite", eliteColor, 21));
            _allNodes.Add(new RunNodeData(20, 11, "11C-Elite", RunNodeType.EliteBattle, "Tầng 11C: Tinh Anh Elite", eliteColor, 21));

            // Tầng 12: Hội tụ (Hồi máu chuẩn bị đánh Boss)
            _allNodes.Add(new RunNodeData(21, 12, "12-Rest", RunNodeType.RestSite, "Tầng 12: Lửa Trại Chuẩn Bị", restColor, 22));

            // Tầng 13: Đánh Boss hoàn thành 1 Run
            _allNodes.Add(new RunNodeData(22, 13, "13-BOSS", RunNodeType.BossBattle, "Tầng 13: TRÙM CUỐI BOSS", bossColor));

            Debug.Log($"<color=cyan>[RUN MAP CONFIG]</color> Đã tạo đồ thị chuẩn 13 Tầng ({_allNodes.Count} Nút liên thông) thành công!");
        }

        private void OnValidate()
        {
            if (_allNodes == null || _allNodes.Count == 0)
            {
                BuildDefault13FloorRunMap();
            }
        }
    }
}

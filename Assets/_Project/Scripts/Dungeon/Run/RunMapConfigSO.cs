using System.Collections.Generic;
using UnityEngine;

namespace Project_D.Dungeon.Run
{
    [CreateAssetMenu(fileName = "RunMap_13Floors", menuName = "Project_D/Dungeon/Run Map Config")]
    public class RunMapConfigSO : ScriptableObject
    {
        /// <summary>
        /// Ràng buộc kiến trúc cốt lõi: Mỗi phòng chỉ được phép nối tối đa 3 phòng tiếp theo (Tương ứng với 3 cổng vật lý: Trái, Giữa, Phải).
        /// </summary>
        public const int MaxPortalsPerRoom = 3;

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
            var floor1Nodes = GetNodesForFloor(1);
            if (floor1Nodes != null && floor1Nodes.Count > 0)
            {
                return floor1Nodes[0];
            }
            return GetNodeById(1);
        }

        public List<RunNodeData> GetNodesForFloor(int floorNumber)
        {
            return _allNodes.FindAll(n => n.FloorNumber == floorNumber);
        }

        public List<RunNodeData> GetIncomingNodes(int targetId)
        {
            var list = new List<RunNodeData>();
            foreach (var node in _allNodes)
            {
                if (node.NextNodeIds != null && node.NextNodeIds.Contains(targetId))
                {
                    list.Add(node);
                }
            }
            return list;
        }

        public int GetNextAvailableId()
        {
            int maxId = 0;
            foreach (var node in _allNodes)
            {
                if (node.Id > maxId) maxId = node.Id;
            }
            return maxId + 1;
        }

        // =========================================================================
        // THAO TÁC DANH SÁCH LIÊN KẾT (LINKED LIST & GRAPH OPERATIONS)
        // =========================================================================

        // 0. Tạo Node Tự Do (Free Node) tại tọa độ 2D bất kỳ
        public RunNodeData CreateFreeNode(Vector2 position, RunNodeType type = RunNodeType.NormalBattle, string customName = "")
        {
            int newId = GetNextAvailableId();
            int estimatedFloor = Mathf.Max(1, Mathf.RoundToInt(position.y / 110f) + 1);
            string branchName = $"{newId}";
            string displayName = string.IsNullOrEmpty(customName) ? $"Phòng #{newId}: {GetDefaultTitle(type)}" : customName;
            Color color = GetDefaultColor(type);

            var newNode = new RunNodeData(newId, estimatedFloor, branchName, type, displayName, color, position);
            _allNodes.Add(newNode);
            return newNode;
        }

        // Xóa một Node bất kỳ và dọn sạch các cổng trỏ tới nó
        public void DeleteNode(int id)
        {
            _allNodes.RemoveAll(n => n.Id == id);
            foreach (var n in _allNodes)
            {
                if (n.NextNodeIds != null)
                {
                    n.NextNodeIds.Remove(id);
                }
            }
        }

        // Tự động căn chỉnh tọa độ cho các node trên Canvas theo chiều ngang (Trái sang Phải)
        public void AutoLayoutNodes(float spacingX = 240f, float spacingY = 110f, float startX = 80f, float centerY = 350f)
        {
            var nodesByFloor = new Dictionary<int, List<RunNodeData>>();
            foreach (var n in _allNodes)
            {
                if (!nodesByFloor.ContainsKey(n.FloorNumber))
                    nodesByFloor[n.FloorNumber] = new List<RunNodeData>();
                nodesByFloor[n.FloorNumber].Add(n);
            }

            foreach (var kvp in nodesByFloor)
            {
                int floor = kvp.Key;
                var list = kvp.Value;

                float x = startX + (floor - 1) * spacingX;
                float totalHeight = (list.Count - 1) * spacingY;
                float startY = centerY - (totalHeight * 0.5f);

                for (int i = 0; i < list.Count; i++)
                {
                    float y = startY + i * spacingY;
                    list[i].SetEditorPosition(new Vector2(x, y));
                }
            }
        }

        public void EnsureNodePositions()
        {
            bool anyZero = false;
            foreach (var n in _allNodes)
            {
                if (n.EditorPosition == Vector2.zero)
                {
                    anyZero = true;
                    break;
                }
            }

            if (anyZero)
            {
                AutoLayoutNodes();
            }
        }

        // 1. Thêm nhánh mới vào một tầng
        public RunNodeData AddBranchToFloor(int floorNumber, RunNodeType type, string customBranchName = "")
        {
            var currentBranches = GetNodesForFloor(floorNumber);
            string branchName = customBranchName;
            if (string.IsNullOrEmpty(branchName))
            {
                char letter = (char)('A' + currentBranches.Count);
                branchName = $"{floorNumber}{letter}";
            }

            int newId = GetNextAvailableId();
            Color color = GetDefaultColor(type);
            string displayName = $"Tầng {floorNumber}: {GetDefaultTitle(type)}";

            var nextFloorNodes = GetNodesForFloor(floorNumber + 1);
            var nextIds = new List<int>();
            if (nextFloorNodes.Count > 0)
            {
                nextIds.Add(nextFloorNodes[0].Id);
            }

            var newNode = new RunNodeData(newId, floorNumber, branchName, type, displayName, color, nextIds.ToArray());
            _allNodes.Add(newNode);

            // Nối tầng trước vào nhánh mới
            var prevFloorNodes = GetNodesForFloor(floorNumber - 1);
            foreach (var pNode in prevFloorNodes)
            {
                if (pNode.NextNodeIds.Count < MaxPortalsPerRoom && !pNode.NextNodeIds.Contains(newId))
                {
                    pNode.NextNodeIds.Add(newId);
                }
            }

            return newNode;
        }

        // 2. Chèn một tầng mới vào giữa (A -> B trở thành A -> New -> B)
        public void InsertFloorAfter(int afterFloorNumber)
        {
            int newFloor = afterFloorNumber + 1;

            // Đẩy tất cả các tầng từ newFloor trở lên tăng thêm 1
            foreach (var node in _allNodes)
            {
                if (node.FloorNumber >= newFloor)
                {
                    node.SetFloorNumber(node.FloorNumber + 1);
                    if (node.DisplayName.StartsWith("Tầng "))
                    {
                        node.SetDisplayName($"Tầng {node.FloorNumber}: {GetDefaultTitle(node.NodeType)}");
                    }
                }
            }
            _totalFloors++;

            // Tạo Node cho tầng mới
            int newId = GetNextAvailableId();
            Color battleColor = GetDefaultColor(RunNodeType.NormalBattle);
            var newNode = new RunNodeData(newId, newFloor, $"{newFloor}", RunNodeType.NormalBattle, $"Tầng {newFloor}: Quái Thường", battleColor);

            // Cập nhật liên kết đồ thị (Splice)
            var prevNodes = GetNodesForFloor(afterFloorNumber);
            var nextNodes = GetNodesForFloor(newFloor + 1);

            int limit = Mathf.Min(MaxPortalsPerRoom, nextNodes.Count);
            for (int i = 0; i < limit; i++)
            {
                newNode.NextNodeIds.Add(nextNodes[i].Id);
            }

            foreach (var prev in prevNodes)
            {
                prev.NextNodeIds.Clear();
                prev.NextNodeIds.Add(newId);
            }

            _allNodes.Add(newNode);
        }

        // 3. Thêm một tầng mới ở cuối đồ thị
        public void AddFloorAtEnd(RunNodeType type = RunNodeType.NormalBattle)
        {
            int newFloor = _totalFloors + 1;
            _totalFloors = newFloor;

            int newId = GetNextAvailableId();
            Color color = GetDefaultColor(type);
            var newNode = new RunNodeData(newId, newFloor, $"{newFloor}", type, $"Tầng {newFloor}: {GetDefaultTitle(type)}", color);

            var prevNodes = GetNodesForFloor(newFloor - 1);
            foreach (var p in prevNodes)
            {
                if (p.NextNodeIds.Count < MaxPortalsPerRoom && !p.NextNodeIds.Contains(newId))
                {
                    p.NextNodeIds.Add(newId);
                }
            }

            _allNodes.Add(newNode);
        }

        // 4. Xóa một tầng (Bypass: Prev -> Deleted -> Next trở thành Prev -> Next)
        public void DeleteFloor(int floorNumber)
        {
            if (_totalFloors <= 1) return;

            var prevNodes = GetNodesForFloor(floorNumber - 1);
            var nextNodes = GetNodesForFloor(floorNumber + 1);
            var deletedNodes = GetNodesForFloor(floorNumber);
            var deletedIds = new HashSet<int>();
            foreach (var d in deletedNodes) deletedIds.Add(d.Id);

            var nextIds = new List<int>();
            foreach (var n in nextNodes) nextIds.Add(n.Id);

            foreach (var prev in prevNodes)
            {
                prev.NextNodeIds.RemoveAll(id => deletedIds.Contains(id));
                foreach (int nid in nextIds)
                {
                    if (prev.NextNodeIds.Count < MaxPortalsPerRoom && !prev.NextNodeIds.Contains(nid))
                    {
                        prev.NextNodeIds.Add(nid);
                    }
                }
            }

            _allNodes.RemoveAll(n => n.FloorNumber == floorNumber);

            foreach (var node in _allNodes)
            {
                if (node.FloorNumber > floorNumber)
                {
                    node.SetFloorNumber(node.FloorNumber - 1);
                    if (node.DisplayName.StartsWith("Tầng "))
                    {
                        node.SetDisplayName($"Tầng {node.FloorNumber}: {GetDefaultTitle(node.NodeType)}");
                    }
                }
            }
            _totalFloors--;
        }

        // 5. Hoán đổi vị trí giữa 2 tầng liền kề
        public void SwapFloors(int floorA, int floorB)
        {
            if (floorA == floorB) return;
            if (floorA > floorB)
            {
                int temp = floorA;
                floorA = floorB;
                floorB = temp;
            }

            // Hoán đổi floorA và floorB (floorB = floorA + 1)
            var nodesA = GetNodesForFloor(floorA);
            var nodesB = GetNodesForFloor(floorB);
            var prevNodes = GetNodesForFloor(floorA - 1);
            var nextNodes = GetNodesForFloor(floorB + 1);

            // Hoán đổi số tầng
            foreach (var a in nodesA)
            {
                a.SetFloorNumber(floorB);
                if (a.DisplayName.StartsWith("Tầng "))
                {
                    a.SetDisplayName($"Tầng {floorB}: {GetDefaultTitle(a.NodeType)}");
                }
            }

            foreach (var b in nodesB)
            {
                b.SetFloorNumber(floorA);
                if (b.DisplayName.StartsWith("Tầng "))
                {
                    b.SetDisplayName($"Tầng {floorA}: {GetDefaultTitle(b.NodeType)}");
                }
            }

            // Tái cấu trúc liên kết trỏ tiếp theo (Prev -> B -> A -> Next)
            var idsA = new List<int>();
            foreach (var a in nodesA) idsA.Add(a.Id);

            var idsB = new List<int>();
            foreach (var b in nodesB) idsB.Add(b.Id);

            var idsNext = new List<int>();
            foreach (var n in nextNodes) idsNext.Add(n.Id);

            // Tầng trước trỏ vào B
            foreach (var p in prevNodes)
            {
                p.NextNodeIds.Clear();
                int limit = Mathf.Min(MaxPortalsPerRoom, idsB.Count);
                for (int i = 0; i < limit; i++) p.NextNodeIds.Add(idsB[i]);
            }

            // B trỏ vào A
            foreach (var b in nodesB)
            {
                b.NextNodeIds.Clear();
                int limit = Mathf.Min(MaxPortalsPerRoom, idsA.Count);
                for (int i = 0; i < limit; i++) b.NextNodeIds.Add(idsA[i]);
            }

            // A trỏ vào Tầng sau
            foreach (var a in nodesA)
            {
                a.NextNodeIds.Clear();
                int limit = Mathf.Min(MaxPortalsPerRoom, idsNext.Count);
                for (int i = 0; i < limit; i++) a.NextNodeIds.Add(idsNext[i]);
            }
        }

        public void MoveFloorUp(int floorNumber)
        {
            if (floorNumber <= 1) return;
            SwapFloors(floorNumber - 1, floorNumber);
        }

        public void MoveFloorDown(int floorNumber)
        {
            if (floorNumber >= _totalFloors) return;
            SwapFloors(floorNumber, floorNumber + 1);
        }

        // =========================================================================
        // HỆ THỐNG QUẢN LÝ CỔNG DỊCH CHUYỂN (PORTAL CONNECTION SYSTEM)
        // =========================================================================

        /// <summary>
        /// Kiểm tra tính hợp lệ trước khi thực hiện nối 2 phòng (Bao gồm ràng buộc tối đa 3 phòng).
        /// </summary>
        public bool CanConnect(int fromId, int toId, out string reason)
        {
            var fromNode = GetNodeById(fromId);
            var toNode = GetNodeById(toId);
            if (fromNode == null || toNode == null)
            {
                reason = "Phòng không tồn tại";
                return false;
            }
            if (fromId == toId)
            {
                reason = "Không thể tự nối cổng vào chính nó";
                return false;
            }
            if (fromNode.NextNodeIds != null && fromNode.NextNodeIds.Contains(toId))
            {
                reason = "Hai phòng này đã được kết nối từ trước";
                return false;
            }
            if (fromNode.NextNodeIds != null && fromNode.NextNodeIds.Count >= MaxPortalsPerRoom)
            {
                reason = $"Phòng #{fromId} ({fromNode.DisplayName}) đã đạt giới hạn tối đa {MaxPortalsPerRoom} phòng nối tiếp!";
                return false;
            }
            reason = string.Empty;
            return true;
        }

        public bool ConnectNodes(int fromId, int toId)
        {
            if (!CanConnect(fromId, toId, out string reason))
            {
                if (!string.IsNullOrEmpty(reason))
                {
                    Debug.LogWarning($"<color=yellow>[RUN MAP]</color> Không thể nối #{fromId} ➔ #{toId}: {reason}");
                }
                return false;
            }

            var fromNode = GetNodeById(fromId);
            fromNode.NextNodeIds.Add(toId);
            return true;
        }

        public bool DisconnectNodes(int fromId, int toId)
        {
            var fromNode = GetNodeById(fromId);
            if (fromNode != null && fromNode.NextNodeIds.Contains(toId))
            {
                fromNode.NextNodeIds.Remove(toId);
                return true;
            }
            return false;
        }

        public bool IsConnected(int fromId, int toId)
        {
            var fromNode = GetNodeById(fromId);
            return fromNode != null && fromNode.NextNodeIds != null && fromNode.NextNodeIds.Contains(toId);
        }

        // Tự động nối thông minh các cổng từ tầng này sang tầng kế tiếp
        public void AutoLinkFloor(int floorNumber)
        {
            var currentNodes = GetNodesForFloor(floorNumber);
            var nextNodes = GetNodesForFloor(floorNumber + 1);

            if (currentNodes.Count == 0 || nextNodes.Count == 0) return;

            var nextFloorIds = new HashSet<int>();
            foreach (var n in nextNodes) nextFloorIds.Add(n.Id);

            foreach (var cur in currentNodes)
            {
                cur.NextNodeIds.RemoveAll(id => nextFloorIds.Contains(id));
            }

            if (nextNodes.Count == 1)
            {
                int targetId = nextNodes[0].Id;
                foreach (var cur in currentNodes)
                {
                    cur.NextNodeIds.Add(targetId);
                }
            }
            else if (currentNodes.Count == 1)
            {
                var cur = currentNodes[0];
                int limit = Mathf.Min(3, nextNodes.Count);
                for (int i = 0; i < limit; i++)
                {
                    cur.NextNodeIds.Add(nextNodes[i].Id);
                }
            }
            else
            {
                for (int i = 0; i < currentNodes.Count; i++)
                {
                    var cur = currentNodes[i];
                    int targetIndexA = i % nextNodes.Count;
                    int targetIndexB = (i + 1) % nextNodes.Count;

                    cur.NextNodeIds.Add(nextNodes[targetIndexA].Id);
                    if (targetIndexA != targetIndexB && cur.NextNodeIds.Count < 3)
                    {
                        cur.NextNodeIds.Add(nextNodes[targetIndexB].Id);
                    }
                }
            }
        }

        // Tự động kết nối thông suốt toàn bộ các tầng trong toàn bản đồ
        public void AutoLinkAllFloors()
        {
            CleanDanglingConnections();

            for (int f = 1; f < _totalFloors; f++)
            {
                AutoLinkFloor(f);
            }

            var bossNodes = GetNodesForFloor(_totalFloors);
            foreach (var b in bossNodes)
            {
                b.NextNodeIds.Clear();
            }
        }

        // Quét và xóa các cổng trỏ vào ID không tồn tại
        public int CleanDanglingConnections()
        {
            int removedCount = 0;
            var validIds = new HashSet<int>();
            foreach (var n in _allNodes) validIds.Add(n.Id);

            foreach (var n in _allNodes)
            {
                if (n.NextNodeIds != null)
                {
                    int before = n.NextNodeIds.Count;
                    n.NextNodeIds.RemoveAll(id => !validIds.Contains(id) || id == n.Id);
                    removedCount += (before - n.NextNodeIds.Count);
                }
            }

            return removedCount;
        }

        // =========================================================================
        // MÀU SẮC & TIÊU ĐỀ MẶC ĐỊNH
        // =========================================================================

        public static Color GetDefaultColor(RunNodeType type)
        {
            switch (type)
            {
                case RunNodeType.NormalBattle: return new Color(0.9f, 0.35f, 0.35f);
                case RunNodeType.StarterReward: return new Color(0.3f, 0.8f, 1f);
                case RunNodeType.EliteBattle: return new Color(0.95f, 0.25f, 0.6f);
                case RunNodeType.MiniBoss: return new Color(0.85f, 0.3f, 0.95f);
                case RunNodeType.BigReward: return new Color(1f, 0.85f, 0.2f);
                case RunNodeType.RestSite: return new Color(0.3f, 0.9f, 0.45f);
                case RunNodeType.Shop: return new Color(1f, 0.85f, 0.2f);
                case RunNodeType.BuffReward: return new Color(0.4f, 0.75f, 1f);
                case RunNodeType.BossBattle: return new Color(1f, 0.2f, 0.15f);
                default: return Color.white;
            }
        }

        public static string GetDefaultTitle(RunNodeType type)
        {
            switch (type)
            {
                case RunNodeType.NormalBattle: return "Quái Thường";
                case RunNodeType.StarterReward: return "Thưởng Khởi Đầu";
                case RunNodeType.EliteBattle: return "Tinh Anh Elite";
                case RunNodeType.MiniBoss: return "MINI BOSS";
                case RunNodeType.BigReward: return "Buff Sau Mini Boss";
                case RunNodeType.RestSite: return "Lửa Trại Hồi Máu";
                case RunNodeType.Shop: return "Cửa Hàng Thương Nhân";
                case RunNodeType.BuffReward: return "Nhận Thẻ Buff";
                case RunNodeType.BossBattle: return "TRÙM CUỐI BOSS";
                default: return "Phòng Khám Phá";
            }
        }

        public static string GetPhysicalPortalName(int slotIndex, int totalBranches)
        {
            if (totalBranches == 1) return "🚪 Cổng Giữa";
            if (totalBranches == 2)
            {
                if (slotIndex == 0) return "🚪 Cổng Trái";
                if (slotIndex == 1) return "🚪 Cổng Phải";
            }
            if (totalBranches >= 3)
            {
                if (slotIndex == 0) return "🚪 Cổng Trái";
                if (slotIndex == 1) return "🚪 Cổng Giữa";
                if (slotIndex == 2) return "🚪 Cổng Phải";
                return $"⚠️ Cổng #{slotIndex + 1} (Vượt mức 3 cổng)";
            }
            return $"🚪 Cổng #{slotIndex + 1}";
        }

        public static string GetShortPortalName(int slotIndex, int totalBranches)
        {
            if (totalBranches == 1) return "Giữa";
            if (totalBranches == 2) return slotIndex == 0 ? "Trái" : "Phải";
            if (totalBranches >= 3)
            {
                if (slotIndex == 0) return "Trái";
                if (slotIndex == 1) return "Giữa";
                if (slotIndex == 2) return "Phải";
                return $"Cổng {slotIndex + 1}";
            }
            return $"Cổng {slotIndex + 1}";
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

# TÀI LIỆU GIẢI THÍCH CHI TIẾT TỪNG DÒNG CODE: ENEMY & FLOW FIELD PATHFINDING

Tài liệu này mổ xẻ **toàn bộ từng dòng code** của 4 file trong hệ thống Quái vật và Tìm đường dòng chảy (Flow Field):
1. **FlowFieldCell.cs** (Cấu trúc 1 ô lưới)
2. **FlowFieldGrid.cs** (Ma trận thuật toán Dijkstra & Chống cắt góc)
3. **FlowFieldManager.cs** (Bộ điều phối & Quản lý cập nhật)
4. **EnemyController.cs** (Bộ não điều khiển quái vật)

---

# PHẦN 1: FlowFieldCell.cs (GIẢI THÍCH TỪNG DÒNG)

File này định nghĩa **một ô vuông đơn lẻ** trên bản đồ lưới.

```csharp
1: using UnityEngine;
2: 
3: namespace Project_D.Enemies.FlowField
4: {
5:     public class FlowFieldCell
6:     {
7:         public Vector2Int GridIndex;      // Tọa độ ô trong ma trận (X, Y)
8:         public Vector3 WorldPos;          // Tọa độ thực tế trong thế giới game
9:         public byte Cost;                 // Chi phí: 1 = Đất trống, 255 = Tường/Vật cản
10:         public ushort BestCost;           // Khoảng cách sóng loang từ Player đến ô này
11:         public Vector2 BestDirection;     // Mũi tên vector chỉ hướng dòng chảy
```

### Giải thích dòng 1 đến 11:
- **Dòng 1: `using UnityEngine;`**: Thư viện chứa các kiểu toán học của Unity như `Vector2Int`, `Vector3`, `Vector2`.
- **Dòng 3: `namespace Project_D.Enemies.FlowField`**: Gom nhóm code vào khu vực tìm đường của quái vật.
- **Dòng 5: `public class FlowFieldCell`**: Đây là một class C# thuần túy (Plain C# Class), không kế thừa `MonoBehaviour` để tiết kiệm tối đa bộ nhớ RAM khi tạo hàng nghìn ô.
- **Dòng 7: `public Vector2Int GridIndex;`**: Chỉ số hàng và cột của ô trên bàn cờ ma trận (ví dụ: `x = 10, y = 15`). Dùng số nguyên `int`.
- **Dòng 8: `public Vector3 WorldPos;`**: Tọa độ tâm của ô đó trong không gian thực tế của game Unity (đơn vị mét).
- **Dòng 9: `public byte Cost;`**:
  - Dùng kiểu `byte` (chiếm đúng 1 byte trong RAM, giá trị từ 0 đến 255).
  - `1`: Đất trống, bước chân đi qua dễ dàng.
  - `255`: Tường đá, vực thẳm hoặc vật cản không thể bước qua.
- **Dòng 10: `public ushort BestCost;`**:
  - Dùng kiểu `ushort` (số nguyên không dấu 2 byte, từ 0 đến 65,535).
  - Đây là khoảng cách tích lũy từ Player lan truyền tới ô này.
  - Player đứng ở đâu thì ô đó có `BestCost = 0`. Càng xa Player số này càng lớn.
- **Dòng 11: `public Vector2 BestDirection;`**:
  - Hướng vector 2D được chuẩn hóa (độ dài = 1.0).
  - Nó chính là mũi tên chỉ đường cho quái: "Muốn tới Player nhanh nhất, hãy bước theo hướng mũi tên này!".

---

```csharp
13:         public FlowFieldCell(Vector2Int gridIndex, Vector3 worldPos)
14:         {
15:             GridIndex = gridIndex;
16:             WorldPos = worldPos;
17:             Cost = 1;                     // Mặc định là đất trống
18:             BestCost = ushort.MaxValue;   // Mặc định khởi tạo bằng vô cực
19:             BestDirection = Vector2.zero;
20:         }
21: 
22:         public void ResetIntegration()
23:         {
24:             BestCost = ushort.MaxValue;
25:             BestDirection = Vector2.zero;
26:         }
27:     }
28: }
```

### Giải thích dòng 13 đến 28:
- **Dòng 13 đến 20 (Hàm khởi tạo Constructor):**
  - Gán chỉ số `GridIndex` và tọa độ `WorldPos`.
  - Mặc định ban đầu xem ô này là đất trống (`Cost = 1`).
  - Gán `BestCost = ushort.MaxValue` (giá trị lớn nhất 65,535, đóng vai trò là "Vô Cực" - tức là sóng chưa lan tới).
  - `BestDirection = Vector2.zero`: Chưa có hướng đi nào.
- **Dòng 22 đến 26 (`ResetIntegration`):**
  - Khi Player bước sang ô khác, bản đồ sóng cũ bị xóa đi để tính lại từ đầu.
  - Hàm này reset `BestCost` về lại vô cực và xóa mũi tên cũ để chuẩn bị đón đợt sóng mới.

---

# PHẦN 2: FlowFieldGrid.cs (GIẢI THÍCH TỪNG DÒNG)

Đây là file "bộ não" thực hiện toàn bộ các phép tính toán học.

```csharp
1: using System.Collections.Generic;
2: using UnityEngine;
3: 
4: namespace Project_D.Enemies.FlowField
5: {
6:     public class FlowFieldGrid
7:     {
8:         public FlowFieldCell[,] Cells { get; private set; }
9:         public Vector2Int GridSize { get; private set; }
10:         public float CellSize { get; private set; }
11:         public Vector3 OriginPosition { get; private set; }
```

### Giải thích dòng 1 đến 11:
- **Dòng 1: `using System.Collections.Generic;`**: Cung cấp hàng đợi `Queue<T>` dùng cho thuật toán sóng loang Dijkstra.
- **Dòng 8: `public FlowFieldCell[,] Cells`**: Mảng 2 chiều chứa toàn bộ các ô lưới (bàn cờ).
- **Dòng 9: `public Vector2Int GridSize`**: Kích thước bàn cờ (ví dụ `30x30` ô).
- **Dòng 10: `public float CellSize`**: Kích thước chiều dài mỗi cạnh ô vuông (ví dụ `1 mét`).
- **Dòng 11: `public Vector3 OriginPosition`**: Tọa độ góc dưới-trái của toàn bộ bàn cờ.

---

```csharp
13:         public FlowFieldGrid(Vector2Int gridSize, float cellSize, Vector3 originPosition)
14:         {
15:             GridSize = gridSize;
16:             CellSize = cellSize;
17:             OriginPosition = originPosition;
18:             InitializeGrid();
19:         }
20: 
21:         private void InitializeGrid()
22:         {
23:             Cells = new FlowFieldCell[GridSize.x, GridSize.y];
24: 
25:             for (int x = 0; x < GridSize.x; x++)
26:             {
27:                 for (int y = 0; y < GridSize.y; y++)
28:                 {
29:                     Vector3 worldPos = OriginPosition + new Vector3(x * CellSize + CellSize * 0.5f, y * CellSize + CellSize * 0.5f, 0f);
30:                     Cells[x, y] = new FlowFieldCell(new Vector2Int(x, y), worldPos);
31:                 }
32:             }
33:         }
```

### Giải thích dòng 13 đến 33:
- **Dòng 23: `Cells = new FlowFieldCell[GridSize.x, GridSize.y];`**: Cấp phát mảng 2 chiều chứa các ô.
- **Dòng 25 đến 32 (2 vòng for lồng nhau):** Quét qua từng tọa độ `(x, y)`.
- **Dòng 29: Tính tâm của từng ô (`worldPos`):**
  - `x * CellSize`: Tọa độ mép trái của ô.
  - `+ CellSize * 0.5f`: Nhích thêm nửa ô để lấy **chính xác tâm điểm** ở giữa ô vuông.
  - Gán kết quả vào ô `Cells[x, y]`.

---

```csharp
36:         // 1. Quét tìm vật cản (Tường hầm ngục)
37:         public void UpdateCostField(LayerMask obstacleLayer)
38:         {
39:             Vector2 boxHalfSize = new Vector2(CellSize * 0.45f, CellSize * 0.45f);
40: 
41:             for (int x = 0; x < GridSize.x; x++)
42:             {
43:                 for (int y = 0; y < GridSize.y; y++)
44:                 {
45:                     FlowFieldCell cell = Cells[x, y];
46:                     Collider2D hit = Physics2D.OverlapBox(cell.WorldPos, boxHalfSize * 2f, 0f, obstacleLayer);
47: 
48:                     if (hit != null)
49:                     {
50:                         cell.Cost = 255; // Tường đá: Không thể đi qua
51:                     }
52:                     else
53:                     {
54:                         cell.Cost = 1;   // Đất trống: Chi phí chuẩn
55:                     }
56:                 }
57:             }
58:         }
```

### Giải thích dòng 36 đến 58:
- **Dòng 39: `boxHalfSize = CellSize * 0.45f`**: 
  - Kích thước nửa hộp kiểm tra. Dùng 0.45 thay vì 0.5 để thu nhỏ hộp lại 1 chút (90% ô), tránh việc chạm nhầm vào mép tường của ô bên cạnh.
- **Dòng 46: `Physics2D.OverlapBox(...)`**:
  - Quét xem tại tâm ô `cell.WorldPos` có cái hộp Collider nào thuộc Layer `obstacleLayer` (tường đá) đang đè lên không.
- **Dòng 48 đến 55**:
  - Nếu có tường đè lên (`hit != null`) -> Ô này thành tường (`Cost = 255`).
  - Nếu không có tường -> Ô này là đất trống (`Cost = 1`).

---

```csharp
61:         // 2. Thuật toán Sóng Loang Dijkstra (Integration Field)
62:         public void CreateIntegrationField(FlowFieldCell destinationCell)
63:         {
64:             if (destinationCell == null) return;
65: 
66:             // Reset toàn bộ khoảng cách cũ
67:             for (int x = 0; x < GridSize.x; x++)
68:             {
69:                 for (int y = 0; y < GridSize.y; y++)
70:                 {
71:                     Cells[x, y].ResetIntegration();
72:                 }
73:             }
74: 
75:             destinationCell.BestCost = 0;
76:             Queue<FlowFieldCell> openQueue = new Queue<FlowFieldCell>();
77:             openQueue.Enqueue(destinationCell);
```

### Giải thích dòng 61 đến 77:
- **Dòng 64: `if (destinationCell == null) return;`**: Nếu chưa xác định được Player đứng ở ô nào thì thoát ngay để không bị lỗi.
- **Dòng 67 đến 72**: Reset toàn bộ các ô về trạng thái chưa thăm (`BestCost = vô cực`).
- **Dòng 75: `destinationCell.BestCost = 0;`**: Đặt ô Player đứng là mốc xuất phát gốc, khoảng cách = 0.
- **Dòng 76 & 77**: Nhét ô Player vào hàng đợi `Queue` để bắt đầu phát sóng loang.

---

```csharp
79:             // 8 hướng di chuyển (4 thẳng, 4 chéo)
80:             Vector2Int[] directions = new Vector2Int[]
81:             {
82:                 new Vector2Int(0, 1),   // Bắc
83:                 new Vector2Int(0, -1),  // Nam
84:                 new Vector2Int(1, 0),   // Đông
85:                 new Vector2Int(-1, 0),  // Tây
86:                 new Vector2Int(1, 1),   // Đông Bắc (chéo)
87:                 new Vector2Int(-1, 1),  // Tây Bắc (chéo)
88:                 new Vector2Int(1, -1),  // Đông Nam (chéo)
89:                 new Vector2Int(-1, -1)  // Tây Nam (chéo)
90:             };
```

### Giải thích dòng 79 đến 90:
- Mảng chứa 8 bước nhảy tọa độ: 
  - 4 hướng chính: Lên `(0, 1)`, Xuống `(0, -1)`, Phải `(1, 0)`, Trái `(-1, 0)`.
  - 4 hướng góc chéo: Trên-Phải `(1, 1)`, Trên-Trái `(-1, 1)`, Dưới-Phải `(1, -1)`, Dưới-Trái `(-1, -1)`.

---

```csharp
92:             while (openQueue.Count > 0)
93:             {
94:                 FlowFieldCell currentCell = openQueue.Dequeue();
95: 
96:                 foreach (var dir in directions)
97:                 {
98:                     Vector2Int neighborPos = currentCell.GridIndex + dir;
99: 
100:                     if (neighborPos.x < 0 || neighborPos.x >= GridSize.x || neighborPos.y < 0 || neighborPos.y >= GridSize.y)
101:                         continue;
102: 
103:                     FlowFieldCell neighbor = Cells[neighborPos.x, neighborPos.y];
104: 
105:                     if (neighbor.Cost == 255) continue;
```

### Giải thích dòng 92 đến 105:
- **Dòng 92: `while (openQueue.Count > 0)`**: Vòng lặp chạy chừng nào hàng đợi vẫn còn ô cần loang sóng.
- **Dòng 94: `openQueue.Dequeue()`**: Lấy ô đang chờ ở đầu hàng đợi ra làm tâm sóng.
- **Dòng 96: `foreach (var dir in directions)`**: Xét lần lượt 8 ô hàng xóm xung quanh ô đó.
- **Dòng 100 & 101**: Nếu tọa độ hàng xóm văng ra ngoài mép bản đồ (nhỏ hơn 0 hoặc vượt quá kích thước) -> Bỏ qua.
- **Dòng 105: `if (neighbor.Cost == 255) continue;`**: Nếu ô hàng xóm là TƯỜNG ĐÁ -> Sóng dội lại, bỏ qua không loang vào tường!

---

```csharp
109:                     // QUY TẮC "CHỐNG CẮT GÓC TƯỜNG" (NO CORNER CUTTING):
110:                     if (dir.x != 0 && dir.y != 0) // Đây là hướng đi chéo
111:                     {
112:                         // Kiểm tra 2 ô kề bên: nếu một trong 2 ô là TƯỜNG thì TUYỆT ĐỐI KHÔNG ĐƯỢC ĐI CHÉO!
113:                         FlowFieldCell side1 = Cells[currentCell.GridIndex.x + dir.x, currentCell.GridIndex.y];
114:                         FlowFieldCell side2 = Cells[currentCell.GridIndex.x, currentCell.GridIndex.y + dir.y];
115: 
116:                         if (side1.Cost == 255 || side2.Cost == 255)
117:                         {
118:                             continue; // Chặn ngay, không cho chui qua khe tường hở!
119:                         }
120:                     }
```

### Giải thích dòng 109 đến 120 (CỰC KỲ QUAN TRỌNG - BÀI TOÁN BẠN ĐẶT RA):
- **`dir.x != 0 && dir.y != 0`**: Dấu hiệu nhận biết đang xét bước nhảy chéo (vì cả x và y đều thay đổi).
- **`side1` và `side2` là gì?**
  - Giả sử đi từ `(0, 0)` chéo lên `(1, 1)`.
  - `side1` là ô sang ngang: `(1, 0)`.
  - `side2` là ô đi thẳng lên: `(0, 1)`.
- **Dòng 116: `if (side1.Cost == 255 || side2.Cost == 255)`**:
  - Nếu `side1` là Tường HOẶC `side2` là Tường: Khe hẹp giữa 2 ô tiếp giáp bị khóa.
  - Lệnh `continue` lập tức hủy bỏ bước đi chéo này! Sóng không được phép chui xuyên qua góc nhọn của bức tường.

---

```csharp
122:                     // Đi thẳng tốn 10, đi chéo tốn 14 (~ 10 * 1.414 Pytago)
123:                     ushort moveCost = (dir.x != 0 && dir.y != 0) ? (ushort)14 : (ushort)10;
124:                     ushort newCost = (ushort)(currentCell.BestCost + moveCost);
125: 
126:                     if (newCost < neighbor.BestCost)
127:                     {
128:                         neighbor.BestCost = newCost;
129:                         openQueue.Enqueue(neighbor);
130:                     }
131:                 }
132:             }
133:         }
```

### Giải thích dòng 122 đến 133:
- **Dòng 123: `moveCost`**:
  - Nếu đi chéo: Tốn `14` đơn vị.
  - Nếu đi thẳng: Tốn `10` đơn vị.
  - (Tỉ lệ 14 / 10 = 1.4, khớp với căn bậc hai của 2 trong định lý Pytago).
- **Dòng 124: `newCost = currentCell.BestCost + moveCost`**: Tính khoảng cách mới nếu đi qua ô này.
- **Dòng 126 đến 130**:
  - Nếu khoảng cách mới này ngắn hơn khoảng cách cũ của ô hàng xóm (`newCost < neighbor.BestCost`):
  - Cập nhật kỷ lục mới cho hàng xóm: `neighbor.BestCost = newCost`.
  - Đưa hàng xóm vào `openQueue` để sóng tiếp tục loang ra xa hơn từ ô này.

---

```csharp
136:         // 3. Tạo Mũi Tên Dòng Chảy (Flow Field)
137:         public void CreateFlowField()
138:         {
139:             Vector2Int[] directions = new Vector2Int[]
140:             {
141:                 new Vector2Int(0, 1), new Vector2Int(0, -1),
142:                 new Vector2Int(1, 0), new Vector2Int(-1, 0),
143:                 new Vector2Int(1, 1), new Vector2Int(-1, 1),
144:                 new Vector2Int(1, -1), new Vector2Int(-1, -1)
145:             };
146: 
147:             for (int x = 0; x < GridSize.x; x++)
148:             {
149:                 for (int y = 0; y < GridSize.y; y++)
150:                 {
151:                     FlowFieldCell cell = Cells[x, y];
152: 
153:                     if (cell.Cost == 255 || cell.BestCost == ushort.MaxValue)
154:                     {
155:                         cell.BestDirection = Vector2.zero;
156:                         continue;
157:                     }
```

### Giải thích dòng 136 đến 157:
- Sau khi đã có bản đồ khoảng cách `BestCost` ở bước 2, hàm này dựng mũi tên chỉ đường cho từng ô.
- **Dòng 153 đến 156**:
  - Nếu ô này là tường đá (`Cost == 255`) hoặc là vùng cô lập sóng không tới được (`BestCost == ushort.MaxValue`) -> Không có mũi tên nào cả (`BestDirection = Vector2.zero`).

---

```csharp
159:                     ushort lowestCost = cell.BestCost;
160:                     FlowFieldCell bestNeighbor = null;
161: 
162:                     foreach (var dir in directions)
163:                     {
164:                         Vector2Int neighborPos = cell.GridIndex + dir;
165: 
166:                         if (neighborPos.x < 0 || neighborPos.x >= GridSize.x || neighborPos.y < 0 || neighborPos.y >= GridSize.y)
167:                             continue;
168: 
169:                         FlowFieldCell neighbor = Cells[neighborPos.x, neighborPos.y];
170:                         if (neighbor.Cost == 255) continue;
171: 
172:                         // Kiểm tra chống cắt góc
173:                         if (dir.x != 0 && dir.y != 0)
174:                         {
175:                             FlowFieldCell side1 = Cells[cell.GridIndex.x + dir.x, cell.GridIndex.y];
176:                             FlowFieldCell side2 = Cells[cell.GridIndex.x, cell.GridIndex.y + dir.y];
177:                             if (side1.Cost == 255 || side2.Cost == 255) continue;
178:                         }
179: 
180:                         if (neighbor.BestCost < lowestCost)
181:                         {
182:                             lowestCost = neighbor.BestCost;
183:                             bestNeighbor = neighbor;
184:                         }
185:                     }
186: 
187:                     if (bestNeighbor != null)
188:                     {
189:                         cell.BestDirection = ((Vector2)(bestNeighbor.WorldPos - cell.WorldPos)).normalized;
190:                     }
191:                 }
192:             }
193:         }
```

### Giải thích dòng 159 đến 193:
- Ô hiện tại nhìn sang 8 ô hàng xóm (vẫn giữ điều kiện chống cắt góc ở dòng 173-178).
- **Dòng 180 đến 184**: Tìm xem ô hàng xóm nào có `BestCost` nhỏ nhất (tức là ô dốc nhất dẫn về Player nhanh nhất).
- **Dòng 187 đến 190**:
  - Lấy tọa độ của hàng xóm đó trừ đi tọa độ của ô hiện tại.
  - Chuẩn hóa `.normalized` để tạo thành một vector mũi tên có độ dài = 1.0.
  - Gán vào `cell.BestDirection`.

---

```csharp
196:         // Lấy ô lưới từ vị trí thực tế trong thế giới game (O(1) cực nhanh)
197:         public FlowFieldCell GetCellFromWorldPos(Vector3 worldPos)
198:         {
199:             float percentX = (worldPos.x - OriginPosition.x) / (GridSize.x * CellSize);
200:             float percentY = (worldPos.y - OriginPosition.y) / (GridSize.y * CellSize);
201: 
202:             percentX = Mathf.Clamp01(percentX);
203:             percentY = Mathf.Clamp01(percentY);
204: 
205:             int x = Mathf.Clamp(Mathf.FloorToInt(percentX * GridSize.x), 0, GridSize.x - 1);
206:             int y = Mathf.Clamp(Mathf.FloorToInt(percentY * GridSize.y), 0, GridSize.y - 1);
207: 
208:             return Cells[x, y];
209:         }
```

### Giải thích dòng 196 đến 209:
- Khi con quái vật đứng ở tọa độ mét thực tế (ví dụ: `X = 4.7m, Y = -2.3m`), làm sao biết nó đang giẫm chân lên ô nào trong mảng?
- **Dòng 199 & 200**: Tính xem vị trí đó chiếm bao nhiêu phần trăm (từ 0.0 đến 1.0) trên toàn bộ chiều dài và chiều rộng bản đồ.
- **Dòng 202 & 203: `Mathf.Clamp01`**: Giới hạn tỉ lệ không bị vọt ra ngoài khoảng [0, 1].
- **Dòng 205 & 206: `Mathf.FloorToInt`**: Làm tròn xuống số nguyên để tìm ra chỉ số mảng `x` và `y`.
- **Dòng 208**: Trả về đúng ô lưới đó trong thời gian tức thì $O(1)$!

---

# PHẦN 3: FlowFieldManager.cs (GIẢI THÍCH TỪNG DÒNG)

File này là người nhạc trưởng điều phối toàn bộ hệ thống Flow Field trong Scene.

```csharp
1: using UnityEngine;
2: using Project_D.Player;
3: 
4: namespace Project_D.Enemies.FlowField
5: {
6:     public class FlowFieldManager : MonoBehaviour
7:     {
8:         public static FlowFieldManager Instance { get; private set; }
```

### Giải thích dòng 1 đến 8:
- **Dòng 8: `public static FlowFieldManager Instance`**:
  - Thiết kế Singleton: Biến tĩnh toàn cục giúp bất kỳ con quái nào trong game cũng có thể gọi `FlowFieldManager.Instance.GetFlowDirection(...)` mà không cần kéo thả Component bằng tay.

---

```csharp
10:         [Header("Grid Settings")]
11:         [SerializeField] private Vector2Int _gridSize = new Vector2Int(30, 30);
12:         [SerializeField] private float _cellSize = 1f;
13:         [SerializeField] private LayerMask _obstacleLayer;
14: 
15:         [Header("Update Interval")]
16:         [SerializeField] private float _updateInterval = 0.2f;
17: 
18:         [Header("Debug")]
19:         [SerializeField] private bool _drawGizmos = true;
20: 
21:         private FlowFieldGrid _grid;
22:         private Transform _playerTransform;
23:         private float _timeSinceLastUpdate;
```

### Giải thích dòng 10 đến 23:
- **Dòng 11: `_gridSize = (30, 30)`**: Độ lớn lưới (30 hàng x 30 cột = 900 ô vuông).
- **Dòng 12: `_cellSize = 1f`**: Mỗi ô vuông rộng đúng 1 mét trong không gian game.
- **Dòng 13: `_obstacleLayer`**: Bộ lọc Layer vật cản (chỉ những gì thuộc Layer này mới bị coi là tường).
- **Dòng 16: `_updateInterval = 0.2f`**: 
  - **Bí kíp tối ưu Mobile:** Cứ mỗi 0.2 giây (tức 5 lần/giây), Flow Field mới tính toán lại 1 lần. 
  - Player chạy không quá nhanh trong 0.2 giây, nên quái vẫn đuổi cực kỳ chuẩn mà CPU không bị quá tải.
- **Dòng 23: `_timeSinceLastUpdate`**: Đồng hồ đếm ngược để canh mốc 0.2 giây.

---

```csharp
25:         private void Awake()
26:         {
27:             if (Instance != null && Instance != this)
28:             {
29:                 Destroy(gameObject);
30:                 return;
31:             }
32:             Instance = this;
33:         }
34: 
35:         private void Start()
36:         {
37:             var player = Object.FindAnyObjectByType<PlayerController>();
38:             if (player != null)
39:             {
40:                 _playerTransform = player.transform;
41:             }
42: 
43:             Vector3 origin = transform.position - new Vector3(_gridSize.x * _cellSize * 0.5f, _gridSize.y * _cellSize * 0.5f, 0f);
44:             _grid = new FlowFieldGrid(_gridSize, _cellSize, origin);
45: 
46:             UpdateFlowField();
47:         }
```

### Giải thích dòng 25 đến 47:
- **Dòng 25 đến 33 (`Awake`)**: Đăng ký Singleton an toàn.
- **Dòng 37: `Object.FindAnyObjectByType<PlayerController>()`**: Tìm Player chuẩn Unity 6.
- **Dòng 43: Tính `origin`**: Lùi góc dưới-trái về một khoảng bằng nửa chiều dài và chiều rộng của bàn cờ, giúp vị trí đặt GameObject `FlowFieldManager` nằm đúng ở **chính giữa tâm** của lưới.
- **Dòng 44**: Tạo mới đối tượng lưới `FlowFieldGrid`.
- **Dòng 46**: Chạy tính toán đợt đầu tiên ngay khi vào game.

---

```csharp
49:         private void Update()
50:         {
51:             _timeSinceLastUpdate += Time.deltaTime;
52: 
53:             if (_timeSinceLastUpdate >= _updateInterval)
54:             {
55:                 _timeSinceLastUpdate = 0f;
56:                 UpdateFlowField();
57:             }
58:         }
59: 
60:         public void UpdateFlowField()
61:         {
62:             if (_playerTransform == null || _grid == null) return;
63: 
64:             Vector3 center = _playerTransform.position;
65:             Vector3 newOrigin = center - new Vector3(_gridSize.x * _cellSize * 0.5f, _gridSize.y * _cellSize * 0.5f, 0f);
66:             
67:             _grid = new FlowFieldGrid(_gridSize, _cellSize, newOrigin);
68:             _grid.UpdateCostField(_obstacleLayer);
69: 
70:             FlowFieldCell playerCell = _grid.GetCellFromWorldPos(_playerTransform.position);
71:             _grid.CreateIntegrationField(playerCell);
72:             _grid.CreateFlowField();
73:         }
```

### Giải thích dòng 49 đến 73:
- **Dòng 51 đến 57**: Cộng dồn thời gian mỗi frame, khi chạm ngưỡng 0.2 giây thì reset về 0 và gọi `UpdateFlowField()`.
- **Dòng 64 & 65 (Lưới động chạy theo Player):**
  - Khi Player di chuyển khắp bản đồ thế giới rộng lớn, chiếc lưới 30x30 này sẽ **tự động dời tâm theo chân Player**!
- **Dòng 68**: Quét lại vật cản.
- **Dòng 70**: Tìm xem Player đang đứng ở ô nào.
- **Dòng 71 & 72**: Phát sóng loang và vẽ lại toàn bộ mũi tên vector dòng chảy.

---

```csharp
79:         // Hàm để bất kỳ con quái nào cũng có thể xin hướng đi trong O(1)
80:         public Vector2 GetFlowDirection(Vector3 worldPos)
81:         {
82:             if (_grid == null) return Vector2.zero;
83: 
84:             FlowFieldCell cell = _grid.GetCellFromWorldPos(worldPos);
85:             return cell != null ? cell.BestDirection : Vector2.zero;
86:         }
87: 
88:         // Vẽ các mũi tên dòng chảy trong cửa sổ Scene
89:         private void OnDrawGizmos()
90:         {
91:             if (!_drawGizmos || _grid == null) return;
92: 
93:             for (int x = 0; x < _grid.GridSize.x; x++)
94:             {
95:                 for (int y = 0; y < _grid.GridSize.y; y++)
96:                 {
97:                     FlowFieldCell cell = _grid.Cells[x, y];
98: 
99:                     if (cell.Cost == 255)
100:                     {
101:                         Gizmos.color = Color.red; // Ô tường: Màu đỏ
102:                         Gizmos.DrawWireCube(cell.WorldPos, Vector3.one * (_grid.CellSize * 0.9f));
103:                     }
104:                     else if (cell.BestDirection != Vector2.zero)
105:                     {
106:                         Gizmos.color = Color.yellow; // Mũi tên dòng chảy: Màu vàng
107:                         Vector3 start = cell.WorldPos;
108:                         Vector3 end = start + (Vector3)(cell.BestDirection * (_grid.CellSize * 0.4f));
109:                         Gizmos.DrawLine(start, end);
110:                     }
111:                 }
112:             }
113:         }
```

### Giải thích dòng 79 đến 113:
- **Dòng 80 đến 86 (`GetFlowDirection`)**:
  - Quái vật gọi hàm này, đưa vị trí của nó vào.
  - Hàm tra cứu ô tương ứng và trả về mũi tên vector `cell.BestDirection`.
- **Dòng 89 đến 112 (`OnDrawGizmos`)**:
  - Dành cho Game Developer và Designer quan sát trực quan trong cửa sổ Scene:
  - Nếu là ô Tường (`Cost == 255`): Vẽ chiếc hộp màu đỏ bao quanh.
  - Nếu là ô có dòng chảy: Vẽ một đoạn thẳng màu vàng từ tâm ô chỉ theo hướng vector dòng chảy.

---

# PHẦN 4: EnemyController.cs (GIẢI THÍCH TỪNG DÒNG)

File điều khiển hành vi nhân vật quái vật.

```csharp
1: using UnityEngine;
2: using Project_D.Player;
3: 
4: namespace Project_D.Enemies
5: {
6:     [RequireComponent(typeof(Rigidbody2D))]
7:     public class EnemyController : MonoBehaviour
8:     {
9:         [Header("Movement Settings")]
10:         [SerializeField] private float _moveSpeed = 3f;
11: 
12:         [Header("Health Settings")]
13:         [SerializeField] private int _maxHealth = 30;
14:         private int _currentHealth;
15: 
16:         [Header("Visual")]
17:         [SerializeField] private SpriteRenderer _spriteRenderer;
18: 
19:         private Rigidbody2D _rb;
20:         private Transform _playerTransform;
```

### Giải thích dòng 1 đến 20:
- **Dòng 6: `[RequireComponent(typeof(Rigidbody2D))]`**: Đảm bảo quái luôn có sẵn `Rigidbody2D`, không bao giờ bị quên lúc tạo Prefab.
- **Dòng 10: `_moveSpeed = 3f`**: Tốc độ chạy của quái vật (3 mét/giây).
- **Dòng 13 & 14**: `_maxHealth` (máu tối đa 30) và `_currentHealth` (lượng máu còn lại).
- **Dòng 17**: Tham chiếu đến `SpriteRenderer` để lật mặt nhân vật sang trái/phải.
- **Dòng 19 & 20**: Biến nội bộ chứa Rigidbody và vị trí của Player.

---

```csharp
22:         private void Awake()
23:         {
24:             _rb = GetComponent<Rigidbody2D>();
25:             _rb.gravityScale = 0f;
26:             _rb.freezeRotation = true;
27: 
28:             if (_spriteRenderer == null)
29:             {
30:                 _spriteRenderer = GetComponent<SpriteRenderer>();
31:             }
32:         }
33: 
34:         private void OnEnable()
35:         {
36:             _currentHealth = _maxHealth;
37:         }
38: 
39:         private void Start()
40:         {
41:             FindPlayer();
42:         }
43: 
44:         private void FindPlayer()
45:         {
46:             var player = Object.FindAnyObjectByType<PlayerController>();
47:             if (player != null)
48:             {
49:                 _playerTransform = player.transform;
50:             }
51:         }
```

### Giải thích dòng 22 đến 51:
- **Dòng 25: `_rb.gravityScale = 0f;`**: Khóa trọng lực để quái không bị rơi xuống dưới đáy màn hình.
- **Dòng 26: `_rb.freezeRotation = true;`**: Khóa xoay trục Z để quái không bị lộn nhào khi va quẹt.
- **Dòng 34 đến 37 (`OnEnable`)**: Reset máu đầy cây `_currentHealth = _maxHealth` mỗi khi lấy quái từ Pool ra dùng lại.
- **Dòng 44 đến 51 (`FindPlayer`)**: Tìm Player 1 lần duy nhất bằng hàm chuẩn Unity 6 `FindAnyObjectByType`.

---

```csharp
54:         private void FixedUpdate()
55:         {
56:             Move();
57:         }
58: 
59:         private void Update()
60:         {
61:             HandleFlip();
62:         }
63: 
64:         private void Move()
65:         {
66:             if (_playerTransform == null)
67:             {
68:                 FindPlayer();
69:                 return;
70:             }
71: 
72:             Vector2 moveDirection = Vector2.zero;
73: 
74:             // 1. Ưu tiên lấy hướng từ dòng chảy FlowField
75:             if (FlowField.FlowFieldManager.Instance != null)
76:             {
77:                 Vector2 flowDir = FlowField.FlowFieldManager.Instance.GetFlowDirection(transform.position);
78:                 if (flowDir.sqrMagnitude > 0.01f)
79:                 {
80:                     moveDirection = flowDir;
81:                 }
82:             }
83: 
84:             // 2. Dự phòng: Nếu quái đứng ngoài lưới thì chạy thẳng về phía Player
85:             if (moveDirection == Vector2.zero)
86:             {
87:                 Vector2 toPlayer = (Vector2)_playerTransform.position - (Vector2)transform.position;
88:                 moveDirection = toPlayer.normalized;
89:             }
90: 
91:             _rb.linearVelocity = moveDirection * _moveSpeed;
92:         }
```

### Giải thích dòng 54 đến 92:
- **Dòng 54: `FixedUpdate`**: Chạy đồng bộ với vật lý để gọi hàm `Move()`.
- **Dòng 59: `Update`**: Chạy theo khung hình hiển thị để gọi `HandleFlip()` (lật mặt sprite).
- **Dòng 74 đến 82 (HỎI HƯỚNG TỪ FLOW FIELD):**
  - Quái hỏi: `FlowFieldManager.Instance.GetFlowDirection(transform.position)`.
  - Nếu có mũi tên dòng chảy dưới chân -> Đi theo mũi tên đó (`moveDirection = flowDir`). Quái sẽ tự biết luồn lách qua ngóc ngách!
- **Dòng 84 đến 89 (DỰ PHÒNG):**
  - Nếu quái đứng ở quá xa ngoài rìa bản đồ chưa có lưới -> Lấy hướng chạy thẳng về Player.
- **Dòng 91: `_rb.linearVelocity = moveDirection * _moveSpeed;`**:
  - Gán vận tốc di chuyển chuẩn Unity 6.

---

```csharp
94:         private void HandleFlip()
95:         {
96:             if (_spriteRenderer == null) return;
97: 
98:             if (_rb.linearVelocity.x > 0.05f)
99:             {
100:                 _spriteRenderer.flipX = false;
101:             }
102:             else if (_rb.linearVelocity.x < -0.05f)
103:             {
104:                 _spriteRenderer.flipX = true;
105:             }
106:         }
107: 
108:         public void TakeDamage(int damage)
109:         {
110:             _currentHealth -= damage;
111:             Debug.Log($"<color=red>[ENEMY HIT]</color> {gameObject.name} bị trừ {damage} máu! Còn: {_currentHealth}/{_maxHealth}");
112: 
113:             if (_currentHealth <= 0)
114:             {
115:                 Die();
116:             }
117:         }
118: 
119:         private void Die()
120:         {
121:             Debug.Log($"<color=orange>[ENEMY DEAD]</color> {gameObject.name} đã bị tiêu diệt!");
122:             gameObject.SetActive(false);
123:         }
124:     }
125: }
```

### Giải thích dòng 94 đến 125:
- **Dòng 98 đến 105 (`HandleFlip`)**:
  - Dùng ngưỡng an toàn `0.05f` để chống rung lắc số thực.
  - Vận tốc X dương (chạy sang phải) -> `flipX = false` (mặt nhìn sang phải).
  - Vận tốc X âm (chạy sang trái) -> `flipX = true` (mặt nhìn sang trái).
- **Dòng 108 đến 117 (`TakeDamage`)**:
  - Khi đạn bắn trúng, trừ lượng máu tương ứng.
  - In thông báo đỏ ra Console.
  - Nếu máu `<= 0` -> Gọi hàm `Die()`.
- **Dòng 119 đến 123 (`Die`)**:
  - Tạm thời tắt đối tượng `gameObject.SetActive(false)` để chuẩn bị đưa vào hệ thống Object Pooling và rơi ngọc kinh nghiệm.

---

Toàn bộ 4 file mã nguồn đã được mổ xẻ tường tận từng dòng lệnh, từng biến và từng cấu trúc điều khiển!
Bạn có thể mở tài liệu này và xem lại bất cứ lúc nào trong file:
👉 **[FLOWFIELD_AND_ENEMY_GUIDE.md](file:///d:/Project_D/FLOWFIELD_AND_ENEMY_GUIDE.md)**

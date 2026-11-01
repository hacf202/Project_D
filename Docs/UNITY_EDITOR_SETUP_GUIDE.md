# HƯỚNG DẪN CHI TIẾT THIẾT LẬP & TÍCH HỢP HỆ THỐNG HẦM NGỤC TRÊN UNITY EDITOR

**Tài liệu dành cho:** Lập trình viên / Game Designer dự án _Project_D_  
**Mục tiêu:** Từng bước thiết lập, gắn kết linh kiện (wiring) và đưa toàn bộ hệ thống Hầm Ngục 13 Tầng vào hoạt động trơn tru trong Unity Editor từ A đến Z.

---

## MỤC LỤC HƯỚNG DẪN

- [PHẦN 1: CÂY CẤU TRÚC HIERARCHY MẪU (TỔNG QUAN)](#phần-1-cây-cấu-trúc-hierarchy-mẫu-tổng-quan)
- [PHẦN 2: THIẾT LẬP CÁC MANAGER TRONG SCENE](#phần-2-thiết-lập-các-manager-trong-scene)
  - [2.1. Thiết lập RunManager](#21-thiết-lập-runmanager)
  - [2.2. Thiết lập RoomController](#22-thiết-lập-roomcontroller)
- [PHẦN 3: TẠO & CẤU HÌNH 3 CỔNG DỊCH CHUYỂN (ROOM PORTALS)](#phần-3-tạo--cấu-hình-3-cổng-dịch-chuyển-room-portals)
- [PHẦN 4: TẠO & CẤU HÌNH CÁC ĐIỂM TƯƠNG TÁC (INTERACTION POINTS)](#phần-4-tạo--cấu-hình-các-điểm-tương-tác-interaction-points)
  - [4.1. Điểm Lửa Trại (RestSite Altar)](#41-điểm-lửa-trại-restsite-altar)
  - [4.2. Điểm Quầy Hàng (Shop Counter)](#42-điểm-quầy-hàng-shop-counter)
  - [4.3. Điểm Bệ Thờ Buff (Buff Altar)](#43-điểm-bệ-thờ-buff-buff-altar)
- [PHẦN 5: THIẾT LẬP CÁC BẢNG GIAO DIỆN TRÊN CANVAS](#phần-5-thiết-lập-các-bảng-giao-diện-trên-canvas)
  - [5.1. Thiết lập RunHUD (Tầng, Souls, Đợt Wave)](#51-thiết-lập-runhud-tầng-souls-đợt-wave)
  - [5.2. Thiết lập Bảng Lửa Trại (RestSitePanel)](#52-thiết-lập-bảng-lửa-trại-restsitepanel)
  - [5.3. Thiết lập Bảng Cửa Hàng (ShopPanel)](#53-thiết-lập-bảng-cửa-hàng-shoppanel)
  - [5.4. Thiết lập Bảng Vinh Danh Chiến Thắng (VictoryPanel)](#54-thiết-lập-bảng-vinh-danh-chiến-thắng-victorypanel)
- [PHẦN 6: KỊCH BẢN KIỂM THỬ PLAYTEST TOÀN BỘ VÒNG LẶP (CHECKLIST)](#phần-6-kịch-bản-kiểm-thử-playtest-toàn-bộ-vòng-lặp-checklist)
- [PHẦN 7: CÁC LỖI THƯỜNG GẶP & CÁCH KHẮC PHỤC (TROUBLESHOOTING)](#phần-7-các-lỗi-thường-gặp--cách-khắc-phục-troubleshooting)
- [PHẦN 8: BỘ CÔNG CỤ TẠO PHÒNG & HỆ THỐNG BIẾN THỂ (ROOM CREATOR TOOLSET)](#phần-8-bộ-công-cụ-tạo-phòng--hệ-thống-biến-thể-room-creator-toolset)

---

## PHẦN 1: CÂY CẤU TRÚC HIERARCHY MẪU (TỔNG QUAN)

Dưới đây là sơ đồ bố trí hoàn chỉnh các GameObject trong Scene `SampleScene`:

```
SampleScene
├── Main Camera
├── Global Light 2D
├── Player
├── PoolManager
├── FlowFieldManager
├── Wall_Test
├── DamageTextManager
│
├── [DUNGEON MANAGERS]
│   ├── RunManager                 <-- Component: RunManager.cs
│   └── RoomController             <-- Component: RoomController.cs
│       ├── Portals                <-- Thư mục chứa 3 cổng
│       │   ├── Portal_Left        <-- Component: RoomPortal.cs + Collider
│       │   ├── Portal_Center      <-- Component: RoomPortal.cs + Collider
│       │   └── Portal_Right       <-- Component: RoomPortal.cs + Collider
│       └── InteractionPoints      <-- Thư mục chứa điểm tương tác
│           ├── RestSite_Altar     <-- Component: InteractionPoint.cs (RestSite)
│           ├── Shop_Counter       <-- Component: InteractionPoint.cs (Shop)
│           └── Buff_Altar         <-- Component: InteractionPoint.cs (BuffReward)
│
└── Canvas
    ├── JoystickBackground
    ├── JoystickTouchZone
    ├── HUDPanel                   <-- Máu, Exp, Đồng hồ cũ
    ├── LevelUpPanel               <-- Bảng 3 thẻ bài
    ├── GameOverPanel              <-- Bảng khi chết
    ├── RunHUDPanel                <-- Component: RunHUD.cs (Tầng, Souls, Wave)
    ├── RestSitePanel              <-- Component: RestSiteUI.cs (Lửa trại)
    ├── ShopPanel                  <-- Component: ShopUI.cs (Cửa hàng)
    └── VictoryPanel               <-- Component: VictoryUI.cs (Chiến thắng Tầng 13)
```

---

## PHẦN 2: THIẾT LẬP CÁC MANAGER TRONG SCENE

### 2.1. Thiết lập `RunManager`

1. Tại cửa sổ **Hierarchy**, nhấp chuột phải vào vùng trống $\to$ chọn **Create Empty**.
2. Đổi tên GameObject thành: **`RunManager`**.
3. Chọn `RunManager`, sang bảng **Inspector** $\to$ bấm nút **Add Component** $\to$ gõ và chọn [RunManager.cs](file:///d:/Project_D/Assets/_Project/Scripts/Dungeon/Run/RunManager.cs).
4. **Cấu hình trên Inspector:**
   - **Map Config:** Để trống (`None`).
     _(Code đã tích hợp cơ chế Auto-Fallback: lúc ấn Play, nếu để trống nó sẽ tự động nạp đồ thị 13 tầng chuẩn 22 nút mà không cần tạo file asset thủ công)_.
   - **Current Floor:** Mặc định là `1`.
   - **Current Souls:** Mặc định là `0`.

---

### 2.2. Thiết lập `RoomController`

1. Tại cửa sổ **Hierarchy**, nhấp chuột phải $\to$ chọn **Create Empty**.
2. Đổi tên GameObject thành: **`RoomController`**.
3. Chọn `RoomController`, sang bảng **Inspector** $\to$ bấm nút **Add Component** $\to$ gõ và chọn [RoomController.cs](file:///d:/Project_D/Assets/_Project/Scripts/Dungeon/Room/RoomController.cs).
4. **Cấu hình trên Inspector:**
   - **Fallback Enemy Types:** Bấm dấu `+` để thêm 1 phần tử $\to$ kéo file `Enemy_Dummy` (hoặc file ScriptableObject quái trong `Assets/_Project/ScriptableObjects/Enemies/`) vào ô này.
   - **Min Spawn Radius:** `8`
   - **Max Spawn Radius:** `12`
   - Các ô Wave Config và Portal Visual Config cứ **để trống**, script sẽ tự động tạo cấu hình 3 Wave cho phòng thường và 4 Wave cho phòng Elite khi chạy game.

---

## PHẦN 3: TẠO & CẤU HÌNH 3 CỔNG DỊCH CHUYỂN (ROOM PORTALS)

Chúng ta cần chuẩn bị sẵn **3 vị trí Cổng Dịch Chuyển** trong phòng để phục vụ cho các tầng phân 1, 2 hoặc 3 nhánh rẽ.

### Bước 3.1: Tạo GameObject Cổng

1. Nhấp chuột phải vào GameObject **`RoomController`** $\to$ chọn **Create Empty** $\to$ đổi tên thành **`Portal_Center`**.
2. Đặt vị trí (Transform) của cổng ở mép trên phòng:
   - **Position:** `X = 0, Y = 6.5, Z = 0`
   - **Scale:** `X = 1, Y = 1, Z = 1`

### Bước 3.2: Thêm các Component vào Cổng

Vẫn đang chọn `Portal_Center`:

1. **Thêm Collider:** Bấm **Add Component** $\to$ thêm **`Circle Collider 2D`**:
   - Tick chọn ô **`Is Trigger = true`** _(BẮT BUỘC để người chơi bước xuyên qua được)_.
   - **Radius:** Chỉnh lên khoảng `1.2`.
2. **Thêm Sprite Hiển Thị Cổng:** Bấm **Add Component** $\to$ thêm **`Sprite Renderer`**:
   - Ô **Sprite**: Chọn một hình tròn có sẵn (như `Knob`, `UISprite` hoặc Sprite vòng xoay portal nếu có).
   - Ô **Color**: Chọn màu trắng tinh (script sẽ tự động đổi màu theo từng loại phòng khi mở cổng).
   - **Order in Layer:** Đặt là `5` (để cổng nổi lên trên mặt sàn).
3. **Thêm Text Hiển Thị Biểu Tượng Icon:**
   - Nhấp chuột phải vào `Portal_Center` $\to$ chọn **3D Object** $\to$ chọn **Text - TextMeshPro** $\to$ đổi tên thành **`Symbol_Text`**.
   - **Rect Transform:** `Pos X = 0, Pos Y = 0, Pos Z = 0`, `Width = 2, Height = 2`.
   - **Font Size:** `8` (In đậm **B**).
   - **Alignment:** Chọn **Center** và **Middle**.
   - **Extra Settings $\to$ Order in Layer:** Đặt là `10`.
4. **Thêm Text Hiển Thị Tên Tầng:**
   - Nhấp chuột phải vào `Portal_Center` $\to$ chọn **3D Object** $\to$ chọn **Text - TextMeshPro** $\to$ đổi tên thành **`Label_Text`**.
   - **Rect Transform:** `Pos X = 0, Pos Y = -1.5, Pos Z = 0`, `Width = 4, Height = 1.5`.
   - **Font Size:** `4` (In đậm **B**).
   - **Alignment:** Chọn **Center** và **Middle**.
   - **Extra Settings $\to$ Order in Layer:** Đặt là `10`.
5. **Gắn Script Cổng:**
   - Bấm vào GameObject cha `Portal_Center` $\to$ Bấm **Add Component** $\to$ thêm [RoomPortal.cs](file:///d:/Project_D/Assets/_Project/Scripts/Dungeon/Room/RoomPortal.cs).
   - Kéo chính component `Sprite Renderer` trên nó vào ô **`Portal Renderer`**.
   - Kéo GameObject con `Symbol_Text` vào ô **`Symbol Text`**.
   - Kéo GameObject con `Label_Text` vào ô **`Label Text`**.

### Bước 3.3: Nhân bản thành 3 Cổng (Left, Center, Right)

1. Bấm chuột phải vào `Portal_Center` $\to$ chọn **Duplicate** (`Ctrl + D`) $\to$ đổi tên thành **`Portal_Left`**.
   - Chỉnh lại vị trí **Position:** `X = -5, Y = 5.5, Z = 0`.
2. Bấm chuột phải vào `Portal_Center` $\to$ chọn **Duplicate** (`Ctrl + D`) $\to$ đổi tên thành **`Portal_Right`**.
   - Chỉnh lại vị trí **Position:** `X = 5, Y = 5.5, Z = 0`.

### Bước 3.4: Gán 3 Cổng vào `RoomController`

1. Bấm chọn GameObject **`RoomController`** trong Hierarchy.
2. Tại mục **Room Portals** trong Inspector:
   - Nhập ô **Size** = `3`.
   - Kéo `Portal_Left` vào phần tử **Element 0**.
   - Kéo `Portal_Center` vào phần tử **Element 1**.
   - Kéo `Portal_Right` vào phần tử **Element 2**.

---

## PHẦN 4: TẠO & CẤU HÌNH CÁC ĐIỂM TƯƠNG TÁC (INTERACTION POINTS)

Trong các phòng phi chiến đấu (Lửa trại, Cửa hàng, Nhận buff), người chơi cần một vật thể vật lý trong phòng để chạm vào mở giao diện.

### 4.1. Điểm Lửa Trại (`RestSite_Altar`)

1. Nhấp chuột phải vào `RoomController` $\to$ chọn **Create Empty** $\to$ đổi tên thành **`RestSite_Altar`**.
2. **Position:** `X = -2, Y = 2, Z = 0`.
3. Bấm **Add Component** $\to$ thêm **`Circle Collider 2D`**:
   - Tick chọn **`Is Trigger = true`**.
   - **Radius:** `1.8`.
4. Bấm **Add Component** $\to$ thêm **`Sprite Renderer`** $\to$ gán hình đống lửa hoặc hình tròn màu cam/đỏ.
5. Bấm **Add Component** $\to$ thêm [InteractionPoint.cs](file:///d:/Project_D/Assets/_Project/Scripts/Dungeon/Interactions/InteractionPoint.cs):
   - **Type:** Chọn **`RestSite`**.
   - **Prompt Text:** Gõ `"Chạm để Nghỉ Ngơi"`.
   - **One Time Use:** Tick chọn `true`.

### 4.2. Điểm Quầy Hàng (`Shop_Counter`)

1. Nhấp chuột phải vào `RoomController` $\to$ chọn **Create Empty** $\to$ đổi tên thành **`Shop_Counter`**.
2. **Position:** `X = 2, Y = 2, Z = 0`.
3. Bấm **Add Component** $\to$ thêm **`Circle Collider 2D`** (Tick chọn **`Is Trigger = true`**, Radius = `1.8`).
4. Bấm **Add Component** $\to$ thêm **`Sprite Renderer`** $\to$ gán hình màu vàng kim.
5. Bấm **Add Component** $\to$ thêm [InteractionPoint.cs](file:///d:/Project_D/Assets/_Project/Scripts/Dungeon/Interactions/InteractionPoint.cs):
   - **Type:** Chọn **`Shop`**.
   - **Prompt Text:** Gõ `"Chạm để Mua Sắm"`.
   - **One Time Use:** Bỏ tick (người chơi có thể chạm lại để mua nhiều món đồ).

### 4.3. Điểm Bệ Thờ Buff (`Buff_Altar`)

1. Nhấp chuột phải vào `RoomController` $\to$ chọn **Create Empty** $\to$ đổi tên thành **`Buff_Altar`**.
2. **Position:** `X = 0, Y = 2, Z = 0`.
3. Bấm **Add Component** $\to$ thêm **`Circle Collider 2D`** (Tick chọn **`Is Trigger = true`**, Radius = `c`).
4. Bấm **Add Component** $\to$ thêm **`Sprite Renderer`** $\to$ gán hình màu xanh ngọc.
5. Bấm **Add Component** $\to$ thêm [InteractionPoint.cs](file:///d:/Project_D/Assets/_Project/Scripts/Dungeon/Interactions/InteractionPoint.cs):
   - **Type:** Chọn **`BuffReward`**.
   - **Prompt Text:** Gõ `"Chạm để Nhận Buff"`.
   - **One Time Use:** Tick chọn `true`.

### Bước 4.4: Gán các điểm tương tác vào `RoomController`

1. Chọn GameObject **`RoomController`**.
2. Tại mục **Interaction Points** trong Inspector:
   - Nhập **Size** = `3`.
   - Kéo `RestSite_Altar` vào **Element 0**.
   - Kéo `Shop_Counter` vào **Element 1**.
   - Kéo `Buff_Altar` vào **Element 2**.

---

## PHẦN 5: THIẾT LẬP CÁC BẢNG GIAO DIỆN TRÊN CANVAS

### 5.1. Thiết lập `RunHUD` (Tầng, Souls, Đợt Wave)

1. Trong cửa sổ **Hierarchy**, tìm và chọn GameObject **`Canvas`**.
2. Nhấp chuột phải vào `Canvas` $\to$ chọn **Create Empty** $\to$ đổi tên thành **`RunHUDPanel`**.
3. Bấm **Add Component** $\to$ thêm [RunHUD.cs](file:///d:/Project_D/Assets/_Project/Scripts/Dungeon/UI/RunHUD.cs).
4. **Tạo các Text con dưới `RunHUDPanel`:**
   - **Text Tầng (`FloorText`):** Chuột phải vào `RunHUDPanel` $\to$ UI $\to$ Text - TextMeshPro $\to$ Đặt ở góc trên bên trái (`Anchor: Top-Left`), gõ mẫu `"TẦNG 1 / 13"`, Font Size 24.
   - **Text Loại Phòng (`RoomTypeText`):** Đặt ngay bên dưới FloorText, Font Size 18, gõ mẫu `"Quái Thường"`.
   - **Text Linh Hồn (`SoulsText`):** Đặt ở góc trên bên phải (`Anchor: Top-Right`), Font Size 22, màu vàng kim, gõ mẫu `"Linh Hồn: 0 Souls"`.
   - **Panel Wave (`WaveInfoPanel`):** Tạo 1 GameObject rỗng ở giữa trên cùng màn hình (`Anchor: Top-Center`). Bên trong tạo 2 Text:
     - `WaveProgressText`: `"ĐỢT: 1 / 3"`.
     - `RemainingEnemiesText`: `"Quái: 6 / 6"`.
   - **Banner Chiến Thắng (`RoomClearedBanner`):** Tạo 1 Text to ở giữa màn hình:
     - Text: `"PHÒNG ĐÃ DỌN SẠCH! HÃY BƯỚC VÀO CỔNG"`, Font Size 36, màu vàng rực.
     - Tắt dấu tick Active của GameObject này đi (script sẽ tự bật khi dọn sạch phòng).
5. **Kéo thả vào Inspector của `RunHUD`:** Kéo các Text vừa tạo tương ứng vào các ô của component `RunHUD`.

---

### 5.2. Thiết lập Bảng Lửa Trại (`RestSitePanel`)

1. Nhấp chuột phải vào `Canvas` $\to$ UI $\to$ **Panel** $\to$ đổi tên thành **`RestSitePanel`**.
2. Đặt màu nền mờ tối (`Color: Black, Alpha ~ 200`).
3. **Thêm Component:**
   - Bấm **Add Component** $\to$ thêm **`Canvas Group`**.
   - Bấm **Add Component** $\to$ thêm [RestSiteUI.cs](file:///d:/Project_D/Assets/_Project/Scripts/Dungeon/UI/RestSiteUI.cs).
4. **Tạo các thành phần bên trong `RestSitePanel`:**
   - **Tiêu đề:** Text TMP `"KHU NGHỈ NGƠI - LỬA TRẠI"`.
   - **Text Máu:** Text TMP hiển thị `"Máu hiện tại: 100 / 100"`.
   - **Nút 1 - Hồi miễn phí (`FreeRestButton`):** Button UI có nhãn `"Nghỉ Ngơi Tĩnh Dưỡng (Hồi 30% Máu - Miễn Phí)"`.
   - **Nút 2 - Hồi đầy bằng Souls (`FullHealSoulsButton`):** Button UI có nhãn `"Cầu Nguyện Linh Hồn (Hồi Đầy 100% Máu - Tốn 50 Souls)"`.
   - **Nút 3 - Rời đi (`CloseButton`):** Button UI có nhãn `"Rời Lửa Trại"`.
5. **Kéo thả vào Inspector của `RestSiteUI`:**
   - Kéo chính GameObject `RestSitePanel` vào ô **`Panel`**.
   - Kéo component `Canvas Group` vào ô **`Canvas Group`**.
   - Kéo các Nút và Text tương ứng vào các ô trong Inspector.

---

### 5.3. Thiết lập Bảng Cửa Hàng (`ShopPanel`)

1. Nhấp chuột phải vào `Canvas` $\to$ UI $\to$ **Panel** $\to$ đổi tên thành **`ShopPanel`**.
2. **Thêm Component:** Thêm **`Canvas Group`** và [ShopUI.cs](file:///d:/Project_D/Assets/_Project/Scripts/Dungeon/UI/ShopUI.cs).
3. **Tạo các thành phần bên trong `ShopPanel`:**
   - **Tiêu đề:** `"CỬA HÀNG THƯƠNG NHÂN"`.
   - **Text Số Dư:** Text TMP `"Linh Hồn: 0 Souls"`.
   - **Nút Rời Đi:** Button `"Rời Cửa Hàng"`.
   - **4 Nút Mua Hàng:**
     1. Nút Bình Máu (`HealPotionButton`): `"Bình Máu Khẩn Cấp (+40 HP) - Giá: 20 Souls"`.
     2. Nút Tăng Sát Thương (`DamageButton`): `"Tăng Sát Thương (+5 Dmg) - Giá: 45 Souls"`.
     3. Nút Tăng Tốc Bắn (`FireRateButton`): `"Tăng Tốc Độ Bắn (+20%) - Giá: 35 Souls"`.
     4. Nút Tăng Đạn Chùm (`MultiShotButton`): `"Tăng Đạn Chùm (+1 Đạn) - Giá: 60 Souls"`.
4. **Kéo thả vào Inspector của `ShopUI`:** Kéo `Panel`, `Canvas Group`, các Button và Text vào các ô tương ứng.

---

### 5.4. Thiết lập Bảng Vinh Danh Chiến Thắng (`VictoryPanel`)

1. Nhấp chuột phải vào `Canvas` $\to$ UI $\to$ **Panel** $\to$ đổi tên thành **`VictoryPanel`**.
2. **Thêm Component:** Thêm **`Canvas Group`** và [VictoryUI.cs](file:///d:/Project_D/Assets/_Project/Scripts/Dungeon/UI/VictoryUI.cs).
3. **Tạo các thành phần bên trong `VictoryPanel`:**
   - **Tiêu đề:** `"CHIẾN THẮNG RUN - CHINH PHỤC 13 TẦNG!"`, màu vàng rực rỡ.
   - **Text Thời Gian:** `"Thời Gian: 12:45"`.
   - **Text Cấp Độ:** `"Cấp Độ: Cấp 12"`.
   - **Text Souls:** `"Linh Hồn Thu Thập: 350 Souls"`.
   - **Nút Bắt Đầu Lại:** Button `"Chơi Lại Trận Mới"`.
4. **Kéo thả vào Inspector của `VictoryUI`:** Kéo các ô tương ứng vào Inspector.

---

## PHẦN 6: KỊCH BẢN KIỂM THỬ PLAYTEST TOÀN BỘ VÒNG LẶP (CHECKLIST)

Sau khi thiết lập xong, bấm nút **Play** trong Unity và kiểm tra theo danh sách:

1. **Khởi Động Tầng 1 (Quái Thường):**
   - [ ] Góc màn hình hiển thị: `TẦNG 1 / 13 - Quái Thường`.
   - [ ] Toàn bộ 3 cổng dịch chuyển đang đóng/tắt.
   - [ ] Đợt 1 sinh ra 6 quái vật.
   - [ ] Bắn hạ mỗi con quái -> Souls nhảy trực tiếp `+2 Souls` trên HUD (không cần nhặt đồ rơi).
   - [ ] Tiêu diệt hết 6 con -> Tự động chuyển sang Đợt 2 (10 con) -> Đợt 3 (15 con).
2. **Hoàn Thành Tầng 1 (Room Cleared):**
   - [ ] Diệt sạch con quái cuối cùng -> Hiện banner `"PHÒNG ĐÃ DỌN SẠCH!"`.
   - [ ] Toàn bộ ngọc Exp còn sót trên sàn tự động bay về người chơi.
   - [ ] **1 Cổng Dịch Chuyển xuất hiện** ở giữa (`Portal_Center`) hiển thị huy hiệu `[ GIFT ]` và nhãn `Tầng 2: Thưởng Khởi Đầu`.
3. **Tiến Vào Tầng 2 (Thưởng Khởi Đầu - Phi Chiến Đấu):**
   - [ ] Điều khiển nhân vật bước vào cổng `Portal_Center`.
   - [ ] Tầng chuyển thành: `TẦNG 2 / 13 - Thưởng Khởi Đầu`.
   - [ ] Nhân vật được đưa về tâm phòng `(0, 0)`.
   - [ ] **2 Cổng Dịch Chuyển mở sẵn ngay lập tức:**
     - Cổng bên trái: `Tầng 3A - Quái Thường [ VS ]`.
     - Cổng bên phải: `Tầng 3B - Quái Thường [ VS ]`.
   - [ ] Bệ thờ nhận buff xuất hiện ở giữa phòng. Người chơi có thể chạm vào để nhận buff hoặc bước thẳng qua 1 trong 2 cổng để sang Tầng 3!
4. **Tầng 3 $\to$ Tầng 4 (Phân 3 Nhánh: Cửa Hàng, Buff, Hồi Máu):**
   - [ ] Chọn 1 cổng ở Tầng 3 $\to$ Đánh sạch quái $\to$ **Xuất hiện đủ cả 3 Cổng dịch chuyển** dẫn tới 3 lựa chọn của Tầng 4!
   - [ ] Bước vào cổng Cửa Hàng $\to$ Điểm tương tác Shop xuất hiện $\to$ Chạm vào mở bảng mua sắm bằng Souls.

---

## PHẦN 7: CÁC LỖI THƯỜNG GẶP & CÁCH KHẮC PHỤC (TROUBLESHOOTING)

| Hiện tượng | Nguyên nhân | Cách khắc phục |
| :--- | :--- | :--- |
| **Cảnh báo `The character with Unicode value \u2694 was not found in [LiberationSans SDF]`** | Font mặc định `LiberationSans SDF` của TextMeshPro chỉ hỗ trợ ASCII / Latin cơ bản, không có các ký tự Emoji Unicode dải cao (như thanh kiếm ⚔, khiên, v.v.). | Hệ thống đã tự động chuyển đổi các biểu tượng chữ sang huy hiệu chuẩn ASCII (`VS`, `GIFT`, `ELITE`, `M-BOSS`, `REST`, `SHOP`, `BUFF`, `BOSS`, `GATE`). Ngoài ra bạn có thể gán file ảnh Sprite vào ô **Custom Icon** trong [PortalVisualConfigSO.cs](file:///d:/Project_D/Assets/_Project/Scripts/Dungeon/Room/PortalVisualConfigSO.cs) để hiển thị hình đồ họa đẹp mắt. |
| **Bước vào cổng nhưng không có gì xảy ra** | `Circle Collider 2D` trên Cổng chưa tick `Is Trigger`, hoặc Player thiếu thẻ Tag `Player`. | Chọn GameObject Cổng, kiểm tra xem ô **`Is Trigger`** đã được bật chưa. Chọn Player, kiểm tra mục **Tag** trên đầu Inspector xem đã là `Player` chưa. |
| **Không thấy Cổng xuất hiện sau khi diệt quái** | Mảng `Room Portals` trên `RoomController` đang bị trống. | Chọn `RoomController`, kéo 3 cổng `Portal_Left`, `Portal_Center`, `Portal_Right` vào mảng `Room Portals`. |
| **Quái sinh liên tục không dừng thành Wave** | GameObject `EnemySpawner` cũ trong Scene vẫn đang chạy độc lập. | Script `RoomController` đã có lệnh tự động dừng Spawner cũ, nhưng bạn cũng có thể tắt dấu tick Active của GameObject `EnemySpawner` cũ đi. |
| **Chữ số trên Cổng bị chìm dưới mặt sàn** | `Order in Layer` của TextMeshPro đang là `0`. | Bấm mở mục **Extra Settings** trên component TextMeshPro của cổng, đổi **`Order in Layer`** thành `10`. |
| **Bảng Lửa Trại / Shop hiện đè lên màn hình lúc mới vào game** | `Canvas Group` hoặc GameObject Panel chưa được tắt. | [RestSiteUI.cs](file:///d:/Project_D/Assets/_Project/Scripts/Dungeon/UI/RestSiteUI.cs) và [ShopUI.cs](file:///d:/Project_D/Assets/_Project/Scripts/Dungeon/UI/ShopUI.cs) đã tự động ẩn trong `Awake()`, bạn chỉ cần đảm bảo đã kéo `Canvas Group` vào ô tương ứng trong Inspector. |

---

## PHẦN 8: BỘ CÔNG CỤ TẠO PHÒNG & HỆ THỐNG BIẾN THỂ (ROOM CREATOR TOOLSET)

Để mang lại trải nghiệm phong phú cho hành trình 13 tầng hầm ngục, hệ thống hỗ trợ **tải phòng động** từ danh mục các biến thể khác nhau (`RoomVariationSO`) được quản lý bởi [RoomDatabaseSO.cs](file:///d:/Project_D/Assets/_Project/Scripts/Dungeon/Room/RoomDatabaseSO.cs) và [RoomManager.cs](file:///d:/Project_D/Assets/_Project/Scripts/Dungeon/Room/RoomManager.cs).

### 8.1. Thiết lập 1-Click: Gắn `RoomManager` vào Scene

1. Trên thanh Menu đỉnh của Unity Editor, chọn:  
   **`Project_D` $\to$ `Dungeon` $\to$ `Setup RoomManager In Scene`**.
2. **Hệ thống tự động thực hiện:**
   - Tìm hoặc tạo GameObject `[RoomManager]` trong Scene.
   - Gắn component [RoomManager.cs](file:///d:/Project_D/Assets/_Project/Scripts/Dungeon/Room/RoomManager.cs).
   - Tự động tạo hoặc liên kết file asset [RoomDatabase.asset](file:///d:/Project_D/Assets/_Project/ScriptableObjects/Dungeon/RoomDatabase.asset).
   - Đánh dấu lưu Scene sẵn sàng.

---

### 8.2. 1-Click: Tự Động Sinh Toàn Bộ 7 Phòng Mẫu Chuẩn

Bạn có thể tạo ngay toàn bộ 7 biến thể phòng chuẩn mực cho game mà không cần cấu hình thủ công bất kỳ thứ gì:

1. Trên Menu Unity Editor, chọn:  
   **`Project_D` $\to$ `Dungeon` $\to$ `Batch Generate All 7 Rooms`**  
   _(hoặc bấm nút màu vàng trong cửa sổ Room Creator Tool)_.
2. Nhấn nút **"Sinh ngay"** trên hộp thoại xác nhận.
3. **Kết quả đạt được ngay lập tức:**
   - **7 Prefabs phòng** hoàn chỉnh được tạo tại `Assets/_Project/Prefabs/Dungeon/Rooms/`:
     1. `Room_Normal_OpenField.prefab` (30x30, Sân mở quái thường)
     2. `Room_Normal_FourPillars.prefab` (30x30, 4 Cột che chắn)
     3. `Room_Elite_Chokepoints.prefab` (34x34, Chữ thập chia làn quái Elite)
     4. `Room_RestSite_Campfire.prefab` (20x20, Khu lửa trại hồi máu)
     5. `Room_Shop_MerchantOutpost.prefab` (22x22, Quầy hàng thương nhân)
     6. `Room_BuffReward_AltarSanctum.prefab` (24x24, Điện thờ ban phước)
     7. `Room_Boss_Colosseum.prefab` (44x44, Đại đấu trường Boss giác đấu)
   - **7 ScriptableObjects** tương ứng tại `Assets/_Project/ScriptableObjects/Dungeon/Rooms/`.
   - **Tự động liên kết toàn bộ 7 phòng vào [RoomDatabase.asset](file:///d:/Project_D/Assets/_Project/ScriptableObjects/Dungeon/RoomDatabase.asset)**.

---

### 8.3. Tự Thiết Kế Phòng Mới Bằng Cửa Sổ "Room Creator Tool"

Để thiết kế thêm các căn phòng độc đáo theo ý đồ Level Design riêng của bạn:

1. Mở cửa sổ công cụ: **`Project_D` $\to$ `Dungeon` $\to$ `Room Creator Tool`**.
2. **Cấu hình các tham số:**
   - **Tên Phòng (Prefab Name):** Đặt tên định danh (ví dụ: `Room_Normal_Labyrinth`).
   - **Loại Node Hỗ Trợ:** Chọn loại phòng (`NormalBattle`, `EliteBattle`, `BossBattle`, `RestSite`, `Shop`, `BuffReward`). Công cụ sẽ tự động điều chỉnh số lượng spawn và kiểu bố cục tương thích!
   - **Trọng Số Ngẫu Nhiên (Selection Weight):** Tỷ lệ được bốc thăm trong Database (1-100).
   - **Kích Thước Mẫu:** Chọn nhanh `20x20`, `30x30`, `40x40`, `45x45` hoặc `Custom` tùy ý.
   - **Kiểu Bố Cục Kiến Trúc:**
     - `OpenArena`: Đấu trường mở thoáng.
     - `FourCornerPillars`: 4 cột trụ góc cản đạn.
     - `CenterCrossChokepoints`: Chướng ngại vật chữ thập chia làn giao tranh hẹp.
     - `RingOfBoulders`: Vòng tròn trụ đá bao quanh tâm đấu trường.
     - `RestSiteCamp`: Bố cục lửa trại và ghế đá.
     - `ShopOutpost`: Bố cục quầy bar ngăn cách và hòm đồ.
     - `BuffAltarSanctum`: Hàng cột dẫn lối và bệ thờ trung tâm.
   - **Điểm Xuất Hiện & Cổng:** Tùy chọn 4-16 điểm quái spawn và 1-3 Cổng dịch chuyển.
   - **Màu Sắc:** Tự do phối màu Sàn, Tường bao và Vật cản.
3. **Thực thi:**
   - Bấm **"Tạo Phòng Trực Tiếp Vào Scene"**: Để sinh phòng ngay trên Scene view, giúp bạn chạy thử hoặc trang trí thêm asset.
   - Bấm **"Xuất Bản Prefab + RoomVariationSO + Đăng Ký Database"**: Công cụ sẽ tự động đóng gói Prefab, tạo file dữ liệu và đưa phòng vào danh sách chơi ngẫu nhiên của game!

---

### 8.4. Cơ Chế Hoạt Động & Tương Thích Tự Động Với `FlowFieldManager`

- **Tự động chuyển phòng không cần load scene:** Khi người chơi bước qua cổng dịch chuyển sang tầng mới, [RoomManager.cs](file:///d:/Project_D/Assets/_Project/Scripts/Dungeon/Room/RoomManager.cs) sẽ:
  1. Hủy (Destroy) GameObject phòng cũ để giải phóng bộ nhớ.
  2. Bốc ngẫu nhiên một biến thể phòng tương ứng từ Database (tự động tránh trùng lặp với phòng vừa chơi).
  3. Instantiate Prefab phòng mới và gắn kết vào [RoomController.cs](file:///d:/Project_D/Assets/_Project/Scripts/Dungeon/Room/RoomController.cs).
  4. Đặt nhân vật vào đúng điểm xuất hiện `PlayerSpawnPoint` của phòng.
  5. Gọi `FlowFieldManager.Instance.UpdateFlowField()` để lưới tìm đường 2D quét lại các bức tường/chướng ngại vật trên Layer `Obstacle ` $\to$ **Quái vật di chuyển thông minh, tự động né tránh các cột đá của phòng mới ngay lập tức mà không bao giờ bị kẹt!**


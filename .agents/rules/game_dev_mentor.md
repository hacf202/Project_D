# BẢN QUY TẮC ĐỒNG HÀNH & TIÊU CHUẨN LẬP TRÌNH GAME (UNITY 6)
**Dự án:** Project_D — Hybrid Survivor x Dungeon Crawler (Vampire Survivors x Soul Knight)  
**Nền tảng mục tiêu:** Mobile (Android / iOS)  
**Phiên bản Engine:** Unity 6 (URP 2D)  
**Vai trò:** Senior Game Dev & Technical Mentor (AI) 🤝 Junior / Hands-on Game Developer (User)

---

## I. NGUYÊN TẮC HƯỚNG DẪN & TƯ DUY LÀM VIỆC (MENTORSHIP PROTOCOL)

1. **"Người Hướng Dẫn Kèm Cặp - Bạn Tự Tay Code"**:
   - Quy trình mỗi tính năng: 
     1. **Phân tích cơ chế game (Mechanics)**.
     2. **Thiết kế kiến trúc (Architecture & Data Flow)**: Xác định vai trò từng Class/Component, luồng dữ liệu trước khi viết.
     3. **Cung cấp Pseudo-code hoặc Code mẫu chuẩn (Best Practices)** kèm chú thích sâu về nguyên lý hoạt động.
     4. **Chỉ ra các cạm bẫy (Gotchas / Anti-patterns)** thường gặp trong thực tế.
     5. **Người học tự tay gõ code và kiểm thử**, mentor hỗ trợ review và tối ưu hóa.

2. **Tư duy Kiến trúc & Tránh Nợ Kỹ thuật (Scalability & Clean Architecture)**:
   - **Composition over Inheritance**: Ưu tiên ghép nối Component độc lập (`HealthComponent`, `HitboxComponent`, `MovementController`) thay vì tạo chuỗi kế thừa sâu.
   - **ScriptableObject-Driven Design**: Tách toàn bộ chỉ số vũ khí, quái, thẻ kỹ năng ra `ScriptableObject` để Designer/Dev dễ cân bằng game mà không chạm vào logic code.
   - **Decoupled via Events (Observer Pattern)**: Giao tiếp giữa các hệ thống (Combat -> UI, LevelUp -> Audio, Player -> Spawner) dùng C# Events hoặc ScriptableObject Event Channels, hạn chế tham chiếu trực tiếp.
   - **Game State Machine**: Quản lý tập trung trạng thái toàn cục (`MainMenu`, `CombatPlaying`, `LevelUpPaused`, `RoomTransitioning`, `GameOver`) thay vì rải rác biến cờ `bool`.

3. **Tư duy Tối ưu Mobile First (Mobile Performance Standards)**:
   - **Zero Allocation trong `Update()` / `FixedUpdate()`**: Tuyệt đối không `new List/Array`, không nối chuỗi (`"Score: " + score`), không gọi `GetComponent` hay `FindObjectOfType`.
   - **Bắt buộc dùng Object Pooling**: Cho mọi đối tượng xuất hiện/biến mất liên tục (Quái, Đạn, Hiệu ứng VFX, Chữ sát thương Floating Damage, Ngọc Exp, Audio SFX).
   - **Sử dụng API Physics NonAlloc**: `Physics2D.OverlapCircleNonAlloc`, `Physics2D.RaycastNonAlloc` với mảng đệm tĩnh (`buffer`).
   - **Target 60 FPS mượt mà**: Luôn thiết lập `Application.targetFrameRate = 60` và quản lý `Screen.sleepTimeout`. Tối ưu Sprite Atlas và hạn chế Overdraw.
   - **Xử lý Safe Area**: Đảm bảo UI không bị che bởi tai thỏ / camera nốt ruồi trên các thiết bị di động hiện đại.

---

## II. ĐẶC TẢ TÍNH NĂNG TRÒ CHƠI (CORE GAME MECHANICS)

Sự kết hợp giữa 2 phong cách chơi kinh điển:
1. **Cơ chế Phòng Hầm Ngục (Soul Knight Style)**:
   - Bản đồ chia thành các phòng (Room Grid).
   - Khi vào phòng: Cửa đóng sập, rào chắn kích hoạt, kích hoạt wave quái.
   - Hoàn thành phòng: Mở cửa, xuất hiện rương thưởng / bùa lợi / cổng dịch chuyển sang phòng tiếp theo hoặc phòng Boss.
2. **Cơ chế Sinh Tồn & Xả Đạn (Vampire Survivors Style)**:
   - Di chuyển bằng Virtual Joystick mượt mà, tự động tìm mục tiêu & khai hỏa (Auto-Aim / Auto-Cast).
   - Số lượng quái đông đảo (Swarm Mob), rớt ngọc kinh nghiệm (Exp Gems) có cơ chế nam châm hút.
   - Thăng cấp giữa trận: Dừng thời gian (Pause Game), xuất hiện giao diện chọn 1 trong 3 nâng cấp ngẫu nhiên.
3. **Cảm Giác Đánh & Độ Trọng Lượng ("Juice" / Game Feel)**:
   - **Hit-Stop (Micro-freeze)**: Khựng hình 0.03 - 0.05s khi đòn đánh mạnh trúng đích.
   - **Flash White on Hit**: Quái nháy trắng 1 frame báo hiệu nhận sát thương.
   - **Camera Shake**: Rung lắc camera nhẹ khi nổ hoặc chịu sát thương lớn.
   - **Knockback**: Đạn tạo lực đẩy lùi quái để cảm nhận được uy lực của vũ khí.
4. **Meta-Progression & Lưu Trữ (Save/Load System)**:
   - Thu thập vàng/ngọc trong trận đấu để nâng cấp chỉ số vĩnh viễn trong sảnh (Lobby) hoặc mở khóa nhân vật/vũ khí mới.

---

## III. QUY CHUẨN TỔ CHỨC DỰ ÁN & MÃ NGUỒN UNITY 6

### 1. Cấu Trúc Thư Mục Chuẩn (`Assets/_Project/`)
Tất cả tài nguyên tự viết phải nằm trong thư mục `_Project` để không lẫn với các Asset/Plugin bên thứ 3:
```text
Assets/
└── _Project/
    ├── Art/                (Sprites, UI Icons, Animations, Materials)
    ├── Audio/              (SFX, BGM)
    ├── Prefabs/            (Player, Enemies, Projectiles, UI, Rooms)
    ├── Scenes/             (Boot, Lobby, Dungeon)
    ├── ScriptableObjects/  (Weapons, Enemies, Upgrades, GameEvents)
    └── Scripts/
        ├── Core/           (Bootstrapper, GameStateManager, ObjectPooler, Safe Area)
        ├── Player/         (PlayerController, PlayerStats, Movement)
        ├── Combat/         (Weapons, Projectiles, HealthSystem, DamageSystem)
        ├── Enemies/        (EnemyAI, EnemySpawner, WaveManager)
        ├── Dungeon/        (RoomController, Door, DungeonGenerator)
        ├── Upgrades/       (UpgradeManager, UpgradeDataSO, CardUI)
        ├── UI/             (HUD, Joystick, LevelUpUI, GameOverUI)
        └── Audio/          (AudioManager, SoundPool)
```

### 2. Quy Tắc Code C#
- **Naming Conventions**:
  - `PascalCase` cho Tên Class, Struct, Enum, Public Method, Public Property.
  - `_camelCase` cho Private Field (Ví dụ: `[SerializeField] private float _moveSpeed;`).
  - Hậu tố định danh rõ ràng: `PlayerController`, `WeaponDataSO`, `EnemySpawner`, `RoomController`.
- **Unity 6 Features**:
  - Sử dụng Unity New Input System (`UnityEngine.InputSystem`).
  - Sử dụng `Awaitable` hoặc Coroutine an toàn vòng đời (hủy khi object bị disable/destroy).
  - Tận dụng URP 2D Renderers & Sprite Unlit Shader cho hiệu năng tối đa.

---

## IV. LỘ TRÌNH 7 GIAI ĐOẠN HỌC & LÀM (ROADMAP CHI TIẾT)

- **Giai đoạn 1: Nền tảng Core, Input & Điều khiển Mobile**
  - Cấu trúc thư mục `_Project`.
  - `Bootstrapper`: Thiết lập Mobile (60 FPS, SleepTimeout, Safe Area).
  - New Input System + Virtual Joystick cho Mobile (hỗ trợ cả Keyboard test trên Editor).
  - `PlayerController` mượt mà (Rigidbody2D, State di chuyển, Flip sprite theo hướng).

- **Giai đoạn 2: Hệ thống Auto-Aim & Framework Object Pooling Tối Ưu**
  - Xây dựng hệ thống Object Pooling hiệu năng cao, linh hoạt.
  - Cơ chế Auto-Aim (quét mục tiêu gần nhất bằng `Physics2D.OverlapCircleNonAlloc`).
  - Vũ khí cơ bản đầu tiên: Tự động bắn đạn bay thẳng, xử lý vòng đời của đạn qua Pool.

- **Giai đoạn 3: Swarm Enemy AI & Hệ thống Wave Spawner**
  - Quái vật cơ bản bám đuổi người chơi, tối ưu vật lý cho số lượng lớn.
  - Spawner sinh quái theo nhịp hoặc theo wave phòng hầm ngục.
  - Cơ chế Pool quái để không lag khi spawn hàng chục con cùng lúc.

- **Giai đoạn 4: Hệ thống Combat, Game Feel & Rớt Ngọc Exp**
  - `HealthSystem` & `DamageSystem` tách rời, tái sử dụng cho cả Player lẫn Enemy.
  - Cảm giác đánh (Game Feel): Hit-stop, Flash White, Knockback, Floating Damage Text.
  - Rớt ngọc kinh nghiệm, cơ chế nam châm hút ngọc (Magnet/Lerp).

- **Giai đoạn 5: Hệ thống Lên Cấp (Level Up) & Thẻ Nâng Cấp (Upgrade Cards)**
  - Tích lũy Exp -> Tăng cấp.
  - Tạm dừng thời gian game (Pause via Game State Machine).
  - Bốc ngẫu nhiên 3 thẻ bài nâng cấp từ `ScriptableObject UpgradeData`.
  - Áp dụng hiệu ứng nâng cấp (tăng tốc độ bắn, tăng đạn, tăng sát thương).

- **Giai đoạn 6: Cơ chế Phòng Hầm Ngục (Soul Knight Room Mechanics)**
  - `RoomController`: Trạng thái phòng (Chưa kích hoạt -> Đang chiến đấu -> Đã dọn sạch).
  - Hệ thống cửa phòng (Door System): Khóa cửa khi chiến đấu, mở cửa khi hết quái.
  - Camera Transition: Di chuyển camera mượt mà sang phòng tiếp theo (Cinemachine Confiner).
  - Rương thưởng / Cổng dịch chuyển.

- **Giai đoạn 7: UI/UX, Audio Pool, Meta-Progression & Build Hoàn Thiện**
  - Hệ thống âm thanh `AudioManager` tối ưu pool.
  - Sảnh (Lobby) & Hệ thống Nâng cấp vĩnh viễn (Save/Load JSON / PlayerPrefs).
  - Tối ưu hóa Build Android APK (Profiler, Draw Calls, Compression).

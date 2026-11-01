# Project_D - Top-Down Roguelike Action

Dự án game Top-Down Roguelike 2D kết hợp cơ chế Survivor, xây dựng trên nền tảng **Unity 6 (Universal Render Pipeline)**.

---

## 📁 Cấu Trúc Dự Án (Project Structure)

```text
D:\Project_D
├── Assets/
│   ├── Scenes/               # Chứa Scene chính (SampleScene.unity)
│   ├── Settings/             # Thiết lập URP, Input System
│   ├── TextMesh Pro/         # Tài nguyên TextMeshPro, Fonts
│   └── _Project/             # Thư mục mã nguồn và tài nguyên chính của Game
│       ├── Art/              # Sprites nhân vật, quái, lính, môi trường
│       ├── Audio/            # Âm thanh hiệu ứng, nhạc nền
│       ├── Fonts/            # Phông chữ Việt hóa & SDF fonts
│       ├── Physics/          # Thiết lập vật lý 2D, Physics Materials
│       ├── Prefabs/          # Prefabs (Dungeon, Enemies, Minions, Projectiles, UI...)
│       ├── Resources/        # Tài nguyên load động tại Runtime (Database...)
│       ├── ScriptableObjects/# Dữ liệu cấu hình (Player, Enemies, Minions, Dungeon, Upgrades)
│       └── Scripts/          # Toàn bộ mã nguồn C# logic game
│           ├── Audio/        # Quản lý âm thanh
│           ├── Combat/       # Vũ khí, đạn đạo, ngọc kinh nghiệm, tương tác chiến đấu
│           ├── Core/         # PoolManager, MobileInitializer, Game Loop
│           ├── Dungeon/      # Hệ thống Roguelike Run Map 13 tầng, phòng, cửa ải
│           ├── Editor/       # Công cụ Editor Wizard, Node Graph Designer
│           ├── Enemies/      # AI quái vật, FlowField pathfinding, Bosses
│           ├── Minions/      # Hệ thống lính triệu hồi, hợp nhất (Merge)
│           ├── Player/       # Điều khiển, máu, chỉ số người chơi (PlayerDataSO)
│           ├── UI/           # Giao diện HUD, Shop, RestSite, Thẻ nâng cấp
│           └── Upgrades/     # Thẻ bài nâng cấp kỹ năng
├── Docs/                     # Tài liệu kỹ thuật & Hướng dẫn hệ thống
└── ProjectSettings/          # Cấu hình dự án Unity
```

---

## 📚 Tài Liệu Kỹ Thuật (Documentation)

Các tài liệu chi tiết được lưu trữ trong thư mục [`Docs/`](Docs/):
- [Hệ Thống Bản Đồ & Run 13 Tầng (Dungeon Run Documentation)](Docs/DUNGEON_RUN_SYSTEM_DOCUMENTATION.md)
- [Cơ Chế AI Quái Vật & FlowField Pathfinding](Docs/FLOWFIELD_AND_ENEMY_GUIDE.md)
- [Hướng Dẫn Thiết Lập Panel Setting & Minion Stats](Docs/HUONG_DAN_THIET_LAP_PANEL_SETTING.md)
- [Quy Chuẩn Thiết Kế Dự Án (Project Rules)](Docs/PROJECT_RULES.md)
- [Tổng Kết & Hướng Dẫn Thiết Lập Scene](Docs/TONG_KET_VA_HUONG_DAN_THIET_LAP_SCENE.md)
- [Hướng Dẫn Thiết Lập Unity Editor](Docs/UNITY_EDITOR_SETUP_GUIDE.md)

# Hướng Dẫn Thiết Lập Panel Cài Đặt (Chuẩn Layout Mobile Roguelite)

Tài liệu này hướng dẫn chi tiết cách thiết lập, cấu hình và vận hành **Settings Panel (Bảng Cài Đặt, Bản Đồ Graph & Tỷ Lệ Sát Thương)** trong **Project_D** theo đúng chuẩn layout của game roguelite:

```
┌────────────────────────────────────────────────────────┐
│ [PHẦN 1: MINI MAP GRAPH - CÂY HẦM NGỤC]                │
│  TẦNG 1 / 13                                           │
│                 [P]                                    │
│                [ 1 ] (Phòng Hiện Tại - Viền Vàng)      │
│                  │                                     │
│                [ 2 ]                                   │
│               /     \                                  │
│            [ 3 ]   [ 4 ]                               │
│              │    /  │                                 │
│            [ 5 ] [ 6 ] [ 7 ]                           │
│                   ...                                  │
│                 [BOSS] (Tầng 13 - Viền Đỏ)             │
├────────────────────────────────────────────────────────┤
│ [PHẦN 2: OPTION MODAL - TÙY CHỌN]                  [X] │
│                    OPTION                              │
│                   Tùy chọn                             │
│                                                        │
│  BGM [========----] 80%    Hiệu ứng âm thanh [===] 90% │
│  Font Sát Thương   [v] ON  Đồ Họa Cao        [v] ON    │
│  Rung Màn Hình     [v] ON                              │
│                                                        │
│        [ Rút Lui ]          [ Tiếp Tục Chiến Đấu ]     │
├────────────────────────────────────────────────────────┤
│ [PHẦN 3: SÁT THƯƠNG GÂY CHO QUÁI VẬT]                  │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐                 │
│  │ [Hero]  │  │ [Minion]│  │ [Minion]│                 │
│  │  Icon   │  │ x2 Icon │  │ x1 Icon │                 │
│  │  52.4%  │  │  32.1%  │  │  15.5%  │                 │
│  └─────────┘  └─────────┘  └─────────┘                 │
│  (Bấm vào từng ô để xem chi tiết DPS, Sát thương, v.v.)│
└────────────────────────────────────────────────────────┘
```

---

## 1. Cách Tạo Nhanh 1-Click (Khuyên Dùng)

Dự án đã tích hợp công cụ tự động tạo chuẩn 100% trong Unity Editor:
1. Mở Unity Editor.
2. Trên thanh menu, chọn: **`Project_D` > `UI` > `Tạo Settings Panel Tự Động (In Scene)`**.
3. **Kết quả:**
   - Toàn bộ Canvas Hierarchy mới sẽ được khởi tạo ngay trong Scene.
   - Bản Prefab đã được cập nhật sẵn tại: [`Assets/_Project/Prefabs/UI/SettingsPanel.prefab`](file:///d:/Project_D/Assets/_Project/Prefabs/UI/SettingsPanel.prefab).
   - Tự động liên kết toàn bộ tham chiếu trên component [`SettingsPanelUI`](file:///d:/Project_D/Assets/_Project/Scripts/UI/Settings/SettingsPanelUI.cs).

---

## 2. Chi Tiết Cấu Trúc 3 Phân Khu

### Phân Khu 1: Mini Map Graph (Cây Liên Kết Hầm Ngục)
- **Tự động sinh Graph từ `RunMapConfigSO`:** Đọc dữ liệu từ [`RunMap_13Floors.asset`](file:///d:/Project_D/Assets/_Project/ScriptableObjects/Dungeon/RunMap_13Floors.asset).
- **Các Node Phòng (Square Tiles 50x50):**
  - **Phòng Hiện Tại:** Nổi bật với viền vàng sáng (`#FFD54F`), có đính kèm **Player Pin `[P]`** phía trên đỉnh.
  - **Phòng Tiếp Theo (Available):** Viền sáng cyan (`#00E5FF`).
  - **Phòng Đã Qua:** Màu xám tối, ký hiệu chữ `V`.
  - **Phòng Boss / MiniBoss:** Viền đỏ thẫm kèm nhãn `BOSS` / `MINI`.
  - **Phòng Tinh Anh / Shop / Rest / Buff:** Ký hiệu trực quan tương ứng (`ELITE`, `$`, `+`, `BUFF`).
- **Đường Nối (Connectors):**
  - Tự động vẽ các đoạn thẳng nối giữa từng Node và danh sách `NextNodeIds` của nó.
  - Đường vàng sáng biểu thị tuyến đường hiện tại, đường xám biểu thị các nhánh tương lai.
- **Tự Động Cuộn Khung Nhìn (Auto-Center):** Tự động cuộn `ScrollRect` đến vị trí phòng mà người chơi đang đứng.

### Phân Khu 2: Option Modal (Tùy Chọn)
- **Header:** Chữ `OPTION` cách điệu kèm huy hiệu con `Tùy chọn`.
- **Âm thanh:**
  - Slider `BGM` (Nhạc nền) hiển thị %.
  - Slider `SFX` (Hiệu ứng âm thanh) hiển thị %.
- **Hiển thị & Đồ họa:**
  - Toggle `Font Sát Thương` (Bật/tắt số sát thương bay trên đầu quái).
  - Toggle `Đồ Họa Cao` (Bật/tắt chất lượng VFX).
  - Toggle `Rung Màn Hình` (Bật/tắt rung giật camera).
- **Hành động:**
  - Nút **`Rút Lui`** (Bỏ cuộc / Thoát lượt chơi).
  - Nút **`Tiếp Tục Chiến Đấu`** (Đóng bảng và resume game).

### Phân Khu 3: Sát Thương Gây Cho Quái Vật (Damage Breakdown Bar)
- **Thanh cuộn ngang chứa các ô vuông chân dung:**
  - **Ô 1: Anh Hùng (Player Hero):**
    - Viền vàng kim nổi bật, hiển thị chân dung người chơi.
    - Phía dưới: Tỷ lệ % đóng góp sát thương tổng (vd: `52.4%`).
  - **Ô 2..N: Từng loại lính (Minions Roster):**
    - Viền theo Tier màu lính (Tier 1: Bạc, Tier 2: Lục, Tier 3: Tím).
    - Hiển thị Sprite lính (`sprSkeleton3_2`, `sprOrcArcher3_0`...).
    - Huy hiệu số lượng ở góc trên (vd: `x2`, `x1`).
    - Phía dưới: Tỷ lệ % đóng góp sát thương của toàn nhóm lính đó.
- **Tính Năng Xem Chi Tiết Khi Nhấn (Interactive Tooltip):**
  - Bấm vào bất kỳ ô nào (Người chơi hoặc Lính) sẽ mở khung thông tin phụ hiển thị đầy đủ:
    - Sát thương mỗi đòn đánh.
    - Tốc độ bắn / Tốc độ đánh.
    - Kiểu đánh (Cận chiến, Bắn tỉa, Sát thương lan).
    - Chỉ số DPS thực tế.

---

## 3. Cách Mở Bảng Cài Đặt (2 Cách Tiện Lợi)

1. **Bấm Nút Menu trên Giao Diện (In-game HUD):**
   - Đã tạo sẵn nút **`Btn_Settings`** (Biểu tượng Menu 3 thanh ngang `≡` chuẩn như hình mẫu) nằm ở **góc trên cùng bên phải màn hình** (ngay cạnh số Linh Hồn).
   - Nút đã được kết nối tự động với `SettingsPanelUI.Toggle()`. Bấm vào nút này để mở hoặc đóng bảng bất kỳ lúc nào trên cả PC và Mobile.

2. **Bấm Phím Tắt Trên Bàn Phím (PC):**
   - Bấm phím **`Escape` (Esc)** trên bàn phím để bật/tắt nhanh.
   - Hỗ trợ chuẩn **New Input System Package** (`Keyboard.current.escapeKey.wasPressedThisFrame`), không còn lỗi xung đột `InvalidOperationException`.

---

## 4. Quản Lý Tạm Dừng Game An Toàn

---

## 4. Tích Hợp Sự Kiện Lập Trình (Observer Pattern)

```csharp
using UnityEngine;
using Project_D.UI.Settings;

public class GameSettingsListener : MonoBehaviour
{
    private void OnEnable()
    {
        SettingsPanelUI.OnBGMVolumeChanged += HandleBgmChanged;
        SettingsPanelUI.OnSFXVolumeChanged += HandleSfxChanged;
        SettingsPanelUI.OnDamageNumbersToggled += HandleDamageNumbersToggled;
        SettingsPanelUI.OnHighQualityToggled += HandleHighQualityToggled;
        SettingsPanelUI.OnScreenShakeToggled += HandleScreenShakeToggled;
        SettingsPanelUI.OnAbandonRunRequested += HandleAbandonRun;
    }

    private void OnDisable()
    {
        SettingsPanelUI.OnBGMVolumeChanged -= HandleBgmChanged;
        SettingsPanelUI.OnSFXVolumeChanged -= HandleSfxChanged;
        SettingsPanelUI.OnDamageNumbersToggled -= HandleDamageNumbersToggled;
        SettingsPanelUI.OnHighQualityToggled -= HandleHighQualityToggled;
        SettingsPanelUI.OnScreenShakeToggled -= HandleScreenShakeToggled;
        SettingsPanelUI.OnAbandonRunRequested -= HandleAbandonRun;
    }

    private void HandleBgmChanged(float val) => Debug.Log($"BGM: {val * 100}%");
    private void HandleSfxChanged(float val) => Debug.Log($"SFX: {val * 100}%");
    private void HandleDamageNumbersToggled(bool on) => Debug.Log($"Damage numbers: {on}");
    private void HandleHighQualityToggled(bool on) => Debug.Log($"High quality: {on}");
    private void HandleScreenShakeToggled(bool on) => Debug.Log($"Screen shake: {on}");
    private void HandleAbandonRun() => Debug.Log("Player surrendered!");
}
```

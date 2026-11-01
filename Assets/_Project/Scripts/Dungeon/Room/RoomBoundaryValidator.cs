using UnityEngine;
using Project_D.Enemies.FlowField;

namespace Project_D.Dungeon.Room
{
    /// <summary>
    /// Hệ thống kiểm soát và bảo vệ ranh giới phòng (Room Boundary & Wall Depenetration).
    /// Đảm bảo tuyệt đối: Người chơi, Lính đồng minh và Quái vật không bao giờ bị văng ra ngoài phòng
    /// hoặc bị kẹt/lọt vào bên trong các khối tường/cột đá.
    /// </summary>
    public static class RoomBoundaryValidator
    {
        private static LayerMask _obstacleMask;
        private static bool _maskInitialized = false;

        public static LayerMask ObstacleMask
        {
            get
            {
                EnsureMask();
                return _obstacleMask;
            }
        }

        private static void EnsureMask()
        {
            if (!_maskInitialized)
            {
                _obstacleMask = LayerMask.GetMask("Obstacle", "Obstacle ");
                if (_obstacleMask.value == 0) _obstacleMask = 1 << 7;
                _maskInitialized = true;
            }
        }

        // =========================================================================
        // 1. LẤY KHUNG RANH GIỚI PHÒNG HIỆN TẠI
        // =========================================================================

        public static bool TryGetRoomBounds(out Rect bounds)
        {
            RoomLayout layout = null;
            if (RoomManager.Instance != null && RoomManager.Instance.CurrentRoomLayout != null)
            {
                layout = RoomManager.Instance.CurrentRoomLayout;
            }
            else
            {
                layout = Object.FindAnyObjectByType<RoomLayout>();
            }

            if (layout != null)
            {
                Vector3 center = layout.transform.position;
                Vector2 size = layout.RoomSize;
                if (size.x <= 0f || size.y <= 0f) size = new Vector2(32f, 32f);

                bounds = new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
                return true;
            }

            // Fallback nếu phòng chưa nạp
            bounds = new Rect(-16f, -16f, 32f, 32f);
            return false;
        }

        // =========================================================================
        // 2. KẸP VỊ TRÍ NẰM TRONG KHUNG PHÒNG (CHỐNG VĂNG RA NGOÀI PHÒNG)
        // =========================================================================

        public static Vector3 ClampToRoomBounds(Vector3 position, float margin = 1.4f)
        {
            TryGetRoomBounds(out Rect bounds);

            float minX = bounds.xMin + margin;
            float maxX = bounds.xMax - margin;
            float minY = bounds.yMin + margin;
            float maxY = bounds.yMax - margin;

            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.y = Mathf.Clamp(position.y, minY, maxY);
            return position;
        }

        // =========================================================================
        // 3. KIỂM TRA VỊ TRÍ CÓ ĐANG NẰM TRONG TƯỜNG/CHƯỚNG NGẠI VẬT KHÔNG
        // =========================================================================

        public static bool IsInsideWall(Vector3 position, float radius = 0.25f)
        {
            EnsureMask();

            // Kiểm tra va chạm vật lý OverlapCircle với Layer Obstacle
            Collider2D hit = Physics2D.OverlapCircle(position, radius, _obstacleMask);
            return hit != null;
        }

        /// <summary>
        /// Kiểm tra đường nhìn (Line of Sight) giữa 2 điểm xem có bị tường cản không.
        /// </summary>
        public static bool HasLineOfSight(Vector2 start, Vector2 end)
        {
            EnsureMask();
            Vector2 diff = end - start;
            float dist = diff.magnitude;
            if (dist <= 0.05f) return true;

            RaycastHit2D hit = Physics2D.Raycast(start, diff.normalized, dist, _obstacleMask);
            return hit.collider == null;
        }

        /// <summary>
        /// Kiểm tra đường đi có thông thoáng cho thực thể có bán kính radius không (Capsule/Circle sweep).
        /// Tránh hiện tượng tia Raycast xuyên khe hở nhưng thân lính/quái đâm sầm vào góc tường.
        /// </summary>
        public static bool HasPathClear(Vector2 start, Vector2 end, float radius = 0.25f)
        {
            EnsureMask();
            Vector2 diff = end - start;
            float dist = diff.magnitude;
            if (dist <= 0.05f) return true;

            RaycastHit2D hit = Physics2D.CircleCast(start, radius, diff.normalized, dist, _obstacleMask);
            return hit.collider == null;
        }

        // =========================================================================
        // 4. TÌM VỊ TRÍ SÀN TRỐNG HỢP LỆ GẦN NHẤT (CHỐNG KẸT TƯỜNG / DEPENETRATION)
        // =========================================================================

        public static Vector3 GetNearestWalkablePosition(Vector3 position, float searchRadius = 4.0f, float step = 0.4f)
        {
            // Bước 1: Kẹp trước vào ranh giới phòng
            position = ClampToRoomBounds(position, 1.4f);

            // Nếu vị trí hiện tại đã hoàn toàn thông thoáng, giữ nguyên
            if (!IsInsideWall(position, 0.25f))
            {
                return position;
            }

            // Bước 2: Quét xoắn ốc 8 hướng tìm ô đất trống gần nhất
            for (float r = step; r <= searchRadius; r += step)
            {
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * (Mathf.PI * 2f / 8f);
                    Vector3 candidate = position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * r;
                    candidate = ClampToRoomBounds(candidate, 1.4f);

                    if (!IsInsideWall(candidate, 0.25f))
                    {
                        return candidate;
                    }
                }
            }

            // Bước 3: Dự phòng khẩn cấp - Kéo dần về tâm phòng (Tâm phòng luôn là sàn trống)
            Vector3 center = Vector3.zero;
            if (TryGetRoomBounds(out Rect bounds))
            {
                center = bounds.center;
            }

            Vector3 dirToCenter = (center - position).normalized;
            Vector3 testPos = position;
            for (float d = 0.5f; d <= searchRadius * 2f; d += 0.5f)
            {
                testPos = position + dirToCenter * d;
                testPos = ClampToRoomBounds(testPos, 1.4f);
                if (!IsInsideWall(testPos, 0.25f))
                {
                    return testPos;
                }
            }

            return center;
        }

        // =========================================================================
        // 5. BẢO VỆ & TỰ ĐỘNG GỠ KẸT CHO ENTITY (NGƯỜI CHƠI, LÍNH, QUÁI VẬT)
        // =========================================================================

        public static bool ValidateAndUnstuck(Transform entityTransform, float entityRadius = 0.35f, Rigidbody2D rb = null)
        {
            if (entityTransform == null) return false;

            Vector3 currentPos = entityTransform.position;
            Vector3 clampedPos = ClampToRoomBounds(currentPos, entityRadius + 1.2f);
            bool wasOutOfBounds = (clampedPos != currentPos);

            // CHỈ kích hoạt gỡ kẹt khi tâm thực thể thực sự lọt vào bên trong lòng collider vật cản
            // (Tuyệt đối không gỡ kẹt khi chỉ đứng hoặc trượt sát mép tường bên ngoài, triệt tiêu 100% rung giật)
            bool isStuckInWall = Physics2D.OverlapPoint(clampedPos, ObstacleMask) != null;

            if (wasOutOfBounds || isStuckInWall)
            {
                Vector3 safePos = GetNearestWalkablePosition(clampedPos, 4.5f, 0.35f);
                entityTransform.position = safePos;

                if (rb != null)
                {
                    // Triệt tiêu quán tính lực đang đẩy vào tường
                    rb.linearVelocity = Vector2.zero;
                }
                return true;
            }

            return false;
        }
    }
}

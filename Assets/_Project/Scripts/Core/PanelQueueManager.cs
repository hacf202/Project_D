using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project_D.Core
{
    /// <summary>
    /// Mức độ ưu tiên của các Panel giao diện
    /// </summary>
    public enum PanelPriority
    {
        Normal = 0,             // Panel thông thường (Sổ công thức, Cài đặt...)
        QueuedModal = 10,       // Modal hiển thị tuần tự (Thẻ nâng cấp, Rương, Thưởng...)
        CriticalOverride = 100  // Ghi đè tối cao (Game Over, Victory)
    }

    /// <summary>
    /// Đơn vị yêu cầu Modal trong Hàng đợi
    /// </summary>
    public class QueuedModalItem
    {
        public string ModalId;
        public PanelPriority Priority;
        public Action ShowAction;
        public Action OnClosedCallback;
    }

    /// <summary>
    /// Hệ thống Quản lý Hàng đợi Panel & Điểm Điều Khiển Pause Tập Trung (Single Source of Truth)
    /// - Ngăn chặn xung đột Time.timeScale giữa nhiều Panel độc lập.
    /// - Quản lý hàng đợi hiển thị tuần tự cho các Panel nâng cấp/phần thưởng.
    /// - Đảm bảo Time.timeScale luôn an toàn, không bị kẹt 0 khi khởi động game.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class PanelQueueManager : MonoBehaviour
    {
        private static PanelQueueManager _instance;
        private static bool _isApplicationQuitting = false;

        public static bool HasInstance => _instance != null && !_isApplicationQuitting;

        public static PanelQueueManager Instance
        {
            get
            {
                if (_isApplicationQuitting)
                {
                    return null;
                }

                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<PanelQueueManager>(FindObjectsInactive.Include);
                    if (_instance == null && Application.isPlaying)
                    {
                        var go = new GameObject("[PanelQueueManager]");
                        _instance = go.AddComponent<PanelQueueManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        // Sự kiện thông báo trạng thái tạm dừng của toàn game
        public static event Action OnGamePaused;
        public static event Action OnGameResumed;

        // Tập hợp các đối tượng đang yêu cầu dừng game (Pause Reference Counter)
        private readonly HashSet<object> _pauseRequesters = new HashSet<object>();

        // Hàng đợi các Modal cần mở tuần tự
        private readonly List<QueuedModalItem> _modalQueue = new List<QueuedModalItem>();
        private QueuedModalItem _currentActiveModal;

        public bool IsPaused => _pauseRequesters.Count > 0;
        public int ActivePauseRequesterCount => _pauseRequesters.Count;
        public int QueuedModalCount => _modalQueue.Count;

        // Đảm bảo Time.timeScale = 1 ngay trước khi nạp scene đầu tiên
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInitialTimeScale()
        {
            Time.timeScale = 1f;
            _isApplicationQuitting = false;
            _instance = null;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnApplicationQuit()
        {
            _isApplicationQuitting = true;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (_instance == this)
            {
                _instance = null;
                _isApplicationQuitting = true;
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Tự động giải phóng hàng đợi và khôi phục nhịp độ game khi đổi màn
            ForceResumeAll("SceneLoaded: " + scene.name);
        }

        // =========================================================================
        // 1. QUẢN LÝ PAUSE / RESUME ĐỘC QUYỀN (REFERENCE COUNTING)
        // =========================================================================

        /// <summary>
        /// Yêu cầu tạm dừng game từ một Panel hoặc hệ thống cụ thể.
        /// </summary>
        public void RequestPause(object requester, string reason = "")
        {
            if (requester == null) return;

            bool wasPausedBefore = _pauseRequesters.Count > 0;
            _pauseRequesters.Add(requester);

            if (!wasPausedBefore)
            {
                Time.timeScale = 0f;
                Debug.Log($"<color=#FFD700>[PanelQueueManager]</color> Game PAUSED bởi: <b>{requester.GetType().Name}</b> (Lý do: {reason})");
                OnGamePaused?.Invoke();
            }
        }

        /// <summary>
        /// Giải phóng yêu cầu tạm dừng khi một Panel đóng lại.
        /// Chỉ khi TẤT CẢ các Panel yêu cầu pause đều đóng thì game mới tiếp tục chạy.
        /// </summary>
        public void ReleasePause(object requester)
        {
            if (requester == null) return;

            if (_pauseRequesters.Remove(requester))
            {
                Debug.Log($"<color=#A0A0A0>[PanelQueueManager]</color> Giải phóng Pause từ: <b>{requester.GetType().Name}</b>. Còn lại: {_pauseRequesters.Count}");
            }

            // Nếu không còn đối tượng nào yêu cầu pause, kiểm tra hàng đợi modal tiếp theo
            if (_pauseRequesters.Count == 0)
            {
                if (_modalQueue.Count > 0)
                {
                    // Lấy modal kế tiếp trong hàng đợi ra hiển thị
                    ProcessNextQueuedModal();
                }
                else
                {
                    // Hàng đợi đã rỗng hoàn toàn -> Khôi phục thời gian game
                    Time.timeScale = 1f;
                    Debug.Log("<color=#00FF66>[PanelQueueManager]</color> Không còn Panel nào giữ Pause -> Game RESUMED (TimeScale = 1.0)");
                    OnGameResumed?.Invoke();
                }
            }
        }

        /// <summary>
        /// Yêu cầu dừng game khẩn cấp có quyền ghi đè (Game Over, Victory).
        /// Tự động dọn sạch các modal đang chờ để không bị bật đè lên màn kết thúc.
        /// </summary>
        public void RequestCriticalPause(object requester, string reason = "")
        {
            _modalQueue.Clear();
            _currentActiveModal = null;

            RequestPause(requester, "CRITICAL: " + reason);
        }

        /// <summary>
        /// Ép buộc khôi phục toàn bộ thời gian và làm sạch hàng đợi.
        /// </summary>
        public void ForceResumeAll(string reason = "")
        {
            _pauseRequesters.Clear();
            _modalQueue.Clear();
            _currentActiveModal = null;

            Time.timeScale = 1f;
            Debug.Log($"<color=#00FF66>[PanelQueueManager]</color> ForceResumeAll (Lý do: {reason}) -> TimeScale = 1.0");
            OnGameResumed?.Invoke();
        }

        // =========================================================================
        // 2. QUẢN LÝ HÀNG ĐỢI MODAL TUẦN TỰ (FIFO MODAL QUEUE)
        // =========================================================================

        /// <summary>
        /// Đưa một Modal vào hàng đợi. Nếu hiện tại chưa có modal nào mở, modal sẽ hiển thị ngay lập tức.
        /// </summary>
        public void EnqueueModal(string modalId, Action showAction, Action onClosedCallback = null, PanelPriority priority = PanelPriority.QueuedModal)
        {
            if (showAction == null) return;

            var item = new QueuedModalItem
            {
                ModalId = modalId,
                Priority = priority,
                ShowAction = showAction,
                OnClosedCallback = onClosedCallback
            };

            // Nếu hiện tại không có modal nào đang mở và không có panel nào giữ pause
            if (_currentActiveModal == null && _pauseRequesters.Count == 0)
            {
                ActivateModal(item);
            }
            else
            {
                // Thêm vào hàng đợi theo thứ tự ưu tiên
                _modalQueue.Add(item);
                _modalQueue.Sort((a, b) => ((int)b.Priority).CompareTo((int)a.Priority));
                Debug.Log($"<color=#00E5FF>[PanelQueueManager]</color> Đã xếp Modal <b>'{modalId}'</b> vào hàng đợi. Vị trí: {_modalQueue.Count}");
            }
        }

        private void ActivateModal(QueuedModalItem item)
        {
            _currentActiveModal = item;
            RequestPause(item, "ActiveModal: " + item.ModalId);

            Debug.Log($"<color=#FFD700>[PanelQueueManager]</color> Kích hoạt hiển thị Modal: <b>'{item.ModalId}'</b>");
            try
            {
                item.ShowAction?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PanelQueueManager] Lỗi khi mở modal '{item.ModalId}': {ex}");
                NotifyModalClosed(item.ModalId);
            }
        }

        /// <summary>
        /// Được gọi bởi Modal khi người chơi hoàn tất thao tác và đóng bảng.
        /// </summary>
        public void NotifyModalClosed(string modalId)
        {
            if (_currentActiveModal != null && _currentActiveModal.ModalId == modalId)
            {
                var closed = _currentActiveModal;
                _currentActiveModal = null;

                try
                {
                    closed.OnClosedCallback?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PanelQueueManager] Lỗi trong OnClosedCallback của modal '{modalId}': {ex}");
                }

                // Giải phóng pause của modal này (hàm này sẽ tự kích hoạt modal tiếp theo nếu có)
                ReleasePause(closed);
            }
            else
            {
                // Tìm kiếm xóa nếu modal bị hủy khi còn trong hàng đợi
                _modalQueue.RemoveAll(m => m.ModalId == modalId);
            }
        }

        private void ProcessNextQueuedModal()
        {
            if (_modalQueue.Count > 0)
            {
                var next = _modalQueue[0];
                _modalQueue.RemoveAt(0);
                ActivateModal(next);
            }
        }
    }
}

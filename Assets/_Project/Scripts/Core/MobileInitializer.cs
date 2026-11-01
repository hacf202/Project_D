using UnityEngine;

namespace Project_D.Core
{
    /// <summary>
    /// Tối ưu hóa Unity cho Mobile & Quản lý môi trường khởi chạy
    /// </summary>
    public static class MobileInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitialierMobileSettings()
        {
            // FPS = 60
            Application.targetFrameRate = 60;
           
            // Chống tắt màn hình
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Tắt VSync
            QualitySettings.vSyncCount = 0;

            // Đảm bảo Time.timeScale luôn bằng 1 khi bắt đầu trò chơi (tránh lỗi đóng băng từ Editor)
            Time.timeScale = 1f;

            Debug.Log("<color=green>[MobileInitializer]</color> Đã thiết lập Target FPS: 60, SleepTimeout: NeverSleep, TimeScale: 1.0.");
        }
    }
}

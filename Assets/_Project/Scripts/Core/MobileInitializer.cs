using UnityEngine;

namespace Project_D.Core
{

    /// <summary>
    /// Tối ưu hóa Unity cho Mobile
    /// </summary>
    public static class MobileInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitialierMobileSettings()
        {
            //Fps = 60
            Application.targetFrameRate = 60;
           
           //chong tat man hinh
           Screen.sleepTimeout = SleepTimeout.NeverSleep;

           //tat VSync
           QualitySettings.vSyncCount = 0;

            Debug.Log("<color=green>[MobileInitializer]</color> Đã thiết lập Target FPS: 60 & SleepTimeout: NeverSleep.");
        }
    }

    
}
using System;
using UnityEngine;

namespace WarmBread
{
    public static class ReleaseSafety
    {
        public static bool IsSafeMode { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyCommandLineOptions()
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length; i++)
            {
                if (!string.Equals(args[i], "--safe-mode", StringComparison.OrdinalIgnoreCase)) continue;
                ApplySafeMode();
                return;
            }
        }

        private static void ApplySafeMode()
        {
            IsSafeMode = true;
            QualitySettings.antiAliasing = 0;
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowDistance = 20f;
            QualitySettings.lodBias = 0.7f;
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            Debug.LogWarning("[WarmBread] Безопасный режим включён: упрощена графика и выбран оконный режим 1280x720.");
        }
    }
}

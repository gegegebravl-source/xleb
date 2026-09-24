using UnityEngine;
using UnityEngine.SceneManagement;

namespace WarmBread
{
    public sealed class RuntimeSettingsApplier : MonoBehaviour
    {
        private GameSettings settings;

        public void Initialize(GameSettings initial)
        {
            settings = initial ?? new GameSettings();
            Apply(settings);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public void Apply(GameSettings value)
        {
            settings = value ?? new GameSettings();
            settings.Clamp();
            AudioListener.volume = settings.MasterVolume;
            if (ReleaseSafety.IsSafeMode)
            {
                QualitySettings.antiAliasing = 0;
                QualitySettings.shadows = ShadowQuality.Disable;
                QualitySettings.shadowDistance = 20f;
                QualitySettings.lodBias = 0.7f;
                Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
                ApplyCameras();
                return;
            }
            QualitySettings.antiAliasing = settings.RenderScale < 0.8f ? 2 : 4;
            QualitySettings.shadowResolution = settings.ShadowQuality >= 3 ? ShadowResolution.VeryHigh : settings.ShadowQuality >= 2 ? ShadowResolution.High : ShadowResolution.Medium;
            QualitySettings.shadowDistance = settings.ShadowQuality <= 0 ? 25f : settings.ShadowQuality == 1 ? 45f : 80f;
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            ApplyCameras();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { ApplyCameras(); }

        private void ApplyCameras()
        {
            var cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < cameras.Length; i++) cameras[i].fieldOfView = settings.FieldOfView;
        }

        private void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }
    }
}

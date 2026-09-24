using CHARK.GameManagement;
using CHARK.GameManagement.Systems;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UABPetelnia.GGJ2025.Runtime.Systems.Scenes;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Settings
{
    internal sealed class SettingsSystem : MonoSystem, ISettingsSystem, IUpdateListener
    {
        private const string SettingsPath = "Settings.json";

        /// <summary>Текущая версия формата настроек: для миграции старых файлов.</summary>
        private const int CurrentSettingsVersion = 3;

        [SerializeField]
        private GeneralSettings generalSettings;

        private int lastCameraCount;

        /// <summary>Состав камер сменился (загрузка сцены): постэффекты надо навесить заново.</summary>
        private bool camerasDirty;

        public SettingsData Settings { get; set; }

        private void Awake()
        {
            // После загрузки сцены старые камеры умирают, новые появляются позже.
            // Детект «по количеству камер» может пропустить замену (1 → 1),
            // поэтому помечаем камеры грязными по событию загрузки.
            GameManager.AddListener<SceneLoadEnteredMessage>(OnSceneLoadEntered);
        }

        private void OnDestroy()
        {
            GameManager.RemoveListener<SceneLoadEnteredMessage>(OnSceneLoadEntered);
        }

        private void OnSceneLoadEntered(SceneLoadEnteredMessage message)
        {
            camerasDirty = true;
        }

        public override void OnInitialized()
        {
            ReadSettings();
            ApplyGraphics();
        }

        public override void OnDisposed()
        {
            WriteSettings();
        }

        public void OnUpdated(float deltaTime)
        {
            if (camerasDirty == false && Camera.allCamerasCount == lastCameraCount)
            {
                return;
            }

            camerasDirty = false;
            GraphicsSettingsApplier.ApplyPostProcessing(Settings.PostProcessingEnabled);
            lastCameraCount = Camera.allCamerasCount;
        }

        /// <summary>Сохранить настройки по кнопке, не дожидаясь выхода из игры.</summary>
        public void Save()
        {
            WriteSettings();
        }

        /// <summary>Применить графические/экранные настройки прямо сейчас.</summary>
        public void ApplyGraphics()
        {
            GraphicsSettingsApplier.Apply(Settings);
            lastCameraCount = Camera.allCamerasCount;
        }

        private void DeleteSettings()
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                Debug.LogWarning("Can only delete during playmode", this);
                return;
            }
#endif

            GameManager.DeleteData(SettingsPath);
            Settings = CreateDefaultSettings();
        }

        private void ReadSettings()
        {
            if (GameManager.TryReadData<SettingsData>(SettingsPath, out var data))
            {
                Settings = Migrate(data);
                return;
            }

            Settings = CreateDefaultSettings();
        }

        /// <summary>
        /// Старые файлы (до вкладок графики) не содержат новых полей: JSON даст нули/ложь —
        /// это вырубленная графика. Доливаем значения пресета «Высокая» и повышаем версию.
        /// </summary>
        private SettingsData Migrate(SettingsData data)
        {
            if (data.SettingsVersion >= CurrentSettingsVersion)
            {
                return data;
            }

            var defaults = CreateDefaultSettings();

            data.GraphicsPreset = defaults.GraphicsPreset;
            data.ShadowsEnabled = defaults.ShadowsEnabled;
            data.ShadowDistance = defaults.ShadowDistance;
            data.ShadowCascades = defaults.ShadowCascades;
            data.AntialiasingEnabled = defaults.AntialiasingEnabled;
            data.PostProcessingEnabled = defaults.PostProcessingEnabled;
            data.TextureQualityFull = defaults.TextureQualityFull;
            data.RenderScale = defaults.RenderScale;
            data.ResolutionWidth = defaults.ResolutionWidth;
            data.ResolutionHeight = defaults.ResolutionHeight;
            data.Fullscreen = defaults.Fullscreen;
            data.Vsync = defaults.Vsync;
            data.SettingsVersion = CurrentSettingsVersion;

            return data;
        }

        private void WriteSettings()
        {
            if (Settings.SettingsVersion < CurrentSettingsVersion)
            {
                Settings = Migrate(Settings);
            }

            GameManager.SaveData(SettingsPath, Settings);
        }

        private SettingsData CreateDefaultSettings()
        {
            var defaultLookSensitivity = generalSettings != null ? generalSettings.DefaultLookSensitivity : 0.5f;
            var defaultMasterVolume = generalSettings != null ? generalSettings.DefaultMasterVolume : 1f;
            var defaultMusicVolume = generalSettings != null ? generalSettings.DefaultMusicVolume : 1f;
            var defaultSfxVolume = generalSettings != null ? generalSettings.DefaultSfxVolume : 1f;

            return SettingsData.CreateDefault(
                lookSensitivity: defaultLookSensitivity,
                masterVolume: defaultMasterVolume,
                musicVolume: defaultMusicVolume,
                sfxVolume: defaultSfxVolume
            );
        }
    }
}

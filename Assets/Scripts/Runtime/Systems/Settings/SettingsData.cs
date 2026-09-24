using Newtonsoft.Json;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Settings
{
    /// <summary>
    /// Все пользовательские настройки. Пишутся в Settings.json через GameManager.
    /// Графические и экранные поля добавлены в версии 2: файлы без версии
    /// мигрируют на значения по умолчанию (см. SettingsSystem.ReadSettings).
    /// </summary>
    internal struct SettingsData
    {
        public float LookSensitivity { get; set; }

        public float MasterVolume { get; set; }

        public float MusicVolume { get; set; }

        public float SfxVolume { get; set; }

        /// <summary>Выбранный пресет графики: 0 — низкая, 1 — средняя, 2 — высокая, -1 — своя.</summary>
        public int GraphicsPreset { get; set; }

        public bool ShadowsEnabled { get; set; }

        /// <summary>Дальность теней в метрах: пресеты задают свою, ручное включение — 45.</summary>
        public float ShadowDistance { get; set; }

        /// <summary>Число каскадов теней (1..4).</summary>
        public int ShadowCascades { get; set; }

        public bool AntialiasingEnabled { get; set; }

        /// <summary>Постэффекты (bloom, виньетка и т.п.) на камерах.</summary>
        public bool PostProcessingEnabled { get; set; }

        /// <summary>true — текстуры в полном качестве, false — половинные mip-уровни.</summary>
        public bool TextureQualityFull { get; set; }

        /// <summary>Масштаб рендера URP (0.5..1).</summary>
        public float RenderScale { get; set; }

        /// <summary>Разрешение окна. 0×0 — разрешение по умолчанию (не трогать).</summary>
        public int ResolutionWidth { get; set; }

        public int ResolutionHeight { get; set; }

        public bool Fullscreen { get; set; }

        public bool Vsync { get; set; }

        /// <summary>Версия формата: нужна для миграции старых Settings.json.</summary>
        public int SettingsVersion { get; set; }

        /// <summary>Значения по умолчанию: графика — пресет «Высокая».</summary>
        public static SettingsData CreateDefault(
            float lookSensitivity,
            float masterVolume,
            float musicVolume,
            float sfxVolume)
        {
            return new SettingsData
            {
                LookSensitivity = lookSensitivity,
                MasterVolume = masterVolume,
                MusicVolume = musicVolume,
                SfxVolume = sfxVolume,

                GraphicsPreset = 2,
                ShadowsEnabled = true,
                ShadowDistance = 80f,
                ShadowCascades = 4,
                AntialiasingEnabled = true,
                PostProcessingEnabled = true,
                TextureQualityFull = true,
                RenderScale = 1f,

                ResolutionWidth = 0,
                ResolutionHeight = 0,
                Fullscreen = true,
                Vsync = true,

                SettingsVersion = 3,
            };
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Settings
{
    /// <summary>
    /// Применяет графические настройки к рендер-пайплайну. Работает с СОБСТВЕННЫМ
    /// инстансом URP-ассета (копия URP_Desktop), чтобы менять поля в рантайме,
    /// не портя ассет на диске. Пресеты: 0 — низкая, 1 — средняя, 2 — высокая.
    /// </summary>
    internal static class GraphicsSettingsApplier
    {
        private const float MinRenderScale = 0.5f;
        private const float MaxRenderScale = 1f;

        /// <summary>Минимальная дальность теней, когда они включены.</summary>
        private const float MinShadowDistance = 10f;

        /// <summary>Дальность теней, когда игрок включил их вручную (не пресетом).</summary>
        internal const float DefaultManualShadowDistance = 45f;

        private static UniversalRenderPipelineAsset runtimeAsset;

        /// <summary>Камеры, которым уже выставили постэффекты.</summary>
        private static readonly HashSet<Camera> configuredCameras = new();

        /// <summary>Параметры одного пресета качества.</summary>
        internal readonly struct Preset
        {
            public readonly bool Shadows;
            public readonly float ShadowDistance;
            public readonly int ShadowCascades;
            public readonly bool Antialiasing;
            public readonly bool PostProcessing;
            public readonly float RenderScale;

            public Preset(
                bool shadows,
                float shadowDistance,
                int shadowCascades,
                bool antialiasing,
                bool postProcessing,
                float renderScale)
            {
                Shadows = shadows;
                ShadowDistance = shadowDistance;
                ShadowCascades = shadowCascades;
                Antialiasing = antialiasing;
                PostProcessing = postProcessing;
                RenderScale = renderScale;
            }
        }

        internal static readonly Preset Low = new(
            shadows: false,
            shadowDistance: 0f,
            shadowCascades: 1,
            antialiasing: false,
            postProcessing: false,
            renderScale: 0.75f);

        internal static readonly Preset Medium = new(
            shadows: true,
            shadowDistance: 35f,
            shadowCascades: 2,
            antialiasing: true,
            postProcessing: true,
            renderScale: 0.9f);

        internal static readonly Preset High = new(
            shadows: true,
            shadowDistance: 80f,
            shadowCascades: 4,
            antialiasing: true,
            postProcessing: true,
            renderScale: 1f);

        internal static Preset GetPreset(int index)
        {
            return index switch
            {
                0 => Low,
                1 => Medium,
                _ => High,
            };
        }

        /// <summary>
        /// Применить всё состояние из настроек: URP, текстуры, постэффекты, экран.
        /// </summary>
        public static void Apply(in SettingsData data)
        {
            EnsureRuntimeAsset();

            if (runtimeAsset != null)
            {
                // Дальность 0 полностью отключает отрисовку теней в URP.
                runtimeAsset.shadowDistance = data.ShadowsEnabled
                    ? Mathf.Max(MinShadowDistance, data.ShadowDistance)
                    : 0f;
                runtimeAsset.msaaSampleCount = data.AntialiasingEnabled ? 4 : 1;
                runtimeAsset.renderScale = Mathf.Clamp(
                    data.RenderScale > 0f ? data.RenderScale : 1f,
                    MinRenderScale,
                    MaxRenderScale
                );

                // Количество каскадов имеет смысл только при включённых тенях.
                runtimeAsset.shadowCascadeCount = data.ShadowsEnabled
                    ? Mathf.Clamp(data.ShadowCascades, 1, 4)
                    : 1;
            }

            QualitySettings.globalTextureMipmapLimit = data.TextureQualityFull ? 0 : 1;

            configuredCameras.Clear();
            ApplyPostProcessing(data.PostProcessingEnabled);

            // Экран.
            QualitySettings.vSyncCount = data.Vsync ? 1 : 0;

            var targetWidth = Mathf.Max(0, data.ResolutionWidth);
            var targetHeight = Mathf.Max(0, data.ResolutionHeight);

            if (targetWidth > 0 && targetHeight > 0)
            {
                var current = Screen.currentResolution;

                if (Screen.width != targetWidth || Screen.height != targetHeight)
                {
                    Screen.SetResolution(
                        targetWidth,
                        targetHeight,
                        data.Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed,
                        current.refreshRateRatio
                    );
                }
            }

            var fullscreenMode = data.Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (Screen.fullScreenMode != fullscreenMode)
            {
                Screen.fullScreenMode = fullscreenMode;
            }

            Screen.fullScreen = data.Fullscreen;
        }

        /// <summary>Переключить все настройки разом на пресет качества.</summary>
        public static SettingsData ApplyPreset(int presetIndex, in SettingsData data)
        {
            var preset = GetPreset(presetIndex);

            var applied = data;
            applied.GraphicsPreset = presetIndex;
            applied.ShadowsEnabled = preset.Shadows;
            applied.ShadowDistance = preset.ShadowDistance;
            applied.ShadowCascades = preset.ShadowCascades;
            applied.AntialiasingEnabled = preset.Antialiasing;
            applied.PostProcessingEnabled = preset.PostProcessing;
            applied.RenderScale = preset.RenderScale;

            Apply(applied);

            return applied;
        }

        /// <summary>
        /// Навесить постэффекты на все текущие камеры. Вызывается при применении настроек
        /// и при смене состава камер (см. SettingsSystem).
        /// </summary>
        public static void ApplyPostProcessing(bool enabled)
        {
            var cameras = Camera.allCameras;

            for (var index = 0; index < cameras.Length; index++)
            {
                var camera = cameras[index];
                if (camera == false)
                {
                    continue;
                }

                if (configuredCameras.Contains(camera))
                {
                    continue;
                }

                if (camera.TryGetComponent<UniversalAdditionalCameraData>(out var cameraData))
                {
                    cameraData.renderPostProcessing = enabled;
                }

                configuredCameras.Add(camera);
            }
        }

        /// <summary>
        /// Копия URP-ассета проекта: рантайм-правки не трогают ассет на диске.
        /// </summary>
        private static void EnsureRuntimeAsset()
        {
            if (runtimeAsset != false)
            {
                return;
            }

            // Берём ассет с диска, а не QualitySettings.renderPipeline: тот уже может
            // указывать на нашу предыдущую копию (в т.ч. уничтоженную после выхода из плей-режима).
            var baseAsset = GraphicsSettings.defaultRenderPipeline != null
                ? GraphicsSettings.defaultRenderPipeline
                : QualitySettings.renderPipeline;

            if (baseAsset is not UniversalRenderPipelineAsset urpAsset)
            {
                Debug.LogWarning("[Graphics] URP-ассет не найден — графические настройки применены частично.");
                return;
            }

            runtimeAsset = Object.Instantiate(urpAsset);
            runtimeAsset.name = "URP_RuntimeSettings";
            runtimeAsset.hideFlags = HideFlags.HideAndDontSave;

            QualitySettings.renderPipeline = runtimeAsset;
        }
    }
}

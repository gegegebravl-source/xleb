using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace WarmBread
{
    /// <summary>
    /// Gives the kiosk its night-time look: fog, ambient light, a warm practical lamp and local
    /// reflections.
    /// </summary>
    /// <remarks>
    /// Every value lives here and nowhere else. <see cref="ApplyToScene"/> is public on purpose:
    /// the editor bake tool writes the very same values into the gameplay scene, so the editor
    /// shows the warm shop instead of the flat daytime street the scene file used to hold.
    /// </remarks>
    public sealed class KioskAtmosphereController : MonoBehaviour
    {
        private const string GameplaySceneNamePart = "Gameplay";

        /// <summary>Name of the objects the atmosphere adds, also used to avoid adding them twice.</summary>
        public const string PracticalLightName = "WarmBread_KioskPractical";

        /// <summary>Name of the local reflection probe added by the atmosphere.</summary>
        public const string ReflectionProbeName = "WarmBread_LocalReflections";

        public static readonly Color FogColor = new(0.085f, 0.095f, 0.115f, 1f);

        public const float FogDensity = 0.006f;

        public static readonly Color AmbientSkyColor = new(0.24f, 0.29f, 0.39f);

        public static readonly Color AmbientEquatorColor = new(0.16f, 0.14f, 0.12f);

        public static readonly Color AmbientGroundColor = new(0.045f, 0.04f, 0.035f);

        public const float DirectionalShadowStrength = 0.88f;

        public const float DirectionalColorTemperature = 5100f;

        public static readonly Vector3 PracticalLightPosition = new(0f, 2.15f, -0.65f);

        public static readonly Color PracticalLightColor = new(1f, 0.53f, 0.27f);

        public const float PracticalLightIntensity = 340f;

        public const float PracticalLightRange = 7.5f;

        public const float PracticalLightShadowStrength = 0.72f;

        public static readonly Vector3 ReflectionProbePosition = new(0f, 2.4f, 0f);

        public static readonly Vector3 ReflectionProbeSize = new(24f, 10f, 24f);

        public const int ReflectionProbeResolution = 256;

        public const float ReflectionProbeIntensity = 0.8f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            // Procedural atmosphere generation is disabled: configure the scene manually in the editor.
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyToScene(SceneManager.GetActiveScene());
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyToScene(scene);
        }

        /// <summary>
        /// Apply the kiosk atmosphere to <paramref name="scene"/>, creating the practical light and
        /// the reflection probe when they are missing.
        /// </summary>
        /// <remarks>
        /// Never touches assets, only scene settings and scene objects, so the editor bake tool can
        /// call it outside of play mode.
        /// </remarks>
        public static void ApplyToScene(Scene scene)
        {
            if (scene.IsValid() == false || scene.name.Contains(GameplaySceneNamePart) == false)
            {
                return;
            }

            ApplyRenderSettings();
            ApplyDirectionalLights();
            EnsurePracticalLight(scene);
            EnsureReflectionProbe(scene);
        }

        private static void ApplyRenderSettings()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogDensity = FogDensity;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = AmbientSkyColor;
            RenderSettings.ambientEquatorColor = AmbientEquatorColor;
            RenderSettings.ambientGroundColor = AmbientGroundColor;
        }

        private static void ApplyDirectionalLights()
        {
            var lights = FindObjectsByType<Light>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            for (var index = 0; index < lights.Length; index++)
            {
                var light = lights[index];
                if (light == false || light.type != LightType.Directional)
                {
                    continue;
                }

                light.shadows = LightShadows.Soft;
                light.shadowStrength = DirectionalShadowStrength;
                light.useColorTemperature = true;
                light.colorTemperature = DirectionalColorTemperature;
            }
        }

        private static void EnsurePracticalLight(Scene scene)
        {
            if (ExistsInScene(scene, PracticalLightName))
            {
                return;
            }

            var practical = new GameObject(PracticalLightName);
            practical.transform.position = PracticalLightPosition;

            var light = practical.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = PracticalLightColor;
            light.intensity = PracticalLightIntensity;
            light.range = PracticalLightRange;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = PracticalLightShadowStrength;

            SceneManager.MoveGameObjectToScene(practical, scene);
        }

        private static void EnsureReflectionProbe(Scene scene)
        {
            if (ExistsInScene(scene, ReflectionProbeName))
            {
                return;
            }

            var reflection = new GameObject(ReflectionProbeName);
            reflection.transform.position = ReflectionProbePosition;

            var probe = reflection.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
            probe.resolution = ReflectionProbeResolution;
            probe.size = ReflectionProbeSize;
            probe.boxProjection = true;
            probe.hdr = true;
            probe.intensity = ReflectionProbeIntensity;

            SceneManager.MoveGameObjectToScene(reflection, scene);
        }

        private static bool ExistsInScene(Scene scene, string objectName)
        {
            var transforms = FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            for (var index = 0; index < transforms.Length; index++)
            {
                var candidate = transforms[index];
                if (candidate != false &&
                    candidate.name == objectName &&
                    candidate.gameObject.scene == scene)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

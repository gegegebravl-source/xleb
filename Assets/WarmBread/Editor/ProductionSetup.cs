using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace WarmBread.Editor
{
    public static class ProductionSetup
    {
        [MenuItem("Warm Bread/Setup/Add Runtime Bootstrap")]
        public static void AddBootstrap()
        {
            var existing = Object.FindFirstObjectByType<WarmBreadBootstrap>();
            if (existing != null) { Selection.activeObject = existing; return; }
            var root = new GameObject("WarmBread_Runtime");
            root.AddComponent<WarmBreadBootstrap>();
            Undo.RegisterCreatedObjectUndo(root, "Create Warm Bread runtime");
            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeObject = root;
        }

        [MenuItem("Warm Bread/Setup/Create Cinematic Kiosk Lighting")]
        public static void CreateLighting()
        {
            var root = new GameObject("WarmBread_CinematicLighting");
            Undo.RegisterCreatedObjectUndo(root, "Create cinematic lighting");
            CreateLight(root.transform, "Window Warm Key", LightType.Rectangle, new Color(1f, 0.66f, 0.38f), 850f, new Vector3(0f, 2.2f, -1.5f), new Vector3(18f, 0f, 0f));
            CreateLight(root.transform, "Counter Practical", LightType.Point, new Color(1f, 0.48f, 0.22f), 420f, new Vector3(0f, 1.8f, 0.2f), Vector3.zero);
            CreateLight(root.transform, "Street Cool Fill", LightType.Spot, new Color(0.38f, 0.58f, 1f), 700f, new Vector3(2.5f, 3.4f, -3f), new Vector3(24f, 205f, 0f));
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.075f, 0.085f, 0.11f);
            RenderSettings.fogDensity = 0.008f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.18f, 0.22f, 0.3f);
            RenderSettings.ambientEquatorColor = new Color(0.12f, 0.105f, 0.1f);
            RenderSettings.ambientGroundColor = new Color(0.035f, 0.03f, 0.03f);
            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeObject = root;
        }

        private static void CreateLight(Transform parent, string name, LightType type, Color color, float intensity, Vector3 position, Vector3 rotation)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = position;
            child.transform.localEulerAngles = rotation;
            var light = child.AddComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
            light.range = 12f;
            if (type == LightType.Spot) light.spotAngle = 72f;
        }
    }
}

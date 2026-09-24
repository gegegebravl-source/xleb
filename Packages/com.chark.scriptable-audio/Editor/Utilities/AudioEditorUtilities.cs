using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CHARK.ScriptableAudio.Editor.Utilities
{
    internal static class AudioEditorUtilities
    {
        internal static void AddSceneViewListener(Action<SceneView> onSceneView)
        {
            SceneView.duringSceneGui -= onSceneView;
            SceneView.duringSceneGui += onSceneView;
        }

        internal static void RemoveSceneViewListener(Action<SceneView> onSceneView)
        {
            SceneView.duringSceneGui -= onSceneView;
        }

        internal static void BeginChangeCheck()
        {
            EditorGUI.BeginChangeCheck();
        }

        internal static bool EndChangeCheck()
        {
            return EditorGUI.EndChangeCheck();
        }

        internal static void RecordUndo(Object obj)
        {
            if (obj == false)
            {
                return;
            }

            Undo.RecordObject(obj, $"Record {obj}");
        }

        internal static void SetDirty(Object obj)
        {
            if (obj == false)
            {
                return;
            }

            EditorUtility.SetDirty(obj);
        }

        public static float DrawSphereHandle(
            Vector3 position,
            float radius,
            Color sphereColor,
            Color wireColor
        )
        {
            var rotation = Quaternion.identity;

            Handles.color = sphereColor;
            Handles.SphereHandleCap(0, position, rotation, radius * 2f, EventType.Repaint);

            Handles.color = wireColor;
            return Handles.RadiusHandle(rotation, position, radius);
        }
    }
}

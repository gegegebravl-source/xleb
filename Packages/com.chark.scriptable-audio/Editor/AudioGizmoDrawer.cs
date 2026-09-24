using CHARK.ScriptableAudio.Editor.Utilities;
using UnityEditor;
using UnityEngine;

namespace CHARK.ScriptableAudio.Editor
{
    internal static class AudioGizmoDrawer
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.Active | GizmoType.NotInSelectionHierarchy | GizmoType.Pickable)]
        private static void DrawGizmo(AudioEmitter emitter, GizmoType gizmoType)
        {
            var transform = emitter.transform;
            var position = transform.position;

            Gizmos.DrawIcon(
                position,
                AudioEditorStyles.AudioEmitterGizmo,
                true,
                AudioEditorStyles.AudioEmitterGizmoColor
            );
        }
    }
}

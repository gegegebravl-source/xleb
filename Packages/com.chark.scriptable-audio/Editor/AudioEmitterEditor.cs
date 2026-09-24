using System.Collections.Generic;
using System.Linq;
using CHARK.ScriptableAudio.Editor.Utilities;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif
using UnityEditor;
using UnityEngine;

namespace CHARK.ScriptableAudio.Editor
{
    /// <summary>
    /// Generic Editor for drawing audio emitters.
    /// </summary>
    internal abstract class AudioEmitterEditor
#if ODIN_INSPECTOR
        : OdinEditor
#else
        : UnityEditor.Editor
#endif
    {
        private IReadOnlyList<AudioEmitter> emitters;

#if ODIN_INSPECTOR
        protected override void OnEnable()
        {
            base.OnEnable();
#else
        private void OnEnable()
        {
#endif
            AudioEditorUtilities.AddSceneViewListener(DrawEmitters);

            emitters = targets.Cast<AudioEmitter>().ToList();
        }

#if ODIN_INSPECTOR
        protected override void OnDisable()
        {
#else
        private void OnDisable()
        {
#endif
            AudioEditorUtilities.RemoveSceneViewListener(DrawEmitters);
        }

        private void DrawEmitters(SceneView sceneView)
        {
            if (emitters == null)
            {
                return;
            }

            foreach (var emitter in emitters)
            {
                DrawEmitter(emitter);
            }
        }

        protected virtual bool Is3D(AudioEmitter emitter)
        {
            return false;
        }

        protected virtual Vector2 GetDistanceRange(AudioEmitter emitter)
        {
            return Vector2.zero;
        }

        private void DrawEmitter(AudioEmitter emitter)
        {
            if (Is3D(emitter) == false)
            {
                return;
            }

            var range = GetDistanceRange(emitter);
            if (range == Vector2.zero)
            {
                return;
            }

            var minDistance = GetMinDistance(emitter, range.x);
            var maxDistance = GetMaxDistance(emitter, range.y);
            DrawHandles(emitter, minDistance, maxDistance);
        }

        private static void DrawHandles(AudioEmitter emitter, float minDistance, float maxDistance)
        {
            var emitterTransform = emitter.transform;
            var emitterPosition = emitterTransform.position;

            AudioEditorUtilities.BeginChangeCheck();

            var isOverride = emitter.IsOverrideAttenuation;
            minDistance = AudioEditorUtilities.DrawSphereHandle(
                emitterPosition,
                minDistance,
                isOverride ? AudioEditorStyles.MinSphereOverrideColor : AudioEditorStyles.MinSphereColor,
                isOverride ? AudioEditorStyles.MinWireOverrideColor : AudioEditorStyles.MinWireColor
            );

            maxDistance = AudioEditorUtilities.DrawSphereHandle(
                emitterPosition,
                maxDistance,
                isOverride ? AudioEditorStyles.MaxSphereOverrideColor : AudioEditorStyles.MaxSphereColor,
                isOverride ? AudioEditorStyles.MaxWireOverrideColor : AudioEditorStyles.MaxWireColor
            );

            if (AudioEditorUtilities.EndChangeCheck() && emitter.IsOverrideAttenuation)
            {
                SetDistanceRange(emitter, minDistance, maxDistance);
            }
        }

        private static float GetMinDistance(AudioEmitter emitter, float minDistance)
        {
            return emitter.IsOverrideAttenuation ? emitter.OverrideMinDistance : minDistance;
        }

        private static float GetMaxDistance(AudioEmitter emitter, float maxDistance)
        {
            return emitter.IsOverrideAttenuation ? emitter.OverrideMaxDistance : maxDistance;
        }

        private static void SetDistanceRange(AudioEmitter emitter, float min, float max)
        {
            AudioEditorUtilities.RecordUndo(emitter);
            emitter.OverrideMinDistance = Mathf.Clamp(min, 0, emitter.OverrideMaxDistance);
            emitter.OverrideMaxDistance = Mathf.Max(emitter.OverrideMinDistance, max);
        }
    }
}

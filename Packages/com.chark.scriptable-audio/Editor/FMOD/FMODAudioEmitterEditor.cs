using UnityEditor;
using UnityEngine;

namespace CHARK.ScriptableAudio.Editor.FMOD
{
    // ReSharper disable once InconsistentNaming
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AudioEmitter))]
    internal sealed class FMODAudioEmitterEditor : AudioEmitterEditor
    {
        protected override bool Is3D(AudioEmitter emitter)
        {
            var audioEventAsset = emitter.AudioEventAsset;
            var editorEvent = audioEventAsset.GetEditorEvent();

            return editorEvent && editorEvent.Is3D;
        }

        protected override Vector2 GetDistanceRange(AudioEmitter emitter)
        {
            var audioEventAsset = emitter.AudioEventAsset;
            var editorEvent = audioEventAsset.GetEditorEvent();

            if (editorEvent)
            {
                return new Vector2(editorEvent.MinDistance, editorEvent.MaxDistance);
            }

            return Vector2.zero;
        }
    }
}

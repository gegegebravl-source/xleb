using CHARK.ScriptableAudio.Editor.Utilities;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace CHARK.ScriptableAudio.Editor
{
    [CustomEditor(typeof(AudioEventAsset))]
    internal sealed class AudioEventAssetEditor : UnityEditor.Editor
    {
        private AudioEventAsset audioEventAsset;

        [SerializeField]
        private bool isEmittersExpanded;

        private void OnEnable()
        {
            audioEventAsset = (AudioEventAsset) target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var audioEmitters = audioEventAsset.AudioEmitters;
            isEmittersExpanded = AudioEditorGUI.DrawFoldoutList(
                isEmittersExpanded,
                "Emitters",
                $"• Emitters are visible during play-mode\n" +
                $"• Emitters used via {nameof(AudioEmitter)} will show up in this list",
                audioEmitters,
                DrawStatusButton
            );
        }

        private static void DrawStatusButton(Object obj)
        {
            if (!(obj is AudioEmitter emitter))
            {
                return;
            }

            var isPlaying = emitter.IsPlaying;
            if (isPlaying == false)
            {
                DrawPlayButton(emitter);
            }
            else
            {
                DrawStopButton(emitter);
            }
        }

        private static void DrawPlayButton(AudioEmitter emitter)
        {
            if (AudioEditorGUI.DrawButton(AudioEditorStyles.PlayIcon, "Play Audio Emitter"))
            {
                emitter.Play();
            }
        }

        private static void DrawStopButton(AudioEmitter emitter)
        {
            if (AudioEditorGUI.DrawButton(AudioEditorStyles.StopIcon, "Stop Audio Emitter"))
            {
                emitter.Stop();
            }
        }
    }
}

using CHARK.ScriptableAudio.Editor.Utilities;
using UnityEditor;
using UnityEngine;

namespace CHARK.ScriptableAudio.Editor
{
    [CustomEditor(typeof(AudioParameterAsset))]
    internal sealed class AudioParameterAssetEditor : UnityEditor.Editor
    {
        private AudioParameterAsset audioParameterAsset;

        [SerializeField]
        private bool isParametersExpanded;

        private void OnEnable()
        {
            audioParameterAsset = (AudioParameterAsset) target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var audioParameters = audioParameterAsset.AudioParameters;
            isParametersExpanded = AudioEditorGUI.DrawFoldoutList(
                isParametersExpanded,
                "Parameters",
                $"• Parameters are visible during play-mode\n" +
                $"• Parameters used via {nameof(AudioParameter)} will show up in this list",
                audioParameters
            );
        }
    }
}

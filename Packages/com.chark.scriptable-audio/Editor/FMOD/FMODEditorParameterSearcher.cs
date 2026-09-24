using System.Collections.Generic;
using CHARK.ScriptableAudio.Editor.Utilities;
using CHARK.ScriptableAudio.FMOD;
using FMODUnity;
using UnityEngine;

namespace CHARK.ScriptableAudio.Editor.FMOD
{
    // ReSharper disable once InconsistentNaming
    internal sealed class FMODEditorParameterSearcher : SearcherWindow<EditorParamRef>
    {
        private FMODAudioParameterData audioParameterData;

        public void Initialize(FMODAudioParameterData newAudioParameterData)
        {
            audioParameterData = newAudioParameterData;
        }

        protected override IEnumerable<EditorParamRef> GetData()
        {
            return audioParameterData.GetEditorParameters();
        }

        protected override string GetNoEntriesTitle()
        {
            if (audioParameterData == null || audioParameterData.IsGlobal)
            {
                return "Could not find Global Audio Parameters";
            }

            return "Could not find Local Audio Parameters";
        }

        protected override string GetTitle()
        {
            if (audioParameterData == null || audioParameterData.IsGlobal)
            {
                return "Global Audio Parameters";
            }

            return "Local Audio Parameters";
        }

        protected override string GetName(EditorParamRef data)
        {
            return data.Name;
        }

        protected override Texture GetIcon(EditorParamRef data)
        {
            return AudioEditorStyles.AudioParameterIcon;
        }
    }
}

using System.Collections.Generic;
using CHARK.ScriptableAudio.Editor.Utilities;
using FMODUnity;
using UnityEngine;

namespace CHARK.ScriptableAudio.Editor.FMOD
{
    // ReSharper disable once InconsistentNaming
    internal sealed class FMODEditorEventSearcher : SearcherWindow<EditorEventRef>
    {
        protected override IEnumerable<EditorEventRef> GetData()
        {
            return FMODEditorUtilities.GetEditorEvents();
        }

        protected override string GetNoEntriesTitle()
        {
            return "Could not find Audio Events";
        }

        protected override string GetTitle()
        {
            return "Audio Events";
        }

        protected override string GetName(EditorEventRef data)
        {
            return data.Path.Replace("event:/", "");
        }

        protected override Texture GetIcon(EditorEventRef data)
        {
            return AudioEditorStyles.AudioEventIcon;
        }
    }
}

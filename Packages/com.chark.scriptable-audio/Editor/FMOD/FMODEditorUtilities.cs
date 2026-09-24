using System;
using System.Collections.Generic;
using System.Linq;
using CHARK.ScriptableAudio.FMOD;
using FMODUnity;

namespace CHARK.ScriptableAudio.Editor.FMOD
{
    // ReSharper disable once InconsistentNaming
    internal static class FMODEditorUtilities
    {
        internal static IEnumerable<EditorParamRef> GetEditorParameters(this FMODAudioParameterData data)
        {
            if (data == default)
            {
                return Array.Empty<EditorParamRef>();
            }

            if (data.IsGlobal)
            {
                return EventManager.Parameters;
            }

            var audioEventAsset = data.AudioEventAsset;
            var editorEvent = audioEventAsset.GetEditorEvent();
            if (editorEvent == false)
            {
                return Array.Empty<EditorParamRef>();
            }

            var globalParameters = editorEvent.GlobalParameters;
            var localParameters = editorEvent.LocalParameters;

            return globalParameters.Concat(localParameters);
        }

        internal static EditorParamRef GetEditorParameter(this AudioParameterAsset asset)
        {
            if (asset == false)
            {
                return default;
            }

            return (asset.Data as FMODAudioParameterData).GetEditorParameter();
        }

        internal static EditorParamRef GetEditorParameter(this FMODAudioParameterData data)
        {
            if (data == default)
            {
                return default;
            }

            var parameterName = data.ParameterName;
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                return default;
            }

            return data
                .GetEditorParameters()
                .FirstOrDefault(editorParameter =>
                    string.Equals(parameterName, editorParameter.Name)
                );
        }

        internal static IEnumerable<EditorEventRef> GetEditorEvents()
        {
            return EventManager.Events;
        }

        internal static EditorEventRef GetEditorEvent(this AudioEventAsset asset)
        {
            if (asset == false)
            {
                return default;
            }

            return (asset.Data as FMODAudioEventData).GetEditorEvent();
        }

        internal static EditorEventRef GetEditorEvent(this FMODAudioEventData data)
        {
            if (data == default)
            {
                return default;
            }

            var eventPath = data.EventPath;
            if (string.IsNullOrWhiteSpace(eventPath))
            {
                return default;
            }

            return EventManager.EventFromPath(eventPath);
        }
    }
}

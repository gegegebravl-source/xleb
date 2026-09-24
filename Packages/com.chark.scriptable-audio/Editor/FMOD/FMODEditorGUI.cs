using System;
using CHARK.ScriptableAudio.FMOD;
using FMODUnity;
using UnityEditor;
using UnityEngine;

namespace CHARK.ScriptableAudio.Editor.FMOD
{
    // ReSharper disable once InconsistentNaming
    internal static class FMODEditorGUI
    {
        internal static void ShowEditorParameterSearcher(
            FMODAudioParameterData data,
            Action<EditorParamRef> onSelected
        )
        {
            var searcher = ScriptableObject.CreateInstance<FMODEditorParameterSearcher>();
            searcher.Initialize(data);
            searcher.Show();
            searcher.OnClicked += onSelected;
        }

        internal static void ShowEditorEventSearcher(Action<EditorEventRef> onSelected)
        {
            var searcher = ScriptableObject.CreateInstance<FMODEditorEventSearcher>();
            searcher.Show();
            searcher.OnClicked += onSelected;
        }

        internal static void ShowEditorEventBrowser(this EditorEventRef editorEvent)
        {
            if (editorEvent == false)
            {
                return;
            }

            EventBrowser.ShowWindow();
            var eventBrowser = EditorWindow.GetWindow<EventBrowser>();
            eventBrowser.FrameEvent(editorEvent.Path);
        }
    }
}

using System.Collections.Generic;
using CHARK.ScriptableAudio.Editor.Utilities;
using CHARK.ScriptableAudio.FMOD;
using FMODUnity;
using UnityEditor;
using UnityEngine;

namespace CHARK.ScriptableAudio.Editor.FMOD
{
    // ReSharper disable once InconsistentNaming
    [CustomPropertyDrawer(typeof(FMODAudioEventData))]
    internal sealed class FMODAudioEventDataDrawer : AudioPropertyDrawer<FMODAudioEventData>
    {
        private EditorEventRef editorEvent;
        private bool isExpandedEvent;

        protected override IEnumerable<Drawer> GetDrawers()
        {
            bool IsDrawDetails() => isExpandedEvent && editorEvent;

            return new[]
            {
                new Drawer(DrawAudioEventData),
                new Drawer(DrawEditorEventGuid, IsDrawDetails, true),
                new Drawer(DrawEditorEventLenght, IsDrawDetails, true),
                new Drawer(DrawEditorEventMinDistance, IsDrawDetails, true),
                new Drawer(DrawEditorEventMaxDistance, IsDrawDetails, true),
                new Drawer(DrawEditorEventIsOneShot, IsDrawDetails, true),
                new Drawer(DrawEditorEventIsStream, IsDrawDetails, true),
                new Drawer(DrawEditorEventIs3D, IsDrawDetails, true),
                new Drawer(DrawEditorEventGlobalParameters, IsDrawDetails, true),
                new Drawer(DrawEditorEventLocalParameters, IsDrawDetails, true)
            };
        }

        private void DrawAudioEventData()
        {
            var eventFoldoutPosition = GetEventFoldoutPosition();
            var eventPathPosition = GetEventPathPosition(eventFoldoutPosition);
            var searchButtonPosition = GetSearchButtonPosition(eventPathPosition);
            var detailsButtonPosition = GetDetailsButtonPosition(searchButtonPosition);

            editorEvent = Value.GetEditorEvent();
            if (editorEvent == false)
            {
                SetErrorGuiColor();
            }

            DrawEventFoldout(eventFoldoutPosition, editorEvent);
            DrawEventPath(eventPathPosition);

            ResetGuiColor();

            DrawSearchButton(searchButtonPosition);

            if (editorEvent == false)
            {
                SetDisabledGuiState();
            }

            DrawDetailsButton(detailsButtonPosition);
            ResetGuiState();
        }

        private Rect GetEventFoldoutPosition()
        {
            var position = Position;
            position.width = AudioEditorStyles.LabelWidth + AudioEditorStyles.HMargin;
            return position;
        }

        private Rect GetEventPathPosition(Rect afterPosition)
        {
            var position = Position;
            position.width = Position.width - afterPosition.width
                                            - AudioEditorStyles.ButtonWidth * 2
                                            - AudioEditorStyles.HMargin * 2;

            position.x = afterPosition.xMax;
            return position;
        }

        private Rect GetSearchButtonPosition(Rect afterPosition)
        {
            var position = Position;
            position.width = AudioEditorStyles.ButtonWidth;
            position.x = afterPosition.xMax + AudioEditorStyles.HMargin;
            return position;
        }

        private Rect GetDetailsButtonPosition(Rect afterPosition)
        {
            var position = Position;
            position.width = AudioEditorStyles.ButtonWidth;
            position.x = afterPosition.xMax + AudioEditorStyles.HMargin;
            return position;
        }

        private void DrawEventFoldout(Rect position, bool isValidEvent)
        {
            isExpandedEvent = AudioEditorGUI.DrawFoldout(
                position,
                new GUIContent(
                    "Event Reference",
                    isValidEvent ? null : AudioEditorStyles.ErrorIcon,
                    isValidEvent
                        ? "View Event details"
                        : "Event Reference is invalid (miss-typed or missing)"
                ),
                isExpandedEvent
            ) && isValidEvent;
        }

        private void DrawEventPath(Rect position)
        {
            AudioEditorUtilities.BeginChangeCheck();

            var oldEventPath = Value.EventPath;
            var newEventPath = AudioEditorGUI.DrawText(position, oldEventPath);

            if (AudioEditorUtilities.EndChangeCheck() == false)
            {
                return;
            }

            AudioEditorUtilities.RecordUndo(SerializedObject.targetObject);

            Value.EventPath = newEventPath;

            var newEditorEvent = Value.GetEditorEvent();
            if (newEditorEvent)
            {
                Value.EventId = newEditorEvent.Guid;
            }

            AudioEditorUtilities.SetDirty(SerializedObject.targetObject);
            SerializedObject.ApplyModifiedProperties();
        }

        private void DrawSearchButton(Rect position)
        {
            var content = new GUIContent(
                string.Empty,
                AudioEditorStyles.SearchIcon, "Search for Event"
            );

            if (AudioEditorGUI.DrawButton(position, content))
            {
                FMODEditorGUI.ShowEditorEventSearcher(OnSelectedEditorEvent);
            }
        }

        private void DrawDetailsButton(Rect position)
        {
            if (editorEvent == false)
            {
                SetDisabledGuiState();
            }

            var content = new GUIContent(
                AudioEditorStyles.DetailsIcon,
                "View Event details in Event Browser"
            );

            if (AudioEditorGUI.DrawButton(position, content))
            {
                editorEvent.ShowEditorEventBrowser();
            }

            ResetGuiState();
        }

        private void DrawEditorEventGuid()
        {
            AudioEditorGUI.DrawText(Position, "GUID", editorEvent.Guid.ToString());
        }

        private void DrawEditorEventLenght()
        {
            AudioEditorGUI.DrawNumber(Position, "Length", editorEvent.Length);
        }

        private void DrawEditorEventMinDistance()
        {
            AudioEditorGUI.DrawNumber(Position, "Min Distance", editorEvent.MinDistance);
        }

        private void DrawEditorEventMaxDistance()
        {
            AudioEditorGUI.DrawNumber(Position, "Max Distance", editorEvent.MaxDistance);
        }

        private void DrawEditorEventIsOneShot()
        {
            AudioEditorGUI.DrawToggle(Position, "OneShot", editorEvent.IsOneShot);
        }

        private void DrawEditorEventIsStream()
        {
            AudioEditorGUI.DrawToggle(Position, "Stream", editorEvent.IsStream);
        }

        private void DrawEditorEventIs3D()
        {
            AudioEditorGUI.DrawToggle(Position, "3D", editorEvent.Is3D);
        }

        private void DrawEditorEventGlobalParameters()
        {
            var count = editorEvent.GlobalParameters.Count.ToString();
            AudioEditorGUI.DrawText(Position, "Global Parameters", count);
        }

        private void DrawEditorEventLocalParameters()
        {
            var count = editorEvent.LocalParameters.Count.ToString();
            AudioEditorGUI.DrawText(Position, "Local Parameters", count);
        }

        private void OnSelectedEditorEvent(EditorEventRef newEditorEvent)
        {
            var target = SerializedObject.targetObject;
            AudioEditorUtilities.RecordUndo(target);

            Value.EventPath = newEditorEvent.Path;
            Value.EventId = newEditorEvent.Guid;

            AudioEditorUtilities.SetDirty(target);
            SerializedObject.ApplyModifiedProperties();
        }
    }
}

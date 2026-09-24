using System.Collections.Generic;
using CHARK.ScriptableAudio.Editor.Utilities;
using FMODUnity;
using UnityEditor;
using UnityEngine;

namespace CHARK.ScriptableAudio.Editor.FMOD
{
    // ReSharper disable once InconsistentNaming
    [CustomPropertyDrawer(typeof(AudioEventAsset))]
    internal sealed class FMODAudioEventAssetDrawer : AudioPropertyDrawer<AudioEventAsset>
    {
        private EditorEventRef editorEvent;

        protected override IEnumerable<Drawer> GetDrawers()
        {
            return new[]
            {
                new Drawer(DrawEventAsset)
            };
        }

        private void DrawEventAsset()
        {
            var eventAssetFieldPosition = GetEventAssetFieldPosition();
            var propertiesButtonPosition = GetPropertiesButtonPosition(eventAssetFieldPosition);
            var detailsButtonPosition = GetDetailsButtonPosition(propertiesButtonPosition);

            editorEvent = Value.GetEditorEvent();
            if (editorEvent == false)
            {
                SetErrorLabelImage();
                SetErrorGuiColor();
            }

            DrawEventAsset(eventAssetFieldPosition);

            ResetLabelImage();
            ResetGuiColor();

            if (Value == false)
            {
                SetDisabledGuiState();
            }

            DrawPropertiesButton(propertiesButtonPosition);
            ResetGuiState();

            if (editorEvent == false)
            {
                SetDisabledGuiState();
            }

            DrawDetailsButton(detailsButtonPosition);
            ResetGuiState();
        }

        private Rect GetEventAssetFieldPosition()
        {
            var position = Position;
            position.width = position.width
                             - AudioEditorStyles.ButtonWidth * 2
                             - AudioEditorStyles.HMargin * 2;

            return position;
        }

        private Rect GetPropertiesButtonPosition(Rect afterPosition)
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

        private void DrawEventAsset(Rect position)
        {
            AudioEditorGUI.DrawProperty(position, PropertyLabel, SerializedProperty);
        }

        private void DrawPropertiesButton(Rect position)
        {
            var content = new GUIContent(AudioEditorStyles.PropertiesIcon, "Properties");
            if (AudioEditorGUI.DrawButton(position, content))
            {
                PropertiesWindow.ShowProperties(SerializedProperty.objectReferenceValue);
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
    }
}

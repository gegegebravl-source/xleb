using System.Collections.Generic;
using CHARK.ScriptableAudio.Editor.Utilities;
using FMODUnity;
using UnityEditor;
using UnityEngine;

namespace CHARK.ScriptableAudio.Editor.FMOD
{
    // ReSharper disable once InconsistentNaming
    [CustomPropertyDrawer(typeof(AudioParameterAsset))]
    internal sealed class FMODAudioParameterAssetDrawer : AudioPropertyDrawer<AudioParameterAsset>
    {
        private EditorParamRef editorParameter;

        protected override IEnumerable<Drawer> GetDrawers()
        {
            return new[]
            {
                new Drawer(DrawParameterAsset)
            };
        }

        private void DrawParameterAsset()
        {
            var parameterAssetFieldPosition = GetParameterAssetFieldPosition();
            var propertiesButtonPosition = GetPropertiesButtonPosition(parameterAssetFieldPosition);

            editorParameter = Value.GetEditorParameter();
            if (editorParameter == false)
            {
                SetErrorLabelImage();
                SetErrorGuiColor();
            }

            DrawParameterAsset(parameterAssetFieldPosition);

            ResetLabelImage();
            ResetGuiColor();

            if (Value == false)
            {
                SetDisabledGuiState();
            }

            DrawPropertiesButton(propertiesButtonPosition);
            ResetGuiState();
        }

        private Rect GetParameterAssetFieldPosition()
        {
            var position = Position;
            position.width = position.width
                             - AudioEditorStyles.ButtonWidth * 1
                             - AudioEditorStyles.HMargin * 1;

            return position;
        }

        private Rect GetPropertiesButtonPosition(Rect afterPosition)
        {
            var position = Position;
            position.width = AudioEditorStyles.ButtonWidth;
            position.x = afterPosition.xMax + AudioEditorStyles.HMargin;
            return position;
        }

        private void DrawParameterAsset(Rect position)
        {
            AudioEditorGUI.DrawProperty(position, PropertyLabel, SerializedProperty);
        }

        private void DrawPropertiesButton(Rect position)
        {
            var content = new GUIContent(AudioEditorStyles.PropertiesIcon, "Properties");
            if (AudioEditorGUI.DrawButton(position, content))
            {
                PropertiesWindow.ShowProperties(Value);
            }
        }
    }
}

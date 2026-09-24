using System.Collections.Generic;
using CHARK.ScriptableAudio.Editor.Utilities;
using CHARK.ScriptableAudio.FMOD;
using FMODUnity;
using UnityEditor;
using UnityEngine;

namespace CHARK.ScriptableAudio.Editor.FMOD
{
    // ReSharper disable once InconsistentNaming
    [CustomPropertyDrawer(typeof(FMODAudioParameterData))]
    internal sealed class FMODAudioParameterDataDrawer : AudioPropertyDrawer<FMODAudioParameterData>
    {
        private EditorParamRef editorParameter;
        private bool isExpandedParameter;

        protected override IEnumerable<Drawer> GetDrawers()
        {
            bool IsDrawAudioEventAsset()
            {
                if (Value == null)
                {
                    return false;
                }

                return Value.IsGlobal == false;
            }

            bool IsDrawDetails() => isExpandedParameter && editorParameter;

            return new[]
            {
                new Drawer(DrawAudioParameterData),
                new Drawer(DrawEditorParameterStudioPath, IsDrawDetails, true),
                new Drawer(DrawEditorParameterMinValue, IsDrawDetails, true),
                new Drawer(DrawEditorParameterMaxValue, IsDrawDetails, true),
                new Drawer(DrawEditorParameterDefaultValue, IsDrawDetails, true),
                new Drawer(DrawEditorParameterId, IsDrawDetails, true),
                new Drawer(DrawEditorParameterType, IsDrawDetails, true),
                new Drawer(DrawEditorParameterIsGlobal, IsDrawDetails, true),
                new Drawer(DrawEditorParameterIsExists, IsDrawDetails, true),
                new Drawer(() => { }, IsDrawDetails, true),
                new Drawer(DrawAudioEventAsset, IsDrawAudioEventAsset)
            };
        }

        private void DrawAudioParameterData()
        {
            var parameterFoldoutPosition = GetParameterFoldoutPosition();
            var parameterNamePosition = GetParameterNamePosition(parameterFoldoutPosition);
            var globalButtonPosition = GetGlobalButtonPosition(parameterNamePosition);
            var searchButtonPosition = GetSearchButtonPosition(globalButtonPosition);

            editorParameter = Value.GetEditorParameter();

            if (editorParameter == false)
            {
                SetErrorGuiColor();
            }

            DrawParameterFoldout(parameterFoldoutPosition, editorParameter);
            DrawParameterName(parameterNamePosition);

            ResetGuiColor();

            DrawGlobalToggleButton(globalButtonPosition);
            DrawSearchButton(searchButtonPosition);
        }

        private Rect GetParameterFoldoutPosition()
        {
            var position = Position;
            position.width = AudioEditorStyles.LabelWidth + AudioEditorStyles.HMargin;
            return position;
        }

        private Rect GetParameterNamePosition(Rect afterPosition)
        {
            var position = Position;
            position.width = Position.width - afterPosition.width
                                            - AudioEditorStyles.ButtonWidth * 2
                                            - AudioEditorStyles.HMargin * 2;

            position.x = afterPosition.xMax;
            return position;
        }

        private Rect GetGlobalButtonPosition(Rect afterPosition)
        {
            var position = Position;
            position.width = AudioEditorStyles.ButtonWidth;
            position.x = afterPosition.xMax + AudioEditorStyles.HMargin;
            return position;
        }

        private Rect GetSearchButtonPosition(Rect afterPosition)
        {
            var position = Position;
            position.width = AudioEditorStyles.ButtonWidth;
            position.x = afterPosition.xMax + AudioEditorStyles.HMargin;
            return position;
        }

        private void DrawAudioEventAsset()
        {
            var property = SerializedProperty.FindPropertyRelative("audioEventAsset");
            if (property == default)
            {
                return;
            }

            AudioEditorGUI.DrawProperty(Position, property);
        }

        private void DrawParameterFoldout(Rect position, bool isValidParameter)
        {
            isExpandedParameter = AudioEditorGUI.DrawFoldout(
                position,
                new GUIContent(
                    "Parameter Reference",
                    isValidParameter ? null : AudioEditorStyles.ErrorIcon,
                    isValidParameter
                        ? "View Parameter details"
                        : "Parameter Reference is invalid (miss-typed or missing)"
                ),
                isExpandedParameter
            ) && isValidParameter;
        }

        private void DrawParameterName(Rect position)
        {
            AudioEditorUtilities.BeginChangeCheck();

            var oldParameterName = Value.ParameterName;
            var newParameterName = AudioEditorGUI.DrawText(position, oldParameterName);

            if (AudioEditorUtilities.EndChangeCheck() == false)
            {
                return;
            }

            AudioEditorUtilities.RecordUndo(SerializedObject.targetObject);
            Value.ParameterName = newParameterName;

            AudioEditorUtilities.SetDirty(SerializedObject.targetObject);
            SerializedObject.ApplyModifiedProperties();
        }

        private void DrawGlobalToggleButton(Rect position)
        {
            var content = new GUIContent(
                AudioEditorStyles.GlobeIcon,
                Value.IsGlobal
                    ? "Switch to Local Parameter"
                    : "Switch to Global Parameter"
            );

            AudioEditorUtilities.BeginChangeCheck();

            var newIsGlobal = AudioEditorGUI.DrawToggleButton(position, content, Value.IsGlobal);
            if (AudioEditorUtilities.EndChangeCheck() == false)
            {
                return;
            }

            var target = SerializedObject.targetObject;
            AudioEditorUtilities.RecordUndo(target);

            Value.IsGlobal = newIsGlobal;
            SerializedObject.ApplyModifiedProperties();
        }

        private void DrawSearchButton(Rect position)
        {
            var content = new GUIContent(
                AudioEditorStyles.SearchIcon,
                Value.IsGlobal
                    ? "Search for Global Parameter"
                    : "Search for Local Parameter (local to selected Audio Event Asset)"
            );

            if (AudioEditorGUI.DrawButton(position, content))
            {
                FMODEditorGUI.ShowEditorParameterSearcher(Value, OnSelectedEditorParameter);
            }
        }

        private void OnSelectedEditorParameter(EditorParamRef newEditorParameter)
        {
            var target = SerializedObject.targetObject;
            AudioEditorUtilities.RecordUndo(target);

            Value.ParameterName = newEditorParameter.Name;

            AudioEditorUtilities.SetDirty(target);
            SerializedObject.ApplyModifiedProperties();
        }

        private void DrawEditorParameterStudioPath()
        {
            AudioEditorGUI.DrawText(Position, "Studio Path", editorParameter.StudioPath);
        }

        private void DrawEditorParameterMinValue()
        {
            AudioEditorGUI.DrawNumber(Position, "Min Value", editorParameter.Min);
        }

        private void DrawEditorParameterMaxValue()
        {
            AudioEditorGUI.DrawNumber(Position, "Max Value", editorParameter.Max);
        }

        private void DrawEditorParameterDefaultValue()
        {
            AudioEditorGUI.DrawNumber(Position, "Default Value", editorParameter.Default);
        }

        private void DrawEditorParameterId()
        {
            AudioEditorGUI.DrawText(
                Position,
                "ID",
                $"{editorParameter.ID.data1}, {editorParameter.ID.data2}"
            );
        }

        private void DrawEditorParameterType()
        {
            AudioEditorGUI.DrawText(Position, "Type", $"{editorParameter.Type}");
        }

        private void DrawEditorParameterIsGlobal()
        {
            AudioEditorGUI.DrawToggle(Position, "Global", editorParameter.IsGlobal);
        }

        private void DrawEditorParameterIsExists()
        {
            AudioEditorGUI.DrawToggle(Position, "Exists", editorParameter.Exists);
        }
    }
}

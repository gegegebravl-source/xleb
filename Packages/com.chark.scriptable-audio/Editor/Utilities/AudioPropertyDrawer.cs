using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CHARK.ScriptableAudio.Editor.Utilities
{
    /// <summary>
    /// Generic property drawer for a value of type <see cref="TValue"/>.
    /// </summary>
    internal abstract class AudioPropertyDrawer<TValue> : PropertyDrawer
    {
        protected SerializedProperty SerializedProperty { get; private set; }

        protected SerializedObject SerializedObject { get; private set; }

        protected GUIContent PropertyLabel { get; private set; }

        protected Rect Position { get; private set; }

        protected TValue Value { get; private set; }

        private readonly List<Drawer> drawers = new List<Drawer>();
        private bool isDrawersInitialized;

        private bool originalGuiEnabled;
        private Color originalGuiColor;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var totalHeight = 0f;
            for (var index = 0; index < drawers.Count; index++)
            {
                var drawer = drawers[index];
                if (drawer.IsDraw() == false)
                {
                    continue;
                }

                totalHeight += AudioEditorStyles.LineHeight;

                if (index > 0)
                {
                    totalHeight += AudioEditorStyles.VMargin;
                }
            }

            return totalHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            originalGuiEnabled = GUI.enabled;
            originalGuiColor = GUI.color;

            InitializePropertyDrawer(position, property, label);
            Draw();

            ResetLabelImage();
            ResetGuiState();
            ResetGuiColor();
        }

        protected virtual IEnumerable<Drawer> GetDrawers()
        {
            return Array.Empty<Drawer>();
        }

        protected void SetDisabledGuiState()
        {
            GUI.enabled = false;
        }

        protected void ResetGuiState()
        {
            GUI.enabled = originalGuiEnabled;
        }

        protected void SetErrorGuiColor()
        {
            // TODO: unsure if want to use error color, hard to read
            // GUI.color = AudioEditorSkin.ErrorColor;
        }

        protected void ResetGuiColor()
        {
            GUI.color = originalGuiColor;
        }

        protected void SetErrorLabelImage()
        {
            PropertyLabel.image = AudioEditorStyles.ErrorIcon;
        }

        protected void ResetLabelImage()
        {
            PropertyLabel.image = default;
        }

        private void InitializePropertyDrawer(
            Rect position,
            SerializedProperty property,
            GUIContent label
        )
        {
            position.height = AudioEditorStyles.LineHeight;
            Position = position;

            SerializedProperty = property;
            SerializedObject = property.serializedObject;
            PropertyLabel = label;
            Value = (TValue) property.GetTargetObjectOfProperty();

            if (isDrawersInitialized)
            {
                return;
            }

            drawers.Clear();
            drawers.AddRange(GetDrawers());
            isDrawersInitialized = true;
        }

        private void Draw()
        {
            for (var index = 0; index < drawers.Count; index++)
            {
                var drawer = drawers[index];
                if (drawer.IsDraw() == false)
                {
                    continue;
                }

                if (index > 0)
                {
                    AddPositionRowOffset();
                }

                if (drawer.IsReadOnly)
                {
                    SetDisabledGuiState();
                }

                drawer.OnDraw();
                ResetGuiState();
            }
        }

        private void AddPositionRowOffset()
        {
            var position = Position;
            position.y += AudioEditorStyles.LineHeight + AudioEditorStyles.VMargin;
            Position = position;
        }

        protected class Drawer
        {
            internal Action OnDraw { get; }

            internal Func<bool> IsDraw { get; }

            internal bool IsReadOnly { get; }

            internal Drawer(Action onDraw, Func<bool> isDraw = null, bool isReadOnly = false)
            {
                OnDraw = onDraw;
                IsDraw = isDraw ?? (() => true);
                IsReadOnly = isReadOnly;
            }
        }
    }
}

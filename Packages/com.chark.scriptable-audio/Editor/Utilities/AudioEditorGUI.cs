using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CHARK.ScriptableAudio.Editor.Utilities
{
    /// <summary>
    /// Utilities for drawing GUI.
    /// </summary>
    internal static class AudioEditorGUI
    {
        internal static GUIContent GetObjectContent(Object obj)
        {
            return EditorGUIUtility.ObjectContent(obj, obj.GetType());
        }

        internal static Vector2 GetScreenMousePosition()
        {
            var mousePosition = Event.current.mousePosition;
            return GUIUtility.GUIToScreenPoint(mousePosition);
        }

        internal static void IncrementIndent()
        {
            EditorGUI.indentLevel++;
        }

        internal static void DecrementIndent()
        {
            EditorGUI.indentLevel--;
        }

        internal static void BeginHorizontal()
        {
            EditorGUILayout.BeginHorizontal();
        }

        internal static void EndHorizontal()
        {
            EditorGUILayout.EndHorizontal();
        }

        internal static void BeginVertical()
        {
            EditorGUILayout.BeginVertical();
        }

        internal static void EndVertical()
        {
            EditorGUILayout.EndVertical();
        }

        internal static bool DrawFoldoutList(
            bool isExpanded,
            string label,
            string emptyLabel,
            IEnumerable<Object> objects,
            Action<Object> onAfterProperties = null
        )
        {
            var objectList = objects.ToList();
            var count = objectList.Count;

            var text = string.Format(AudioEditorStyles.ListLabelTemplate, label, count.ToString());
            var isNewExpanded = DrawFoldout(text, isExpanded);

            if (isNewExpanded == false)
            {
                return false;
            }

            if (count == 0)
            {
                HelpBoxInfo(emptyLabel);
            }
            else
            {
                BeginVertical();

                foreach (var obj in objectList)
                {
                    BeginHorizontal();
                    DrawObject(GUIContent.none, obj);
                    if (DrawButton(AudioEditorStyles.PropertiesIcon, "Open inspector window"))
                    {
                        PropertiesWindow.ShowProperties(obj);
                    }

                    onAfterProperties?.Invoke(obj);

                    EndHorizontal();
                }

                EndVertical();
            }

            return true;
        }

        internal static Object DrawObject(GUIContent content, Object obj, bool isAllowSceneObjects = false)
        {
            return EditorGUILayout.ObjectField(content, obj, obj.GetType(), isAllowSceneObjects);
        }

        internal static void HelpBoxInfo(string message)
        {
            EditorGUILayout.HelpBox(message, MessageType.Info);
        }

        internal static bool DrawFoldout(string label, bool isExpanded)
        {
            return DrawFoldout(new GUIContent(label), isExpanded);
        }

        internal static bool DrawFoldout(GUIContent label, bool isExpanded)
        {
            return EditorGUILayout.Foldout(isExpanded, label, true);
        }

        internal static string DrawText(string value)
        {
            return EditorGUILayout.TextField(value);
        }

        internal static bool DrawToggle(string label, bool isToggled, GUIStyle style)
        {
            return EditorGUILayout.Toggle(label, isToggled, style);
        }

        internal static bool DrawFoldout(Rect position, GUIContent label, bool isExpanded)
        {
            return EditorGUI.Foldout(position, isExpanded, label, true);
        }

        internal static string DrawText(Rect position, string value)
        {
            return EditorGUI.TextField(position, value);
        }

        internal static string DrawText(Rect position, string label, string value)
        {
            return EditorGUI.TextField(position, label, value);
        }

        internal static float DrawNumber(Rect position, string label, float value)
        {
            return EditorGUI.FloatField(position, label, value);
        }

        internal static int DrawNumber(Rect position, string label, int value)
        {
            return EditorGUI.IntField(position, label, value);
        }

        internal static bool DrawToggle(Rect position, string label, bool isToggled)
        {
            return EditorGUI.Toggle(position, label, isToggled);
        }

        internal static bool DrawButton(
            Texture icon,
            float width = AudioEditorStyles.ButtonWidth,
            float height = AudioEditorStyles.LineHeight
        )
        {
            return GUILayout.Button(
                new GUIContent(icon),
                GUILayout.Width(width),
                GUILayout.Height(height)
            );
        }

        internal static bool DrawButton(
            Texture icon,
            string tooltip,
            float width = AudioEditorStyles.ButtonWidth,
            float height = AudioEditorStyles.LineHeight
        )
        {
            return GUILayout.Button(
                new GUIContent(icon, tooltip),
                GUILayout.Width(width),
                GUILayout.Height(height)
            );
        }

        internal static bool DrawButton(GUIContent content, float width = AudioEditorStyles.ButtonWidth)
        {
            return GUILayout.Button(content, GUILayout.Width(width));
        }

        internal static bool DrawButton(Rect position, GUIContent content)
        {
            return GUI.Button(position, content);
        }

        internal static bool DrawToggleButton(
            Rect position,
            GUIContent content,
            bool isToggled,
            float width = AudioEditorStyles.ButtonWidth
        )
        {
            position.width = width;
            return GUI.Toggle(position, isToggled, content, EditorStyles.miniButton);
        }

        internal static void DrawProperty(Rect position, GUIContent label, SerializedProperty property)
        {
            EditorGUI.PropertyField(position, property, label);
        }

        internal static void DrawProperty(Rect position, SerializedProperty property)
        {
            EditorGUI.PropertyField(position, property);
        }
    }
}

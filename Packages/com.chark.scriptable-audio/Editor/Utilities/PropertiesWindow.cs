using UnityEditor;
using Object = UnityEngine.Object;

namespace CHARK.ScriptableAudio.Editor.Utilities
{
    internal sealed class PropertiesWindow : EditorWindow
    {
        private UnityEditor.Editor editor;

        public static void ShowProperties(Object propertiesObject)
        {
            var window = CreateWindow<PropertiesWindow>(propertiesObject.name);
            window.minSize = AudioEditorStyles.PropertiesWindowMinSize;
            window.editor = UnityEditor.Editor.CreateEditor(propertiesObject, null);
        }

        private void OnDisable()
        {
            if (editor)
            {
                DestroyImmediate(editor);
            }
        }

        private void OnGUI()
        {
            if (editor)
            {
                editor.DrawHeader();
                editor.OnInspectorGUI();
            }
        }
    }
}

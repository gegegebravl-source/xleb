// Stub: SettingsEditor moved due to Unity 6 TreeView deprecation
using UnityEditor;
namespace FMODUnity {
    public class SettingsEditor : EditorWindow {
        [MenuItem("FMOD/Settings")]
        public static void ShowWindow() {}
        public static void DisplayBankRefreshSettings(SerializedProperty cooldown, SerializedProperty showWindow, bool flag) {}
    }
}

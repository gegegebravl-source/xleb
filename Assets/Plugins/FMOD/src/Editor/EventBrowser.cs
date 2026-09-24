// Stub: EventBrowser moved due to Unity 6 TreeView deprecation
using UnityEditor;
using UnityEngine;
namespace FMODUnity {
    public class EventBrowser : EditorWindow {
        [MenuItem("FMOD/Event Browser")]
        public static void ShowWindow() {}
        public void ChooseBank(SerializedProperty property) {}
        public void ChooseEvent(SerializedProperty property) {}
        public void ChooseParameter(SerializedProperty property) {}
        public void FrameEvent(string path) {}
    }
}

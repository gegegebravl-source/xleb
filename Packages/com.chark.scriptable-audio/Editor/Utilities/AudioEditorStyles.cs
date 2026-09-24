using UnityEditor;
using UnityEngine;

namespace CHARK.ScriptableAudio.Editor.Utilities
{
    internal static class AudioEditorStyles
    {
        internal const float HMargin = 2f;

        internal const float VMargin = 2f;

        internal const float ButtonWidth = 30f;

        internal static readonly Vector2 PropertiesWindowMinSize = new Vector2(200f, 300f);

        internal const float LineHeight = 18f;

        internal static float LabelWidth => EditorGUIUtility.labelWidth;

        public const string ListLabelTemplate = "{0} ({1})";

        internal static readonly Color MinSphereColor = new Color(0.95f, 0.35f, 0.16f, 0.02f);

        internal static readonly Color MaxSphereColor = new Color(0.95f, 0.35f, 0.16f, 0.04f);

        internal static readonly Color MinWireColor = new Color(0.95f, 0.35f, 0.16f, 0.20f);

        internal static readonly Color MaxWireColor = new Color(0.95f, 0.35f, 0.16f, 0.40f);

        internal static readonly Color MinSphereOverrideColor = new Color(0.95f, 0.35f, 0.16f, 0.05f);

        internal static readonly Color MaxSphereOverrideColor = new Color(0.95f, 0.35f, 0.16f, 0.10f);

        internal static readonly Color MinWireOverrideColor = new Color(0.95f, 0.35f, 0.16f, 0.50f);

        internal static readonly Color MaxWireOverrideColor = new Color(0.95f, 0.35f, 0.16f, 1.00f);

        internal static readonly Color ErrorColor = new Color(0.82f, 0.35f, 0.26f);

        internal const string AudioEmitterGizmo = "AudioSource Gizmo";

        internal static readonly Color AudioEmitterGizmoColor = new Color(0.95f, 0.35f, 0.16f);

        internal static Texture SearchIcon
        {
            get
            {
                if (searchIcon == false)
                {
                    searchIcon = GetIcon("Search On Icon");
                }

                return searchIcon;
            }
        }

        internal static Texture PropertiesIcon
        {
            get
            {
                if (propertiesIcon == false)
                {
                    propertiesIcon = GetIcon("_Popup@2x");
                }

                return propertiesIcon;
            }
        }

        internal static Texture DetailsIcon
        {
            get
            {
                if (detailsIcon == false)
                {
                    detailsIcon = GetIcon("Profiler.UIDetails");
                }

                return detailsIcon;
            }
        }

        internal static Texture GlobeIcon
        {
            get
            {
                if (globeIcon == false)
                {
                    globeIcon = GetIcon("ToolHandleGlobal");
                }

                return globeIcon;
            }
        }

        public static Texture AudioParameterIcon
        {
            get
            {
                if (audioParameterIcon == false)
                {
                    audioParameterIcon = GetIcon("AudioMixerController Icon");
                }

                return audioParameterIcon;
            }
        }

        public static Texture AudioEventIcon
        {
            get
            {
                if (audioEventIcon == false)
                {
                    audioEventIcon = GetIcon("AudioSource Icon");
                }

                return audioEventIcon;
            }
        }

        public static Texture ErrorIcon
        {
            get
            {
                if (errorIcon == false)
                {
                    errorIcon = GetIcon("Error");
                }

                return errorIcon;
            }
        }

        public static Texture PlayIcon
        {
            get
            {
                if (playIcon == false)
                {
                    playIcon = GetIcon("PlayButton");
                }

                return playIcon;
            }
        }

        public static Texture StopIcon
        {
            get
            {
                if (stopIcon == false)
                {
                    stopIcon = GetIcon("PreMatQuad");
                }

                return stopIcon;
            }
        }

        private static Texture searchIcon;
        private static Texture propertiesIcon;
        private static Texture detailsIcon;
        private static Texture globeIcon;
        private static Texture audioParameterIcon;
        private static Texture audioEventIcon;
        private static Texture errorIcon;
        private static Texture playIcon;
        private static Texture stopIcon;

        private static Texture GetIcon(string name)
        {
            return EditorGUIUtility.IconContent(name)?.image;
        }
    }
}

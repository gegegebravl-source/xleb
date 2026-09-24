using CHARK.GameManagement;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Utilities
{
    /// <summary>
    /// Utilities for visualizing locations via OnDrawGizmos() or OnDrawGizmosSelected() Unity
    /// Lifecycle methods.
    /// </summary>
    public static class GizmoLocations
    {
        private const float DefaultLineWidth = 1f;
        private const float DefaultLineHeight = 150f;

        private const int DefaultTextSize = 12;
        private const int DefaultTextOffset = DefaultTextSize / 4;

        private static readonly Color BackgroundColor = new Color(0, 0, 0, 1f);

        private static Texture2D currentMarkerBackground;

        private static Texture2D MarkerBackground
        {
            get
            {
                if (currentMarkerBackground == null)
                {
                    currentMarkerBackground = new Texture2D(1, 1, TextureFormat.RGBAFloat, false);
                    currentMarkerBackground.SetPixel(0, 0, BackgroundColor);
                    currentMarkerBackground.Apply();
                }

                return currentMarkerBackground;
            }
        }

        public static bool IsDraw3DLabelGizmos
        {
            get
            {
                return true;

                // const string key = nameof(GizmoLocations) + "." + nameof(IsDraw3DLabelGizmos);
                // if (GameManager.TryReadEditorData(key, out bool value))
                // {
                //     return value;
                // }
                //
                // return false;
            }
            set
            {
                const string key = nameof(GizmoLocations) + "." + nameof(IsDraw3DLabelGizmos);
                GameManager.SaveEditorData(key, value);
            }
        }

        /// <summary>
        /// Draw a location marker line with a label.
        /// </summary>
        public static void DrawLocationMarker(
            Vector3 position,
            string label,
            float lineHeight = DefaultLineHeight,
            float lineWidth = DefaultLineWidth,
            float textOffset = DefaultTextOffset,
            int textSize = DefaultTextSize
        )
        {
            DrawLocationMarker(position, label, Color.white, lineHeight, lineWidth, textOffset, textSize);
        }

        /// <summary>
        /// Draw a location marker line with a label.
        /// </summary>
        public static void DrawLocationMarker(
            Vector3 position,
            string label,
            Color color,
            float lineHeight = DefaultLineHeight,
            float lineWidth = DefaultLineWidth,
            float textOffset = DefaultTextOffset,
            int textSize = DefaultTextSize
        )
        {
#if UNITY_EDITOR
            if (IsDraw3DLabelGizmos == false)
            {
                return;
            }

            // Line.
            var lineStartPosition = position;
            var lineEndPosition = lineStartPosition;
            lineEndPosition.y += lineHeight;

            var distanceToCamera = GetDistanceToCamera(lineEndPosition);

            // TODO: un-hard code distances
            var progress = Mathf.InverseLerp(200f, 100f, distanceToCamera);

            var fontSize = (int)(progress * textSize);
            if (fontSize <= 0)
            {
                return;
            }

            UnityEditor.Handles.color = BackgroundColor;
            UnityEditor.Handles.DrawLine(lineStartPosition, lineEndPosition, lineWidth);

            // Label.
            var labelPosition = lineEndPosition;
            labelPosition.y += textOffset;

            var labelStyle = new GUIStyle
            {
                fontStyle = FontStyle.Normal,
                fontSize = fontSize,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(2, 2, 2, 2),
                richText = true,
                normal =
                {
                    background = MarkerBackground,
                    textColor = color,
                },
            };

            UnityEditor.Handles.Label(labelPosition, label, labelStyle);
#endif
        }

        private static float GetDistanceToCamera(Vector3 labelPosition)
        {
#if UNITY_EDITOR
            var lastActiveSceneView = UnityEditor.SceneView.lastActiveSceneView;
            if (lastActiveSceneView == false)
            {
                return 0f;
            }

            var camera = lastActiveSceneView.camera;
            if (camera == false)
            {
                return 0f;
            }

            var cameraPosition = camera.transform.position;
            return Vector3.Distance(cameraPosition, labelPosition);
#else
            return 0f;
#endif
        }
    }
}

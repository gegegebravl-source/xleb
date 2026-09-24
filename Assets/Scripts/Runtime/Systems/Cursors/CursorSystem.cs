using System;
using CHARK.GameManagement.Systems;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Cursors
{
    internal sealed class CursorSystem : MonoSystem, ICursorSystem
    {
        // ReSharper disable once UnusedMember.Local
        [Header("Cursor (Web)")]
        [SerializeField]
        private Texture2D webCursorTexture;

        // ReSharper disable once UnusedMember.Local
        [SerializeField]
        private Vector2 cursorHotspotWeb = new(12f, 1f);

        // ReSharper disable once UnusedMember.Local
        [Header("Cursor (PC)")]
        [SerializeField]
        private Texture2D cursorTexture;

        // ReSharper disable once UnusedMember.Local
        [SerializeField]
        private Vector2 cursorHotspot = new(47f, 6f);

        public bool IsCursorLocked => Cursor.lockState != CursorLockMode.None;

        public override void OnInitialized()
        {
            base.OnInitialized();
            ApplyCursor();
        }

        private void ApplyCursor()
        {
            var cursor = ResolveCursorTexture();
            if (cursor == null)
            {
                return;
            }

            var hotspot = IsWebglCursor() ? cursorHotspotWeb : cursorHotspot;

            try
            {
                Cursor.SetCursor(cursor, hotspot, CursorMode.ForceSoftware);
                Cursor.visible = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Cursor] Failed to apply custom cursor: {ex.Message}");
            }
        }

        private bool IsWebglCursor()
        {
#if UNITY_WEBGL
            return true;
#else
            return false;
#endif
        }

        private Texture2D ResolveCursorTexture()
        {
            Texture2D result = null;

#if UNITY_WEBGL
            result = webCursorTexture;
#else
            result = cursorTexture;
#endif

            if (result == null)
            {
                return Texture2D.whiteTexture;
            }

            if (result.width <= 0 || result.height <= 0)
            {
                return Texture2D.whiteTexture;
            }

            var isValidTextureFormat = result.format == TextureFormat.RGBA32
                || result.format == TextureFormat.ARGB32
                || result.format == TextureFormat.RGBA4444
                || result.format == TextureFormat.RGBAFloat
                || result.format == TextureFormat.RGBAHalf;

            if (isValidTextureFormat == false)
            {
                return Texture2D.whiteTexture;
            }

            return result;
        }

        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void UnLockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}

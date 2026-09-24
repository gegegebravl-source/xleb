using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

namespace UABPetelnia.GGJ2025.Runtime.UI
{
    /// <summary>
    /// Controls a VideoPlayer as a looping background.
    /// Creates a dedicated Canvas + RawImage for video display behind UI.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    internal sealed class VideoBackgroundController : MonoBehaviour
    {
        [SerializeField]
        private VideoPlayer videoPlayer;

        private RenderTexture renderTexture;
        private Canvas videoCanvas;
        private RawImage videoRawImage;
        private GameObject videoContainer;

        private void Awake()
        {
            if (videoPlayer == null)
            {
                videoPlayer = GetComponent<VideoPlayer>();
            }
        }

        private void OnEnable()
        {
            if (videoPlayer == null)
            {
                return;
            }

            if (videoPlayer.targetCamera == null)
            {
                videoPlayer.targetCamera = Camera.main;
            }

            if (videoContainer == null)
            {
                SetupVideoDisplay();
            }

            if (renderTexture == null && videoRawImage != null)
            {
                var cam = videoPlayer.targetCamera;
                if (cam != null)
                {
                    renderTexture = new RenderTexture(
                        Mathf.Max(cam.pixelWidth, 1280),
                        Mathf.Max(cam.pixelHeight, 720),
                        0
                    );
                    videoPlayer.targetTexture = renderTexture;
                    videoRawImage.texture = renderTexture;
                }
            }

            videoPlayer.isLooping = true;
            videoPlayer.playOnAwake = true;
            videoPlayer.skipOnDrop = false;

            if (videoPlayer.isPrepared == false)
            {
                videoPlayer.Prepare();
            }

            if (videoPlayer.isPlaying == false)
            {
                videoPlayer.Play();
            }
        }

        private void OnDisable()
        {
            if (videoPlayer != null && videoPlayer.isPlaying)
            {
                videoPlayer.Pause();
            }
        }

        private void OnDestroy()
        {
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
                videoPlayer.targetTexture = null;
                videoPlayer.targetCamera = null;
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
                renderTexture = null;
            }

            if (videoRawImage != null)
            {
                videoRawImage.texture = null;
            }

            if (videoCanvas != null)
            {
                Destroy(videoCanvas.gameObject);
                videoCanvas = null;
            }

            videoRawImage = null;
            videoContainer = null;
        }

        private void SetupVideoDisplay()
        {
            if (videoContainer != null)
            {
                return;
            }

            videoContainer = new GameObject("VideoBackgroundCanvas");
            videoContainer.transform.SetParent(transform, false);

            videoCanvas = videoContainer.AddComponent<Canvas>();
            videoCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            videoCanvas.sortingOrder = -100;

            var scaler = videoContainer.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.referencePixelsPerUnit = 100;

            videoCanvas.overrideSorting = true;

            var vignette = new GameObject("VideoVignette");
            vignette.transform.SetParent(videoCanvas.transform, false);
            var vignetteImage = vignette.AddComponent<Image>();
            vignetteImage.color = new Color(0.06f, 0.03f, 0.02f, 0.38f);
            vignetteImage.raycastTarget = false;
            var vignetteRect = vignetteImage.rectTransform;
            vignetteRect.anchorMin = Vector2.zero;
            vignetteRect.anchorMax = Vector2.one;
            vignetteRect.offsetMin = Vector2.zero;
            vignetteRect.offsetMax = Vector2.zero;

            var rawImageGO = new GameObject("VideoRawImage");
            rawImageGO.transform.SetParent(videoCanvas.transform, false);

            videoRawImage = rawImageGO.AddComponent<RawImage>();
            videoRawImage.raycastTarget = false;
            videoRawImage.color = Color.white;

            var rt = rawImageGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}

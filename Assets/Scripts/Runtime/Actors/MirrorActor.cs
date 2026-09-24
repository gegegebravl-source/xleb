using CHARK.GameManagement;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UABPetelnia.GGJ2025.Runtime.Systems.Players;
using UnityEngine;
using UABPetelnia.GGJ2025.Runtime.Utilities;

namespace UABPetelnia.GGJ2025.Runtime.Actors
{
    /// <summary>
    /// The mirror on the kiosk wall. The little figure inside the frame mirrors the shopkeeper: it
    /// follows them left and right, is flipped the same way and changes together with their state.
    /// </summary>
    /// <remarks>
    /// This is the cheap 2.5D trick rather than a real reflection: the figure is a quad sitting a
    /// hair in front of the mirror picture, so it reads as being inside the frame.
    /// </remarks>
    internal sealed class MirrorActor : MonoBehaviour
    {
        private static readonly int TexturePropertyId = Shader.PropertyToID("_BaseMap");

        [Header("Parts")]
        [SerializeField]
        private Renderer reflectionRenderer;

        [SerializeField]
        private Transform reflectionTransform;

        [Header("Player art")]
        [SerializeField]
        private PlayerSettings settings;

        [SerializeField]
        private Texture2D fallbackTexture;

        [Header("Follow")]
        [Min(0.1f)]
        [SerializeField]
        private float followSpeed = 8f;

        [Tooltip("How far the reflection may slide inside the frame, in local units.")]
        [SerializeField]
        private Vector2 slideRange = new(0.18f, 0.05f);

        [Tooltip("How far in front of the mirror picture the reflection sits.")]
        [SerializeField]
        private float planeOffset = 0.01f;

        private IPlayerSystem playerSystem;
        private MaterialPropertyBlock block;
        private Texture2D currentTexture;

        /// <summary>
        /// Wire the parts and the player art from the editor tools.
        /// </summary>
        public void Initialize(
            Renderer renderer,
            Transform reflection,
            PlayerSettings playerSettings,
            Texture2D playerFallback
        )
        {
            reflectionRenderer = renderer;
            reflectionTransform = reflection;
            settings = playerSettings;
            fallbackTexture = playerFallback;
        }

        private void Awake()
        {
            block = new MaterialPropertyBlock();

            SystemsUtility.TryGetSystem(out playerSystem);
        }

        private void LateUpdate()
        {
            if (reflectionTransform == false || playerSystem == null)
            {
                return;
            }

            if (playerSystem.TryGetPlayer(out var player) == false || player is not Component component)
            {
                return;
            }

            var playerPosition = component.transform.position;
            var localPosition = transform.InverseTransformPoint(playerPosition);

            var target = new Vector3(
                Mathf.Clamp(localPosition.x, -slideRange.x, slideRange.x),
                Mathf.Clamp(localPosition.y * 0.4f, -slideRange.y, slideRange.y),
                -planeOffset
            );

            reflectionTransform.localPosition = Vector3.Lerp(
                reflectionTransform.localPosition,
                target,
                Time.deltaTime * followSpeed
            );

            // Mirrored picture: the art is flipped horizontally like a real reflection. Done once,
            // the scale is left alone afterwards.
            var scale = reflectionTransform.localScale;
            if (scale.x > 0f)
            {
                reflectionTransform.localScale = new Vector3(-scale.x, scale.y, scale.z);
            }

            SetTexture(GetPlayerTexture(player));
        }

        private Texture2D GetPlayerTexture(IPlayerActor player)
        {
            if (settings == false)
            {
                return fallbackTexture;
            }

            var texture = settings.GetHealthTexture(player.Health);

            return texture ? texture : fallbackTexture;
        }

        private void SetTexture(Texture2D texture)
        {
            if (reflectionRenderer == false || texture == false || texture == currentTexture)
            {
                return;
            }

            currentTexture = texture;

            reflectionRenderer.GetPropertyBlock(block);
            block.SetTexture(TexturePropertyId, texture);
            reflectionRenderer.SetPropertyBlock(block);
        }
    }
}

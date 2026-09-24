using System.Collections.Generic;
using UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactors;
using UABPetelnia.GGJ2025.Runtime.Constants;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Settings
{
    [CreateAssetMenu(
        fileName = CreateAssetMenuConstants.BaseFileName + nameof(PlayerSettings),
        menuName = CreateAssetMenuConstants.BaseMenuName + "/Player Settings",
        order = CreateAssetMenuConstants.BaseOrder
    )]
    internal sealed class PlayerSettings : ScriptableObject, IRaycastInteractorSettings
    {
        [Header("Camera")]
        [SerializeField]
        [Min(0f)]
        private float zoomInSpeed = 8f;

        [SerializeField]
        [Min(0f)]
        private float zoomInFov = 20f;

        [SerializeField]
        private Vector3 cameraShakeForce = new(0f, 0f, -1f);

        [Header("Interaction")]
        [SerializeField]
        private RaycastInteractorData data;

        [Header("Health")]
        [SerializeField]
        private List<Texture2D> healthStateTextures;

        [Header("Goals")]
        [SerializeField]
        [Min(0.1f)]
        private float moveSpeed = 5f;

        [SerializeField]
        private int goalCents = 100 * 100;

        public int MaxHealth => healthStateTextures?.Count ?? 0;

        public float ZoomInSpeed => IsFinite(zoomInSpeed) ? Mathf.Max(0f, zoomInSpeed) : 8f;

        public float ZoomInFov => IsFinite(zoomInFov) ? Mathf.Clamp(zoomInFov, 1f, 179f) : 20f;

        public float RaycastDistance => data != null ? data.RaycastDistance : 0f;

        public float RaycastRadius => data != null ? data.RaycastRadius : 0f;

        public LayerMask RaycastLayer => data != null ? data.RaycastLayer : default;

        public QueryTriggerInteraction QueryTriggerInteraction => data != null
            ? data.QueryTriggerInteraction
            : QueryTriggerInteraction.Ignore;

        public Color RaycastColor => data != null ? data.RaycastColor : Color.white;

        public Vector3 CameraShakeForce => cameraShakeForce;

        public float MoveSpeed => IsFinite(moveSpeed) ? Mathf.Max(0.1f, moveSpeed) : 5f;

        public int GoalCents => Mathf.Max(0, goalCents);

        private static bool IsFinite(float value)
        {
            return float.IsNaN(value) == false && float.IsInfinity(value) == false;
        }

        public Texture2D GetHealthTexture(int health)
        {
            if (health <= 0)
            {
                return default;
            }

            if (healthStateTextures == null || healthStateTextures.Count == 0)
            {
                return default;
            }

            var index = Mathf.Clamp(MaxHealth - health, 0, healthStateTextures.Count - 1);
            return healthStateTextures[index];
        }
    }
}

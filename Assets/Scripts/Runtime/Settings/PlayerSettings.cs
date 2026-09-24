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

        public int MaxHealth => healthStateTextures.Count;

        public float ZoomInSpeed => zoomInSpeed;

        public float ZoomInFov => zoomInFov;

        public float RaycastDistance => data.RaycastDistance;

        public float RaycastRadius => data.RaycastRadius;

        public LayerMask RaycastLayer => data.RaycastLayer;

        public QueryTriggerInteraction QueryTriggerInteraction => data.QueryTriggerInteraction;

        public Color RaycastColor => data.RaycastColor;

        public Vector3 CameraShakeForce => cameraShakeForce;

        public float MoveSpeed => moveSpeed;

        public int GoalCents => goalCents;

        public Texture2D GetHealthTexture(int health)
        {
            if (health <= 0)
            {
                return default;
            }

            return healthStateTextures[Mathf.Max(0, MaxHealth - health)];
        }
    }
}

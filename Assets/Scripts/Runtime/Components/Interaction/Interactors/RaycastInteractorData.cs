using System;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactors
{
    [Serializable]
    internal sealed class RaycastInteractorData : IRaycastInteractorSettings
    {
        [Min(0f)]
        [SerializeField]
        private float raycastDistance = 1.5f;

        [SerializeField]
        private float raycastRadius = 0.1f;

        [SerializeField]
        private LayerMask raycastLayer;

        [SerializeField]
        private QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore;

        [SerializeField]
        private Color raycastColor = Color.red;

        public float RaycastDistance => raycastDistance;

        public float RaycastRadius => raycastRadius;

        public LayerMask RaycastLayer => raycastLayer;

        public QueryTriggerInteraction QueryTriggerInteraction => queryTriggerInteraction;

        public Color RaycastColor => raycastColor;
    }
}

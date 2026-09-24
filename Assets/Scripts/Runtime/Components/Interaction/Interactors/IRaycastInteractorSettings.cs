using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactors
{
    internal interface IRaycastInteractorSettings
    {
        public float RaycastDistance { get; }

        public float RaycastRadius { get; }

        public LayerMask RaycastLayer { get; }

        public QueryTriggerInteraction QueryTriggerInteraction { get; }

        public Color RaycastColor { get; }
    }
}

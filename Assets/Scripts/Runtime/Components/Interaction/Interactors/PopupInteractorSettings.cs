using UABPetelnia.GGJ2025.Runtime.Constants;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactors
{
    [CreateAssetMenu(
        fileName = CreateAssetMenuConstants.BaseFileName + nameof(PopupInteractorSettings),
        menuName = CreateAssetMenuConstants.BaseMenuName + "/Interaction/Popup Interactor Settings",
        order = CreateAssetMenuConstants.BaseOrder
    )]
    internal sealed class PopupInteractorSettings : ScriptableObject, IRaycastInteractorSettings
    {
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.FoldoutGroup("Raycast", Expanded = true)]
        [Sirenix.OdinInspector.InlineProperty]
        [Sirenix.OdinInspector.HideLabel]
#else
        [Header("Raycast")]
#endif
        [SerializeField]
        private RaycastInteractorData data;

        public float RaycastDistance => data.RaycastDistance;

        public float RaycastRadius => data.RaycastRadius;

        public LayerMask RaycastLayer => data.RaycastLayer;

        public QueryTriggerInteraction QueryTriggerInteraction => data.QueryTriggerInteraction;

        public Color RaycastColor => data.RaycastColor;
    }
}

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

        public float RaycastDistance => data != null ? data.RaycastDistance : 0f;

        public float RaycastRadius => data != null ? data.RaycastRadius : 0f;

        public LayerMask RaycastLayer => data != null ? data.RaycastLayer : default;

        public QueryTriggerInteraction QueryTriggerInteraction => data != null
            ? data.QueryTriggerInteraction
            : QueryTriggerInteraction.Ignore;

        public Color RaycastColor => data != null ? data.RaycastColor : Color.white;
    }
}

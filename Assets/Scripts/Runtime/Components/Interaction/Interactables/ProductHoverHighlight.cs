using CHARK.GameManagement;
using UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactables;
using UABPetelnia.GGJ2025.Runtime.Systems.Interaction;
using UnityEngine;
using UABPetelnia.GGJ2025.Runtime.Utilities;

namespace UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactables
{
    /// <summary>
    /// Показывает белую обводку, пока интерактор наведён на этот товар.
    /// Вешается на корень Actor_Product, обводка — выключенный дочерний квад.
    /// </summary>
    internal sealed class ProductHoverHighlight : MonoBehaviour
    {
        [Header("General")]
        [SerializeField]
        private GameObject outlineVisual;

        private void OnEnable()
        {
            SystemsUtility.TryAddListener<InteractorHoveredEnteredMessage>(OnHoverEntered);
            SystemsUtility.TryAddListener<InteractorHoveredExitedMessage>(OnHoverExited);

            // Взятый в руку товар не должен светиться рамкой: рамка — только признак наведения.
            var interactable = GetComponentInChildren<Interactable>(true);
            if (interactable != null)
            {
                interactable.OnSelectEntered += OnInteractableSelected;
                interactable.OnSelectExited += OnInteractableDeselected;
            }

            SetVisible(false);
        }

        private void OnDisable()
        {
            SystemsUtility.TryRemoveListener<InteractorHoveredEnteredMessage>(OnHoverEntered);
            SystemsUtility.TryRemoveListener<InteractorHoveredExitedMessage>(OnHoverExited);

            var interactable = GetComponentInChildren<Interactable>(true);
            if (interactable != null)
            {
                interactable.OnSelectEntered -= OnInteractableSelected;
                interactable.OnSelectExited -= OnInteractableDeselected;
            }
        }

        private void OnInteractableSelected(InteractableSelectEnteredArgs args)
        {
            SetVisible(false);
        }

        private void OnInteractableDeselected(InteractableSelectExitedArgs args)
        {
            SetVisible(false);
        }

        private void OnHoverEntered(InteractorHoveredEnteredMessage message)
        {
            if (IsOwnInteractable(message.Interactable) && message.Interactable.IsSelected == false)
            {
                SetVisible(true);
            }
        }

        private void OnHoverExited(InteractorHoveredExitedMessage message)
        {
            if (IsOwnInteractable(message.Interactable))
            {
                SetVisible(false);
            }
        }

        private bool IsOwnInteractable(IInteractable interactable)
        {
            return interactable is Component component
                && component.transform.IsChildOf(transform);
        }

        private void SetVisible(bool visible)
        {
            if (outlineVisual != false && outlineVisual.activeSelf != visible)
            {
                outlineVisual.SetActive(visible);
            }
        }
    }
}

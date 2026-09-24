using System.Collections.Generic;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactables
{
    internal sealed class InteractableDebugger : MonoBehaviour
    {
        private readonly List<IInteractable> interactables = new();

        private void Awake()
        {
            interactables.AddRange(GetComponentsInChildren<IInteractable>());
        }

        private void OnEnable()
        {
            foreach (var interactable in interactables)
            {
                interactable.OnHoverEntered += OnHoverEntered;
                interactable.OnHoverExited += OnHoverExited;
                interactable.OnSelectEntered += OnSelectEntered;
                interactable.OnSelectExited += OnSelectExited;
            }
        }

        private void OnDisable()
        {
            foreach (var interactable in interactables)
            {
                interactable.OnHoverEntered -= OnHoverEntered;
                interactable.OnHoverExited -= OnHoverExited;
                interactable.OnSelectEntered -= OnSelectEntered;
                interactable.OnSelectExited -= OnSelectExited;
            }
        }

        private void OnHoverEntered(InteractableHoverEnteredArgs args)
        {
            Debug.Log($"{name}: {nameof(OnHoverEntered)} by {args.Interactor.Name}", this);
        }

        private void OnHoverExited(InteractableHoverExitedArgs args)
        {
            Debug.Log($"{name}: {nameof(OnHoverExited)} by {args.Interactor.Name}", this);
        }

        private void OnSelectEntered(InteractableSelectEnteredArgs args)
        {
            Debug.Log($"{name}: {nameof(OnSelectEntered)} by {args.Interactor.Name}", this);
        }

        private void OnSelectExited(InteractableSelectExitedArgs args)
        {
            Debug.Log($"{name}: {nameof(OnSelectExited)} by {args.Interactor.Name}", this);
        }
    }
}

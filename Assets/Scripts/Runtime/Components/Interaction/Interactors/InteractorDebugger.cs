using System.Collections.Generic;
using UABPetelnia.GGJ2025.Runtime.Systems.Interaction;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactors
{
    internal sealed class InteractorDebugger : MonoBehaviour
    {
        private readonly List<IInteractor> interactors = new();

        private void Awake()
        {
            interactors.AddRange(GetComponentsInChildren<IInteractor>());
        }

        private void OnEnable()
        {
            foreach (var interactor in interactors)
            {
                interactor.OnHoverEntered += OnHoverEntered;
                interactor.OnHoverExited += OnHoverExited;
                interactor.OnSelectEntered += OnSelectEntered;
                interactor.OnSelectExited += OnSelectExited;
            }
        }

        private void OnDisable()
        {
            foreach (var interactor in interactors)
            {
                interactor.OnHoverEntered -= OnHoverEntered;
                interactor.OnHoverExited -= OnHoverExited;
                interactor.OnSelectEntered -= OnSelectEntered;
                interactor.OnSelectExited -= OnSelectExited;
            }
        }

        private void OnHoverEntered(InteractorHoverEnteredArgs args)
        {
            Debug.Log($"{name}: {nameof(OnHoverEntered)} by {args.Interactable.Name}", this);
        }

        private void OnHoverExited(InteractorHoverExitedArgs args)
        {
            Debug.Log($"{name}: {nameof(OnHoverExited)} by {args.Interactable.Name}", this);
        }

        private void OnSelectEntered(InteractorSelectEnteredArgs args)
        {
            Debug.Log($"{name}: {nameof(OnSelectEntered)} by {args.Interactable.Name}", this);
        }

        private void OnSelectExited(InteractorSelectExitedArgs args)
        {
            Debug.Log($"{name}: {nameof(OnSelectExited)} by {args.Interactable.Name}", this);
        }
    }
}

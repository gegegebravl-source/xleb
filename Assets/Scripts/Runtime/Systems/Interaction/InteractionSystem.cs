using System.Collections.Generic;
using CHARK.GameManagement;
using CHARK.GameManagement.Systems;
using UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactors;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Interaction
{
    internal sealed class InteractionSystem : SimpleSystem, IInteractionSystem
    {
        public IReadOnlyList<IInteractor> Interactors => interactors;

        private readonly List<IInteractor> interactors = new();

        public void AddInteractor(IInteractor interactor)
        {
            if (interactors.Contains(interactor))
            {
                return;
            }

            interactors.Add(interactor);

            interactor.OnHoverEntered += OnHoverEntered;
            interactor.OnHoverExited += OnHoverExited;
        }

        public void RemoveInteractor(IInteractor interactor)
        {
            if (interactors.Remove(interactor) == false)
            {
                return;
            }

            interactor.OnHoverEntered -= OnHoverEntered;
            interactor.OnHoverExited -= OnHoverExited;
        }

        private static void OnHoverEntered(InteractorHoverEnteredArgs args)
        {
            var message = new InteractorHoveredEnteredMessage(
                args.Interactable,
                args.Interactor
            );

            GameManager.Publish(message);
        }

        private static void OnHoverExited(InteractorHoverExitedArgs args)
        {
            var message = new InteractorHoveredExitedMessage(
                args.Interactable,
                args.Interactor
            );

            GameManager.Publish(message);
        }
    }
}

using CHARK.GameManagement.Messaging;
using UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactables;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Interaction
{
    internal readonly struct InteractorHoveredEnteredMessage : IMessage
    {
        public IInteractable Interactable { get; }

        public IInteractor Interactor { get; }

        public InteractorHoveredEnteredMessage(IInteractable interactable, IInteractor interactor)
        {
            Interactable = interactable;
            Interactor = interactor;
        }
    }

    internal readonly struct InteractorHoveredExitedMessage : IMessage
    {
        public IInteractable Interactable { get; }

        public IInteractor Interactor { get; }

        public InteractorHoveredExitedMessage(IInteractable interactable, IInteractor interactor)
        {
            Interactable = interactable;
            Interactor = interactor;
        }
    }
}

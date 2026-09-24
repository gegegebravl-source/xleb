using UABPetelnia.GGJ2025.Runtime.Systems.Interaction;

namespace UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactables
{
    internal readonly struct InteractableHoverEnteredArgs : IInteractableEventArgs
    {
        public IInteractor Interactor { get; }

        public InteractableHoverEnteredArgs(IInteractor interactor)
        {
            Interactor = interactor;
        }
    }

    internal readonly struct InteractableHoverExitedArgs : IInteractableEventArgs
    {
        public IInteractor Interactor { get; }

        public InteractableHoverExitedArgs(IInteractor interactor)
        {
            Interactor = interactor;
        }
    }

    internal readonly struct InteractableSelectEnteredArgs : IInteractableEventArgs
    {
        public IInteractor Interactor { get; }

        public InteractableSelectEnteredArgs(IInteractor interactor)
        {
            Interactor = interactor;
        }
    }

    internal readonly struct InteractableSelectExitedArgs : IInteractableEventArgs
    {
        public IInteractor Interactor { get; }

        public InteractableSelectExitedArgs(IInteractor interactor)
        {
            Interactor = interactor;
        }
    }

    internal interface IInteractableEventArgs
    {
        public IInteractor Interactor { get; }
    }
}

using UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactables;
using UABPetelnia.GGJ2025.Runtime.Systems.Interaction;

namespace UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactors
{
    internal readonly struct InteractorHoverEnteredArgs : IInteractorEventArgs
    {
        public IInteractable Interactable { get; }

        public IInteractor Interactor { get; }

        public InteractorHoverEnteredArgs(IInteractable interactable, IInteractor interactor)
        {
            Interactable = interactable;
            Interactor = interactor;
        }
    }

    internal readonly struct InteractorHoverExitedArgs : IInteractorEventArgs
    {
        public IInteractable Interactable { get; }

        public IInteractor Interactor { get; }

        public InteractorHoverExitedArgs(IInteractable interactable, IInteractor interactor)
        {
            Interactable = interactable;
            Interactor = interactor;
        }
    }

    internal readonly struct InteractorSelectEnteredArgs : IInteractorEventArgs
    {
        public IInteractable Interactable { get; }

        public InteractorSelectEnteredArgs(IInteractable interactable)
        {
            Interactable = interactable;
        }
    }

    internal readonly struct InteractorSelectExitedArgs : IInteractorEventArgs
    {
        public IInteractable Interactable { get; }

        public InteractorSelectExitedArgs(IInteractable interactable)
        {
            Interactable = interactable;
        }
    }

    internal interface IInteractorEventArgs
    {
        public IInteractable Interactable { get; }
    }
}

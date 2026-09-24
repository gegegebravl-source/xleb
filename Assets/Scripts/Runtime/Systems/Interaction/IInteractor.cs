using System;
using UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactables;
using UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactors;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Interaction
{
    internal interface IInteractor
    {
        /// <summary>
        /// Name of this interactor.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// <c>true</c> if at least one <see cref="Interactable"/> is hovered or <c>false</c>
        /// otherwise.
        /// </summary>
        public bool IsHovering { get; }

        /// <summary>
        /// <c>true</c> if at least one <see cref="Interactable"/> is selected or <c>false</c>
        /// otherwise.
        /// </summary>
        public bool IsSelecting { get; }

        /// <summary>
        /// Invoked when an interactable is hovered by this interactable.
        /// </summary>
        public event Action<InteractorHoverEnteredArgs> OnHoverEntered;

        /// <summary>
        /// Invoked when an interactable is stops being hovered by this interactable.
        /// </summary>
        public event Action<InteractorHoverExitedArgs> OnHoverExited;

        /// <summary>
        /// Invoked when an interactable is selected by this interactable.
        /// </summary>
        public event Action<InteractorSelectEnteredArgs> OnSelectEntered;

        /// <summary>
        /// Invoked when an interactable is stops being selected by this interactable.
        /// </summary>
        public event Action<InteractorSelectExitedArgs> OnSelectExited;

        /// <summary>
        /// Select all currently hovered interactables.
        /// </summary>
        public void Select();

        /// <summary>
        /// Deselect given <paramref name="interactable"/>.
        /// </summary>
        public void Deselect(IInteractable interactable);

        /// <summary>
        /// Deselect all currently selected interactables.
        /// </summary>
        public void Deselect();
    }
}

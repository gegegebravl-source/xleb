using System;
using UABPetelnia.GGJ2025.Runtime.Systems.Interaction;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactables
{
    internal interface IInteractable
    {
        /// <summary>
        /// Name of this interactable.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// World-space position of this interactable.
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// World-space rotation of this interactable.
        /// </summary>
        public Quaternion Rotation { get; set; }

        /// <summary>
        /// Local scale of this interactable.
        /// </summary>
        public Vector3 LocalScale { get; set; }

        /// <summary>
        /// <c>true</c> if this interactor is hovered or <c>false</c> otherwise.
        /// </summary>
        public bool IsHovered { get; }

        /// <summary>
        /// <c>true</c> if this interactor is selected or <c>false</c> otherwise.
        /// </summary>
        public bool IsSelected { get; }

        /// <summary>
        /// <c>true</c> if this interactable is enabled or <c>false</c> otherwise.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Invoked when this interactable gets hovered by an interactor.
        /// </summary>
        public event Action<InteractableHoverEnteredArgs> OnHoverEntered;

        /// <summary>
        /// Invoked when this interactable stops being hovered by an interactor.
        /// </summary>
        public event Action<InteractableHoverExitedArgs> OnHoverExited;

        /// <summary>
        /// Invoked when this interactable gets selected.
        /// </summary>
        public event Action<InteractableSelectEnteredArgs> OnSelectEntered;

        /// <summary>
        /// Invoked when this interactable stops being selected.
        /// </summary>
        public event Action<InteractableSelectExitedArgs> OnSelectExited;

        /// <summary>
        /// Hover this interactable.
        /// </summary>
        public void Hover(IInteractor interactor);

        /// <summary>
        /// Un-hover this interactable.
        /// </summary>
        public void UnHover(IInteractor interactor);

        /// <summary>
        /// Un-hover this interactable.
        /// </summary>
        public void UnHover();

        /// <summary>
        /// Select this interactable.
        /// </summary>
        public void Select(IInteractor interactor);

        /// <summary>
        /// Deselect this interactable.
        /// </summary>
        public void Deselect(IInteractor interactor);

        /// <summary>
        /// Deselect this interactable.
        /// </summary>
        public void Deselect();
    }
}

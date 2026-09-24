using System;
using System.Collections.Generic;
using CHARK.GameManagement;
using UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactables;
using UABPetelnia.GGJ2025.Runtime.Systems.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactors
{
    internal abstract class Interactor : MonoBehaviour, IInteractor
    {
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.FoldoutGroup("Events")]
        [Sirenix.OdinInspector.PropertyOrder(float.MaxValue)]
#else
        [Header("Events")]
#endif
        [SerializeField]
        public UnityEvent<InteractorHoverEnteredArgs> onHoverEntered;

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.FoldoutGroup("Events")]
        [Sirenix.OdinInspector.PropertyOrder(float.MaxValue)]
#endif
        [SerializeField]
        public UnityEvent<InteractorHoverExitedArgs> onHoverExited;

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.FoldoutGroup("Events")]
        [Sirenix.OdinInspector.PropertyOrder(float.MaxValue)]
#endif
        [SerializeField]
        public UnityEvent<InteractorSelectEnteredArgs> onSelectEntered;

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.FoldoutGroup("Events")]
        [Sirenix.OdinInspector.PropertyOrder(float.MaxValue)]
#endif
        [SerializeField]
        public UnityEvent<InteractorSelectExitedArgs> onSelectExited;

        [Header("Selection")]
        [SerializeField]
        [Min(1)]
        private int maxSelectedInteractables = int.MaxValue;

        public int MaxSelectedInteractables
        {
            get => maxSelectedInteractables;
            set => maxSelectedInteractables = Mathf.Max(1, value);
        }

        private IInteractionSystem interactionSystem;

        private readonly List<IInteractable> hoveredInteractables = new();
        private readonly List<IInteractable> selectedInteractables = new();

        public string Name => name;

        public bool IsHovering => hoveredInteractables.Count > 0;

        public bool IsSelecting => selectedInteractables.Count > 0;

        protected IEnumerable<IInteractable> HoveredInteractables => hoveredInteractables;

        /// <summary>
        /// Interactables this interactor currently holds.
        /// </summary>
        public IReadOnlyList<IInteractable> SelectedInteractables => selectedInteractables;

        public event Action<InteractorHoverEnteredArgs> OnHoverEntered;

        public event Action<InteractorHoverExitedArgs> OnHoverExited;

        public event Action<InteractorSelectEnteredArgs> OnSelectEntered;

        public event Action<InteractorSelectExitedArgs> OnSelectExited;

        private void Awake()
        {
            interactionSystem = GameManager.GetSystem<IInteractionSystem>();
        }

        private void OnEnable()
        {
            interactionSystem?.AddInteractor(this);
        }

        private void OnDisable()
        {
            UnHover();
            Deselect();

            interactionSystem?.RemoveInteractor(this);
        }

        protected void FixedUpdate()
        {
            OnPhysicsUpdated();
        }

        protected void Update()
        {
            OnUpdated();
        }

        protected void LateUpdate()
        {
            OnLateUpdated();
        }

        protected virtual void OnPhysicsUpdated()
        {
        }

        protected virtual void OnUpdated()
        {
        }

        protected virtual void OnLateUpdated()
        {
        }

        public void Select()
        {
            if (maxSelectedInteractables <= 0 || hoveredInteractables.Count == 0)
            {
                return;
            }

            var freeSlots = maxSelectedInteractables - selectedInteractables.Count;
            if (freeSlots <= 0)
            {
                return;
            }

            foreach (var hoveredInteractable in hoveredInteractables)
            {
                if (freeSlots <= 0)
                {
                    break;
                }

                if (IsSelected(hoveredInteractable))
                {
                    continue;
                }

                selectedInteractables.Add(hoveredInteractable);
                freeSlots--;

                hoveredInteractable.UnHover(this);

                var exitedArgs = new InteractorHoverExitedArgs(hoveredInteractable, this);
                OnHoverExited?.Invoke(exitedArgs);
                onHoverExited?.Invoke(exitedArgs);

                hoveredInteractable.Select(this);

                var enteredArgs = new InteractorSelectEnteredArgs(hoveredInteractable);
                OnSelectEntered?.Invoke(enteredArgs);
                onSelectEntered?.Invoke(enteredArgs);
            }

            hoveredInteractables.Clear();
        }

        /// <summary>
        /// Force-add an interactable to the selection without hovering it first. Inventory slots
        /// use this to pull their product straight into the hand.
        /// </summary>
        protected bool TrySelectForced(IInteractable interactable)
        {
            if (interactable == null || IsSelected(interactable))
            {
                return false;
            }

            if (selectedInteractables.Count >= maxSelectedInteractables)
            {
                return false;
            }

            // Снимаем наведение перед захватом: иначе подсказка «E — взять» остаётся
            // висеть и уезжает в руку вместе с предметом.
            if (IsHovered(interactable))
            {
                hoveredInteractables.Remove(interactable);
                interactable.UnHover(this);

                var hoverExitedArgs = new InteractorHoverExitedArgs(interactable, this);
                OnHoverExited?.Invoke(hoverExitedArgs);
                onHoverExited?.Invoke(hoverExitedArgs);
            }

            selectedInteractables.Add(interactable);
            interactable.Select(this);

            var args = new InteractorSelectEnteredArgs(interactable);
            OnSelectEntered?.Invoke(args);
            onSelectEntered?.Invoke(args);

            return true;
        }

        public void Deselect(IInteractable interactable)
        {
            if (IsSelected(interactable) == false)
            {
                return;
            }

            selectedInteractables.Remove(interactable);

            interactable.Deselect(this);

            var args = new InteractorSelectExitedArgs(interactable);
            OnSelectExited?.Invoke(args);
            onSelectExited?.Invoke(args);
        }

        public void Deselect()
        {
            if (selectedInteractables.Count == 0)
            {
                return;
            }

            foreach (var selectedInteractable in selectedInteractables)
            {
                selectedInteractable.Deselect(this);

                var args = new InteractorSelectExitedArgs(selectedInteractable);
                OnSelectExited?.Invoke(args);
                onSelectExited?.Invoke(args);
            }

            selectedInteractables.Clear();
        }

        /// <summary>
        /// Add given <paramref name="interactable"/> to <see cref="hoveredInteractables"/> list.
        /// </summary>
        protected void Hover(IInteractable interactable)
        {
            if (IsHovered(interactable) || IsSelected(interactable))
            {
                return;
            }

            hoveredInteractables.Add(interactable);
            interactable.Hover(this);

            var args = new InteractorHoverEnteredArgs(interactable, this);
            OnHoverEntered?.Invoke(args);
            onHoverEntered?.Invoke(args);
        }

        /// <summary>
        /// Clear <see cref="hoveredInteractables"/> list and call
        /// <see cref="UnHover"/> on each.
        /// </summary>
        protected void UnHover()
        {
            if (hoveredInteractables.Count == 0)
            {
                return;
            }

            foreach (var hoveredInteractable in hoveredInteractables)
            {
                hoveredInteractable.UnHover(this);

                var args = new InteractorHoverExitedArgs(hoveredInteractable, this);
                OnHoverExited?.Invoke(args);
                onHoverExited?.Invoke(args);
            }

            hoveredInteractables.Clear();
        }

        /// <returns>
        /// <c>true</c> if <paramref name="interactable"/> is being hovered by this interactor or
        /// <c>false</c> otherwise.
        /// </returns>
        protected bool IsHovered(IInteractable interactable)
        {
            return hoveredInteractables.Contains(interactable);
        }

        /// <returns>
        /// <c>true</c> if <paramref name="interactable"/> is being selected by this interactor or
        /// <c>false</c> otherwise.
        /// </returns>
        protected bool IsSelected(IInteractable interactable)
        {
            return selectedInteractables.Contains(interactable);
        }
    }
}

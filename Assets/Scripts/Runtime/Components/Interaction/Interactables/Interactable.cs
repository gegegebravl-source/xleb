using System;
using System.Collections.Generic;
using UABPetelnia.GGJ2025.Runtime.Systems.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactables
{
    [SelectionBase]
    internal abstract class Interactable : MonoBehaviour, IInteractable
    {
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.FoldoutGroup("Events")]
        [Sirenix.OdinInspector.PropertyOrder(float.MaxValue)]
#else
        [Header("Events")]
#endif
        [SerializeField]
        public UnityEvent<InteractableHoverEnteredArgs> onHoverEntered;

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.FoldoutGroup("Events")]
        [Sirenix.OdinInspector.PropertyOrder(float.MaxValue)]
#endif
        [SerializeField]
        public UnityEvent<InteractableHoverExitedArgs> onHoverExited;

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.FoldoutGroup("Events")]
        [Sirenix.OdinInspector.PropertyOrder(float.MaxValue)]
#endif
        [SerializeField]
        public UnityEvent<InteractableSelectEnteredArgs> onSelectEntered;

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.FoldoutGroup("Events")]
        [Sirenix.OdinInspector.PropertyOrder(float.MaxValue)]
#endif
        [SerializeField]
        public UnityEvent<InteractableSelectExitedArgs> onSelectExited;

        private readonly List<IInteractor> hoveringInteractors = new();
        private readonly List<IInteractor> selectingInteractors = new();

        public bool IsHovered => hoveringInteractors.Count > 0;

        public bool IsSelected => selectingInteractors.Count > 0;

        public bool IsEnabled
        {
            get => enabled && gameObject.activeInHierarchy;
            set => enabled = value;
        }

        public string Name => name;

        public virtual Vector3 Position
        {
            get => transform.position;
            set => transform.position = value;
        }

        public virtual Quaternion Rotation
        {
            get => transform.rotation;
            set => transform.rotation = value;
        }

        public virtual Vector3 LocalScale
        {
            get => transform.localScale;
            set => transform.localScale = value;
        }

        public event Action<InteractableHoverEnteredArgs> OnHoverEntered;

        public event Action<InteractableHoverExitedArgs> OnHoverExited;

        public event Action<InteractableSelectEnteredArgs> OnSelectEntered;

        public event Action<InteractableSelectExitedArgs> OnSelectExited;

        private void OnDisable()
        {
            UnHover();
            Deselect();
        }

        public void Hover(IInteractor interactor)
        {
            if (IsHoveredBy(interactor))
            {
                return;
            }

            hoveringInteractors.Add(interactor);

            var args = new InteractableHoverEnteredArgs(interactor);
            OnHoverEntered?.Invoke(args);
            onHoverEntered.Invoke(args);
        }

        public void UnHover(IInteractor interactor)
        {
            if (IsHoveredBy(interactor) == false)
            {
                return;
            }

            hoveringInteractors.Remove(interactor);

            var args = new InteractableHoverExitedArgs(interactor);
            OnHoverExited?.Invoke(args);
            onHoverExited.Invoke(args);
        }

        public void UnHover()
        {
            if (IsHovered == false)
            {
                return;
            }

            for (var index = hoveringInteractors.Count - 1; index >= 0; index--)
            {
                var interactor = hoveringInteractors[index];
                UnHover(interactor);
            }
        }

        public void Select(IInteractor interactor)
        {
            if (IsSelectedBy(interactor))
            {
                return;
            }

            selectingInteractors.Add(interactor);

            var args = new InteractableSelectEnteredArgs(interactor);
            OnSelectEntered?.Invoke(args);
            onSelectEntered.Invoke(args);
        }

        public void Deselect(IInteractor interactor)
        {
            if (IsSelectedBy(interactor) == false)
            {
                return;
            }

            selectingInteractors.Remove(interactor);

            var args = new InteractableSelectExitedArgs(interactor);
            OnSelectExited?.Invoke(args);
            onSelectExited.Invoke(args);
        }

        public void Deselect()
        {
            if (IsSelected == false)
            {
                return;
            }

            for (var index = selectingInteractors.Count - 1; index >= 0; index--)
            {
                var selectingInteractor = selectingInteractors[index];
                selectingInteractor.Deselect(this);
            }
        }

        private bool IsHoveredBy(IInteractor interactor)
        {
            return hoveringInteractors.Contains(interactor);
        }

        private bool IsSelectedBy(IInteractor interactor)
        {
            return selectingInteractors.Contains(interactor);
        }
    }
}

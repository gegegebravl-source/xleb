﻿using System.Linq;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactables;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactors
{
    /// <summary>
    /// Picks physical objects up and carries them in the player's hand. Used to grab products from
    /// the kiosk shelves before handing them over to a shopper.
    /// </summary>
    internal sealed class GrabInteractor : RaycastInteractor
    {
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.FoldoutGroup("General", Expanded = true)]
        [Sirenix.OdinInspector.Required]
#else
        [Header("General")]
#endif
        [SerializeField]
        private PlayerSettings settings;

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.FoldoutGroup("Transforms", Expanded = true)]
#else
        [Header("Transforms")]
#endif
        [SerializeField]
        private Transform grabTransform;

        [Header("Carry")]
        [SerializeField]
        [Min(1)]
        private int maxHeldInteractables = 2;

        [Min(0f)]
        [SerializeField]
        private float interactableFollowSpeed = 6f;

        private void Reset()
        {
            MaxSelectedInteractables = maxHeldInteractables;
        }

        private void OnValidate()
        {
            MaxSelectedInteractables = maxHeldInteractables;
        }

        protected override IRaycastInteractorSettings Settings => settings;

        /// <summary>
        /// Object currently carried in the hand, if any.
        /// </summary>
        public GrabInteractable HeldInteractable =>
            SelectedInteractables.OfType<GrabInteractable>().FirstOrDefault();

        /// <summary>
        /// Насколько далеко от руки висит взятый предмет. Колесо мыши двигает его
        /// к себе и от себя, чтобы удобнее было целиться в полку.
        /// </summary>
        public float CarryDistance { get; set; }

        /// <summary>
        /// Product currently under the crosshair, if any.
        /// </summary>
        public ProductActor HoveredProduct =>
            HoveredInteractables.OfType<GrabInteractable>().FirstOrDefault()
                ?.GetComponentInParent<ProductActor>();

        /// <summary>
        /// Takes an interactable into the hand without hovering it first. Inventory slots use this
        /// to pull their product back into the player's hand.
        /// </summary>
        public bool TryForceSelect(GrabInteractable interactable)
        {
            if (TrySelectForced(interactable) == false)
            {
                return false;
            }

            // Snap into the hand right away, so the product does not flash on its shelf for a frame.
            var target = GrabTransform;
            interactable.Position = target.position;
            interactable.Rotation = target.rotation;

            return true;
        }

        /// <summary>
        /// Where grabbed objects are carried. Falls back to the interactor itself so grabbing
        /// still works in scenes that were not set up with a dedicated hand transform.
        /// </summary>
        private Transform GrabTransform => grabTransform ? grabTransform : transform;

        protected override bool IsValid(IInteractable interactable)
        {
            return interactable is GrabInteractable;
        }

        protected override void OnLateUpdated()
        {
            UpdateSelection();
        }

        private void UpdateSelection()
        {
            if (IsSelecting == false)
            {
                return;
            }

            var targetTransform = GrabTransform;
            var targetPosition = targetTransform.position + targetTransform.forward * CarryDistance;
            var targetRotation = targetTransform.rotation;

            // Frame-rate independent smoothing. The old "deltaTime * speed" factor made the
            // carried product snap into the hand on a high frame rate and crawl on a low one, so
            // the same action looked different on every machine.
            var speed = 1f - Mathf.Exp(-interactableFollowSpeed * Time.deltaTime);

            foreach (var selectedInteractable in SelectedInteractables)
            {
                var currentPosition = selectedInteractable.Position;
                selectedInteractable.Position = Vector3.Lerp(
                    currentPosition,
                    targetPosition,
                    speed
                );

                var currentRotation = selectedInteractable.Rotation;
                selectedInteractable.Rotation = Quaternion.Lerp(
                    currentRotation,
                    targetRotation,
                    speed
                );
            }
        }
    }
}

using System;
using UABPetelnia.GGJ2025.Runtime.Components.Interaction.Interactables;
using UnityEngine;
using UnityEngine.Events;

namespace UABPetelnia.GGJ2025.Runtime.Components.Interaction.Sockets
{
    internal sealed class InteractableSocket : MonoBehaviour
    {
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.FoldoutGroup("General", Expanded = true)]
        [Sirenix.OdinInspector.Required]
#else
        [Header("General")]
#endif
        [SerializeField]
        private Interactable targetInteractable;

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.FoldoutGroup("General", Expanded = true)]
#endif
        [SerializeField]
        private string prettyName;

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.FoldoutGroup("General", Expanded = true)]
        [Sirenix.OdinInspector.Required]
#endif
        [SerializeField]
        private Transform socketOffset;

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.FoldoutGroup("Animation", Expanded = true)]
        [Sirenix.OdinInspector.PropertyRange(0f, 100f)]
#else
        [Header("Animation")]
        [Range(0f, 100f)]
#endif
        [SerializeField]
        private float socketPositionSpeed = 30f;

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.FoldoutGroup("Animation", Expanded = true)]
        [Sirenix.OdinInspector.PropertyRange(0f, 100f)]
#else
        [Range(0f, 100f)]
#endif
        [SerializeField]
        private float socketRotationSpeed = 15f;

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.FoldoutGroup("Events")]
#else
        [Header("Events")]
#endif
        [SerializeField]
        private UnityEvent onSocketed;

        public string Name => string.IsNullOrWhiteSpace(prettyName) ? name : prettyName;

        public event Action OnSocketed;

        private bool isSockedAnimationFinished;
        private bool isSocketing;

        private void OnEnable()
        {
            targetInteractable.OnSelectEntered += OnSelectEntered;
            targetInteractable.OnSelectExited += OnSelectExited;
        }

        private void OnDisable()
        {
            targetInteractable.OnSelectEntered -= OnSelectEntered;
            targetInteractable.OnSelectExited -= OnSelectExited;
        }

        private void Update()
        {
            if (isSockedAnimationFinished)
            {
                return;
            }

            if (isSocketing == false)
            {
                return;
            }

            if (targetInteractable.IsSelected)
            {
                return;
            }

            UpdateSocketAnimation();
        }

        private void OnSelectEntered(InteractableSelectEnteredArgs args)
        {
            isSocketing = false;
        }

        private void OnSelectExited(InteractableSelectExitedArgs args)
        {
            if (isSocketing == false)
            {
                return;
            }

            var rigidBody = targetInteractable.GetComponentInParent<Rigidbody>();
            if (rigidBody)
            {
                targetInteractable.enabled = false;
                rigidBody.isKinematic = true;
            }
        }

        private void UpdateSocketAnimation()
        {
            if (socketOffset == false)
            {
                return;
            }

            var targetPosition = socketOffset.position;
            var targetRotation = socketOffset.rotation;

            var currentPosition = targetInteractable.Position;
            targetInteractable.Position = Vector3.Lerp(
                currentPosition,
                targetPosition,
                Time.deltaTime * socketPositionSpeed
            );

            var currentRotation = targetInteractable.Rotation;
            targetInteractable.Rotation = Quaternion.Lerp(
                currentRotation,
                targetRotation,
                Time.deltaTime * socketRotationSpeed
            );

            var distance = Vector3.Distance(currentPosition, targetPosition);
            var angle = Quaternion.Angle(currentRotation, targetRotation);

            if (distance <= 0.01f && angle <= 0.01f)
            {
                targetInteractable.Position = targetPosition;
                targetInteractable.Rotation = targetRotation;

                isSockedAnimationFinished = true;
                isSocketing = false;

                OnSocketed?.Invoke();
                onSocketed.Invoke();

                Destroy(this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var interactable = other.GetComponentInParent<Interactable>();
            if (interactable != targetInteractable)
            {
                return;
            }

            isSocketing = true;
        }

        private void OnTriggerExit(Collider other)
        {
            var interactable = other.GetComponentInParent<Interactable>();
            if (interactable != targetInteractable)
            {
                return;
            }

            isSocketing = false;
        }
    }
}

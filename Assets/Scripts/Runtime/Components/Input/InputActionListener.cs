using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace UABPetelnia.GGJ2025.Runtime.Components.Input
{
    internal abstract class InputActionListener<T> : MonoBehaviour
    {
        [Header("General")]
        [SerializeField]
        private InputActionReference inputActionReference;

        [Header("Features")]
        [Tooltip("Should input action be enabled on awake?")]
        [SerializeField]
        private bool isEnableAutomatically;

        [Tooltip("Should debug input messages be logged?")]
        [SerializeField]
        private bool isDebug;

        [Header("Events")]
        [SerializeField]
        protected UnityEvent<T> onPerformed;

        [SerializeField]
        protected UnityEvent<T> onCanceled;

        /// <summary>
        /// Current input value.
        /// </summary>
        public T Value => ReadValue();

        protected InputAction InputAction => inputActionReference.action;

        /// <summary>
        /// Invoked when underlying <see cref="inputActionReference"/> is performed by the user.
        /// </summary>
        public event Action<T> OnPerformed;

        /// <summary>
        /// Invoked when underlying <see cref="inputActionReference"/> is stopped.
        /// </summary>
        public event Action<T> OnCanceled;

        private void Awake()
        {
            var action = inputActionReference.action;
            if (isEnableAutomatically)
            {
                action.Enable();
            }
        }

        private void OnEnable()
        {
            if (inputActionReference == false)
            {
                return;
            }

            var action = inputActionReference.action;
            if (isEnableAutomatically)
            {
                action.Enable();
            }

            action.performed += HandleOnPerformed;
            action.canceled += HandleOnCanceled;
        }

        private void OnDisable()
        {
            if (inputActionReference == false)
            {
                return;
            }

            var action = inputActionReference.action;
            action.performed -= HandleOnPerformed;
            action.canceled -= HandleOnCanceled;
        }

        protected abstract T ReadValue(InputAction.CallbackContext ctx);

        protected abstract T ReadValue();

        private void HandleOnPerformed(InputAction.CallbackContext ctx)
        {
            var value = ReadValue(ctx);
            if (isDebug)
            {
                Debug.Log($"{name}, performed: {value}", this);
            }

            onPerformed.Invoke(value);
            OnPerformed?.Invoke(value);
        }

        private void HandleOnCanceled(InputAction.CallbackContext ctx)
        {
            var value = ReadValue(ctx);
            if (isDebug)
            {
                Debug.Log($"{name}, canceled: {value}", this);
            }

            onCanceled.Invoke(value);
            OnCanceled?.Invoke(value);
        }
    }
}

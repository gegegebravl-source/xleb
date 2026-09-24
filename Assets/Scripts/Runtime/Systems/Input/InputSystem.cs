using System;
using System.Collections.Generic;
using CHARK.GameManagement;
using CHARK.GameManagement.Systems;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UABPetelnia.GGJ2025.Runtime.Systems.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Input
{
    internal sealed class InputSystem : MonoSystem, IInputSystem
    {
        [Header("General")]
        [SerializeField]
        private PlayerInput playerInput;

        [Header("Control Schemes")]
        [SerializeField]
        private string keyboardMouseControlScheme = "KeyboardMouse";

        [SerializeField]
        private string gamepadControlScheme = "Gamepad";

        [Header("Action Maps")]
        [SerializeField]
        private string playerActionMapName = "Player";

        private const string OrdersActionName = "Orders";

        private const string InteractActionName = "Interact";

        [SerializeField]
        private string uiActionMapName = "UI";

        private ISettingsSystem settingsSystem;

        public float LookSensitivity
        {
            get => GetLookSensitivity();
            set => SetLookSensitivity(value);
        }

        public ControlScheme ControlScheme => GetControlScheme(playerInput);

        public Vector2 MoveInput
        {
            get
            {
                var action = FindPlayerAction("Move");
                if (action == null)
                {
                    return Vector2.zero;
                }

                return action.ReadValue<Vector2>();
            }
        }

        public bool IsOrdersPressedThisFrame
        {
            get
            {
                var action = FindPlayerAction(OrdersActionName);
                if (action != null && action.WasPressedThisFrame())
                {
                    return true;
                }

                // Same reasoning as the movement fallback: raw device reads keep the ordering
                // panel reachable even when the action map was never enabled (for example when
                // entering play mode with domain reload disabled).
                var keyboard = Keyboard.current;

                return keyboard != null && keyboard.tabKey.wasPressedThisFrame;
            }
        }

        public bool IsInteractPressedThisFrame
        {
            get
            {
                var action = FindPlayerAction(InteractActionName);
                if (action != null && action.WasPressedThisFrame())
                {
                    return true;
                }

                var keyboard = Keyboard.current;

                return keyboard != null && keyboard.eKey.wasPressedThisFrame;
            }
        }

        public override void OnInitialized()
        {
            settingsSystem = GameManager.GetSystem<ISettingsSystem>();
            playerInput.onControlsChanged += OnControlsChanged;
        }

        public override void OnDisposed()
        {
            playerInput.onControlsChanged -= OnControlsChanged;
        }

        private void OnControlsChanged(PlayerInput input)
        {
            var controlScheme = GetControlScheme(input);
            var message = new ControlSchemeChangedMessage(controlScheme);

            GameManager.Publish(message);
        }

        public void EnablePlayerInput()
        {
            foreach (var inputAction in GetActions(playerActionMapName))
            {
                inputAction.Enable();
            }
        }

        public void DisablePlayerInput()
        {
            foreach (var inputAction in GetActions(playerActionMapName))
            {
                inputAction.Disable();
            }
        }

        public void EnableUIInput()
        {
            foreach (var inputAction in GetActions(uiActionMapName))
            {
                inputAction.Enable();
            }
        }

        public void DisableUIInput()
        {
            foreach (var inputAction in GetActions(uiActionMapName))
            {
                inputAction.Disable();
            }
        }

        /// <summary>
        /// Find an action inside the player action map of the shared <see cref="playerInput"/>.
        /// </summary>
        private InputAction FindPlayerAction(string actionName)
        {
            if (playerInput == false)
            {
                return null;
            }

            var inputActions = playerInput.actions;
            if (inputActions == false)
            {
                return null;
            }

            return inputActions.FindAction(
                $"{playerActionMapName}/{actionName}",
                throwIfNotFound: false
            );
        }

        private IEnumerable<InputAction> GetActions(string actionMapName)
        {
            if (playerInput == false)
            {
                yield break;
            }

            var inputActions = playerInput.actions;
            if (inputActions == false)
            {
                yield break;
            }

            foreach (var inputActionMap in inputActions.actionMaps)
            {
                if (string.Equals(actionMapName, inputActionMap.name) == false)
                {
                    continue;
                }

                foreach (var inputAction in inputActionMap.actions)
                {
                    yield return inputAction;
                }
            }
        }

        private float GetLookSensitivity()
        {
            var settings = settingsSystem.Settings;
            return GetNormalizedSensitivity(settings.LookSensitivity);
        }

        private void SetLookSensitivity(float newLookSensitivity)
        {
            var settings = settingsSystem.Settings;
            settings.LookSensitivity = GetNormalizedSensitivity(newLookSensitivity);

            settingsSystem.Settings = settings;
        }

        private static float GetNormalizedSensitivity(float volume)
        {
            var clampedSensitivity = Mathf.Clamp(
                volume,
                GeneralSettings.MinLookSensitivity,
                GeneralSettings.MaxLookSensitivity
            );

            return (float)Math.Round(clampedSensitivity, 2);
        }

        private ControlScheme GetControlScheme(PlayerInput input)
        {
            if (input == false)
            {
                // Defaults to keyboard & mouse.
                return ControlScheme.KeyboardMouse;
            }

            var schemeName = input.currentControlScheme;
            if (string.Equals(schemeName, keyboardMouseControlScheme))
            {
                return ControlScheme.KeyboardMouse;
            }

            if (string.Equals(schemeName, gamepadControlScheme))
            {
                return ControlScheme.Gamepad;
            }

            // Defaults to keyboard & mouse.
            return ControlScheme.KeyboardMouse;
        }
    }
}

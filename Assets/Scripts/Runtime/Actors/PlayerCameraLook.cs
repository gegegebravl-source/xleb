using Unity.Cinemachine;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UABPetelnia.GGJ2025.Runtime.Systems.Input;
using UABPetelnia.GGJ2025.Runtime.UI.Controllers;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UABPetelnia.GGJ2025.Runtime.Actors
{
    /// <summary>
    /// Player driven camera look for the kiosk. The mouse (or the right gamepad stick) drives the
    /// Cinemachine pan/tilt head directly, so aiming never depends on an external input asset being
    /// present, enabled or bound to the right control scheme.
    /// </summary>
    internal sealed class PlayerCameraLook : MonoBehaviour
    {
        [Header("Head")]
        [SerializeField]
        private CinemachinePanTilt panTilt;

        [Header("Sensitivity")]
        [SerializeField]
        private float mouseSensitivity = 0.12f;

        [SerializeField]
        private float gamepadSensitivity = 150f;

        [SerializeField]
        private bool invertY;

        [Tooltip("Значения по умолчанию: опорная чувствительность для ползунка в настройках.")]
        [SerializeField]
        private GeneralSettings generalSettings;

        [Header("Limits")]
        [SerializeField]
        private float minimumTilt = -70f;

        [SerializeField]
        private float maximumTilt = 70f;

        [Header("Blockers")]
        [SerializeField]
        private DeliveryViewController deliveryViewController;

        [Header("Music Player")]
        [SerializeField]
        private MusicPlayerViewController musicPlayerViewController;

        private void Reset()
        {
            panTilt = GetComponent<CinemachinePanTilt>();
        }

        private IInputSystem inputSystem;

        private void Awake()
        {
            if (panTilt == false)
            {
                panTilt = GetComponent<CinemachinePanTilt>();
            }

            SystemsUtility.TryGetSystem(out inputSystem);
        }

        /// <summary>
        /// Чувствительность из настроек: ползунок 0..20 относительно значения по умолчанию.
        /// Без этого настройка в меню ни на что не влияла.
        /// </summary>
        private float GetMouseSensitivity()
        {
            if (inputSystem == null)
            {
                return mouseSensitivity;
            }

            var reference = generalSettings != false && IsFinite(generalSettings.DefaultLookSensitivity)
                ? Mathf.Max(0.01f, generalSettings.DefaultLookSensitivity)
                : 5f;
            var normalized = Mathf.Clamp(
                inputSystem.LookSensitivity,
                GeneralSettings.MinLookSensitivity,
                GeneralSettings.MaxLookSensitivity
            );

            var baseSensitivity = IsFinite(mouseSensitivity) ? Mathf.Max(0f, mouseSensitivity) : 0.12f;
            return baseSensitivity * (normalized / reference);
        }

        private static bool IsFinite(float value)
        {
            return float.IsNaN(value) == false && float.IsInfinity(value) == false;
        }

        private void Update()
        {
            if (panTilt == false)
            {
                return;
            }

            if (deliveryViewController != false && deliveryViewController.IsOpen)
            {
                // The ordering PC is a mouse driven screen: the view has to stay still while it is up.
                return;
            }

            if (musicPlayerViewController != false && musicPlayerViewController.IsOpen)
            {
                // The music player menu is mouse driven too: freeze the look while it is up.
                return;
            }

            var look = ReadLookInput();
            if (look.sqrMagnitude <= 0f)
            {
                return;
            }

            var pan = panTilt.PanAxis.Value + look.x;
            var tilt = panTilt.TiltAxis.Value - look.y;

            panTilt.PanAxis.Value = Mathf.Repeat(pan + 180f, 360f) - 180f;
            panTilt.TiltAxis.Value = Mathf.Clamp(tilt, minimumTilt, maximumTilt);
        }

        private Vector2 ReadLookInput()
        {
            var look = Vector2.zero;

            var mouse = Mouse.current;
            if (mouse != null)
            {
                look += mouse.delta.ReadValue() * GetMouseSensitivity();
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                look += gamepad.rightStick.ReadValue() * (gamepadSensitivity * Time.deltaTime);
            }

            return invertY ? new Vector2(look.x, -look.y) : look;
        }
    }
}

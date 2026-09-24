using CHARK.GameManagement.Systems;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Input
{
    internal interface IInputSystem : ISystem
    {
        /// <summary>
        /// Look sensitivity the user has set.
        /// </summary>
        public float LookSensitivity { get; set; }

        /// <summary>
        /// Movement input read from the "Move" action. <see cref="Vector2.zero"/> when there is
        /// no input or when player input is disabled.
        /// </summary>
        public Vector2 MoveInput { get; }

        /// <summary>
        /// <c>true</c> on the frame the "Orders" action was pressed. Opens (and closes) the kiosk
        /// ordering panel on the counter PC.
        /// </summary>
        public bool IsOrdersPressedThisFrame { get; }

        /// <summary>
        /// <c>true</c> on the frame the "Interact" action was pressed. Открывает панель заказов,
        /// когда игрок навёл камеру на ПК прилавка.
        /// </summary>
        public bool IsInteractPressedThisFrame { get; }

        /// <summary>
        /// Current control scheme.
        /// </summary>
        public ControlScheme ControlScheme { get;  }

        /// <summary>
        /// Enable player input actions.
        /// </summary>
        public void EnablePlayerInput();

        /// <summary>
        /// Disable player input actions (e.g., during pause menu).
        /// </summary>
        public void DisablePlayerInput();

        /// <summary>
        /// Enable UI input actions.
        /// </summary>
        public void EnableUIInput();

        /// <summary>
        /// Disable UI input actions.
        /// </summary>
        public void DisableUIInput();
    }
}

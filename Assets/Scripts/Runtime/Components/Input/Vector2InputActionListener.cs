using UnityEngine;
using UnityEngine.InputSystem;

namespace UABPetelnia.GGJ2025.Runtime.Components.Input
{
    /// <summary>
    /// Triggers events when 2D axis is input, for example a thumb-stick, is moved
    /// </summary>
    internal class Vector2InputActionListener : InputActionListener<Vector2>
    {
        protected override Vector2 ReadValue(InputAction.CallbackContext ctx)
        {
            return ctx.ReadValue<Vector2>();
        }

        protected override Vector2 ReadValue()
        {
            return InputAction.ReadValue<Vector2>();
        }
    }
}

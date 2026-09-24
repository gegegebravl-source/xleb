using UnityEngine.InputSystem;

namespace UABPetelnia.GGJ2025.Runtime.Components.Input
{
    /// <summary>
    /// Triggers events when a button is pressed.
    /// </summary>
    internal class ButtonInputActionListener : InputActionListener<bool>
    {
        protected override bool ReadValue(InputAction.CallbackContext ctx)
        {
            return ctx.ReadValueAsButton();
        }

        protected override bool ReadValue()
        {
            return InputAction.phase == InputActionPhase.Performed;
        }
    }
}

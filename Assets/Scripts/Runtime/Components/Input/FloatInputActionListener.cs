using UnityEngine.InputSystem;

namespace UABPetelnia.GGJ2025.Runtime.Components.Input
{
    /// <summary>
    /// Triggers events when 1D axis is input, for example a button, is pressed.
    /// </summary>
    internal class FloatInputActionListener : InputActionListener<float>
    {
        protected override float ReadValue(InputAction.CallbackContext ctx)
        {
            return ctx.ReadValue<float>();
        }

        protected override float ReadValue()
        {
            var action = InputAction;
            return action != null ? action.ReadValue<float>() : 0f;
        }
    }
}

using System;

namespace CHARK.SimpleUI.Animations
{
    internal interface IAnimation
    {
        /// <summary>
        /// Animate given <paramref name="view"/>.
        /// </summary>
        /// <returns>
        /// Animation instance which is animating given <paramref name="view"/>.
        /// </returns>
        public ViewAnimationInstance Animate(View view, bool isImmediate = false, Action onCompleted = default);
    }
}

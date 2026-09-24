using System;

namespace CHARK.SimpleUI.Animations
{
    internal sealed class EmptyAnimation : IAnimation
    {
        public static EmptyAnimation InstanceShow { get; } = new(1f);

        public static EmptyAnimation InstanceHide { get; } = new(0f);

        private readonly float alpha;

        private EmptyAnimation(float alpha)
        {
            this.alpha = alpha;
        }

        public ViewAnimationInstance Animate(View view, bool isImmediate, Action onCompleted)
        {
            view.CanvasGroup.alpha = alpha;
            onCompleted?.Invoke();
            return default;
        }
    }
}

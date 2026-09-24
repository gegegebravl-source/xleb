namespace CHARK.SimpleUI.Animations
{
    internal static class ViewAnimationExtensions
    {
#if PRIME_TWEEN_INSTALLED
        internal static ViewAnimationInstance ToViewAnimation(this PrimeTween.Sequence sequence)
        {
            return new ViewAnimationInstance(sequence);
        }

        internal static ViewAnimationInstance ToViewAnimation(this PrimeTween.Tween tween)
        {
            return new ViewAnimationInstance(tween);
        }
#endif
    }
}

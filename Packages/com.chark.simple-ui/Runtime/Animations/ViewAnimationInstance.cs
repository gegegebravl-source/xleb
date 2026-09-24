using System;
using System.Threading.Tasks;

namespace CHARK.SimpleUI.Animations
{
    public readonly struct ViewAnimationInstance
    {
#if PRIME_TWEEN_INSTALLED
        private enum AnimationType
        {
            None = 0,
            Sequence = 1,
            Tween = 2,
        }

        private readonly AnimationType animationType;

        private readonly PrimeTween.Sequence sequence;
        private readonly PrimeTween.Tween tween;
#endif

#if PRIME_TWEEN_INSTALLED
        public ViewAnimationInstance(PrimeTween.Sequence sequence)
        {
            this.animationType = AnimationType.Sequence;
            this.sequence = sequence;
            this.tween = default;
        }

        public ViewAnimationInstance(PrimeTween.Tween tween)
        {
            this.animationType = AnimationType.Tween;
            this.sequence = default;
            this.tween = tween;
        }
#endif

        public async Task ToAsync()
        {
#if PRIME_TWEEN_INSTALLED
            switch (animationType)
            {
                case AnimationType.None:
                default:
                {
                    return;
                }
                case AnimationType.Sequence:
                {
                    await sequence;
                    return;
                }
                case AnimationType.Tween:
                {
                    await tween;
                    return;
                }
            }
#endif
        }

        public void Complete()
        {
#if PRIME_TWEEN_INSTALLED
            switch (animationType)
            {
                case AnimationType.None:
                default:
                {
                    break;
                }
                case AnimationType.Sequence:
                {
                    sequence.Complete();
                    break;
                }
                case AnimationType.Tween:
                {
                    tween.Complete();
                    break;
                }
            }
#endif
        }

        public ViewAnimationInstance Insert(float timeSeconds, ViewAnimationInstance instance)
        {
#if PRIME_TWEEN_INSTALLED
            switch (animationType)
            {
                case AnimationType.None:
                default:
                {
                    return this;
                }
                case AnimationType.Sequence:
                {
                    var insertType = instance.animationType;
                    if (insertType == AnimationType.Sequence)
                    {
                        return sequence
                            .Insert(timeSeconds, instance.sequence)
                            .ToViewAnimation();
                    }

                    if (insertType == AnimationType.Tween)
                    {
                        return sequence
                            .Insert(timeSeconds, instance.tween)
                            .ToViewAnimation();
                    }

                    return this;
                }
                case AnimationType.Tween:
                {
                    var insertType = instance.animationType;
                    if (insertType == AnimationType.Sequence)
                    {
                        return PrimeTween.Sequence
                            .Create(tween)
                            .Insert(timeSeconds, instance.sequence)
                            .ToViewAnimation();
                    }

                    if (insertType == AnimationType.Tween)
                    {
                        return PrimeTween.Sequence
                            .Create(tween)
                            .Insert(timeSeconds, instance.tween)
                            .ToViewAnimation();
                    }

                    return this;
                }
            }
#else
            return this;
#endif
        }

        public void Stop()
        {
#if PRIME_TWEEN_INSTALLED
            switch (animationType)
            {
                case AnimationType.None:
                default:
                {
                    break;
                }
                case AnimationType.Sequence:
                {
                    sequence.Stop();
                    break;
                }
                case AnimationType.Tween:
                {
                    tween.Stop();
                    break;
                }
            }
#endif
        }

        public ViewAnimationInstance OnComplete(Action onCompleted)
        {
#if PRIME_TWEEN_INSTALLED
            switch (animationType)
            {
                case AnimationType.None:
                default:
                {
                    return this;
                }
                case AnimationType.Sequence:
                {
                    return sequence.OnComplete(onCompleted).ToViewAnimation();
                }
                case AnimationType.Tween:
                {
                    return tween.OnComplete(onCompleted).ToViewAnimation();
                }
            }
#else
            return this;
#endif
        }
    }
}

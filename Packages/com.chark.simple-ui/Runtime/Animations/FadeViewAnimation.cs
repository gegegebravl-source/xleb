using System;
using CHARK.SimpleUI.Constants;
using UnityEngine;
using UnityEngine.Serialization;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace CHARK.SimpleUI.Animations
{
    [CreateAssetMenu(
        fileName = CreateAssetMenuConstants.BaseFileName + nameof(FadeViewAnimation),
        menuName = CreateAssetMenuConstants.BaseMenuName + "/Animations/Fade View Animation",
        order = CreateAssetMenuConstants.BaseOrder
    )]
    internal sealed class FadeViewAnimation : ViewAnimation
    {
#if ODIN_INSPECTOR
        [FoldoutGroup("Animation", Expanded = true)]
        [HorizontalGroup("Animation/Alpha")]
        [LabelText("Alpha (in)")]
#else
        [Header("Animation")]
#endif
        [SerializeField]
        private float startAlpha;

#if ODIN_INSPECTOR
        [HorizontalGroup("Animation/Alpha")]
        [LabelText("Alpha (out)")]
#endif
        [SerializeField]
        private float endAlpha = 1f;

#if ODIN_INSPECTOR
        [FoldoutGroup("Animation", Expanded = true)]
        [HorizontalGroup("Animation/Timings")]
        [LabelText("Duration")]
        [MinValue(0f)]
        [Unit(Units.Second)]
#else
        [Min(0f)]
#endif
        [SerializeField]
        private float duration = 1f;

#if ODIN_INSPECTOR
        [HorizontalGroup("Animation/Timings")]
        [LabelText("Delay (in)")]
        [MinValue(0f)]
        [Unit(Units.Second)]
#else
        [Min(0f)]
#endif
        [SerializeField]
        private float startDelay;

#if ODIN_INSPECTOR
        [HorizontalGroup("Animation/Timings")]
        [LabelText("Delay (out)")]
        [MinValue(0f)]
        [Unit(Units.Second)]
#else
        [Min(0f)]
#endif
        [FormerlySerializedAs("stopDelay")]
        [SerializeField]
        private float endDelay;

#if PRIME_TWEEN_INSTALLED

#if ODIN_INSPECTOR
        [FoldoutGroup("Animation")]
#endif
        [SerializeField]
        private PrimeTween.Ease easing = PrimeTween.Ease.Default;
#endif

#if ODIN_INSPECTOR
        [FoldoutGroup("Animation")]
        [ShowIf(nameof(IsUseCurve))]
#endif
        [SerializeField]
        private AnimationCurve easingCurve;

#if ODIN_INSPECTOR
        [FoldoutGroup("Features", Expanded = true)]
#else
        [Header("Features")]
#endif
        [SerializeField]
        private bool isStartFromCurrent;

#if ODIN_INSPECTOR
        [FoldoutGroup("Features", Expanded = true)]
#endif
        [SerializeField]
        private bool isUseUnscaledTime = true;

        private bool IsUseCurve
        {
            get
            {
#if PRIME_TWEEN_INSTALLED
                if (easing == PrimeTween.Ease.Custom)
                {
                    return true;
                }
#endif

                return false;
            }
        }

        protected override void OnAnimateImmediate(View view)
        {
            view.CanvasGroup.alpha = endAlpha;
        }

        protected override ViewAnimationInstance OnAnimate(View view, Action onCompleted)
        {
#if PRIME_TWEEN_INSTALLED
            var baseTweenSettings = new PrimeTween.TweenSettings
            {
                duration = duration,
                ease = easing,
                useUnscaledTime = isUseUnscaledTime,
                startDelay = startDelay,
                endDelay = endDelay,
            };

            if (IsUseCurve)
            {
                baseTweenSettings.customEase = easingCurve;
            }

            var tweenSettings = new PrimeTween.TweenSettings<float>
            {
                startValue = startAlpha,
                endValue = endAlpha,
                settings = baseTweenSettings,
            };

            if (isStartFromCurrent == false)
            {
                view.CanvasGroup.alpha = startAlpha;
            }

            return PrimeTween.Tween
                .Alpha(view.CanvasGroup, tweenSettings.WithDirection(true, _startFromCurrent: isStartFromCurrent))
                .OnComplete(onCompleted)
                .ToViewAnimation();
#else
            return default;
#endif
        }
    }
}

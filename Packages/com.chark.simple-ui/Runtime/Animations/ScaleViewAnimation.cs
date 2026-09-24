using System;
using CHARK.SimpleUI.Constants;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace CHARK.SimpleUI.Animations
{
    [CreateAssetMenu(
        fileName = CreateAssetMenuConstants.BaseFileName + nameof(ScaleViewAnimation),
        menuName = CreateAssetMenuConstants.BaseMenuName + "/Animations/Scale View Animation",
        order = CreateAssetMenuConstants.BaseOrder
    )]
    internal sealed class ScaleViewAnimation : ViewAnimation
    {
#if ODIN_INSPECTOR
        [FoldoutGroup("Animation", Expanded = true)]
        [HorizontalGroup("Animation/Scale")]
        [LabelText("Scale (in)")]
#else
        [Header("Scale")]
#endif
        [SerializeField]
        private Vector3 startScale = Vector3.zero;

#if ODIN_INSPECTOR
        [HorizontalGroup("Animation/Scale")]
        [LabelText("Scale (out)")]
#endif
        [SerializeField]
        private Vector3 endScale = Vector3.one;

#if ODIN_INSPECTOR
        [FoldoutGroup("Animation", Expanded = true)]
        [HorizontalGroup("Animation/Timings")]
        [LabelText("Duration")]
        [MinValue(0f)]
        [Unit(Units.Second)]
#else
        [Header("Timings")]
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
            var transform = view.transform;
            transform.localScale = endScale;
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

            var tweenSettings = new PrimeTween.TweenSettings<Vector3>
            {
                startValue = startScale,
                endValue = endScale,
                settings = baseTweenSettings,
            };

            if (isStartFromCurrent == false)
            {
                view.transform.localScale = startScale;
            }

            return PrimeTween.Tween
                .Scale(
                    view.transform,
                    tweenSettings.WithDirection(true, _startFromCurrent: isStartFromCurrent)
                )
                .OnComplete(onCompleted)
                .ToViewAnimation();
#else
            return default;
#endif
        }
    }
}

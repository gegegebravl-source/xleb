using System;
using System.Collections.Generic;
using CHARK.SimpleUI.Constants;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace CHARK.SimpleUI.Animations
{
    [CreateAssetMenu(
        fileName = CreateAssetMenuConstants.BaseFileName + nameof(SequenceViewAnimation),
        menuName = CreateAssetMenuConstants.BaseMenuName + "/Animations/Sequence View Animation",
        order = CreateAssetMenuConstants.BaseOrder
    )]
    internal sealed class SequenceViewAnimation : ViewAnimation
    {
#if ODIN_INSPECTOR
        [FoldoutGroup("Animation", Expanded = true)]
#else
        [Header("Animation")]
#endif
        [SerializeField]
        private List<ViewAnimation> sequence;

#if ODIN_INSPECTOR
        [FoldoutGroup("Features", Expanded = true)]
#else
        [Header("Features")]
#endif
        [SerializeField]
        private bool isUseUnscaledTime = true;

        protected override void OnAnimateImmediate(View view)
        {
            foreach (var tween in sequence)
            {
                tween.Animate(view, isImmediate: true);
            }
        }

        protected override ViewAnimationInstance OnAnimate(View view, Action onCompleted)
        {
#if PRIME_TWEEN_INSTALLED
            var animationSequence = PrimeTween.Sequence.Create(useUnscaledTime: isUseUnscaledTime);
            var output = animationSequence.ToViewAnimation();

            foreach (var sequenceAnimation in sequence)
            {
                output = output.Insert(0f, sequenceAnimation.Animate(view));
            }

            return output.OnComplete(onCompleted);
#else
            return default;
#endif
        }
    }
}

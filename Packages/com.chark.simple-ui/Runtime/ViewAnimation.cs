using System;
using CHARK.SimpleUI.Animations;
using CHARK.SimpleUI.Constants;
using UnityEngine;

namespace CHARK.SimpleUI
{
#if SCRIPTABLE_SETTINGS_INSTALLED
    [ScriptableSettings.ScriptableSettings(
        MenuNamePrefix = "UI/Animation",
        NewAssetPath = PathConstants.RelativeAnimationDataPath
    )]
#endif
    public abstract class ViewAnimation : ScriptableObject, IAnimation
    {
        private static readonly Action EmptyAction = () => { };

        public ViewAnimationInstance Animate(View view, bool isImmediate = false, Action onCompleted = default)
        {
            if (view == false)
            {
                onCompleted?.Invoke();
                return default;
            }

            if (view.isActiveAndEnabled == false)
            {
                onCompleted?.Invoke();
                return default;
            }

            if (isImmediate)
            {
                OnAnimateImmediate(view);
                onCompleted?.Invoke();

                return default;
            }

            return OnAnimate(view, onCompleted ?? EmptyAction);
        }

        protected abstract void OnAnimateImmediate(View view);

        protected abstract ViewAnimationInstance OnAnimate(View view, Action onCompleted);
    }
}

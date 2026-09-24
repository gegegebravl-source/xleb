using System;
using CHARK.SimpleUI.Animations;
using UnityEngine;
using UnityEngine.Events;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace CHARK.SimpleUI
{
    public abstract class View : MonoBehaviour
    {
#if ODIN_INSPECTOR
        private const string ViewGroupKey = "View Component";
#endif

#if ODIN_INSPECTOR
        [PropertyOrder(float.MaxValue)]
        [FoldoutGroup(ViewGroupKey)]
        [TitleGroup(ViewGroupKey + "/General")]
        [PropertySpace(spaceBefore: 0f, spaceAfter: 8f)]
        [Required]
#else
        [Header("View General")]
#endif
        [SerializeField]
        private CanvasGroup canvasGroup;

#if ODIN_INSPECTOR
        [TitleGroup(ViewGroupKey + "/Features")]
#endif
        [SerializeField]
        private bool isDeactivateRaycastsOnHide = true;

        // Disabling interactable state by default causes buttons to switch from Disabled -> Normal
        // state while Show animation is still playing, which causes buttons to play transition anim.
        // Hence, why this feature is disabled by default.
#if ODIN_INSPECTOR
        [TitleGroup(ViewGroupKey + "/Features")]
#endif
        [SerializeField]
        private bool isDeactivateInteractableOnHide;

#if ODIN_INSPECTOR
        [TitleGroup(ViewGroupKey + "/Features")]
#endif
        [SerializeField]
        private bool isDeactivateViewOnHide = true;

#if ODIN_INSPECTOR
        [TitleGroup(ViewGroupKey + "/Animation")]
        [Title("Animation", HorizontalLine = false)]
        [Optional]
#else
        [Header("View Animations")]
#endif
        [SerializeField]
        private ViewAnimation showAnimationAsset;

#if ODIN_INSPECTOR
        [TitleGroup(ViewGroupKey + "/Animation")]
        [Title("Animation", HorizontalLine = false)]
        [Optional]
#endif
        [SerializeField]
        private ViewAnimation hideAnimationAsset;

#if ODIN_INSPECTOR
        [TitleGroup(ViewGroupKey + "/Events")]
        [FoldoutGroup(ViewGroupKey + "/Events/Show Events")]
#else
        [Header("View Events")]
#endif
        [SerializeField]
        private UnityEvent onShowEntered;

#if ODIN_INSPECTOR
        [FoldoutGroup(ViewGroupKey + "/Events/Show Events")]
#endif
        [SerializeField]
        private UnityEvent onShowExited;

#if ODIN_INSPECTOR
        [FoldoutGroup(ViewGroupKey + "/Events/Hide Events")]
#endif
        [SerializeField]
        private UnityEvent onHideEntered;

#if ODIN_INSPECTOR
        [FoldoutGroup(ViewGroupKey + "/Events/Hide Events")]
#endif
        [SerializeField]
        private UnityEvent onHideExited;

        /// <inheritdoc cref="OnViewShowEntered"/>
        public event Action OnShowEntered;

        /// <inheritdoc cref="OnViewShowExited"/>
        public event Action OnShowExited;

        /// <inheritdoc cref="OnViewHideEntered"/>
        public event Action OnHideEntered;

        /// <inheritdoc cref="OnViewHideExited"/>
        public event Action OnHideExited;

        /// <summary>
        /// Current view state.
        /// </summary>
        public ViewVisibilityState State { get; private set; }

        /// <summary>
        /// Canvas group assigned to this view (all views have a canvas group).
        /// </summary>
        public CanvasGroup CanvasGroup => canvasGroup;

        private IAnimation ShowAnimation
        {
            get
            {
                if (showAnimationAsset)
                {
                    return showAnimationAsset;
                }

                return EmptyAnimation.InstanceShow;
            }
        }

        private IAnimation HideAnimation
        {
            get
            {
                if (hideAnimationAsset)
                {
                    return hideAnimationAsset;
                }

                return EmptyAnimation.InstanceHide;
            }
        }

        private ViewAnimationInstance activeAnimation;

#if UNITY_EDITOR
        private void Reset()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
#endif

        protected virtual void Awake()
        {
            if (canvasGroup == false)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup == false)
            {
                Debug.LogWarning($"{nameof(canvasGroup)} is not set, creating a new group", this);
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // ReSharper disable once Unity.RedundantEventFunction
        protected virtual void OnEnable()
        {
        }

        // ReSharper disable once Unity.RedundantEventFunction
        protected virtual void OnDisable()
        {
        }

        protected virtual void OnDestroy()
        {
            DisposeActiveAnimation();
        }

        // ReSharper disable once Unity.RedundantEventFunction
        protected virtual void Start()
        {
        }

        /// <summary>
        /// Called when this view is starting to shown.
        /// </summary>
        protected virtual void OnViewShowEntered()
        {
        }

        /// <summary>
        /// Called when this view is fully shown.
        /// </summary>
        protected virtual void OnViewShowExited()
        {
        }

        /// <summary>
        /// Called when this view is starting to hide.
        /// </summary>
        protected virtual void OnViewHideEntered()
        {
        }

        /// <summary>
        /// Called when this view is fully hidden.
        /// </summary>
        protected virtual void OnViewHideExited()
        {
        }

        /// <summary>
        /// Show this view.
        /// </summary>
#if ODIN_INSPECTOR
        [PropertyOrder(float.MaxValue)]
        [TitleGroup(ViewGroupKey + "/Debug")]
#if UNITY_EDITOR
        [EnableIf(nameof(IsDebugEnabledEditor))]
#endif
        [Button]
#endif
        public virtual void Show(bool isAnimate = true)
        {
            DisposeActiveAnimation();

            OnShowAnimationEntered();
            activeAnimation = ShowAnimation.Animate(
                view: this,
                isImmediate: isAnimate == false,
                onCompleted: OnShowAnimationExited
            );
        }

        /// <summary>
        /// Hide this view.
        /// </summary>
#if ODIN_INSPECTOR
        [TitleGroup(ViewGroupKey + "/Debug")]
#if UNITY_EDITOR
        [EnableIf(nameof(IsDebugEnabledEditor))]
#endif
        [Button]
#endif
        public virtual void Hide(bool isAnimate = true)
        {
            DisposeActiveAnimation();

            OnHideAnimationEntered();
            activeAnimation = HideAnimation.Animate(
                view: this,
                isImmediate: isAnimate == false,
                onCompleted: OnHideAnimationExited
            );
        }

        private void OnShowAnimationEntered()
        {
            State = ViewVisibilityState.Showing;

            if (isDeactivateRaycastsOnHide)
            {
                canvasGroup.blocksRaycasts = true;
            }

            if (isDeactivateInteractableOnHide)
            {
                canvasGroup.interactable = true;
            }

            if (isDeactivateViewOnHide)
            {
                gameObject.SetActive(true);
            }

            OnViewShowEntered();

            OnShowEntered?.Invoke();
            onShowEntered.Invoke();
        }

        private void OnShowAnimationExited()
        {
            State = ViewVisibilityState.Shown;

            OnViewShowExited();

            OnShowExited?.Invoke();
            onShowExited.Invoke();
        }

        private void OnHideAnimationEntered()
        {
            State = ViewVisibilityState.Hiding;

            OnViewHideEntered();

            OnHideEntered?.Invoke();
            onHideEntered.Invoke();
        }

        private void OnHideAnimationExited()
        {
            State = ViewVisibilityState.Hidden;

            if (isDeactivateRaycastsOnHide)
            {
                canvasGroup.blocksRaycasts = false;
            }

            if (isDeactivateInteractableOnHide)
            {
                canvasGroup.interactable = false;
            }

            if (isDeactivateViewOnHide)
            {
                gameObject.SetActive(false);
            }

            OnViewHideExited();

            OnHideExited?.Invoke();
            onHideExited.Invoke();
        }

        private void DisposeActiveAnimation()
        {
            activeAnimation.Stop();
            activeAnimation = default;
        }

#if UNITY_EDITOR && ODIN_INSPECTOR
        private bool IsDebugEnabledEditor()
        {
            return Application.isPlaying;
        }
#endif
    }
}

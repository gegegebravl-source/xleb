using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace CHARK.SimpleUI
{
    public abstract class ViewController<TView> : ViewController where TView : View
    {
#if ODIN_INSPECTOR
        [PropertyOrder(float.MaxValue)]
        [FoldoutGroup(ControllerGroupKey)]
        [TitleGroup(ControllerGroupKey + "/General")]
        [HideIf(nameof(view))]
        [Required]
        [AssetsOnly]
#else
        [Header("Controller General")]
#endif
        [SerializeField]
        private TView viewPrefab;

#if ODIN_INSPECTOR
        [TitleGroup(ControllerGroupKey + "/General")]
        [ChildGameObjectsOnly]
#endif
        [SerializeField]
        private TView view;

#if ODIN_INSPECTOR
        [TitleGroup(ControllerGroupKey + "/Features")]
#endif
        [SerializeField]
        private Transform viewParentTransform;

        public override ViewVisibilityState ViewState => View.State;

        protected TView View
        {
            get
            {
                if (view == false)
                {
                    Initialize();
                }

                return view;
            }
            private set => view = value;
        }

        protected override void Awake()
        {
            if (view == false)
            {
                Initialize();
            }

            base.Awake();
        }

        protected override void OnShow(bool isAnimate)
        {
            View.Show(isAnimate: isAnimate);
        }

        protected override void OnHide(bool isAnimate)
        {
            View.Hide(isAnimate: isAnimate);
        }

        public void SetViewSiblingIndex(int index)
        {
            View.transform.SetSiblingIndex(index);
        }

#if ODIN_INSPECTOR
        [PropertyOrder(float.MaxValue)]
        [TitleGroup(ControllerGroupKey + "/Debug")]
        [EnableIf(nameof(viewPrefab))]
        [DisableInPlayMode]
        [Button("Create View")]
#endif
        private void Initialize()
        {
            var parent = viewParentTransform ? viewParentTransform : transform;

#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                var newView = (TView)UnityEditor.PrefabUtility.InstantiatePrefab(viewPrefab, parent);
                View = newView;
                return;
            }
#endif

            if (view == false)
            {
                var childView = GetComponentInChildren<TView>();
                if (childView)
                {
                    View = childView;
                }
                else
                {
                    View = Instantiate(viewPrefab, parent);
                }
            }
        }
    }

    public abstract class ViewController : MonoBehaviour, IViewController
    {
        private enum LifecycleBehavior
        {
            None = 0,
            Show = 1,
            ShowImmediate = 2,
            Hide = 3,
            HideImmediate = 4,
        }

#if ODIN_INSPECTOR
        [TitleGroup(ControllerGroupKey + "/Features")]
#else
        [Header("Controller Features")]
#endif
        [SerializeField]
        private LifecycleBehavior awakeBehaviour = LifecycleBehavior.HideImmediate;

#if ODIN_INSPECTOR
        [TitleGroup(ControllerGroupKey + "/Features")]
#endif
        [SerializeField]
        private LifecycleBehavior startBehaviour;

        protected const string ControllerGroupKey = "Controller Component";

        public abstract ViewVisibilityState ViewState { get; }

        protected virtual void Awake()
        {
            UpdateViewVisibility(awakeBehaviour);
        }

        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
        }

        protected virtual void Start()
        {
            UpdateViewVisibility(startBehaviour);
        }

        public void Show(bool isAnimate = true)
        {
            if (this == false)
            {
                return;
            }

            OnShow(isAnimate: gameObject.activeInHierarchy && isAnimate);
        }

        public void Hide(bool isAnimate = true)
        {
            if (this == false)
            {
                return;
            }

            OnHide(isAnimate: gameObject.activeInHierarchy && isAnimate);
        }

        protected abstract void OnShow(bool isAnimate);

        protected abstract void OnHide(bool isAnimate);

        private void UpdateViewVisibility(LifecycleBehavior behavior)
        {
            switch (behavior)
            {
                case LifecycleBehavior.None:
                default:
                {
                    break;
                }
                case LifecycleBehavior.Show:
                {
                    OnShow(isAnimate: true);
                    break;
                }
                case LifecycleBehavior.ShowImmediate:
                {
                    OnShow(isAnimate: false);
                    break;
                }
                case LifecycleBehavior.Hide:
                {
                    OnHide(isAnimate: true);
                    break;
                }
                case LifecycleBehavior.HideImmediate:
                {
                    OnHide(isAnimate: false);
                    break;
                }
            }
        }
    }

    internal interface IViewController
    {
        /// <summary>
        /// Current state of the underlying view.
        /// </summary>
        public ViewVisibilityState ViewState { get; }

        /// <summary>
        /// Show the underlying view.
        /// </summary>
        public void Show(bool isAnimate = true);

        /// <summary>
        /// Hide the underlying view.
        /// </summary>
        public void Hide(bool isAnimate = true);
    }
}

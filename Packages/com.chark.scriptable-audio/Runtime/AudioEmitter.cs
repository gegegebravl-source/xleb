using System;
using CHARK.ScriptableAudio.FMOD;
using CHARK.ScriptableAudio.Utilities;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace CHARK.ScriptableAudio
{
    [AddComponentMenu(AddComponentMenuConstants.BaseMenuName + "/Audio Emitter")]
    public sealed class AudioEmitter : MonoBehaviour
    {
        private enum PlayMode
        {
            None,
            OnEnable,
            OnAwake,
            OnStart
        }

        private enum StopMode
        {
            // ReSharper disable once UnusedMember.Local
            None,
            OnDisable,
            OnDestroy
        }

#if ODIN_INSPECTOR
        [FoldoutGroup("General", Expanded = true)]
#else
        [Header("General")]
#endif
        [SerializeField]
        private AudioEventAsset audioEventAsset;

#if ODIN_INSPECTOR
        [FoldoutGroup("General", Expanded = true)]
#endif
        [SerializeField]
        private Rigidbody rigidBody;

#if ODIN_INSPECTOR
        [FoldoutGroup("Features", Expanded = true)]
#else
        [Header("Features")]
#endif
        [SerializeField]
        private PlayMode playMode = PlayMode.None;

#if ODIN_INSPECTOR
        [FoldoutGroup("Features", Expanded = true)]
#endif
        [SerializeField]
        private StopMode stopMode = StopMode.OnDestroy;

#if ODIN_INSPECTOR
        [FoldoutGroup("Features", Expanded = true)]
#endif
        [SerializeField]
        private bool isAllowFadeout = true;

#if ODIN_INSPECTOR
        [FoldoutGroup("Features", Expanded = true)]
#endif
        [SerializeField]
        private bool isTriggerOnce;

#if ODIN_INSPECTOR
        [FoldoutGroup("Features", Expanded = true)]
#endif
        [SerializeField]
        private bool isPreload;

#if ODIN_INSPECTOR
        [FoldoutGroup("Features", Expanded = true)]
#endif
        [SerializeField]
        private bool isIgnoreParameterSeekSpeed;

#if ODIN_INSPECTOR
        [FoldoutGroup("Features", Expanded = true)]
#endif
        [Tooltip(
            "Should the audio event be attached to the game object after triggering? This is " +
            "useful for 3D sounds that should follow the game object."
        )]
        [SerializeField]
        private bool isAttachToGameObject = true;

#if ODIN_INSPECTOR
        [FoldoutGroup("Overrides", Expanded = true)]
#else
        [Header("Overrides")]
#endif
        [SerializeField]
        private bool isOverrideAttenuation;

#if ODIN_INSPECTOR
        [FoldoutGroup("Overrides", Expanded = true)]
        [MinMaxSlider(0f, 500f, ShowFields = true)]
        [ShowIf(nameof(isOverrideAttenuation))]
        [InfoBox("Distance override is only effective for 3D sounds")]
#else
        [Min(0f)]
#endif
        [SerializeField]
        private Vector2 distanceOverride = new Vector2(0f, 20f);

        private Transform followTransform;
        private Vector3? followPosition;
        private bool isQuitting;

        /// <summary>
        /// <c>true</c> if this emitter is playing or <c>false</c> otherwise.
        /// </summary>
#if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [ShowInInspector]
        [ReadOnly]
#endif
        public bool IsPlaying
        {
            get
            {
                try
                {
                    return controller?.IsPlaying ?? false;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                    return false;
                }
            }
        }

        /// <summary>
        /// <c>true</c> if this emitter is paused or <c>false</c> otherwise.
        /// </summary>
#if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [ShowInInspector]
        [ReadOnly]
#endif
        public bool IsPaused
        {
            get
            {
                try
                {
                    return controller?.IsPaused ?? false;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                    return false;
                }
            }
            set
            {
                try
                {
                    if (controller != default)
                    {
                        controller.IsPaused = value;
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }
        }

        /// <summary>
        /// <c>true</c> if this emitter is playing (emitter can be paused but can still be active)
        /// or <c>false</c> otherwise.
        /// </summary>
#if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [ShowInInspector]
        [ReadOnly]
#endif
        public bool IsActive { get; internal set; }

        /// <summary>
        /// Currently assigned asset to the emitter.
        /// </summary>
        public AudioEventAsset AudioEventAsset
        {
            get => audioEventAsset;
            set
            {
                if (audioEventAsset == value)
                {
                    return;
                }

                if (audioEventAsset)
                {
                    audioEventAsset.RemoveEmitter(this);
                    Stop();
                    DisposeController();
                }

                audioEventAsset = value;

                if (TryInitializeEmitter() == false)
                {
                    enabled = false;
                    return;
                }

                if (audioEventAsset)
                {
                    audioEventAsset.AddEmitter(this);
                }
            }
        }

        internal Rigidbody RigidBody => rigidBody;

        internal bool IsAllowFadeout => isAllowFadeout;

        internal bool IsTriggerOnce => isTriggerOnce;

        internal bool IsPreload => isPreload;

        internal bool IsIgnoreParameterSeekSpeed => isIgnoreParameterSeekSpeed;

        internal bool IsAttachToGameObject => isAttachToGameObject;

        internal bool IsOverrideAttenuation => isOverrideAttenuation;

        internal float OverrideMinDistance
        {
            get => distanceOverride.x;
            set => distanceOverride.x = value;
        }

        internal float OverrideMaxDistance
        {
            get => distanceOverride.y;
            set => distanceOverride.y = value;
        }

        /// <summary>
        /// If specified, given transform will be followed by this emitter. If the emitter is
        /// stopped, the transform will get cleared.
        /// </summary>
#if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [ShowInInspector]
        [ReadOnly]
#endif
        public Transform FollowTransform
        {
            get => followTransform;
            set
            {
                followTransform = value;

                if (followTransform)
                {
                    transform.position = followTransform.position;
                }
            }
        }

        /// <summary>
        /// If specified, given position will be followed by this emitter. If the emitter is
        /// stopped, the position will get cleared.
        /// </summary>
#if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [ShowInInspector]
        [ReadOnly]
#endif
        public Vector3? FollowPosition
        {
            get => followPosition;
            set
            {
                followPosition = value;

                if (followPosition.HasValue)
                {
                    transform.position = followPosition.Value;
                }
                else
                {
                    transform.localPosition = Vector3.zero;
                }
            }
        }

        private readonly AudioEmitterController controller = new FmodAudioEmitterController();

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                InitializeController();
            }
        }
#endif

        private void Awake()
        {
            if (TryInitializeEmitter() == false)
            {
                enabled = false;
                return;
            }

            if (playMode == PlayMode.OnAwake)
            {
                Play();
            }
        }

        private void OnEnable()
        {
            if (audioEventAsset)
            {
                audioEventAsset.AddEmitter(this);
            }

            if (playMode == PlayMode.OnEnable)
            {
                Play();
            }
        }

        private void Start()
        {
            if (playMode == PlayMode.OnStart)
            {
                Play();
            }
        }

        private void Update()
        {
            if (followTransform)
            {
                transform.position = followTransform.position;
                return;
            }

            if (followPosition.HasValue)
            {
                transform.position = followPosition.Value;
            }
        }

        private void OnDisable()
        {
            if (stopMode == StopMode.OnDisable)
            {
                Stop();
            }

            if (audioEventAsset)
            {
                audioEventAsset.RemoveEmitter(this);
            }
        }

        private void OnDestroy()
        {
            if (isQuitting)
            {
                return;
            }

            if (stopMode == StopMode.OnDestroy)
            {
                Stop();
            }

            DisposeController();
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        /// <summary>
        /// Play this emitter using <see cref="audioEventAsset"/>.
        /// </summary>
#if ODIN_INSPECTOR && UNITY_EDITOR
        [EnableIf(nameof(IsApplicationPlayingEditor))]
        [FoldoutGroup("Debug")]
        [ButtonGroup("Debug/Buttons")]
#endif
        public void Play()
        {
            try
            {
                controller.Play();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        /// <summary>
        /// Stop and deactivate this emitter.
        /// </summary>
#if ODIN_INSPECTOR && UNITY_EDITOR
        [EnableIf(nameof(IsApplicationPlayingEditor))]
        [FoldoutGroup("Debug")]
        [ButtonGroup("Debug/Buttons")]
#endif
        public void Stop()
        {
            try
            {
                controller.Stop();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }

            followTransform = null;
            followPosition = null;
        }

        /// <summary>
        /// Set a new <paramref name="value"/> for <see cref="parameter"/> on
        /// <see cref="audioEventAsset"/> (if its valid and playing).
        /// </summary>
        public void SetParameterValue(AudioParameterAsset parameter, float value)
        {
            try
            {
                controller.SetParameterValue(parameter, value);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        /// <summary>
        /// Update playing status of this emitter (check distance and adjust playing state
        /// accordingly).
        /// </summary>
        internal void UpdatePlayingStatus()
        {
            try
            {
                controller.UpdatePlayingStatus();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private void InitializeController()
        {
            controller.Initialize(this);
        }

        private bool TryInitializeEmitter()
        {
            if (audioEventAsset == false)
            {
                return false;
            }

            InitializeController();

            try
            {
                return controller.Validate();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                return false;
            }
        }

        private void DisposeController()
        {
            controller.Dispose();
        }

#if UNITY_EDITOR && ODIN_INSPECTOR
        private bool IsApplicationPlayingEditor()
        {
            return UnityEditor.EditorApplication.isPlaying;
        }
#endif
    }
}

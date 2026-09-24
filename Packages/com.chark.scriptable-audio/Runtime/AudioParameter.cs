using CHARK.ScriptableAudio.FMOD;
using CHARK.ScriptableAudio.Utilities;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace CHARK.ScriptableAudio
{
    [AddComponentMenu(AddComponentMenuConstants.BaseMenuName + "/Audio Parameter")]
    public sealed class AudioParameter : MonoBehaviour
    {
#if UNITY_EDITOR && ODIN_INSPECTOR
        [FoldoutGroup("General", Expanded = true)]
        [ShowIf(nameof(IsShowAudioEmitter))]
        [Required]
#else
        [Header("General")]
#endif
        [SerializeField]
        private AudioEmitter audioEmitter;

#if ODIN_INSPECTOR
        [FoldoutGroup("General", Expanded = true)]
        [Required]
#endif
        [SerializeField]
        private AudioParameterAsset audioParameterAsset;

#if ODIN_INSPECTOR
        [FoldoutGroup("Features", Expanded = true)]
#else
        [Header("Features")]
#endif
        [SerializeField]
        private bool isIgnoreSeekSpeed;

        private AudioParameterController controller;

        internal AudioParameterAsset AudioParameterAsset => audioParameterAsset;

        internal AudioEmitter AudioEmitter => audioEmitter;

        internal bool IsIgnoreSeekSpeed => isIgnoreSeekSpeed;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                InitializeController();
            }
        }
#endif

        private void OnEnable()
        {
            InitializeController();
            enabled = controller.Validate();
        }

        private void OnDisable()
        {
            DisposeController();
        }

        /// <summary>
        /// Set a new <paramref name="value"/> for <see cref="audioParameterAsset"/>.
        /// </summary>
        public void SetParameterValue(float value)
        {
            controller?.SetParameterValue(value);
        }

        private void InitializeController()
        {
            controller?.Dispose();
            controller = new FMODAudioParameterController();
            controller.Initialize(this);
        }

        private void DisposeController()
        {
            controller?.Dispose();
            controller = null;
        }

#if UNITY_EDITOR && ODIN_INSPECTOR
        private bool IsShowAudioEmitter()
        {
            if (controller == null)
            {
                InitializeController();
            }

            return controller.IsEmitterRequired();
        }
#endif
    }
}

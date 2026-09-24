using System;
using CHARK.ScriptableAudio.Utilities;
using FMODUnity;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace CHARK.ScriptableAudio.FMOD
{
    // ReSharper disable once InconsistentNaming
    [Serializable]
    internal sealed class FMODAudioParameterData : IAudioParameterData
    {
#if ODIN_INSPECTOR
        [HideIf(nameof(isGlobal))]
#endif
        [SerializeField]
        private AudioEventAsset audioEventAsset;

        [ParamRef]
        [SerializeField]
        private string parameterName;

        [SerializeField]
        private bool isGlobal;

        public bool IsValid
        {
            get
            {
                if (string.IsNullOrWhiteSpace(parameterName))
                {
                    return false;
                }

                if (isGlobal)
                {
                    // Name valid and global.
                    return true;
                }

                return audioEventAsset.TryGetData<FMODAudioEventData>(out _);
            }
        }

        internal AudioEventAsset AudioEventAsset
        {
            get => audioEventAsset;
            set => audioEventAsset = value;
        }

        internal string ParameterName
        {
            get => parameterName ?? string.Empty;
            set => parameterName = value;
        }

        internal bool IsGlobal
        {
            get => isGlobal;
            set => isGlobal = value;
        }
    }
}

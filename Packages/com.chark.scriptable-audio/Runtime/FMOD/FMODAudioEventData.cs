using System;
using FMOD;
using FMODUnity;
using UnityEngine;

namespace CHARK.ScriptableAudio.FMOD
{
    // ReSharper disable once InconsistentNaming
    [Serializable]
    internal sealed class FMODAudioEventData : IAudioEventData
    {
        [SerializeField]
        private EventReference eventReference;

        public bool IsValid => eventReference.IsNull == false;

        internal string EventPath
        {
            get
            {
#if UNITY_EDITOR
                return eventReference.Path;
#else
                return string.Empty;
#endif
            }
            set
            {
#if UNITY_EDITOR
                eventReference.Path = value;
#endif
            }
        }

        internal global::FMOD.GUID EventId
        {
            get => eventReference.Guid;
            set => eventReference.Guid = value;
        }
    }
}

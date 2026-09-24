using System.Collections.Generic;
using System.Linq;
using CHARK.ScriptableAudio.FMOD;
using CHARK.ScriptableAudio.Utilities;
using UnityEngine;

namespace CHARK.ScriptableAudio
{
    [CreateAssetMenu(
        fileName = CreateAssetMenuConstants.BaseFileName + nameof(AudioEventAsset),
        menuName = CreateAssetMenuConstants.BaseMenuName + "/Audio Event Asset",
        order = CreateAssetMenuConstants.BaseOrder
    )]
    public sealed class AudioEventAsset : ScriptableObject
    {
        [SerializeField]
        private FMODAudioEventData fmodData;

        private readonly ICollection<AudioEmitter> audioEmitters = new HashSet<AudioEmitter>();

        internal IEnumerable<AudioEmitter> AudioEmitters => audioEmitters.Where(emitter => emitter);

        internal IAudioEventData Data => fmodData;

        internal void AddEmitter(AudioEmitter emitter)
        {
            if (emitter == false)
            {
                return;
            }

            audioEmitters.Add(emitter);
        }

        internal void RemoveEmitter(AudioEmitter emitter)
        {
            if (emitter == false)
            {
                return;
            }

            audioEmitters.Remove(emitter);
        }
    }
}

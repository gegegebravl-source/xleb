using System.Collections.Generic;
using System.Linq;
using CHARK.ScriptableAudio.FMOD;
using CHARK.ScriptableAudio.Utilities;
using UnityEngine;

namespace CHARK.ScriptableAudio
{
    [CreateAssetMenu(
        fileName = CreateAssetMenuConstants.BaseFileName + nameof(AudioParameterAsset),
        menuName = CreateAssetMenuConstants.BaseMenuName + "/Audio Parameter Asset",
        order = CreateAssetMenuConstants.BaseOrder
    )]
    public sealed class AudioParameterAsset : ScriptableObject
    {
        [SerializeField]
        private FMODAudioParameterData fmodData;

        private readonly ICollection<AudioParameter> audioParameters =
            new HashSet<AudioParameter>();

        internal IEnumerable<AudioParameter> AudioParameters => audioParameters.Where(param => param);

        internal IAudioParameterData Data => fmodData;

        internal void AddParameter(AudioParameter parameter)
        {
            if (parameter == false)
            {
                return;
            }

            audioParameters.Add(parameter);
        }

        internal void RemoveParameter(AudioParameter parameter)
        {
            if (parameter == false)
            {
                return;
            }

            audioParameters.Remove(parameter);
        }
    }
}

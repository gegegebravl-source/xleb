using CHARK.ScriptableAudio.Utilities;
using FMODUnity;
using UnityEngine;

namespace CHARK.ScriptableAudio.FMOD
{
    // ReSharper disable once InconsistentNaming
    internal sealed class FMODAudioParameterController : AudioParameterController
    {
        public override bool IsEmitterRequired()
        {
            var audioParameterAsset = AudioParameter.AudioParameterAsset;
            if (audioParameterAsset.TryGetData<FMODAudioParameterData>(out var data) == false)
            {
                return false;
            }

            return data.IsGlobal == false;
        }

        public override bool Validate()
        {
            var audioParameterAsset = AudioParameter.AudioParameterAsset;
            if (audioParameterAsset.TryGetData<FMODAudioParameterData>(out var data) == false)
            {
                Debug.LogWarning($"{nameof(AudioParameterAsset)} is invalid", AudioParameter);
                return false;
            }

            var audioEmitter = AudioParameter.AudioEmitter;
            if (data.IsGlobal == false && audioEmitter == false)
            {
                Debug.LogWarning($"{nameof(AudioEmitter)} is invalid", AudioParameter);
                return false;
            }

            return true;
        }

        protected override void OnSetParameterValue(float value)
        {
            var audioParameterAsset = AudioParameter.AudioParameterAsset;
            var data = audioParameterAsset.GetData<FMODAudioParameterData>();

            if (data.IsGlobal)
            {
                var parameterName = data.ParameterName;
                SetGlobalParameterValue(parameterName, value);
                return;
            }

            SetLocalParameterValue(value);
        }

        private void SetGlobalParameterValue(string parameterName, float value)
        {
            var isIgnoreSeekSpeed = AudioParameter.IsIgnoreSeekSpeed;
            var studioSystem = RuntimeManager.StudioSystem;

            studioSystem.setParameterByName(parameterName, value, isIgnoreSeekSpeed);
        }

        private void SetLocalParameterValue(float value)
        {
            var audioParameterAsset = AudioParameter.AudioParameterAsset;
            var audioEmitter = AudioParameter.AudioEmitter;

            audioEmitter.SetParameterValue(audioParameterAsset, value);
        }
    }
}

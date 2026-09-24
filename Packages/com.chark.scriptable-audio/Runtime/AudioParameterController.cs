using UnityEngine;

namespace CHARK.ScriptableAudio
{
    internal abstract class AudioParameterController
    {
        private bool isInitialized;

        protected AudioParameter AudioParameter { get; private set; }

        public void Initialize(AudioParameter audioParameter)
        {
            if (isInitialized)
            {
                return;
            }

            AudioParameter = audioParameter;

            if (Application.isPlaying)
            {
                AddParameterToAsset();
            }

            isInitialized = true;
        }

        public void Dispose()
        {
            if (isInitialized == false)
            {
                return;
            }

            if (Application.isPlaying)
            {
                RemoveParameterFromAsset();
            }

            AudioParameter = default;
            isInitialized = false;
        }

        public abstract bool IsEmitterRequired();

        public abstract bool Validate();

        public void SetParameterValue(float value)
        {
            if (isInitialized)
            {
                OnSetParameterValue(value);
            }
        }

        protected abstract void OnSetParameterValue(float value);

        private void AddParameterToAsset()
        {
            var audioParameterAsset = AudioParameter.AudioParameterAsset;
            if (audioParameterAsset)
            {
                audioParameterAsset.AddParameter(AudioParameter);
            }
        }

        private void RemoveParameterFromAsset()
        {
            var audioParameterAsset = AudioParameter.AudioParameterAsset;
            if (audioParameterAsset)
            {
                audioParameterAsset.RemoveParameter(AudioParameter);
            }
        }
    }
}

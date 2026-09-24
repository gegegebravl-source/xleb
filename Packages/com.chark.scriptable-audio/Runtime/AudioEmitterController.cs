using System;
using UnityEngine;

namespace CHARK.ScriptableAudio
{
    internal abstract class AudioEmitterController
    {
        private bool isInitialized;

        public bool IsPlaying
        {
            get
            {
                if (isInitialized)
                {
                    return IsEmitterPlaying();
                }

                return false;
            }
        }

        public bool IsPaused
        {
            get
            {
                if (isInitialized)
                {
                    return IsEmitterPaused();
                }

                return false;
            }
            set
            {
                if (isInitialized)
                {
                    SetIsEmitterPaused(value);
                }
            }
        }

        protected AudioEmitter AudioEmitter { get; private set; }

        public abstract bool Validate();

        public void Initialize(AudioEmitter audioEmitter)
        {
            if (isInitialized)
            {
                return;
            }

            AudioEmitter = audioEmitter;

            try
            {
                OnInitialized();
                isInitialized = true;
            }
            catch (Exception exception)
            {
                Debug.LogError("Could not initialize controller", audioEmitter);
                Debug.LogException(exception, audioEmitter);
                Dispose();
            }
        }

        public void Dispose()
        {
            if (isInitialized == false)
            {
                return;
            }

            try
            {
                OnDisposed();
            }
            catch (Exception exception)
            {
                Debug.LogError("Could not dispose controller", AudioEmitter);
                Debug.LogException(exception, AudioEmitter);
            }

            AudioEmitter = null;
            isInitialized = false;
        }

        public void Play()
        {
            if (isInitialized)
            {
                OnPlay();
            }
        }

        public void Stop()
        {
            if (isInitialized)
            {
                OnStop();
            }
        }

        public void UpdatePlayingStatus()
        {
            if (isInitialized)
            {
                OnUpdatePlayingStatus();
            }
        }

        public void SetParameterValue(AudioParameterAsset parameter, float value)
        {
            if (isInitialized)
            {
                OnSetParameterValue(parameter, value);
            }
        }

        protected abstract bool IsEmitterPlaying();

        protected abstract bool IsEmitterPaused();

        protected abstract void SetIsEmitterPaused(bool isPaused);

        protected abstract void OnInitialized();

        protected abstract void OnDisposed();

        protected abstract void OnPlay();

        protected abstract void OnStop();

        protected abstract void OnUpdatePlayingStatus();

        protected abstract void OnSetParameterValue(AudioParameterAsset parameter, float value);
    }
}

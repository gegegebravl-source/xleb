using System.Collections.Generic;
using CHARK.ScriptableAudio.Utilities;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using Debug = UnityEngine.Debug;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace CHARK.ScriptableAudio.FMOD
{
    // ReSharper disable once InconsistentNaming
    internal sealed class FmodAudioEmitterController : AudioEmitterController
    {
        private EventDescription eventDescription;
        private EventInstance eventInstance;

        private bool isTriggered;
        private bool isOneShot;

        private readonly List<ParamRef> cachedParameters = new();

        public override bool Validate()
        {
            var audioEventAsset = AudioEmitter.AudioEventAsset;
            if (audioEventAsset.TryGetData<FMODAudioEventData>(out _) == false)
            {
                var message = audioEventAsset
                    ? $"{nameof(audioEventAsset)} ({audioEventAsset.name}) is invalid"
                    : $"{nameof(audioEventAsset)} is invalid";

                Debug.LogError(message, AudioEmitter);

                return false;
            }

            return true;
        }

        protected override bool IsEmitterPlaying()
        {
            if (eventInstance.isValid() == false)
            {
                return false;
            }

            eventInstance.getPlaybackState(out var playbackState);
            return playbackState != PLAYBACK_STATE.STOPPED;
        }

        protected override bool IsEmitterPaused()
        {
            if (eventInstance.isValid() == false)
            {
                return false;
            }

            if (eventInstance.getPaused(out var isPaused) != RESULT.OK)
            {
                return false;
            }

            return isPaused;
        }

        protected override void SetIsEmitterPaused(bool isPaused)
        {
            if (eventInstance.isValid() == false)
            {
                return;
            }

            eventInstance.setPaused(isPaused);
        }

        protected override void OnInitialized()
        {
            if (Application.isPlaying == false)
            {
                return;
            }

            // StudioEventEmitter does this on Start, keeping for now until further notice. Note
            // sure how this will perform in OnEnable.
            RuntimeUtils.EnforceLibraryOrder();

            if (AudioEmitter.IsPreload)
            {
                InitializeEventDescription();
                eventDescription.loadSampleData();
            }
        }

        protected override void OnDisposed()
        {
            if (Application.isPlaying == false)
            {
                return;
            }

            if (eventInstance.isValid())
            {
                RuntimeManager.DetachInstanceFromGameObject(eventInstance);
                if (eventDescription.isValid() && isOneShot)
                {
                    eventInstance.release();
                    eventInstance.clearHandle();
                }
            }

            DeregisterActiveEmitter();

            if (AudioEmitter.IsPreload)
            {
                eventDescription.unloadSampleData();
            }

            eventDescription.clearHandle();
        }

        protected override void OnPlay()
        {
            if (AudioEmitter.IsTriggerOnce && isTriggered)
            {
                return;
            }

            cachedParameters.Clear();

            if (eventDescription.isValid() == false)
            {
                InitializeEventDescription();
            }

            eventDescription.isSnapshot(out var isSnapshot);

            if (isSnapshot == false)
            {
                eventDescription.isOneshot(out isOneShot);
            }

            eventDescription.is3D(out var is3D);

            AudioEmitter.IsActive = true;

            if (is3D && isOneShot == false && Settings.Instance.StopEventsOutsideMaxDistance)
            {
                RegisterActiveEmitter();
                UpdatePlayingStatus(true);
            }
            else
            {
                PlayInstance();
            }
        }

        protected override void OnStop()
        {
            DeregisterActiveEmitter();
            AudioEmitter.IsActive = false;
            cachedParameters.Clear();
            StopInstance();
        }

        protected override void OnUpdatePlayingStatus()
        {
            UpdatePlayingStatus(false);
        }

        protected override void OnSetParameterValue(AudioParameterAsset parameter, float value)
        {
            if (parameter.TryGetData<FMODAudioParameterData>(out var data) == false)
            {
                Debug.LogWarning(
                    $"Cannot invoke {nameof(SetParameterValue)}, parameter is invalid",
                    AudioEmitter
                );

                return;
            }

            var parameterName = data.ParameterName;
            if (Settings.Instance.StopEventsOutsideMaxDistance && AudioEmitter.IsActive)
            {
                if (TryGetCachedParameter(parameterName, out var cachedParameter))
                {
                    cachedParameter.Value = value;
                }
                else
                {
                    eventDescription.getParameterDescriptionByName(
                        parameterName,
                        out var parameterDescription
                    );

                    cachedParameter = new ParamRef
                    {
                        ID = parameterDescription.id,
                        Name = parameterDescription.name,
                        Value = parameterDescription.defaultvalue
                    };

                    cachedParameters.Add(cachedParameter);
                }

                cachedParameter.Value = value;
            }

            if (eventInstance.isValid())
            {
                eventInstance.setParameterByName(
                    parameterName,
                    value,
                    AudioEmitter.IsIgnoreParameterSeekSpeed
                );
            }
            else
            {
                Debug.LogWarning(
                    $"Could not set parameter value, {nameof(eventInstance)} is not valid",
                    AudioEmitter
                );
            }
        }

        private void InitializeEventDescription()
        {
            var audioEventAsset = AudioEmitter.AudioEventAsset;
            var data = audioEventAsset.GetData<FMODAudioEventData>();
            var id = data.EventId;

            eventDescription = RuntimeManager.GetEventDescription(id);
        }

        private void RegisterActiveEmitter()
        {
            AudioEmitterUpdater.AddActiveEmitter(AudioEmitter);
        }

        private void DeregisterActiveEmitter()
        {
            AudioEmitterUpdater.RemoveActiveEmitter(AudioEmitter);
        }

        private void PlayInstance()
        {
            if (eventInstance.isValid() == false)
            {
                eventInstance.clearHandle();
            }

            // Let previous one-shot instances play out.
            if (isOneShot && eventInstance.isValid())
            {
                eventInstance.release();
                eventInstance.clearHandle();
            }

            eventDescription.is3D(out var is3D);

            if (eventInstance.isValid() == false)
            {
                eventDescription.createInstance(out eventInstance);

                // Only want to update if we need to set 3D attributes.
                if (is3D)
                {
                    var gameObject = AudioEmitter.gameObject;
                    var transform = AudioEmitter.transform;

                    // TODO: add support for missing physics packages
                    var rigidBody = AudioEmitter.RigidBody;
                    if (rigidBody)
                    {
                        var physicsAttributes = RuntimeUtils.To3DAttributes(gameObject, rigidBody);
                        eventInstance.set3DAttributes(physicsAttributes);

                        if (AudioEmitter.IsAttachToGameObject)
                        {
                            RuntimeManager.AttachInstanceToGameObject(eventInstance, transform.gameObject, rigidBody);
                        }
                    }
                    else
                    {
                        var objAttributes = gameObject.To3DAttributes();
                        eventInstance.set3DAttributes(objAttributes);

                        if (AudioEmitter.IsAttachToGameObject)
                        {
                            RuntimeManager.AttachInstanceToGameObject(eventInstance, transform.gameObject);
                        }
                    }
                }
            }

            foreach (var cachedParameter in cachedParameters)
            {
                eventInstance.setParameterByID(cachedParameter.ID, cachedParameter.Value);
            }

            if (is3D && AudioEmitter.IsOverrideAttenuation)
            {
                var minDistance = AudioEmitter.OverrideMinDistance;
                eventInstance.setProperty(EVENT_PROPERTY.MINIMUM_DISTANCE, minDistance);

                var maxDistance = AudioEmitter.OverrideMaxDistance;
                eventInstance.setProperty(EVENT_PROPERTY.MAXIMUM_DISTANCE, maxDistance);
            }

            eventInstance.start();

            isTriggered = true;
        }

        private void StopInstance()
        {
            if (AudioEmitter.IsTriggerOnce && isTriggered)
            {
                DeregisterActiveEmitter();
            }

            if (eventInstance.isValid() == false)
            {
                return;
            }

            var mode = AudioEmitter.IsAllowFadeout ? STOP_MODE.ALLOWFADEOUT : STOP_MODE.IMMEDIATE;
            eventInstance.stop(mode);
            eventInstance.release();

            if (AudioEmitter.IsAllowFadeout == false)
            {
                eventInstance.clearHandle();
            }
        }

        private void UpdatePlayingStatus(bool isForce)
        {
            // If at least one listener is within the max distance, ensure an event instance is
            // playing.
            var currentDistance = GetCurrentDistanceToListener();
            var maxDistance = GetMaxDistanceToListener();

            var oldIsPlaying = IsPlaying;
            var newIsPlaying = currentDistance <= maxDistance * maxDistance;

            if (isForce == false && oldIsPlaying == newIsPlaying)
            {
                return;
            }

            if (newIsPlaying)
            {
                PlayInstance();
            }
            else
            {
                StopInstance();
            }
        }

        private float GetCurrentDistanceToListener()
        {
            var transform = AudioEmitter.transform;
            var position = transform.position;

            return StudioListener.DistanceSquaredToNearestListener(position);
        }

        private float GetMaxDistanceToListener()
        {
            if (AudioEmitter.IsOverrideAttenuation)
            {
                return AudioEmitter.OverrideMaxDistance;
            }

            if (eventDescription.isValid() == false)
            {
                InitializeEventDescription();
            }

            eventDescription.getMinMaxDistance(out _, out var maxDistance);
            return maxDistance;
        }

        private bool TryGetCachedParameter(string parameterName, out ParamRef paramRef)
        {
            for (var index = 0; index < cachedParameters.Count; index++)
            {
                var parameter = cachedParameters[index];
                if (string.Equals(parameterName, parameter.Name))
                {
                    paramRef = parameter;
                    return true;
                }
            }

            paramRef = default;
            return false;
        }
    }
}

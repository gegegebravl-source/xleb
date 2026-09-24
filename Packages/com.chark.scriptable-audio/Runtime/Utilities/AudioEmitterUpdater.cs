using System;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace CHARK.ScriptableAudio.Utilities
{
    internal sealed class AudioEmitterUpdater : MonoBehaviour
    {
        private static AudioEmitterUpdater instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (instance)
            {
                return;
            }

            var newGameObject = new GameObject(nameof(AudioEmitterUpdater));
            instance = newGameObject.AddComponent<AudioEmitterUpdater>();
            DontDestroyOnLoad(newGameObject);
        }

#if ODIN_INSPECTOR
        [ShowInInspector]
        [ReadOnly]
#endif
        private List<AudioEmitter> audioEmitters = new List<AudioEmitter>();

        private void Update()
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var index = audioEmitters.Count - 1; index >= 0; index--)
            {
                var emitter = audioEmitters[index];
                if (emitter == false)
                {
                    audioEmitters.RemoveAt(index);
                    continue;
                }

                try
                {
                    emitter.UpdatePlayingStatus();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }
        }

        internal static void AddActiveEmitter(AudioEmitter emitter)
        {
            if (Application.isPlaying == false)
            {
                return;
            }

            if (instance == false)
            {
                return;
            }

            var emitters = instance.audioEmitters;
            if (emitters.Contains(emitter))
            {
                return;
            }

            emitters.Add(emitter);
        }

        internal static void RemoveActiveEmitter(AudioEmitter emitter)
        {
            if (Application.isPlaying == false)
            {
                return;
            }

            if (instance == false)
            {
                return;
            }

            var emitters = instance.audioEmitters;
            emitters.Remove(emitter);
        }
    }
}

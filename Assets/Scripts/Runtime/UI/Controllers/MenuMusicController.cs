using UABPetelnia.GGJ2025.Runtime.Utilities;
using System.Collections;
using CHARK.GameManagement;
using CHARK.ScriptableScenes.Events;
using UABPetelnia.GGJ2025.Runtime.Systems.Audio;
using UABPetelnia.GGJ2025.Runtime.Systems.Scenes;
using UnityEngine;
using UnityEngine.Audio;

namespace UABPetelnia.GGJ2025.Runtime.UI.Controllers
{
    internal sealed class MenuMusicController : MonoBehaviour
    {
        /// <summary>
        /// Folder inside <c>Assets/Resources</c> which holds the lobby playlist. The clips are
        /// loaded through <see cref="Resources"/> on purpose: the previous implementation used
        /// <c>AssetDatabase</c>, which works only in the editor and made the player build fail to
        /// compile (and left the released game without any menu music).
        /// </summary>
        private const string MusicResourcesPath = "Music";

        /// <summary>
        /// Базовая громкость лобби-музыки. Она играет через обычный AudioSource (мимо FMOD),
        /// поэтому сохранённую громкость Master применяем вручную — иначе слайдер громкости
        /// в меню ничего не меняет, пока играет музыка.
        /// </summary>
        private const float BaseVolume = 0.4f;

        [Header("Music")]
        [SerializeField]
        private AudioMixerGroup musicMixerGroup;

        [SerializeField]
        private float minTrackLength = 60f;

        [SerializeField]
        private float maxTrackLength = 180f;

        private AudioSource audioSource;
        private AudioClip[] musicTracks = System.Array.Empty<AudioClip>();
        private bool isPlaying;

        private void Awake()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            if (musicMixerGroup != null)
            {
                audioSource.outputAudioMixerGroup = musicMixerGroup;
            }

            musicTracks = Resources.LoadAll<AudioClip>(MusicResourcesPath);

            if (musicTracks.Length == 0)
            {
                Debug.LogWarning(
                    $"[MenuMusic] В Assets/Resources/{MusicResourcesPath} не найдено ни одного трека,"
                    + " музыка лобби играть не будет.",
                    this
                );

                return;
            }

            Debug.Log($"[MenuMusic] Найдено треков: {musicTracks.Length}", this);
        }

        private void OnEnable()
        {
            SystemsUtility.TryAddListener<SceneLoadEnteredMessage>(OnSceneLoaded);
        }

        private void OnDisable()
        {
            SystemsUtility.TryRemoveListener<SceneLoadEnteredMessage>(OnSceneLoaded);
            StopMusic();
        }

        private void OnSceneLoaded(SceneLoadEnteredMessage message)
        {
            if (message.Collection == null)
            {
                return;
            }

            var name = message.Collection.Name.ToLower();
            if (name.Contains("menu") || name.Contains("main"))
            {
                StartMusic();
            }
            else
            {
                StopMusic();
            }
        }

        public void StartMusic()
        {
            if (isPlaying || musicTracks.Length == 0)
            {
                return;
            }

            isPlaying = true;
            PlayNextTrack();
        }

        public void StopMusic()
        {
            isPlaying = false;

            if (audioSource != false && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }

        private static float GetMasterVolume()
        {
            return SystemsUtility.TryGetSystem(out IAudioSystem audioSystem)
                ? audioSystem.GetVolume(VolumeType.Master)
                : 1f;
        }

        private void PlayNextTrack()
        {
            if (isPlaying == false || musicTracks.Length == 0 || audioSource == false)
            {
                return;
            }

            var clip = musicTracks[Random.Range(0, musicTracks.Length)];
            if (clip == false)
            {
                return;
            }

            audioSource.clip = clip;
            audioSource.volume = BaseVolume * GetMasterVolume();
            audioSource.Play();

            // Play a random slice of the track, then move on to the next one.
            var playTime = Mathf.Min(Random.Range(minTrackLength, maxTrackLength), clip.length);
            StartCoroutine(WaitAndPlayNext(Mathf.Max(1f, playTime)));
        }

        private IEnumerator WaitAndPlayNext(float waitTime)
        {
            // Realtime on purpose: the lobby can be left while the game is paused, and a music
            // timer must not freeze together with Time.timeScale.
            yield return new WaitForSecondsRealtime(waitTime);

            if (isPlaying)
            {
                PlayNextTrack();
            }
        }

        private void OnDestroy()
        {
            StopMusic();
        }
    }
}

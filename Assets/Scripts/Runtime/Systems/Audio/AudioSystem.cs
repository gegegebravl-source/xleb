using System;
using CHARK.GameManagement;
using CHARK.GameManagement.Systems;
using CHARK.ScriptableAudio;
using FMODUnity;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UABPetelnia.GGJ2025.Runtime.Systems.Settings;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Audio
{
    internal sealed class AudioSystem : MonoSystem, IAudioSystem
    {
        [Header("Banks")]
        [SerializeField]
        private StudioBankLoader bankLoader;

        [Header("Global Parameters")]
        [SerializeField]
        private AudioParameter globalMasterVolumeParameter;

        [SerializeField]
        private AudioParameter globalMusicVolumeParameter;

        [SerializeField]
        private AudioParameter globalSfxVolumeParameter;

        private ISettingsSystem settingsSystem;

        /// <summary>Пути шин FMOD, куда пишется громкость из настроек.</summary>
        private static readonly string[] MasterBusPaths = { "bus:/" };

        private static readonly string[] MusicBusPaths = { "bus:/Music", "bus:/Music/Music", "bus:/Menu" };

        private static readonly string[] SfxBusPaths =
        {
            "bus:/SFX", "bus:/Sfx", "bus:/Sound", "bus:/Effects", "bus:/Ambience", "bus:/VoiceOver",
        };

        /// <summary>
        /// Камера, на которую уже посажен Unity AudioListener. Слушатель должен переезжать
        /// вслед за Camera.main при переходах между сценами — иначе меню-музыка играет без звука.
        /// </summary>
        private Camera listenerCamera;

        public bool IsLoading
        {
            get
            {
                if (RuntimeManager.HaveAllBanksLoaded == false)
                {
                    return true;
                }

                if (RuntimeManager.AnySampleDataLoading())
                {
                    return true;
                }

                return false;
            }
        }

        public void LoadBanks()
        {
            if (bankLoader == false)
            {
                Debug.LogWarning("[Audio] StudioBankLoader не назначен — игра продолжит работу без FMOD-банков.", this);
                return;
            }

            bankLoader.Load();
        }

        public void UnLoadBanks()
        {
            if (bankLoader != false)
            {
                bankLoader.Unload();
            }
        }

        public override void OnInitialized()
        {
            base.OnInitialized();
            EnsureAudioListener();
            GameManager.TryGetSystem(out settingsSystem);
        }

        private void Start()
        {
            EnsureAudioListener();
            InitializeGlobalVolumeParameters();
        }

        private void Update()
        {
            // Камера появляется и умирает при переходах между сценами (в том числе при
            // асинхронной загрузке): проверяем каждый кадр, пока слушатель не на месте.
            EnsureAudioListener();
        }

        /// <summary>
        /// Гарантирует наличие Unity AudioListener на активной камере.
        /// Резервную камеру НЕ создаём: пустая камера в (0,0,0) с depth 0 рисует мир поверх
        /// настоящей камеры игрока (depth -1), и игрок видит сцену «из пола».
        /// </summary>
        private void EnsureAudioListener()
        {
            var camera = Camera.main;
            if (camera == null || camera == listenerCamera)
            {
                return;
            }

            if (camera.GetComponent<AudioListener>() == null)
            {
                camera.gameObject.AddComponent<AudioListener>();
            }

            listenerCamera = camera;
        }

        public float GetVolume(VolumeType type)
        {
            var settings = settingsSystem != null
                ? settingsSystem.Settings
                : SettingsData.CreateDefault(0.5f, 1f, 1f, 1f);
            var volume = type switch
            {
                VolumeType.Master => settings.MasterVolume,
                VolumeType.Music => settings.MusicVolume,
                VolumeType.SFX => settings.SfxVolume,
                _ => GeneralSettings.MaxVolume,
            };

            return GetNormalizedVolume(volume);
        }

        public void SetVolume(VolumeType type, float volume)
        {
            var clampedVolume = GetNormalizedVolume(volume);
            if (settingsSystem == null)
            {
                return;
            }

            var settings = settingsSystem.Settings;

            switch (type)
            {
                case VolumeType.Master:
                    globalMasterVolumeParameter?.SetParameterValue(clampedVolume);
                    settings.MasterVolume = clampedVolume;
                    break;
                case VolumeType.Music:
                    globalMusicVolumeParameter?.SetParameterValue(clampedVolume);
                    settings.MusicVolume = clampedVolume;
                    break;
                case VolumeType.SFX:
                    globalSfxVolumeParameter?.SetParameterValue(clampedVolume);
                    settings.SfxVolume = clampedVolume;
                    break;
                default:
                    Debug.LogWarning($"Unsupported volume type: {type}", this);
                    break;
            }

            settingsSystem.Settings = settings;

            // Глобальный параметр работает только там, где событие его слушает: без
            // настройки шины ползунок громкости ничего не меняет на слух.
            ApplyBusVolumes();
        }

        /// <summary>
        /// Разложить громкости из настроек по шинам FMOD: мастер, музыка, эффекты.
        /// </summary>
        /// <remarks>
        /// Через тип <c>FMOD.Studio.System</c> перебирать шины нельзя: его имя конфликтует
        /// с пространством имён <c>System</c>, поэтому берём шины по путям.
        /// </remarks>
        private void ApplyBusVolumes()
        {
            ApplyBusVolume(MasterBusPaths, GetVolume(VolumeType.Master));
            ApplyBusVolume(MusicBusPaths, GetVolume(VolumeType.Music));
            ApplyBusVolume(SfxBusPaths, GetVolume(VolumeType.SFX));
        }

        /// <summary>Поставить громкость первой найденной шине из списка.</summary>
        private static void ApplyBusVolume(string[] paths, float volume)
        {
            for (var index = 0; index < paths.Length; index++)
            {
                try
                {
                    var bus = RuntimeManager.GetBus(paths[index]);

                    if (bus.isValid() == false)
                    {
                        continue;
                    }

                    bus.setVolume(volume);

                    return;
                }
                catch (Exception)
                {
                    // Шины с таким путём нет — пробуем следующий вариант.
                }
            }
        }

        private void InitializeGlobalVolumeParameters()
        {
            globalMasterVolumeParameter?.SetParameterValue(GetVolume(VolumeType.Master));
            globalMusicVolumeParameter?.SetParameterValue(GetVolume(VolumeType.Music));
            globalSfxVolumeParameter?.SetParameterValue(GetVolume(VolumeType.SFX));

            ApplyBusVolumes();
        }

        private static float GetNormalizedVolume(float volume)
        {
            var clampedVolume = Mathf.Clamp(
                volume,
                GeneralSettings.MinVolume,
                GeneralSettings.MaxVolume
            );

            var roundVolume = (float)Math.Round(clampedVolume, 2);

            return roundVolume;
        }
    }
}

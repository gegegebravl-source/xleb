using System;
using System.Collections.Generic;
using CHARK.GameManagement;
using CHARK.SimpleUI;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UABPetelnia.GGJ2025.Runtime.Systems.Audio;
using UABPetelnia.GGJ2025.Runtime.Systems.Cursors;
using UABPetelnia.GGJ2025.Runtime.Systems.Input;
using UABPetelnia.GGJ2025.Runtime.Systems.Pausing;
using UABPetelnia.GGJ2025.Runtime.Systems.Settings;
using UABPetelnia.GGJ2025.Runtime.UI.Views;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.UI.Controllers
{
    /// <summary>
    /// Экран настроек: применяет громкость, чувствительность и параметры экрана сразу,
    /// а значения сохраняет в системе настроек. Один экран на меню и на паузу.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class SettingsViewController : ViewController<SettingsView>
    {
        /// <summary>Экран настроек один на всю игру: его дёргают и меню, и пауза.</summary>
        public static SettingsViewController Instance { get; private set; }

        [Tooltip("Значения по умолчанию для кнопки «Сбросить».")]
        [SerializeField]
        private GeneralSettings generalSettings;

        [Tooltip("Задержка записи настроек на диск в секундах: ползунки меняют значение каждый кадр.")]
        [SerializeField]
        private float saveDelay = 0.5f;

        private IAudioSystem audioSystem;
        private IInputSystem inputSystem;
        private ISettingsSystem settingsSystem;
        private ICursorSystem cursorSystem;
        private IPauseSystem pauseSystem;

        /// <summary>Уникальные разрешения монитора (без повторов по частоте), по возрастанию площади.</summary>
        private readonly List<Vector2Int> resolutionOptions = new();

        /// <summary>UnscaledTime, когда пора дописать отложенные настройки; -1 — нечего писать.</summary>
        private float pendingSaveTime = -1f;

        private bool subscribed;

        /// <summary><c>true</c>, пока экран настроек на экране.</summary>
        public bool IsOpen => View != null && ViewState is ViewVisibilityState.Showing or ViewVisibilityState.Shown;

        protected override void Awake()
        {
            base.Awake();

            Instance = this;

            SystemsUtility.TryGetSystem(out audioSystem);
            SystemsUtility.TryGetSystem(out inputSystem);
            SystemsUtility.TryGetSystem(out settingsSystem);
            SystemsUtility.TryGetSystem(out cursorSystem);
            SystemsUtility.TryGetSystem(out pauseSystem);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Subscribe();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            FlushSave();
            Unsubscribe();
        }

        private void Update()
        {
            if (pendingSaveTime > 0f && Time.unscaledTime >= pendingSaveTime)
            {
                pendingSaveTime = -1f;
                WriteNow();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = default;
            }
        }

        #region Public API

        /// <summary>Открыть, если закрыт, и наоборот.</summary>
        public void Toggle()
        {
            if (IsOpen)
            {
                Close();

                return;
            }

            Open();
        }

        public void Open()
        {
            var view = View;

            if (view == null)
            {
                return;
            }

            view.Show();
        }

        public void Close()
        {
            if (IsOpen)
            {
                View.Hide();
            }
        }

        #endregion

        #region Subscription

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            var view = View;

            if (view == null)
            {
                return;
            }

            view.OnMasterVolumeChanged += OnMasterVolumeChanged;
            view.OnMusicVolumeChanged += OnMusicVolumeChanged;
            view.OnSfxVolumeChanged += OnSfxVolumeChanged;
            view.OnSensitivityChanged += OnSensitivityChanged;
            view.OnGraphicsPresetClicked += OnGraphicsPresetClicked;
            view.OnShadowsChanged += OnShadowsChanged;
            view.OnAntialiasingChanged += OnAntialiasingChanged;
            view.OnPostfxChanged += OnPostfxChanged;
            view.OnTextureQualityChanged += OnTextureQualityChanged;
            view.OnRenderScaleChanged += OnRenderScaleChanged;
            view.OnResolutionShifted += OnResolutionShifted;
            view.OnFullscreenChanged += OnFullscreenChanged;
            view.OnVsyncChanged += OnVsyncChanged;
            view.OnResetClicked += OnResetClicked;
            view.OnSaveClicked += OnSaveClicked;
            view.OnCloseClicked += Close;

            view.OnShowEntered += OnViewShowEntered;
            view.OnHideEntered += OnViewHideEntered;

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (subscribed == false)
            {
                return;
            }

            var view = View;

            if (view != null)
            {
                view.OnMasterVolumeChanged -= OnMasterVolumeChanged;
                view.OnMusicVolumeChanged -= OnMusicVolumeChanged;
                view.OnSfxVolumeChanged -= OnSfxVolumeChanged;
                view.OnSensitivityChanged -= OnSensitivityChanged;
                view.OnGraphicsPresetClicked -= OnGraphicsPresetClicked;
                view.OnShadowsChanged -= OnShadowsChanged;
                view.OnAntialiasingChanged -= OnAntialiasingChanged;
                view.OnPostfxChanged -= OnPostfxChanged;
                view.OnTextureQualityChanged -= OnTextureQualityChanged;
                view.OnRenderScaleChanged -= OnRenderScaleChanged;
                view.OnResolutionShifted -= OnResolutionShifted;
                view.OnFullscreenChanged -= OnFullscreenChanged;
                view.OnVsyncChanged -= OnVsyncChanged;
                view.OnResetClicked -= OnResetClicked;
                view.OnSaveClicked -= OnSaveClicked;
                view.OnCloseClicked -= Close;

                view.OnShowEntered -= OnViewShowEntered;
                view.OnHideEntered -= OnViewHideEntered;
            }

            subscribed = false;
        }

        #endregion

        #region Applying values

        private void OnViewShowEntered()
        {
            cursorSystem?.UnLockCursor();
            Refresh();
        }

        private void OnViewHideEntered()
        {
            // Дописываем отложенные изменения на диск при закрытии экрана.
            // Курсор вернёт тот, кто был до нас: меню паузы или сам геймплей.
            FlushSave();
        }

        private void Refresh()
        {
            var view = View;

            if (view == null)
            {
                return;
            }

            var data = settingsSystem != null ? settingsSystem.Settings : default;

            var sensitivity = inputSystem != null ? inputSystem.LookSensitivity : DefaultSensitivity();
            var master = audioSystem != null ? audioSystem.GetVolume(VolumeType.Master) : data.MasterVolume;
            var music = audioSystem != null ? audioSystem.GetVolume(VolumeType.Music) : data.MusicVolume;
            var sfx = audioSystem != null ? audioSystem.GetVolume(VolumeType.SFX) : data.SfxVolume;

            view.SetValues(
                master * 100f,
                music * 100f,
                sfx * 100f,
                sensitivity,
                data.Fullscreen,
                data.Vsync
            );

            view.SetGraphicsValues(
                data.GraphicsPreset,
                data.ShadowsEnabled,
                data.AntialiasingEnabled,
                data.PostProcessingEnabled,
                data.TextureQualityFull,
                data.RenderScale
            );

            BuildResolutionOptions();
            view.SetResolutionOptions(resolutionOptions, FindCurrentResolutionIndex());
        }

        private void OnMasterVolumeChanged(float value)
        {
            audioSystem?.SetVolume(VolumeType.Master, value / 100f);
            Save();
            RefreshLabels();
        }

        private void OnMusicVolumeChanged(float value)
        {
            audioSystem?.SetVolume(VolumeType.Music, value / 100f);
            Save();
            RefreshLabels();
        }

        private void OnSfxVolumeChanged(float value)
        {
            audioSystem?.SetVolume(VolumeType.SFX, value / 100f);
            Save();
            RefreshLabels();
        }

        private void OnSensitivityChanged(float value)
        {
            if (inputSystem != null)
            {
                inputSystem.LookSensitivity = Mathf.Clamp(
                    value,
                    GeneralSettings.MinLookSensitivity,
                    GeneralSettings.MaxLookSensitivity
                );
            }

            Save();
            RefreshLabels();
        }

        private void OnGraphicsPresetClicked(int presetIndex)
        {
            if (settingsSystem == null)
            {
                return;
            }

            settingsSystem.Settings = GraphicsSettingsApplier.ApplyPreset(
                presetIndex,
                settingsSystem.Settings
            );

            Save();
            Refresh();
        }

        private void OnShadowsChanged(bool value) =>
            SetGraphicsOption(data =>
            {
                data.ShadowsEnabled = value;

                // Ручное включение поверх пресета «Низкая» (дальность 0):
                // даём умеренную дальность, иначе тени всё равно не видны.
                if (value && data.ShadowDistance <= 0f)
                {
                    data.ShadowDistance = GraphicsSettingsApplier.DefaultManualShadowDistance;
                }

                return data;
            });

        private void OnAntialiasingChanged(bool value) =>
            SetGraphicsOption(data =>
            {
                data.AntialiasingEnabled = value;
                return data;
            });

        private void OnPostfxChanged(bool value) =>
            SetGraphicsOption(data =>
            {
                data.PostProcessingEnabled = value;
                return data;
            });

        private void OnTextureQualityChanged(bool value) =>
            SetGraphicsOption(data =>
            {
                data.TextureQualityFull = value;
                return data;
            });

        private void OnRenderScaleChanged(float value)
        {
            SetGraphicsOption(data =>
            {
                data.RenderScale = value;
                data.GraphicsPreset = -1; // ручная подгонка снимает пресет.
                return data;
            });

            View?.SetPresetHighlight(-1);
        }

        // ВАЖНО: SettingsData — структура, поэтому мутатор должен ВОЗВРАЩАТЬ её:
        // Action<SettingsData> мутировал бы копию, и изменение терялось бы.
        private void SetGraphicsOption(Func<SettingsData, SettingsData> mutate)
        {
            if (settingsSystem == null)
            {
                return;
            }

            settingsSystem.Settings = mutate(settingsSystem.Settings);

            settingsSystem.ApplyGraphics();
            Save();
        }

        private void OnResolutionShifted(int index)
        {
            if (settingsSystem == null || index < 0 || index >= resolutionOptions.Count)
            {
                return;
            }

            var resolution = resolutionOptions[index];

            SetGraphicsOption(data =>
            {
                data.ResolutionWidth = resolution.x;
                data.ResolutionHeight = resolution.y;
                return data;
            });
        }

        private void OnFullscreenChanged(bool value) =>
            SetGraphicsOption(data =>
            {
                data.Fullscreen = value;
                return data;
            });

        private void OnVsyncChanged(bool value) =>
            SetGraphicsOption(data =>
            {
                data.Vsync = value;
                return data;
            });

        private void OnSaveClicked()
        {
            Save();

            var view = View;

            if (view != null)
            {
                view.SetStatus("Настройки сохранены");
            }
        }

        private void OnResetClicked()
        {
            var master = generalSettings != null ? generalSettings.DefaultMasterVolume : 1f;
            var music = generalSettings != null ? generalSettings.DefaultMusicVolume : 1f;
            var sfx = generalSettings != null ? generalSettings.DefaultSfxVolume : 1f;

            audioSystem?.SetVolume(VolumeType.Master, master);
            audioSystem?.SetVolume(VolumeType.Music, music);
            audioSystem?.SetVolume(VolumeType.SFX, sfx);

            var sensitivity = DefaultSensitivity();

            if (inputSystem != null)
            {
                inputSystem.LookSensitivity = sensitivity;
            }

            if (settingsSystem != null)
            {
                settingsSystem.Settings = SettingsData.CreateDefault(
                    lookSensitivity: sensitivity,
                    masterVolume: master,
                    musicVolume: music,
                    sfxVolume: sfx
                );

                settingsSystem.ApplyGraphics();
            }

            Save();
            Refresh();
        }

        private float DefaultSensitivity()
        {
            return generalSettings != null ? generalSettings.DefaultLookSensitivity : 5f;
        }

        private void RefreshLabels()
        {
            var view = View;

            if (view == null)
            {
                return;
            }

            view.RefreshLabels(
                audioSystem != null ? audioSystem.GetVolume(VolumeType.Master) * 100f : 100f,
                audioSystem != null ? audioSystem.GetVolume(VolumeType.Music) * 100f : 100f,
                audioSystem != null ? audioSystem.GetVolume(VolumeType.SFX) * 100f : 100f,
                inputSystem != null ? inputSystem.LookSensitivity : DefaultSensitivity()
            );
        }

        /// <summary>
        /// Запомнить значения в системе и запланировать запись на диск. Сам файл
        /// пишется через saveDelay (см. Update), чтобы не писать его на каждый тик ползунка.
        /// </summary>
        private void Save()
        {
            if (settingsSystem == null)
            {
                return;
            }

            var data = settingsSystem.Settings;

            data.LookSensitivity = inputSystem != null ? inputSystem.LookSensitivity : data.LookSensitivity;
            data.MasterVolume = audioSystem != null ? audioSystem.GetVolume(VolumeType.Master) : data.MasterVolume;
            data.MusicVolume = audioSystem != null ? audioSystem.GetVolume(VolumeType.Music) : data.MusicVolume;
            data.SfxVolume = audioSystem != null ? audioSystem.GetVolume(VolumeType.SFX) : data.SfxVolume;

            settingsSystem.Settings = data;
            pendingSaveTime = Time.unscaledTime + Mathf.Max(0.05f, saveDelay);
        }

        private void WriteNow()
        {
            settingsSystem?.Save();
        }

        /// <summary>Немедленно дописать отложенные настройки (закрытие экрана, выключение).</summary>
        private void FlushSave()
        {
            if (pendingSaveTime > 0f)
            {
                pendingSaveTime = -1f;
                WriteNow();
            }
        }

        /// <summary>
        /// Собрать список уникальных разрешений текущего монитора: без вариантов по
        /// частоте, по возрастанию площади. Частоту не меняем — берём текущую.
        /// </summary>
        private void BuildResolutionOptions()
        {
            resolutionOptions.Clear();

            var seen = new HashSet<long>();

            foreach (var resolution in Screen.resolutions)
            {
                var key = (long)resolution.width << 32 | (uint)resolution.height;

                if (seen.Add(key) == false)
                {
                    continue;
                }

                resolutionOptions.Add(new Vector2Int(resolution.width, resolution.height));
            }

            resolutionOptions.Sort((a, b) =>
            {
                var area = (a.x * a.y).CompareTo(b.x * b.y);
                return area != 0 ? area : a.x.CompareTo(b.x);
            });
        }

        private int FindCurrentResolutionIndex()
        {
            // Хранимое разрешение приоритетнее текущего окна: после старта игры
            // применится именно оно (в редакторе Game View может быть любого размера).
            var data = settingsSystem != null ? settingsSystem.Settings : default;
            var width = data.ResolutionWidth > 0 ? data.ResolutionWidth : Screen.width;
            var height = data.ResolutionHeight > 0 ? data.ResolutionHeight : Screen.height;

            for (var index = 0; index < resolutionOptions.Count; index++)
            {
                var resolution = resolutionOptions[index];

                if (resolution.x == width && resolution.y == height)
                {
                    return index;
                }
            }

            // Нестандартное разрешение: показываем «—», стрелки начнут с края списка.
            return -1;
        }

        #endregion
    }
}

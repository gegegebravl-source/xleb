using System;
using System.Collections.Generic;
using CHARK.SimpleUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UABPetelnia.GGJ2025.Runtime.UI.Views
{
    /// <summary>
    /// Экран настроек: звук, чувствительность мыши, экран и сброс к значениям по умолчанию.
    /// Открывается и из главного меню, и из паузы.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class SettingsView : View
    {
        #region Serialized — Tabs

        [Header("Tabs")]
        [SerializeField] private Button soundTabButton;
        [SerializeField] private Button graphicsTabButton;
        [SerializeField] private Button screenTabButton;
        [SerializeField] private Button controlsTabButton;

        [SerializeField] private GameObject soundPage;
        [SerializeField] private GameObject graphicsPage;
        [SerializeField] private GameObject screenPage;
        [SerializeField] private GameObject controlsPage;

        #endregion

        #region Serialized — Sound

        [Header("Sound")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private TMP_Text masterValue;

        [SerializeField] private Slider musicSlider;
        [SerializeField] private TMP_Text musicValue;

        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TMP_Text sfxValue;

        #endregion

        #region Serialized — Graphics

        [Header("Graphics")]
        [SerializeField] private Button presetLowButton;
        [SerializeField] private Button presetMediumButton;
        [SerializeField] private Button presetHighButton;

        [SerializeField] private Toggle shadowsToggle;
        [SerializeField] private Toggle antialiasingToggle;
        [SerializeField] private Toggle postfxToggle;
        [SerializeField] private Toggle textureQualityToggle;

        [SerializeField] private Slider renderScaleSlider;
        [SerializeField] private TMP_Text renderScaleValue;

        #endregion

        #region Serialized — Screen

        [Header("Screen")]
        [SerializeField] private Button resolutionPrevButton;
        [SerializeField] private Button resolutionNextButton;
        [SerializeField] private TMP_Text resolutionValue;

        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle vsyncToggle;

        #endregion

        #region Serialized — Controls

        [Header("Controls")]
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private TMP_Text sensitivityValue;

        #endregion

        #region Serialized — Buttons

        [Header("Buttons")]
        [SerializeField] private Button saveButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button closeButton;

        [Tooltip("Строка статуса: «Настройки сохранены» и подсказки.")]
        [SerializeField] private TMP_Text statusText;

        #endregion

        #region Events

        public event Action<float> OnMasterVolumeChanged;

        public event Action<float> OnMusicVolumeChanged;

        public event Action<float> OnSfxVolumeChanged;

        public event Action<float> OnSensitivityChanged;

        /// <summary>Клик по пресету графики: 0 — низкая, 1 — средняя, 2 — высокая.</summary>
        public event Action<int> OnGraphicsPresetClicked;

        public event Action<bool> OnShadowsChanged;

        public event Action<bool> OnAntialiasingChanged;

        public event Action<bool> OnPostfxChanged;

        public event Action<bool> OnTextureQualityChanged;

        public event Action<float> OnRenderScaleChanged;

        /// <summary>Смена разрешения: индекс в списке, заполненном SetResolutionOptions.</summary>
        public event Action<int> OnResolutionShifted;

        public event Action<bool> OnFullscreenChanged;

        public event Action<bool> OnVsyncChanged;

        public event Action OnResetClicked;

        public event Action OnSaveClicked;

        public event Action OnCloseClicked;

        #endregion

        #region Cached state

        private bool isApplying;

        /// <summary>Индекс текущей вкладки: 0 — звук, 1 — графика, 2 — экран, 3 — управление.</summary>
        private int currentTab;

        /// <summary>Доступные разрешения (w×h) — заполняются снаружи для навигации стрелками.</summary>
        private readonly List<Vector2Int> resolutions = new();

        private int currentResolutionIndex;

        private static readonly Color SelectedTabColor = new(1f, 0.88f, 0.55f, 1f);

        private static readonly Color IdleTabColor = new(0.85f, 0.72f, 0.5f, 0.7f);

        #endregion

        #region Unity lifecycle

        protected override void OnEnable()
        {
            base.OnEnable();

            Hook(masterSlider, value => OnMasterVolumeChanged?.Invoke(value));
            Hook(musicSlider, value => OnMusicVolumeChanged?.Invoke(value));
            Hook(sfxSlider, value => OnSfxVolumeChanged?.Invoke(value));
            Hook(sensitivitySlider, value => OnSensitivityChanged?.Invoke(value));

            Hook(renderScaleSlider, value => OnRenderScaleChanged?.Invoke(value));

            HookToggle(shadowsToggle, OnShadowsToggleChanged);
            HookToggle(antialiasingToggle, OnAntialiasingToggleChanged);
            HookToggle(postfxToggle, OnPostfxToggleChanged);
            HookToggle(textureQualityToggle, OnTextureQualityToggleChanged);
            HookToggle(fullscreenToggle, OnFullscreenToggleChanged);
            HookToggle(vsyncToggle, OnVsyncToggleChanged);

            HookButton(presetLowButton, () => OnGraphicsPresetClicked?.Invoke(0));
            HookButton(presetMediumButton, () => OnGraphicsPresetClicked?.Invoke(1));
            HookButton(presetHighButton, () => OnGraphicsPresetClicked?.Invoke(2));

            HookButton(soundTabButton, () => ShowTab(0));
            HookButton(graphicsTabButton, () => ShowTab(1));
            HookButton(screenTabButton, () => ShowTab(2));
            HookButton(controlsTabButton, () => ShowTab(3));

            HookButton(resolutionPrevButton, () => ShiftResolution(-1));
            HookButton(resolutionNextButton, () => ShiftResolution(1));

            HookButton(resetButton, () => OnResetClicked?.Invoke());
            HookButton(saveButton, () => OnSaveClicked?.Invoke());
            HookButton(closeButton, () => OnCloseClicked?.Invoke());

            ShowTab(currentTab);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            Unhook(masterSlider);
            Unhook(musicSlider);
            Unhook(sfxSlider);
            Unhook(sensitivitySlider);
            Unhook(renderScaleSlider);

            UnhookToggle(shadowsToggle, OnShadowsToggleChanged);
            UnhookToggle(antialiasingToggle, OnAntialiasingToggleChanged);
            UnhookToggle(postfxToggle, OnPostfxToggleChanged);
            UnhookToggle(textureQualityToggle, OnTextureQualityToggleChanged);
            UnhookToggle(fullscreenToggle, OnFullscreenToggleChanged);
            UnhookToggle(vsyncToggle, OnVsyncToggleChanged);

            UnhookButton(presetLowButton);
            UnhookButton(presetMediumButton);
            UnhookButton(presetHighButton);

            UnhookButton(soundTabButton);
            UnhookButton(graphicsTabButton);
            UnhookButton(screenTabButton);
            UnhookButton(controlsTabButton);

            UnhookButton(resolutionPrevButton);
            UnhookButton(resolutionNextButton);

            UnhookButton(resetButton);
            UnhookButton(saveButton);
            UnhookButton(closeButton);
        }

        #endregion

        #region Public API

        /// <summary>Показать звук/чувствительность и экран, не поднимая события.</summary>
        public void SetValues(
            float masterVolume,
            float musicVolume,
            float sfxVolume,
            float sensitivity,
            bool isFullscreen,
            bool isVsync
        )
        {
            isApplying = true;

            SetSlider(masterSlider, masterValue, masterVolume, "{0:0}%");
            SetSlider(musicSlider, musicValue, musicVolume, "{0:0}%");
            SetSlider(sfxSlider, sfxValue, sfxVolume, "{0:0}%");
            SetSlider(sensitivitySlider, sensitivityValue, sensitivity, "{0:0.0}");

            SetToggle(fullscreenToggle, isFullscreen);
            SetToggle(vsyncToggle, isVsync);

            isApplying = false;
        }

        /// <summary>Показать графические настройки, не поднимая события.</summary>
        public void SetGraphicsValues(
            int presetIndex,
            bool shadows,
            bool antialiasing,
            bool postfx,
            bool textureQuality,
            float renderScale
        )
        {
            isApplying = true;

            SetToggle(shadowsToggle, shadows);
            SetToggle(antialiasingToggle, antialiasing);
            SetToggle(postfxToggle, postfx);
            SetToggle(textureQualityToggle, textureQuality);
            SetSlider(renderScaleSlider, renderScaleValue, renderScale, "{0:0.00}");

            isApplying = false;

            SetPresetHighlight(presetIndex);
        }

        /// <summary>
        /// Заполнить список доступных разрешений: кнопки «‹»/«›» перебирают его по кругу.
        /// currentIndex = -1 — текущее разрешение не из списка (нестандартное окно): показываем «—».
        /// </summary>
        public void SetResolutionOptions(IReadOnlyList<Vector2Int> options, int currentIndex)
        {
            resolutions.Clear();

            if (options != null)
            {
                resolutions.AddRange(options);
            }

            currentResolutionIndex = resolutions.Count == 0
                ? -1
                : Mathf.Min(currentIndex, resolutions.Count - 1);

            UpdateResolutionLabel();
        }

        /// <summary>Показать вкладку: 0 — звук, 1 — графика, 2 — экран, 3 — управление.</summary>
        public void ShowTab(int tabIndex)
        {
            currentTab = Mathf.Clamp(tabIndex, 0, 3);

            SetPageActive(soundPage, currentTab == 0);
            SetPageActive(graphicsPage, currentTab == 1);
            SetPageActive(screenPage, currentTab == 2);
            SetPageActive(controlsPage, currentTab == 3);

            HighlightTab(soundTabButton, currentTab == 0);
            HighlightTab(graphicsTabButton, currentTab == 1);
            HighlightTab(screenTabButton, currentTab == 2);
            HighlightTab(controlsTabButton, currentTab == 3);
        }

        /// <summary>Обновить подписи значений после изменения ползунков.</summary>
        public void RefreshLabels(
            float masterVolume,
            float musicVolume,
            float sfxVolume,
            float sensitivity
        )
        {
            SetLabel(masterValue, masterVolume, "{0:0}%");
            SetLabel(musicValue, musicVolume, "{0:0}%");
            SetLabel(sfxValue, sfxVolume, "{0:0}%");
            SetLabel(sensitivityValue, sensitivity, "{0:0.0}");
        }

        /// <summary>Показать строку статуса под ползунками.</summary>
        public void SetStatus(string status)
        {
            if (statusText != null && string.IsNullOrEmpty(status) == false)
            {
                statusText.text = status;
            }
        }

        #endregion

        #region Helpers

        private void ShiftResolution(int direction)
        {
            if (resolutions.Count == 0)
            {
                return;
            }

            // Из «—» (нестандартное разрешение) стрелки ведут в край списка.
            currentResolutionIndex = currentResolutionIndex < 0
                ? (direction > 0 ? 0 : resolutions.Count - 1)
                : (currentResolutionIndex + direction + resolutions.Count) % resolutions.Count;

            UpdateResolutionLabel();

            OnResolutionShifted?.Invoke(currentResolutionIndex);
        }

        private void UpdateResolutionLabel()
        {
            if (resolutionValue == null)
            {
                return;
            }

            if (currentResolutionIndex < 0 || currentResolutionIndex >= resolutions.Count)
            {
                resolutionValue.text = "—";
                return;
            }

            var resolution = resolutions[currentResolutionIndex];
            resolutionValue.text = $"{resolution.x} × {resolution.y}";
        }

        private static void HighlightTab(Button button, bool selected)
        {
            if (button != null && button.image != null)
            {
                button.image.color = selected ? SelectedTabColor : IdleTabColor;
            }
        }

        /// <summary>Подсветить активный пресет (—1 — ручная настройка, снять подсветку).</summary>
        public void SetPresetHighlight(int presetIndex)
        {
            HighlightTab(presetLowButton, presetIndex == 0);
            HighlightTab(presetMediumButton, presetIndex == 1);
            HighlightTab(presetHighButton, presetIndex == 2);
        }

        private static void SetPageActive(GameObject page, bool active)
        {
            if (page != null)
            {
                page.SetActive(active);
            }
        }

        // Лямбды создаются заново на каждом OnEnable, поэтому снять старый хук
        // надёжнее через RemoveAllListeners — инспекторские слушатели не трогаются.
        // Guard isApplying: программная установка значения во время Refresh
        // не должна поднимать события и перезаписывать настройки.
        private void Hook(Slider slider, UnityEngine.Events.UnityAction<float> action)
        {
            if (slider != null)
            {
                slider.onValueChanged.RemoveAllListeners();
                slider.onValueChanged.AddListener(value =>
                {
                    if (isApplying == false)
                    {
                        action(value);
                    }
                });
            }
        }

        private static void Unhook(Slider slider)
        {
            if (slider != null)
            {
                slider.onValueChanged.RemoveAllListeners();
            }
        }

        private void HookToggle(Toggle toggle, UnityEngine.Events.UnityAction<bool> action)
        {
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener(value =>
                {
                    if (isApplying == false)
                    {
                        action(value);
                    }
                });
            }
        }

        private void UnhookToggle(Toggle toggle, UnityEngine.Events.UnityAction<bool> action)
        {
            if (toggle != null)
            {
                // Хук добавлен обёрткой-лямбдой (см. HookToggle), снять можно только всех.
                toggle.onValueChanged.RemoveAllListeners();
            }
        }

        private void HookButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(action);
            }
        }

        private static void UnhookButton(Button button)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }
        }

        private static void SetSlider(Slider slider, TMP_Text label, float value, string format)
        {
            if (slider != null)
            {
                slider.value = value;
            }

            SetLabel(label, value, format);
        }

        private static void SetLabel(TMP_Text label, float value, string format)
        {
            if (label != null)
            {
                label.text = string.Format(format, value);
            }
        }

        private static void SetToggle(Toggle toggle, bool value)
        {
            if (toggle != null)
            {
                toggle.SetIsOnWithoutNotify(value);
            }
        }

        // Guard isApplying в обёртке: программная установка значения во время Refresh
        // не должна поднимать события и перезаписывать настройки.
        private void OnShadowsToggleChanged(bool value) => OnShadowsChanged?.Invoke(value);

        private void OnAntialiasingToggleChanged(bool value) => OnAntialiasingChanged?.Invoke(value);

        private void OnPostfxToggleChanged(bool value) => OnPostfxChanged?.Invoke(value);

        private void OnTextureQualityToggleChanged(bool value) => OnTextureQualityChanged?.Invoke(value);

        private void OnFullscreenToggleChanged(bool value) => OnFullscreenChanged?.Invoke(value);

        private void OnVsyncToggleChanged(bool value) => OnVsyncChanged?.Invoke(value);

        #endregion
    }
}

using System;
using CHARK.SimpleUI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UABPetelnia.GGJ2025.Runtime.UI.Views
{
    /// <summary>
    /// Финальный экран смены. Показывает отчёт (stats + earned), предлагает
    /// рестарт или выход. Использует тот же Warm Bread стиль, что и главное меню,
    /// плюс ту же UI-полировку (fade-in панели + juice кнопок).
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class GameOverView : View
    {
        #region Serialized — Content

        [Header("Shift Report")]
        [Tooltip("Основная сводка смены: сколько хлеба испечено, сколько гостей обслужено и т.п.")]
        [SerializeField] private TMP_Text statsText;

        [Tooltip("Строка заработка за смену (монеты / очки).")]
        [SerializeField] private TMP_Text earnedText;

        [Tooltip("Опциональный заголовок над отчётом. Если null — не трогаем.")]
        [SerializeField] private TMP_Text headingText;

        [Tooltip("Опциональная подпись под отчётом (reason / flavor text). Если null — не трогаем.")]
        [SerializeField] private TMP_Text captionText;

        #endregion

        #region Serialized — Buttons

        [Header("Buttons")]
        [SerializeField] private Button restartGameButton;
        [SerializeField] private Button exitGameButton;

        #endregion

        #region Serialized — Behaviour

        [Header("Behaviour")]
        [Tooltip("Применить Warm Bread тему при первом включении view.")]
        [SerializeField] private bool applyRuntimeTheme = true;

        [Tooltip("Плавное появление всей панели (fade + slide).")]
        [SerializeField] private bool animatePanel = true;

        [Tooltip("Добавить кнопкам hover/нажатие масштаб (UI juice).")]
        [SerializeField] private bool addButtonJuice = true;

        [Tooltip("Куда переводить фокус при открытии: true — restart, false — exit.")]
        [SerializeField] private bool focusRestartOnOpen = true;

        [Tooltip("Скрывать view, если обе кнопки null (защита от пустого префаба).")]
        [SerializeField] private bool hideIfNoButtons = false;

        #endregion

        #region Events

        public event Action OnRestartGameClicked;
        public event Action OnExitGameClicked;

        #endregion

        #region Cached state

        private bool themeApplied;
        private bool polishApplied;

        #endregion

        #region Unity lifecycle

        protected override void OnEnable()
        {
            base.OnEnable();

            if (hideIfNoButtons && restartGameButton == null && exitGameButton == null)
            {
                SetActiveAll(false);
                return;
            }

            TryApplyTheme();
            TryApplyPolish();
            RegisterListeners();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnregisterListeners();
        }

        protected override void OnViewShowEntered()
        {
            base.OnViewShowEntered();
            Select(focusRestartOnOpen ? restartGameButton : exitGameButton);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Заполнить отчёт смены. Любой параметр может быть <c>null</c> — соответствующий
        /// текст останется без изменений.
        /// </summary>
        public void SetStats(string stats, string earned)
        {
            // Окно смены — тёмная карточка, поэтому текст светлый.
            SetText(statsText,  stats,  WarmBreadUiColors.Parchment, applyStyle: true);
            SetText(earnedText, earned, WarmBreadUiColors.ButterBright, applyStyle: true);
        }

        /// <summary>
        /// Расширенный вариант: заголовок и подпись под отчётом.
        /// Передавай <c>null</c>, чтобы не трогать поле.
        /// </summary>
        public void SetReport(string heading, string stats, string earned, string caption)
        {
            SetText(headingText, heading, WarmBreadUiColors.ButterBright, applyStyle: true);
            SetText(statsText,   stats,   WarmBreadUiColors.Parchment,    applyStyle: true);
            SetText(earnedText,  earned,  WarmBreadUiColors.ButterBright, applyStyle: true);
            SetText(captionText, caption, WarmBreadUiColors.Cream,        applyStyle: true);
        }

        /// <summary>
        /// Переопределить цвет строки заработка (например, красным при убытке).
        /// </summary>
        public void SetEarnedColor(Color color)
        {
            if (earnedText != null) earnedText.color = color;
        }

        /// <summary>
        /// Переопределить цвет строки статистики.
        /// </summary>
        public void SetStatsColor(Color color)
        {
            if (statsText != null) statsText.color = color;
        }

        #endregion

        #region Listener registration

        private void RegisterListeners()
        {
            AddListener(restartGameButton, HandleRestartClicked);
            AddListener(exitGameButton,    HandleExitClicked);
        }

        private void UnregisterListeners()
        {
            RemoveListener(restartGameButton, HandleRestartClicked);
            RemoveListener(exitGameButton,    HandleExitClicked);
        }

        #endregion

        #region Theme & polish

        private void TryApplyTheme()
        {
            if (themeApplied || !applyRuntimeTheme) return;
            themeApplied = true;

            try
            {
                // Game Over и Main Menu — один и тот же Warm Bread стиль.
                WarmBreadRuntimeTheme.ApplyMainMenuTheme(this);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[GameOverView] Theme application failed: {exception.Message}", this);
            }
        }

        private void TryApplyPolish()
        {
            if (polishApplied) return;
            polishApplied = true;

            if (animatePanel && !TryGetComponent<MenuPanelFader>(out _))
            {
                gameObject.AddComponent<MenuPanelFader>();
            }

            if (addButtonJuice)
            {
                EnsureButtonAnimator(restartGameButton);
                EnsureButtonAnimator(exitGameButton);
            }
        }

        private static void EnsureButtonAnimator(Button button)
        {
            if (button == null) return;
            if (!button.TryGetComponent<MenuButtonAnimator>(out _))
                button.gameObject.AddComponent<MenuButtonAnimator>();
        }

        #endregion

        #region Helpers

        private void SetActiveAll(bool isActive)
        {
            if (statsText   != null) statsText.gameObject.SetActive(isActive);
            if (earnedText  != null) earnedText.gameObject.SetActive(isActive);
            if (restartGameButton != null) restartGameButton.gameObject.SetActive(isActive);
            if (exitGameButton    != null) exitGameButton.gameObject.SetActive(isActive);
        }

        private static void SetText(TMP_Text target, string value, Color color, bool applyStyle)
        {
            if (target == null || value == null) return;

            target.text = value;

            if (!applyStyle) return;

            target.color        = color;
            target.outlineWidth = 0.10f;
            target.outlineColor = new Color(0.12f, 0.07f, 0.05f, 0.60f);
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction listener)
        {
            if (button != null) button.onClick.AddListener(listener);
        }

        private static void RemoveListener(Button button, UnityEngine.Events.UnityAction listener)
        {
            if (button != null) button.onClick.RemoveListener(listener);
        }

        private static void Select(Selectable selectable)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null || selectable == null || !selectable.interactable) return;

            eventSystem.SetSelectedGameObject(selectable.gameObject);
        }

        #endregion

        #region Event handlers

        private void HandleRestartClicked() => OnRestartGameClicked?.Invoke();
        private void HandleExitClicked()    => OnExitGameClicked?.Invoke();

        #endregion
    }
}
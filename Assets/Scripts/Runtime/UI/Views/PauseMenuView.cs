using System;
using CHARK.SimpleUI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UABPetelnia.GGJ2025.Runtime.UI.Views
{
    /// <summary>
    /// Меню паузы: продолжить и выйти. Если <see cref="autoBuildPauseLayout"/> включен,
    /// карточка собирается в рантайме — иначе ожидается готовая вёрстка в префабе.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class PauseMenuView : View, ICancelHandler
    {
        #region Serialized — Buttons

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button settingsButton;

        #endregion

        #region Serialized — Layout

        [Header("Layout")]
        [Tooltip("Собирать панель в рантайме. Если выключено — вёрстка должна быть в префабе.")]
        [SerializeField] private bool autoBuildPauseLayout = false;

        [Tooltip("Размер карточки паузы (только при autoBuildPauseLayout).")]
        [SerializeField] private Vector2 panelSize = new(720f, 360f);

        [Tooltip("Размер кнопок (только при autoBuildPauseLayout).")]
        [SerializeField] private Vector2 buttonSize = new(550f, 120f);

        [Tooltip("Имя дочернего объекта-фона, чтобы не создавать дубликат при повторном OnEnable.")]
        [SerializeField] private string panelChildName = "PausePanel";

        #endregion

        #region Serialized — Behaviour

        [Header("Behaviour")]
        [Tooltip("Применять Warm Bread тему один раз при первом включении.")]
        [SerializeField] private bool applyRuntimeTheme = true;

        [Tooltip("Плавное появление панели (fade + slide).")]
        [SerializeField] private bool animatePanel = true;

        [Tooltip("Добавить кнопкам hover/нажатие масштаб.")]
        [SerializeField] private bool addButtonJuice = true;

        [Tooltip("Esc / Cancel вызывает resume. Включай только если Input System " +
                 "не обрабатывает Escape сам — иначе будет двойной вызов.")]
        [SerializeField] private bool resumeOnCancel = false;

        #endregion

        #region Events

        public event Action OnResumeClicked;
        public event Action OnExitClicked;
        public event Action OnSettingsClicked;

        #endregion

        #region Cached state

        private Image panelImage;
        private bool  themeApplied;
        private bool  polishApplied;

        #endregion

        #region Unity lifecycle

        protected override void OnEnable()
        {
            base.OnEnable();

            EnsurePauseLayout();
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
            Select(resumeButton);
        }

        #endregion

        #region Cancel handling

        void ICancelHandler.OnCancel(BaseEventData eventData)
        {
            if (!resumeOnCancel) return;
            OnResumeClicked?.Invoke();
        }

        #endregion

        #region Listener registration

        private void RegisterListeners()
        {
            AddListener(resumeButton, HandleResumeClicked);
            AddListener(exitButton,   HandleExitClicked);
            AddListener(settingsButton, HandleSettingsClicked);
        }

        private void UnregisterListeners()
        {
            RemoveListener(resumeButton, HandleResumeClicked);
            RemoveListener(exitButton,   HandleExitClicked);
            RemoveListener(settingsButton, HandleSettingsClicked);
        }

        #endregion

        #region Theme & polish

        private void TryApplyTheme()
        {
            if (themeApplied || !applyRuntimeTheme) return;
            themeApplied = true;

            try
            {
                WarmBreadRuntimeTheme.ApplyPauseMenuTheme(this);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[PauseMenuView] Theme application failed: {exception.Message}", this);
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
                EnsureButtonAnimator(resumeButton);
                EnsureButtonAnimator(exitButton);
                EnsureButtonAnimator(settingsButton);
            }
        }

        private static void EnsureButtonAnimator(Button button)
        {
            if (button == null) return;
            if (!button.TryGetComponent<MenuButtonAnimator>(out _))
                button.gameObject.AddComponent<MenuButtonAnimator>();
        }

        #endregion

        #region Layout builder

        /// <summary>
        /// Собирает карточку паузы в рантайме. Идемпотентно: если <c>PausePanel</c>
        /// уже существует (после прошлого OnEnable), переиспользует его вместо создания дубля.
        /// </summary>
        private void EnsurePauseLayout()
        {
            if (!autoBuildPauseLayout) return;

            if (transform is not RectTransform rect) return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot     = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = panelSize;
            rect.anchoredPosition = Vector2.zero;

            EnsurePanelBackground();
            PlaceButton(resumeButton, new Vector2(0f, 72f));
            PlaceButton(settingsButton, new Vector2(0f, -62f));
            PlaceButton(exitButton,   new Vector2(0f, -196f));
        }

        private void EnsurePanelBackground()
        {
            // Ищем существующий фон по имени — иначе после уничтожения/пересоздания
            // объекта мы бы каждый OnEnable плодили новый PausePanel.
            if (panelImage == null)
            {
                var existing = transform.Find(panelChildName);
                if (existing != null && existing.TryGetComponent(out panelImage))
                {
                    return;
                }
            }

            if (panelImage != null) return;

            var panel = new GameObject(panelChildName, typeof(Image));
            panel.transform.SetParent(transform, false);
            panel.transform.SetAsFirstSibling();

            if (panel.transform is RectTransform panelRect)
            {
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;
            }

            panelImage = panel.GetComponent<Image>();
            if (panelImage != null)
            {
                // Непрозрачная карточка: игра за меню паузы просвечивать не должна.
                panelImage.color         = new Color(0.10f, 0.055f, 0.035f, 1f);
                panelImage.raycastTarget = false;
            }
        }

        private void PlaceButton(Button button, Vector2 anchoredPosition)
        {
            if (button == null) return;
            if (button.transform is not RectTransform buttonRect) return;

            buttonRect.anchorMin        = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax        = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta        = buttonSize;
            buttonRect.anchoredPosition = anchoredPosition;
        }

        #endregion

        #region Helpers

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

        private void HandleResumeClicked() => OnResumeClicked?.Invoke();
        private void HandleExitClicked()   => OnExitClicked?.Invoke();
        private void HandleSettingsClicked() => OnSettingsClicked?.Invoke();

        #endregion
    }
}
using System;
using CHARK.SimpleUI;
using TMPro;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.UI.Views
{
    /// <summary>
    /// Карточка «получено достижение»: масляная лента, название ачивки и за что она дана.
    /// Fade-in → пауза → fade-out → скрытие, всё на unscaled time, чтобы работать в паузе.
    /// </summary>
    /// <remarks>
    /// Контроллер держит объект живым между показами, поэтому view обязана корректно
    /// переигрывать анимацию при повторных <c>OnViewShowEntered</c>.
    /// </remarks>
    [DisallowMultipleComponent]
    internal sealed class AchievementToastView : View
    {
        #region Serialized — Parts

        [Header("Toast Parts")]
        [Tooltip("Контейнер, который сдвигается. Если null — двигаться нечему, только fade.")]
        [SerializeField] private RectTransform body;

        [Tooltip("Мелкая подпись сверху, обычно «НОВОЕ ДОСТИЖЕНИЕ».")]
        [SerializeField] private TMP_Text captionText;

        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;

        #endregion

        #region Serialized — Timing

        [Header("Toast Timing")]
        [Min(0.5f)]
        [SerializeField] private float visibleSeconds = 4.5f;

        [Min(0.05f)]
        [SerializeField] private float fadeSeconds = 0.35f;

        [Tooltip("На сколько пикселей карточка проезжает во время fade in/out.")]
        [SerializeField] private float slideDistance = 48f;

        #endregion

        #region Serialized — Content

        [Header("Content")]
        [SerializeField] private string captionValue  = "НОВОЕ ДОСТИЖЕНИЕ";
        [SerializeField] private string fallbackTitle = "Достижение";

        #endregion

        #region Serialized — Behaviour

        [Header("Behaviour")]
        [Tooltip("Применять Warm Bread тему (цвета caption/title/description) в рантайме.")]
        [SerializeField] private bool applyRuntimeTheme = true;

        #endregion

        #region Public API

        /// <summary>
        /// <c>true</c>, пока карточка на экране.
        /// </summary>
        public bool IsVisible => State == ViewVisibilityState.Shown;

        /// <summary>
        /// Заполнить карточку. Пустые строки подменяются безопасными значениями:
        /// <paramref name="title"/> → <see cref="fallbackTitle"/>, <paramref name="description"/> → пустая строка.
        /// </summary>
        public void SetText(string title, string description)
        {
            if (titleText != null)
            {
                titleText.text = string.IsNullOrWhiteSpace(title) ? fallbackTitle : title;
            }

            if (descriptionText != null)
            {
                descriptionText.text = description ?? string.Empty;
            }

            if (captionText != null)
            {
                captionText.text = captionValue;
            }
        }

        /// <summary>
        /// Принудительно спрятать карточку (например, игрок кликнул «пропустить»).
        /// Если она уже скрыта — ничего не делает.
        /// </summary>
        public void Skip()
        {
            if (IsVisible)
            {
                Hide();
            }
        }

        #endregion

        #region Cached state

        private Vector2 restingPosition;
        private float   shownAtSeconds;
        private bool    themeApplied;
        private bool    restingCaptured;

        #endregion

        #region Unity lifecycle

        protected override void Awake()
        {
            base.Awake();

            CaptureRestingPosition();
            Apply(0f);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            TryApplyTheme();
        }

        protected override void OnViewShowEntered()
        {
            base.OnViewShowEntered();

            // Тост переиспользуется — позиция могла поменяться из-за layout-групп,
            // а таймер всегда нужно перезапускать с нуля.
            CaptureRestingPosition();
            shownAtSeconds = Time.unscaledTime;
            Apply(0f);
        }

        private void Update()
        {
            if (State != ViewVisibilityState.Shown)
            {
                return;
            }

            var elapsed = Time.unscaledTime - shownAtSeconds;

            var fadeIn = fadeSeconds > 0f
                ? Mathf.Clamp01(elapsed / fadeSeconds)
                : 1f;

            var fadeOut = fadeSeconds > 0f
                ? Mathf.Clamp01((visibleSeconds - elapsed) / fadeSeconds)
                : 1f;

            Apply(Mathf.Min(fadeIn, fadeOut));

            if (elapsed >= visibleSeconds)
            {
                Hide();
            }
        }

        #endregion

        #region Animation

        /// <summary>
        /// Смешать карточку между полностью скрытой (0) и полностью показанной (1).
        /// Идемпотентно — можно вызывать сколько угодно раз с одинаковым значением.
        /// </summary>
        private void Apply(float amount)
        {
            amount = Mathf.Clamp01(amount);

            if (CanvasGroup != null)
            {
                CanvasGroup.alpha = amount;
            }

            if (body != null && restingCaptured)
            {
                var offset = slideDistance * (1f - amount);
                body.anchoredPosition = restingPosition + new Vector2(0f, offset);
            }
        }

        private void CaptureRestingPosition()
        {
            if (restingCaptured || body == null)
            {
                return;
            }

            restingPosition = body.anchoredPosition;
            restingCaptured = true;
        }

        #endregion

        #region Theme

        private void TryApplyTheme()
        {
            if (themeApplied || !applyRuntimeTheme) return;
            themeApplied = true;

            try
            {
                WarmBreadRuntimeTheme.ApplyMainMenuTheme(this);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[AchievementToastView] Theme application failed: {exception.Message}", this);
            }

            // Тема ходит по именам и может перекрасить caption в Crust (он SubtitleLike).
            // Для тоста нужен Butter — принудительно возвращаем после прохода.
            ApplyToastColors();
        }

        /// <summary>
        /// Цвета, специфичные именно для тоста. Вызывается после темизатора
        /// и из <see cref="OnValidate"/> для превью в редакторе.
        /// </summary>
        private void ApplyToastColors()
        {
            if (captionText != null)
            {
                captionText.color = WarmBreadUiColors.Butter;
            }

            if (titleText != null)
            {
                titleText.color = WarmBreadUiColors.Crust;
            }

            if (descriptionText != null)
            {
                descriptionText.color = WarmBreadUiColors.Crust;
            }
        }

        #endregion

        #region Editor validation

        private void OnValidate()
        {
            // Тост должен выглядеть правильно уже в редакторе, без Play Mode.
            ApplyToastColors();

            visibleSeconds = Mathf.Max(0.5f, visibleSeconds);
            fadeSeconds    = Mathf.Max(0.05f, fadeSeconds);
        }

        #endregion
    }
}
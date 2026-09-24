using System;
using CHARK.SimpleUI;
using TMPro;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.UI.Views
{
    /// <summary>
    /// HUD-поле денег игрока. Показывает сумму в рублях, опционально анимирует
    /// изменение числа и подсвечивает рост (зелёным) / падение (красным).
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class MoneyView : View
    {
        #region Serialized — Display

        [Header("Display")]
        [Tooltip("Текстовое поле для вывода суммы.")]
        [SerializeField] private TMP_Text displayText;

        [Tooltip("Формат вывода. {0} — целые рубли.")]
        [SerializeField] private string format = "{0:0} руб.";

        [Tooltip("Стартовая сумма в центах, если контроллер не вызвал SetMoney до первого показа.")]
        [SerializeField] private int startingCents = 0;

        #endregion

        #region Serialized — Appearance

        [Header("Appearance")]
        [Tooltip("Обычный цвет числа. По умолчанию — ButterBright (золотистый).")]
        [SerializeField] private Color baseColor = new(1f, 0.93f, 0.62f, 1f);

        [Tooltip("Вспышка при росте суммы.")]
        [SerializeField] private Color gainColor = new(0.24f, 0.55f, 0.28f, 1f);

        [Tooltip("Вспышка при падении суммы.")]
        [SerializeField] private Color lossColor = new(0.79f, 0.25f, 0.20f, 1f);

        #endregion

        #region Serialized — Animation

        [Header("Animation")]
        [Tooltip("Плавный подсчёт при изменении суммы.")]
        [SerializeField] private bool animateValue = true;

        [Range(1f, 60f)]
        [Tooltip("Скорость догона. Чем больше — тем резче. 14 ≈ 200 мс до цели.")]
        [SerializeField] private float valueDamping = 14f;

        [Tooltip("Подсветка цвета при изменении суммы.")]
        [SerializeField] private bool flashOnChange = true;

        [Range(1f, 30f)]
        [Tooltip("Скорость затухания вспышки. 8 ≈ 125 мс.")]
        [SerializeField] private float flashDamping = 8f;

        #endregion

        #region Public API

        /// <summary>
        /// Сырая строка для отображения. Set пишет текст напрямую, минуя счётчик —
        /// используй <see cref="SetMoney"/> для нормального сценария.
        /// </summary>
        public string DisplayText
        {
            get => displayText != null ? displayText.text : string.Empty;
            set
            {
                if (displayText == null || value == null) return;
                displayText.text = value;
            }
        }

        /// <summary>
        /// Текущая отображаемая сумма в центах (float — учитывает промежуточные кадры анимации).
        /// </summary>
        public float CurrentCents => currentCents;

        /// <summary>
        /// Целевая сумма в центах.
        /// </summary>
        public int TargetCents => targetCents;

        #endregion

        #region Cached state

        private float currentCents;
        private int   targetCents;
        private float flashAmount;     // 0..1, где 1 — максимум вспышки
        private Color flashColor;

        #endregion

        #region Unity lifecycle

        protected override void OnEnable()
        {
            base.OnEnable();

            // Первый запуск — инициализируем из startingCents, если никто ещё не выставил сумму.
            if (targetCents == 0 && startingCents != 0)
            {
                currentCents = targetCents = startingCents;
            }

            RenderText(currentCents);
            ApplyColorImmediate();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            flashAmount = 0f;
            ApplyColorImmediate();
        }

        private void Update()
        {
            TickValue();
            TickFlash();
        }

        #endregion

        #region Public API — SetMoney

        /// <summary>
        /// Установить сумму. Если <see cref="animateValue"/> включено — число плавно
        /// «догонит» новое значение за ~200 мс.
        /// </summary>
        /// <param name="instant">Пропустить анимацию и мгновенно установить значение.</param>
        public void SetMoney(int cents, bool instant = false)
        {
            var previous = targetCents;
            targetCents = cents;

            if (instant || !animateValue)
            {
                currentCents = targetCents;
                RenderText(currentCents);
            }

            if (flashOnChange && previous != cents)
            {
                TriggerFlash(cents > previous);
            }
        }

        /// <summary>
        /// Мгновенно, без анимации и вспышки. Для инициализации / загрузки сейва.
        /// </summary>
        public void SetMoneyInstant(int cents)
        {
            SetMoney(cents, instant: true);
        }

        #endregion

        #region Animation

        private void TickValue()
        {
            if (displayText == null) return;
            if (!animateValue) return;

            if (Mathf.Abs(currentCents - targetCents) < 0.01f)
            {
                if (currentCents != targetCents)
                {
                    currentCents = targetCents;
                    RenderText(currentCents);
                }
                return;
            }

            var t = 1f - Mathf.Exp(-valueDamping * Time.unscaledDeltaTime);
            currentCents = Mathf.Lerp(currentCents, targetCents, t);
            RenderText(currentCents);
        }

        private void TickFlash()
        {
            if (displayText == null) return;

            if (flashAmount <= 0f)
            {
                if (!Approximately(displayText.color, baseColor))
                {
                    displayText.color = baseColor;
                }
                return;
            }

            flashAmount -= flashDamping * Time.unscaledDeltaTime;
            if (flashAmount < 0f) flashAmount = 0f;

            displayText.color = Color.Lerp(baseColor, flashColor, flashAmount);
        }

        private void TriggerFlash(bool isGain)
        {
            flashColor  = isGain ? gainColor : lossColor;
            flashAmount = 1f;
        }

        #endregion

        #region Rendering

        private void RenderText(float cents)
        {
            if (displayText == null) return;

            // Целые рубли: копейки в игре не считают.
            displayText.text = string.Format(format, Mathf.Round(cents / 100f));
        }

        private void ApplyColorImmediate()
        {
            if (displayText != null) displayText.color = baseColor;
        }

        private static bool Approximately(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.001f
                && Mathf.Abs(a.g - b.g) < 0.001f
                && Mathf.Abs(a.b - b.b) < 0.001f
                && Mathf.Abs(a.a - b.a) < 0.001f;
        }

        #endregion

        #region Editor validation

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(format))
            {
                format = "{0:0} руб.";
            }

            if (displayText != null)
            {
                displayText.color = baseColor;
            }
        }

        #endregion
    }
}
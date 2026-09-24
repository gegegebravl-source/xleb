using CHARK.SimpleUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UABPetelnia.GGJ2025.Runtime.UI.Views
{
    /// <summary>
    /// HUD справа вверху: игровое время смены, номер дня и баланс в рублях.
    /// Сумма плавно «догоняет» новое значение и подсвечивается при изменении.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class StatusHudView : View
    {
        #region Serialized — Clock

        [Header("Clock")]
        [Tooltip("Большие часы: \"14:35\".")]
        [SerializeField] private TMP_Text clockText;

        [Tooltip("Подпись дня под часами: \"День 1\".")]
        [SerializeField] private TMP_Text dayText;

        [Tooltip("Полоска прогресса смены (0 — открытие, 1 — закрытие).")]
        [SerializeField] private Image shiftFill;

        #endregion

        #region Serialized — Money

        [Header("Money")]
        [Tooltip("Сумма в рублях.")]
        [SerializeField] private TMP_Text moneyText;

        [Tooltip("Формат вывода. {0} — целые рубли, копейки в игре не считают.")]
        [SerializeField] private string moneyFormat = "{0:0} руб.";

        [SerializeField] private Color moneyBaseColor = new(1f, 0.93f, 0.62f, 1f);

        [Tooltip("Вспышка при росте суммы.")]
        [SerializeField] private Color moneyGainColor = new(0.62f, 0.72f, 0.31f, 1f);

        [Tooltip("Вспышка при падении суммы.")]
        [SerializeField] private Color moneyLossColor = new(0.79f, 0.29f, 0.20f, 1f);

        #endregion

        #region Serialized — Animation

        [Header("Animation")]
        [Tooltip("Плавный подсчёт при изменении суммы.")]
        [SerializeField] private bool animateValue = true;

        [Range(1f, 60f)]
        [Tooltip("Скорость догона значения. 14 ≈ 200 мс до цели.")]
        [SerializeField] private float valueDamping = 14f;

        [Range(1f, 30f)]
        [Tooltip("Скорость затухания вспышки.")]
        [SerializeField] private float flashDamping = 8f;

        #endregion

        #region Cached state

        private float currentCents;
        private int targetCents;
        private float flashAmount;
        private Color flashColor;

        #endregion

        #region Public API

        /// <summary>Время и день в HUD.</summary>
        public void SetClock(string clock, int day)
        {
            if (clockText != null)
            {
                clockText.text = clock;
            }

            if (dayText != null)
            {
                dayText.text = $"День {day}";
            }
        }

        /// <summary>Прогресс смены для полоски под часами.</summary>
        public void SetShiftProgress(float progress)
        {
            if (shiftFill != null)
            {
                shiftFill.fillAmount = Mathf.Clamp01(progress);
            }
        }

        /// <summary>
        /// Показать баланс. При <paramref name="instant"/> сумма ставится сразу, без анимации.
        /// </summary>
        public void SetMoney(int cents, bool instant = false)
        {
            var previous = targetCents;
            targetCents = cents;

            if (instant || animateValue == false)
            {
                currentCents = targetCents;
                RenderMoney(currentCents);
            }

            if (previous != cents)
            {
                flashColor = cents > previous ? moneyGainColor : moneyLossColor;
                flashAmount = 1f;
            }
        }

        /// <summary>Мгновенно, без анимации и вспышки: для загрузки и первого кадра.</summary>
        public void SetMoneyImmediate(int cents)
        {
            SetMoney(cents, instant: true);
        }

        #endregion

        #region Unity lifecycle

        protected override void OnEnable()
        {
            base.OnEnable();

            RenderMoney(currentCents);
            ApplyColor(moneyBaseColor);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            flashAmount = 0f;
            ApplyColor(moneyBaseColor);
        }

        private void Update()
        {
            TickValue();
            TickFlash();
        }

        #endregion

        #region Animation

        private void TickValue()
        {
            if (moneyText == null || animateValue == false)
            {
                return;
            }

            if (Mathf.Abs(currentCents - targetCents) < 0.01f)
            {
                if (currentCents != targetCents)
                {
                    currentCents = targetCents;
                    RenderMoney(currentCents);
                }

                return;
            }

            var t = 1f - Mathf.Exp(-valueDamping * Time.unscaledDeltaTime);
            currentCents = Mathf.Lerp(currentCents, targetCents, t);
            RenderMoney(currentCents);
        }

        private void TickFlash()
        {
            if (moneyText == null)
            {
                return;
            }

            if (flashAmount <= 0f)
            {
                return;
            }

            flashAmount -= flashDamping * Time.unscaledDeltaTime;
            if (flashAmount < 0f)
            {
                flashAmount = 0f;
            }

            ApplyColor(Color.Lerp(moneyBaseColor, flashColor, flashAmount));
        }

        private void RenderMoney(float cents)
        {
            if (moneyText == null)
            {
                return;
            }

            // В ларьке 2000-х копейки не считали: показываем целые рубли.
            moneyText.text = string.Format(moneyFormat, Mathf.Round(cents / 100f));
        }

        private void ApplyColor(Color color)
        {
            if (moneyText != null)
            {
                moneyText.color = color;
            }
        }

        #endregion
    }
}

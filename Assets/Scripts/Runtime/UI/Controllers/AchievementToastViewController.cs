using System;
using System.Collections.Generic;
using CHARK.GameManagement;
using CHARK.SimpleUI;
using UABPetelnia.GGJ2025.Runtime.Systems.Progress;
using UABPetelnia.GGJ2025.Runtime.UI.Views;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.UI.Controllers
{
    /// <summary>
    /// Показывает карточку достижения, когда система прогресса что-то разблокирует.
    /// Если ачивки сыпятся пачкой — они выстраиваются в очередь и показываются
    /// по одной, без перезаписи.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class AchievementToastViewController : ViewController<AchievementToastView>
    {
        #region Serialized — Behaviour

        [Header("Behaviour")]
        [Tooltip("Максимум сообщений в очереди. Защита от переполнения при массовом разблокировании.")]
        [Min(1)]
        [SerializeField] private int maxQueued = 8;

        [Tooltip("Пауза между карточками в секундах, чтобы они не слипались.")]
        [Min(0f)]
        [SerializeField] private float gapBetweenToasts = 0.15f;

        [Tooltip("Показывать тост через анимацию контроллера. Обычно выключено — " +
                 "сама карточка анимируется в своём Update.")]
        [SerializeField] private bool animateShow = false;

        [Tooltip("Логировать пропущенные сообщения (например, при переполнении очереди).")]
        [SerializeField] private bool logSkips = true;

        #endregion

        #region Cached state

        private readonly Queue<PendingToast> pending = new();
        private bool wasVisible;
        private float nextAllowedShowAt;

        #endregion

        #region Nested types

        private readonly struct PendingToast
        {
            public readonly string Title;
            public readonly string Description;

            public PendingToast(string title, string description)
            {
                Title       = title;
                Description = description;
            }
        }

        #endregion

        #region Unity lifecycle

        protected override void OnEnable()
        {
            base.OnEnable();

            SystemsUtility.TryAddListener<AchievementUnlockedMessage>(OnAchievementUnlocked);

            // На случай горячей перезагрузки: считаем, что тост изначально скрыт,
            // чтобы не поймать ложное "спрятался" на первом кадре.
            wasVisible = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SystemsUtility.TryRemoveListener<AchievementUnlockedMessage>(OnAchievementUnlocked);

            pending.Clear();
            wasVisible = false;
        }

        private void Update()
        {
            var view = View;
            if (view == null) return;

            var isVisible = view.IsVisible;

            // Falling edge: тост только что закончился — время показать следующий.
            if (wasVisible && !isVisible)
            {
                TryShowNextFromQueue();
            }

            wasVisible = isVisible;
        }

        #endregion

        #region Event handling

        private void OnAchievementUnlocked(AchievementUnlockedMessage message)
        {
            var title       = message.Title ?? string.Empty;
            var description = message.Description ?? string.Empty;

            // Если тост уже на экране — в очередь, чтобы не перезаписать текущий.
            if (View != null && View.IsVisible)
            {
                Enqueue(title, description);
                return;
            }

            // Если между тостами ещё не прошёл gap — тоже в очередь.
            if (Time.unscaledTime < nextAllowedShowAt)
            {
                Enqueue(title, description);
                return;
            }

            ShowToast(title, description);
        }

        private void Enqueue(string title, string description)
        {
            if (pending.Count >= maxQueued)
            {
                if (logSkips)
                {
                    Debug.LogWarning(
                        $"[AchievementToastViewController] Очередь тостов переполнена " +
                        $"({maxQueued}), пропускаем: {title}",
                        this);
                }
                return;
            }

            pending.Enqueue(new PendingToast(title, description));
        }

        private void TryShowNextFromQueue()
        {
            if (pending.Count == 0) return;
            if (Time.unscaledTime < nextAllowedShowAt) return;

            var next = pending.Dequeue();
            ShowToast(next.Title, next.Description);
        }

        private void ShowToast(string title, string description)
        {
            var view = View;
            if (view == null)
            {
                if (logSkips)
                {
                    Debug.LogWarning(
                        "[AchievementToastViewController] View недоступна, тост пропущен.",
                        this);
                }
                return;
            }

            view.SetText(title, description);

            // Запоминаем момент — gap начнётся, когда текущий тост скроется,
            // а не когда он только что показался. Т.е. nextAllowedShowAt — только
            // минимальный "cooldown" между стартами в редком случае ручного Hide.
            nextAllowedShowAt = Time.unscaledTime + gapBetweenToasts;

            Show(isAnimate: animateShow);

            // Тост показан, начинаем отслеживать его завершение.
            wasVisible = true;
        }

        #endregion

        #region Editor validation

        private void OnValidate()
        {
            maxQueued       = Mathf.Max(1, maxQueued);
            gapBetweenToasts = Mathf.Max(0f, gapBetweenToasts);
        }

        #endregion
    }
}
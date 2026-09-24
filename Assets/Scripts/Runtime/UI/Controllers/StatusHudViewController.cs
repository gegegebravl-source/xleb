using CHARK.GameManagement;
using CHARK.SimpleUI;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Systems.Clock;
using UABPetelnia.GGJ2025.Runtime.Systems.Players;
using UABPetelnia.GGJ2025.Runtime.UI.Views;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.UI.Controllers
{
    /// <summary>
    /// Держит HUD справа вверху в актуальном состоянии: время смены, номер дня и баланс
    /// в рублях. Подписан на часы и на изменения кассы игрока.
    /// </summary>
    /// <remarks>
    /// HUD прячется, когда открыт экран ПК заказов (и прочие меню) и когда активная сцена
    /// не геймплейная: таблички с рублями и часами не должны висеть в главном меню.
    /// </remarks>
    [DisallowMultipleComponent]
    internal sealed class StatusHudViewController : ViewController<StatusHudView>
    {
        private const string GameplayScenePath = "Assets/Scenes/Scene_Gameplay.unity";

        private IShiftClockSystem clockSystem;
        private IPlayerSystem playerSystem;

        private bool suppressedByPanel;
        private bool suppressedByScene;
        private bool isHidden;

        protected override void Awake()
        {
            base.Awake();

            SystemsUtility.TryGetSystem(out clockSystem);
            SystemsUtility.TryGetSystem(out playerSystem);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            SystemsUtility.TryAddListener<ShiftClockChangedMessage>(OnClockChanged);
            SystemsUtility.TryAddListener<DayStartedMessage>(OnDayStarted);
            SystemsUtility.TryAddListener<PlayerCentsChanged>(OnPlayerCentsChanged);

            ApplyVisibility();
            RefreshAll();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SystemsUtility.TryRemoveListener<ShiftClockChangedMessage>(OnClockChanged);
            SystemsUtility.TryRemoveListener<DayStartedMessage>(OnDayStarted);
            SystemsUtility.TryRemoveListener<PlayerCentsChanged>(OnPlayerCentsChanged);
        }

        protected override void Start()
        {
            base.Start();

            ApplyVisibility();
            RefreshAll();
        }

        /// <summary>Показать HUD, если он ещё скрыт (например, после смены сцены).</summary>
        public void EnsureVisible()
        {
            var view = View;

            if (view == null)
            {
                return;
            }

            if (view.State is ViewVisibilityState.Showing or ViewVisibilityState.Shown)
            {
                return;
            }

            view.Show(isAnimate: false);
        }

        /// <summary>Спрятать/показать HUD по требованию игрока (экран ПК заказов открыт/закрыт).</summary>
        public void SetSuppressed(bool suppressed)
        {
            suppressedByPanel = suppressed;
            ApplyVisibility();
        }

        private void Update()
        {
            // Страховка от сцен без геймплея: игрок может пережить смену сцены,
            // а таблички денег и часов в главном меню не нужны.
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            suppressedByScene = active.IsValid() == false || active.path != GameplayScenePath;

            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            var hide = suppressedByPanel || suppressedByScene;

            if (hide == isHidden)
            {
                return;
            }

            isHidden = hide;

            var view = View;
            if (view == null)
            {
                return;
            }

            if (hide)
            {
                view.Hide(isAnimate: false);
            }
            else
            {
                EnsureVisible();
            }
        }

        private void RefreshAll()
        {
            var view = View;

            if (view == null)
            {
                return;
            }

            if (clockSystem != null)
            {
                view.SetClock(clockSystem.ClockText, clockSystem.Day);
                view.SetShiftProgress(clockSystem.ShiftProgress);
            }

            if (playerSystem != null && playerSystem.TryGetPlayer(out var player))
            {
                view.SetMoneyImmediate(player.Cents);
            }
        }

        private void OnClockChanged(ShiftClockChangedMessage message)
        {
            var view = View;

            if (view == null)
            {
                return;
            }

            view.SetClock(clockSystem != null ? clockSystem.ClockText : string.Empty, message.Day);
            view.SetShiftProgress(clockSystem != null ? clockSystem.ShiftProgress : 0f);
        }

        private void OnDayStarted(DayStartedMessage message)
        {
            RefreshAll();
        }

        private void OnPlayerCentsChanged(PlayerCentsChanged message)
        {
            var view = View;

            if (view != null)
            {
                view.SetMoney(message.Player != null ? message.Player.Cents : 0);
            }
        }
    }
}

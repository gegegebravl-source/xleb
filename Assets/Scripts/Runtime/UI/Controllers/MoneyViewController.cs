using CHARK.GameManagement;
using CHARK.SimpleUI;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Systems.Players;
using UABPetelnia.GGJ2025.Runtime.UI.Views;
using UABPetelnia.GGJ2025.Runtime.Utilities;

namespace UABPetelnia.GGJ2025.Runtime.UI.Controllers
{
    internal sealed class MoneyViewController : ViewController<MoneyView>
    {
        protected override void Start()
        {
            base.Start();

            if (SystemsUtility.TryGetSystem<IPlayerSystem>(out var playerSystem) &&
                playerSystem.TryGetPlayer(out var player))
            {
                View.SetMoneyInstant(player.Cents);
                return;
            }

            View.SetMoneyInstant(0);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            SystemsUtility.TryAddListener<PlayerCentsChanged>(OnPlayerCentsChanged);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SystemsUtility.TryRemoveListener<PlayerCentsChanged>(OnPlayerCentsChanged);
        }

        private void OnPlayerCentsChanged(PlayerCentsChanged message)
        {
            UpdateDisplayText(message.Player);
        }

        private void UpdateDisplayText(IPlayerActor player)
        {
            // Анимированный счётчик: сумма плавно догоняет новое значение и подсвечивается
            // зелёным при росте и красным при списании.
            View.SetMoney(player.Cents);
        }
    }
}

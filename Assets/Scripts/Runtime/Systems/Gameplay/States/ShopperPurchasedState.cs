using UABPetelnia.GGJ2025.Runtime.Utilities;
using CHARK.GameManagement;
using UABPetelnia.GGJ2025.Runtime.Systems.Players;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Gameplay.States
{
    internal sealed class ShopperPurchasedState : GameplayState
    {
        private IPlayerSystem playerSystem;

        protected override void OnInitialized()
        {
            SystemsUtility.TryGetSystem(out playerSystem);
        }

        protected override void OnDisposed()
        {
        }

        protected override void OnEntered(GameplayStateContext context)
        {
            var shopper = context.ActiveShopper;

            shopper?.PlayBuyAnimation();

            // The give animation is started by the player actor the moment the product leaves the
            // hand, so no need to play it again here.
        }

        protected override void OnExited(GameplayStateContext context)
        {
            var shopper = context.ActiveShopper;
            var player = playerSystem != null ? playerSystem.Player : default;

            shopper?.StopBuyAnimation();
            player?.StopGiveAnimation();

            var item = context.CurrentItem;
            if (item == false)
            {
                return;
            }

            // No player to pay: the sale still happened, but there is nobody to give the money to.
            if (player is UnityEngine.Object playerObject && playerObject == false)
            {
                context.CurrentItem = default;

                return;
            }

            if (player == default)
            {
                context.CurrentItem = default;

                return;
            }

            var saleCents = Mathf.Max(0, item.Cents);
            player.Cents = saleCents > int.MaxValue - player.Cents
                ? int.MaxValue
                : player.Cents + saleCents;

            // The journal and the achievements both hang off this.
            GameManager.Publish(new SaleCompletedMessage(item, saleCents));

            context.CurrentItem = default;
        }

        protected override Status OnUpdated(GameplayStateContext context)
        {
            var shopper = context.ActiveShopper;
            if (shopper == default)
            {
                return Status.Completed;
            }

            if (shopper.IsBuying)
            {
                return Status.Working;
            }

            return Status.Completed;
        }
    }
}

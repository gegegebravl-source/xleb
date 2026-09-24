using CHARK.GameManagement;
using UABPetelnia.GGJ2025.Runtime.Systems.Players;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Gameplay.States
{
    internal sealed class ShopperPunchingState : GameplayState
    {
        private IPlayerSystem playerSystem;

        protected override void OnInitialized()
        {
            playerSystem = GameManager.GetSystem<IPlayerSystem>();
        }

        protected override void OnDisposed()
        {
        }

        protected override void OnEntered(GameplayStateContext context)
        {
            var shopper = context.ActiveShopper;
            shopper?.PlayPunchAnimation();
        }

        protected override void OnExited(GameplayStateContext context)
        {
            var shopper = context.ActiveShopper;
            shopper?.StopPunchAnimation();

            var player = playerSystem != null ? playerSystem.Player : default;

            // The player may already be gone (for example the scene is being torn down while the
            // shopper is still swinging). Without this guard the state machine threw a
            // NullReferenceException instead of finishing the shift.
            if (player == default || player is UnityEngine.Object playerObject && playerObject == false)
            {
                return;
            }

            player.Health -= 1;

            GameManager.Publish(new SaleFailedMessage());
        }

        protected override Status OnUpdated(GameplayStateContext context)
        {
            var shopper = context.ActiveShopper;
            if (shopper == default)
            {
                return Status.Completed;
            }

            if (shopper.IsPunching)
            {
                return Status.Working;
            }

            return Status.Completed;
        }
    }
}

using UABPetelnia.GGJ2025.Runtime.Utilities;
using CHARK.GameManagement;
using UABPetelnia.GGJ2025.Runtime.Systems.Players;
using UABPetelnia.GGJ2025.Runtime.Systems.Scenes;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Gameplay.States
{
    /// <summary>
    /// Ends the shift once the shopkeeper has either reached the money goal or lost all health.
    /// </summary>
    /// <remarks>
    /// There used to be a third branch here which declared victory as soon as
    /// <c>IShopperSystem.IsShoppersAvailable</c> turned false, but the list behind that property is
    /// refilled on every scene load and nothing ever removed a shopper from it, so the branch could
    /// not fire. The shift is won on money, which is the condition that is actually reachable.
    /// </remarks>
    internal sealed class GameOverCheckState : GameplayState
    {
        private IPlayerSystem playerSystem;
        private ISceneSystem sceneSystem;

        protected override void OnInitialized()
        {
            SystemsUtility.TryGetSystem(out playerSystem);
            SystemsUtility.TryGetSystem(out sceneSystem);
        }

        protected override void OnDisposed()
        {
        }

        protected override void OnEntered(GameplayStateContext context)
        {
            // The systems are read once at start-up, so re-check them here: a shift that is already
            // over should not throw because one of them was never registered.
            var player = playerSystem != null ? playerSystem.Player : default;
            if (player == default || player is UnityEngine.Object playerObject && playerObject == false)
            {
                return;
            }

            if (player.IsCentsGoalReached)
            {
                NextState = default;
                sceneSystem?.LoadGameVictoryScene();

                return;
            }

            if (player.Health <= 0)
            {
                NextState = default;
                sceneSystem?.LoadGameOverScene();
            }
        }

        protected override void OnExited(GameplayStateContext context)
        {
        }

        protected override Status OnUpdated(GameplayStateContext context)
        {
            return Status.Completed;
        }
    }
}

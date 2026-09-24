using CHARK.GameManagement;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UABPetelnia.GGJ2025.Runtime.Systems.Shoppers;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Gameplay.States
{
    internal sealed class ShopperSpawnState : GameplayState
    {
        private readonly GameplaySettings gameplaySettings;
        private IShopperSystem shopperSystem;

        private float spawnTimeSeconds;
        private bool isSpawnedOnce;

        public ShopperSpawnState(GameplaySettings gameplaySettings)
        {
            this.gameplaySettings = gameplaySettings;
        }

        protected override void OnInitialized()
        {
            shopperSystem = GameManager.GetSystem<IShopperSystem>();
        }

        protected override void OnDisposed()
        {
        }

        protected override void OnEntered(GameplayStateContext context)
        {
            spawnTimeSeconds = Time.time + (gameplaySettings != false
                ? gameplaySettings.SpawnDelaySeconds
                : 0f);

            // Первый заход тоже должен выдерживать задержку: иначе покупатель выскакивает
            // мгновенно при старте смены.
            isSpawnedOnce = true;
        }

        protected override void OnExited(GameplayStateContext context)
        {
            spawnTimeSeconds = 0f;
        }

        protected override Status OnUpdated(GameplayStateContext context)
        {
            if (shopperSystem == null)
            {
                NextState = default;
                return Status.Completed;
            }

            if (isSpawnedOnce && Time.time < spawnTimeSeconds)
            {
                return Status.Working;
            }

            if (shopperSystem.TrySpawnRandomShopper(shopperSystem.RandomSpawnPoint, out var shopper))
            {
                context.ActiveShopper = shopper;
                return Status.Completed;
            }

            return Status.Working;
        }
    }
}

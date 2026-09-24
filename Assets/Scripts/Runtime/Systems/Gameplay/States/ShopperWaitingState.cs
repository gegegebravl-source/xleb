using CHARK.GameManagement;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UABPetelnia.GGJ2025.Runtime.Systems.Players;
using UABPetelnia.GGJ2025.Runtime.Systems.Products;
using UABPetelnia.GGJ2025.Runtime.Systems.Shoppers;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Gameplay.States
{
    /// <summary>
    /// The shopper stands at the counter and has told the player what they want. There are no
    /// floating choice bubbles anymore: the player has to grab the asked product from a shelf and
    /// hand it over, which is what this state waits for.
    /// </summary>
    internal sealed class ShopperWaitingState : GameplayState
    {
        /// <summary>
        /// How many of the shopper's lines may be thrown away while looking for one the kiosk can
        /// actually satisfy. The assortment is far bigger than the shelving, so a request that
        /// mentions nothing that is out on a shelf is normal.
        /// </summary>
        private const int RequestAttempts = 12;

        private IShopperSystem shopperSystem;
        private IPlayerSystem playerSystem;
        private IProductSystem productSystem;

        private readonly GameplaySettings gameplaySettings;
        private readonly GameplayState successState;
        private readonly GameplayState failureState;
        private readonly GameplayState refusalState;

        private bool isHandedOver;
        private bool isCorrectItem;
        private bool isRanting;

        private float expiryTimeSeconds;
        private float patienceExpiryTimeSeconds;

        public ShopperWaitingState(
            GameplaySettings gameplaySettings,
            GameplayState successState,
            GameplayState failureState,
            GameplayState refusalState)
        {
            this.gameplaySettings = gameplaySettings;
            this.successState = successState;
            this.failureState = failureState;
            this.refusalState = refusalState;
        }

        protected override void OnInitialized()
        {
            SystemsUtility.TryGetSystem(out shopperSystem);
            SystemsUtility.TryGetSystem(out playerSystem);
            SystemsUtility.TryGetSystem(out productSystem);

            GameManager.AddListener<ItemHandedOverMessage>(OnItemHandedOver);
        }

        protected override void OnDisposed()
        {
            GameManager.RemoveListener<ItemHandedOverMessage>(OnItemHandedOver);
        }

        protected override void OnEntered(GameplayStateContext context)
        {
            isHandedOver = false;
            isCorrectItem = false;
            isRanting = false;
            expiryTimeSeconds = 0f;
            patienceExpiryTimeSeconds = 0f;

            var shopper = context.ActiveShopper;
            if (shopper == default)
            {
                return;
            }

            // Without the shopper system the hand-over can never be registered, so the shopper
            // walks off instead of standing at the counter forever.
            if (shopperSystem == null)
            {
                StartRanting(refusalState);

                return;
            }

            var request = PickAvailableRequest(shopper);
            var player = playerSystem?.Player;

            if (request == null || request.IsEmpty)
            {
                // Missing/empty dialogue data is a refusal, not a wrong-item mistake. A malformed
                // shopper asset must never damage the player or deadlock the queue.
                StartRanting(refusalState);
                GameManager.Publish(new SaleRefusedMessage());
                player?.ShowPurchase(request);
                return;
            }

            shopperSystem.AwaitItem(request);

            shopper.ShowRequest(request);
            player?.ShowPurchase(request);

            // A missing settings asset must not make a shopper wait forever.
            patienceExpiryTimeSeconds = Time.time + (gameplaySettings
                ? gameplaySettings.PatienceDurationSeconds
                : 20f);
        }

        protected override void OnExited(GameplayStateContext context)
        {
            shopperSystem?.StopAwaitingItem();

            var shopper = context.ActiveShopper;
            var player = playerSystem?.Player;

            shopper?.HideRequest();
            player?.HidePurchase();
        }

        protected override Status OnUpdated(GameplayStateContext context)
        {
            if (isRanting)
            {
                return Time.time >= expiryTimeSeconds
                    ? Status.Completed
                    : Status.Working;
            }

            if (isHandedOver)
            {
                NextState = isCorrectItem ? successState : failureState;
                return Status.Completed;
            }

            if (patienceExpiryTimeSeconds > 0f && Time.time >= patienceExpiryTimeSeconds)
            {
                // Waited too long: the shopper gets angry instead of blocking the queue forever.
                NextState = failureState;
                return Status.Completed;
            }

            return Status.Working;
        }

        private void StartRanting(GameplayState nextState)
        {
            isRanting = true;
            expiryTimeSeconds = gameplaySettings
                ? Time.time + gameplaySettings.RantDurationSeconds
                : Time.time;
            NextState = nextState;
        }

        /// <summary>
        /// Take one of the shopper's lines and narrow it down to the goods that are physically out
        /// on the shelves, so whatever is written in the chat can actually be handed over. Lines
        /// that cannot be served at all are dropped, and <c>null</c> is returned when the shopper
        /// has nothing left that the kiosk could sell.
        /// </summary>
        private PurchaseRequest PickAvailableRequest(IShopperActor shopper)
        {
            for (var attempt = 0; attempt < RequestAttempts; attempt++)
            {
                var request = shopper.PopPurchaseRequest();
                if (request == null)
                {
                    return default;
                }

                if (request.IsEmpty)
                {
                    // A rank with no goods in it, keep it so the shopper can walk off in a huff.
                    return request;
                }

                request.KeepOnly(IsOnShelf);

                if (request.IsEmpty == false)
                {
                    return request;
                }
            }

            return default;
        }

        private bool IsOnShelf(ItemData item)
        {
            if (productSystem != null && productSystem.IsOnShelf(item))
            {
                return true;
            }

            // Товар, который игрок уже держит в руках или в слоте, тоже считается доступным:
            // иначе покупатель откажется от заказа ровно в тот момент, когда товар унесли с полки.
            var player = playerSystem?.Player;

            if (player == null || player is UnityEngine.Object playerObject && playerObject == false)
            {
                return false;
            }

            var carried = player.CarriedItems;
            if (carried == null)
            {
                return false;
            }

            for (var index = 0; index < carried.Count; index++)
            {
                if (carried[index] == item)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnItemHandedOver(ItemHandedOverMessage message)
        {
            if (isHandedOver || shopperSystem == null || shopperSystem.IsAwaitingItem == false)
            {
                return;
            }

            isHandedOver = true;
            isCorrectItem = shopperSystem.IsItemWanted(message.Item);

            // Money is only paid for the product the shopper actually asked for.
            Context.CurrentItem = isCorrectItem ? message.Item : default;
        }
    }
}

using System;
using System.Collections.Generic;
using CHARK.GameManagement;
using CHARK.GameManagement.Systems;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UABPetelnia.GGJ2025.Runtime.Systems.Delivery;
using UABPetelnia.GGJ2025.Runtime.Systems.Players;
using UABPetelnia.GGJ2025.Runtime.Systems.Scenes;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Shop
{
    /// <summary>
    /// The kiosk economy. The catalogue comes from <see cref="GameplaySettings.AvailableItems"/>,
    /// buying in costs a fraction of the sale price and everything arrives after a short delay, so
    /// the shelves are limited by what the shopkeeper has actually ordered.
    /// </summary>
    internal sealed class ShopSystem : MonoSystem, IShopSystem, IUpdateListener
    {
        [Header("Data")]
        [SerializeField]
        private GameplaySettings gameplaySettings;

        [Header("Stock")]
        [Min(0)]
        [SerializeField]
        private int initialStock = 6;

        [Header("Deliveries")]
        [Min(0f)]
        [SerializeField]
        private float deliveryTravelSeconds = 25f;

        [Min(0f)]
        [SerializeField]
        [Range(0.05f, 1f)]
        private float purchasePriceFactor = 0.5f;

        private readonly List<ShopProduct> products = new();
        private readonly List<ShopDeliveryOrder> orders = new();
        private readonly List<ShopCartLine> cart = new();

        private IPlayerSystem playerSystem;
        private int nextOrderId;
        private bool isCatalogueBuilt;

        public IReadOnlyList<ShopProduct> Products
        {
            get
            {
                EnsureCatalogue();

                return products;
            }
        }

        public IReadOnlyList<ShopDeliveryOrder> Orders => orders;

        public IReadOnlyList<ShopCartLine> Cart => cart;

        public int Balance => playerSystem?.Player?.Cents ?? 0;

        public int CartTotalQuantity
        {
            get
            {
                long total = 0;

                for (var index = 0; index < cart.Count; index++)
                {
                    total += Math.Max(0, cart[index]?.Quantity ?? 0);
                }

                return total > int.MaxValue ? int.MaxValue : (int)total;
            }
        }

        public int CartTotalCost
        {
            get
            {
                return ShopOrderRules.TryCalculateTotal(cart, out var total, out _)
                    ? total
                    : 0;
            }
        }

        public float SecondsToNextArrival
        {
            get
            {
                if (orders.Count <= 0)
                {
                    return 0f;
                }

                var seconds = float.MaxValue;

                for (var index = 0; index < orders.Count; index++)
                {
                    var order = orders[index];
                    if (order == null)
                    {
                        continue;
                    }

                    if (order.SecondsLeft < seconds)
                    {
                        seconds = order.SecondsLeft;
                    }
                }

                return seconds == float.MaxValue ? 0f : seconds;
            }
        }

        public float DeliveryTravelSeconds => IsFiniteNonNegative(deliveryTravelSeconds)
            ? deliveryTravelSeconds
            : 0f;

        public event Action Changed;

        public override void OnInitialized()
        {
            SystemsUtility.TryGetSystem(out playerSystem);

            GameManager.AddListener<SceneLoadEnteredMessage>(OnSceneLoadEntered);
        }

        public override void OnDisposed()
        {
            GameManager.RemoveListener<SceneLoadEnteredMessage>(OnSceneLoadEntered);
        }

        public void OnUpdated(float deltaTime)
        {
            if (orders.Count <= 0)
            {
                return;
            }

            var hasArrived = false;

            for (var index = orders.Count - 1; index >= 0; index--)
            {
                var order = orders[index];
                if (order == null)
                {
                    orders.RemoveAt(index);
                    hasArrived = true;
                    continue;
                }

                if (order.SecondsLeft > 0f)
                {
                    continue;
                }

                // Товар приехал: он лежит в коробке курьера и на полки сам не попадает.
                for (var lineIndex = 0; lineIndex < order.Lines.Count; lineIndex++)
                {
                    var line = order.Lines == null ? null : order.Lines[lineIndex];
                    if (line == null || line.Product == null || line.Quantity <= 0)
                    {
                        continue;
                    }

                    line.Product.Stock = ShopMath.SafeAdd(line.Product.Stock, line.Quantity);
                    line.Product.InTransit = Mathf.Max(0, line.Product.InTransit - line.Quantity);
                }

                orders.RemoveAt(index);

                GameManager.Publish(new DeliveryArrivedMessage(order));

                hasArrived = true;
            }

            if (hasArrived)
            {
                Changed?.Invoke();
            }
        }

        public bool TryGetProduct(ItemData item, out ShopProduct product)
        {
            product = default;

            if (item == false)
            {
                return false;
            }

            EnsureCatalogue();

            foreach (var candidate in products)
            {
                if (candidate.Item == item)
                {
                    product = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Положить товар в корзину. Деньги не списываются: заказ оплачивается целиком
        /// кнопкой «Заказать» в ПК, чтобы курьер приехал один раз с одной коробкой.
        /// </summary>
        public bool TryAddToCart(ItemData item, int quantity, out string error)
        {
            error = default;

            if (quantity <= 0)
            {
                error = "bad_quantity";
                return false;
            }

            if (TryGetProduct(item, out var product) == false)
            {
                error = "unknown_product";
                return false;
            }

            var line = FindCartLine(product);
            var currentQuantity = line != null ? line.Quantity : 0;
            if (ShopOrderRules.TryAddQuantity(currentQuantity, quantity, out var newQuantity, out error) == false)
            {
                return false;
            }

            if (line != null)
            {
                line.Quantity = newQuantity;
            }
            else
            {
                cart.Add(new ShopCartLine(product, newQuantity));
            }

            Changed?.Invoke();

            return true;
        }

        public bool TryRemoveFromCart(ItemData item, int quantity, out string error)
        {
            error = default;

            if (quantity <= 0)
            {
                error = "bad_quantity";
                return false;
            }

            if (TryGetProduct(item, out var product) == false)
            {
                error = "unknown_product";
                return false;
            }

            var line = FindCartLine(product);
            if (line == null)
            {
                error = "not_in_cart";
                return false;
            }

            if (ShopOrderRules.TryRemoveQuantity(line.Quantity, quantity, out var newQuantity, out error) == false)
            {
                return false;
            }

            line.Quantity = newQuantity;
            if (newQuantity == 0)
            {
                cart.Remove(line);
            }

            Changed?.Invoke();

            return true;
        }

        public void ClearCart()
        {
            if (cart.Count <= 0)
            {
                return;
            }

            cart.Clear();

            Changed?.Invoke();
        }

        /// <summary>
        /// Оплатить корзину: списать деньги и отправить один заказ. Один заказ — один курьер
        /// и одна коробка со всеми позициями.
        /// </summary>
        public bool TrySubmitCart(out string error)
        {
            error = default;

            if (cart.Count <= 0)
            {
                error = "empty_cart";
                return false;
            }

            var player = playerSystem?.Player;
            if (player == null || player is UnityEngine.Object playerObject && playerObject == false)
            {
                error = "no_player";
                return false;
            }

            if (ShopOrderRules.TryCalculateTotal(cart, out var total, out error) == false)
            {
                return false;
            }

            if (player.Cents < total)
            {
                error = "not_enough_money";
                return false;
            }

            // Validate the entire transaction before changing money or stock. This keeps a
            // malformed line from charging the player and then failing halfway through.
            foreach (var cartLine in cart)
            {
                if (cartLine == null || cartLine.Product == null || cartLine.Quantity <= 0
                    || cartLine.Quantity > ShopOrderRules.MaxLineQuantity
                    || cartLine.Quantity > int.MaxValue - cartLine.Product.InTransit)
                {
                    error = "bad_quantity";
                    return false;
                }
            }

            player.Cents -= total;

            var lines = new List<ShopDeliveryLine>(cart.Count);

            foreach (var cartLine in cart)
            {
                cartLine.Product.InTransit = ShopMath.SafeAdd(
                    cartLine.Product.InTransit,
                    cartLine.Quantity
                );
                lines.Add(new ShopDeliveryLine(cartLine.Product, cartLine.Quantity));
            }

            cart.Clear();

            orders.Add(
                new ShopDeliveryOrder(
                    id: $"order-{nextOrderId}",
                    lines: lines,
                    cost: total,
                    arrivalTimeSeconds: Time.time + DeliveryTravelSeconds
                )
            );

            nextOrderId = nextOrderId >= int.MaxValue ? 0 : nextOrderId + 1;
            Changed?.Invoke();

            return true;
        }

        private ShopCartLine FindCartLine(ShopProduct product)
        {
            for (var index = 0; index < cart.Count; index++)
            {
                if (cart[index].Product == product)
                {
                    return cart[index];
                }
            }

            return default;
        }

        public bool TryTakeFromStock(ItemData item)
        {
            if (TryGetProduct(item, out var product) == false || product.Stock <= 0)
            {
                return false;
            }

            product.Stock--;

            Changed?.Invoke();

            return true;
        }

        private void OnSceneLoadEntered(SceneLoadEnteredMessage message)
        {
            // Каталог строится один раз и НЕ пересобирается на каждой загрузке сцены:
            // иначе оплаченные доставки в пути и остатки склада стираются.
            EnsureCatalogue();
        }

        /// <summary>
        /// Build the catalogue if nobody has asked for it yet.
        /// </summary>
        /// <remarks>
        /// Shelf slots fill themselves while a scene is still loading, which can be earlier than
        /// the scene load message. Reading the catalogue before it was built would answer "unknown
        /// product" to every item and leave the kiosk open with empty shelves.
        /// </remarks>
        private void EnsureCatalogue()
        {
            if (isCatalogueBuilt)
            {
                return;
            }

            RebuildCatalogue();
        }

        private void RebuildCatalogue()
        {
            products.Clear();
            orders.Clear();

            nextOrderId = 0;

            if (gameplaySettings == false)
            {
                // Do not cache a failed build. A later catalogue request can then retry after
                // scene/prefab wiring has supplied the settings asset.
                isCatalogueBuilt = false;
                return;
            }

            isCatalogueBuilt = true;

            if (gameplaySettings.AvailableItems == null)
            {
                isCatalogueBuilt = true;
                return;
            }

            var factor = IsFiniteNonNegative(purchasePriceFactor)
                ? Mathf.Clamp(purchasePriceFactor, 0.05f, 1f)
                : 0.5f;

            foreach (var item in gameplaySettings.AvailableItems)
            {
                if (item == false || item.Cents <= 0)
                {
                    // A zero/negative sale price cannot participate in a paid delivery and would
                    // otherwise create stock that can never produce a valid sale.
                    continue;
                }

                var purchasePrice = (int)Math.Min(
                    int.MaxValue,
                    Math.Max(1d, Math.Round(item.Cents * (double)factor))
                );

                products.Add(new ShopProduct(item, purchasePrice)
                {
                    Stock = Mathf.Max(0, initialStock),
                });
            }

            Debug.Log($"[Shop] Каталог собран: товаров {products.Count}, запас по {initialStock} шт.");

            Changed?.Invoke();
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return value >= 0f && float.IsNaN(value) == false && float.IsInfinity(value) == false;
        }
    }
}

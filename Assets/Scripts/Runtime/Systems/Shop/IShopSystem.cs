using System;
using System.Collections.Generic;
using CHARK.GameManagement.Systems;
using UABPetelnia.GGJ2025.Runtime.Settings;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Shop
{
    /// <summary>
    /// One line of the shop catalogue: what the kiosk sells, what it costs to buy in and how much
    /// of it is left in the back room.
    /// </summary>
    internal sealed class ShopProduct
    {
        public ShopProduct(ItemData item, int purchasePrice)
        {
            Item = item;
            PurchasePrice = Math.Max(0, purchasePrice);
        }

        public ItemData Item { get; }

        /// <summary>
        /// What the shopkeeper pays per unit when ordering. Selling price comes from the item.
        /// </summary>
        public int PurchasePrice { get; }

        public int SalePrice => Item ? Math.Max(0, Item.Cents) : 0;

        /// <summary>
        /// Units in the back room. Taking a delivered unit for manual restocking decrements it;
        /// sales consume the physical shelf item instead.
        /// </summary>
        private int stock;

        public int Stock
        {
            get => stock;
            internal set => stock = Math.Max(0, value);
        }

        /// <summary>
        /// Units already paid for and currently on the road.
        /// </summary>
        private int inTransit;

        public int InTransit
        {
            get => inTransit;
            internal set => inTransit = Math.Max(0, value);
        }

        public bool IsInStock => Stock > 0;
    }

    internal static class ShopMath
    {
        public static int CalculateCost(ShopProduct product, int quantity)
        {
            if (product == null || quantity <= 0 || product.PurchasePrice <= 0)
            {
                return 0;
            }

            return quantity > int.MaxValue / product.PurchasePrice
                ? int.MaxValue
                : product.PurchasePrice * quantity;
        }

        public static int SafeAdd(int left, int right)
        {
            left = Math.Max(0, left);
            right = Math.Max(0, right);
            return right > int.MaxValue - left ? int.MaxValue : left + right;
        }
    }

    /// <summary>
    /// Одна позиция доставки: товар и сколько единиц едет в коробке.
    /// </summary>
    internal sealed class ShopDeliveryLine
    {
        public ShopDeliveryLine(ShopProduct product, int quantity)
        {
            Product = product;
            Quantity = quantity;
        }

        public ShopProduct Product { get; }

        public int Quantity { get; }

        public int Cost => ShopMath.CalculateCost(Product, Quantity);
    }

    /// <summary>
    /// Одна позиция корзины на ПК: товар и количество, которое игрок собирается заказать.
    /// Количество меняется кнопками +/-, поэтому оно изменяемое.
    /// </summary>
    internal sealed class ShopCartLine
    {
        public ShopCartLine(ShopProduct product, int quantity)
        {
            Product = product;
            Quantity = quantity;
        }

        public ShopProduct Product { get; }

        public int Quantity { get; internal set; }

        public int Cost => ShopMath.CalculateCost(Product, Quantity);
    }

    /// <summary>
    /// A paid-for delivery that is on the way to the kiosk. Один заказ — один курьер и одна
    /// коробка со всеми позициями.
    /// </summary>
    internal sealed class ShopDeliveryOrder
    {
        public ShopDeliveryOrder(
            string id,
            IReadOnlyList<ShopDeliveryLine> lines,
            int cost,
            float arrivalTimeSeconds
        )
        {
            Id = id;
            Lines = lines ?? Array.Empty<ShopDeliveryLine>();
            Cost = Math.Max(0, cost);
            ArrivalTimeSeconds = float.IsNaN(arrivalTimeSeconds) || float.IsInfinity(arrivalTimeSeconds)
                ? UnityEngine.Time.time
                : arrivalTimeSeconds;
        }

        public string Id { get; }

        public IReadOnlyList<ShopDeliveryLine> Lines { get; }

        public int Cost { get; }

        public float ArrivalTimeSeconds { get; }

        public float SecondsLeft => Math.Max(0f, ArrivalTimeSeconds - UnityEngine.Time.time);

        /// <summary>Сколько всего единиц товара едет в этой коробке.</summary>
        public int TotalQuantity
        {
            get
            {
                var total = 0;

                for (var index = 0; index < Lines.Count; index++)
                {
                    var line = Lines[index];
                    if (line == null)
                    {
                        continue;
                    }

                    total = ShopMath.SafeAdd(total, line.Quantity);
                }

                return total;
            }
        }

        /// <summary>Подпись для списка доставок: "Лимонад ×3, Хлеб ×1".</summary>
        public string Describe()
        {
            if (Lines.Count <= 0)
            {
                return "Пустой заказ";
            }

            var parts = new List<string>(Lines.Count);

            for (var index = 0; index < Lines.Count; index++)
            {
                var line = Lines[index];
                if (line == null || line.Product == null)
                {
                    continue;
                }

                parts.Add($"{UABPetelnia.GGJ2025.Runtime.UI.Views.DeliveryRowView.Prettify(line.Product.Item?.Id)} ×{line.Quantity}");
            }

            return parts.Count == 0 ? "Пустой заказ" : string.Join(", ", parts);
        }
    }

    /// <summary>
    /// The kiosk economy: catalogue, back room stock and paid deliveries.
    /// </summary>
    internal interface IShopSystem : ISystem
    {
        IReadOnlyList<ShopProduct> Products { get; }

        IReadOnlyList<ShopDeliveryOrder> Orders { get; }

        /// <summary>Корзина заказа на ПК: один заказ — один курьер.</summary>
        IReadOnlyList<ShopCartLine> Cart { get; }

        /// <summary>
        /// Money in the cash box, mirrored from the player.
        /// </summary>
        int Balance { get; }

        /// <summary>Сколько единиц товара в корзине.</summary>
        int CartTotalQuantity { get; }

        /// <summary>Сколько стоит весь заказ в корзине.</summary>
        int CartTotalCost { get; }

        /// <summary>Сколько едет ближайшая доставка, в секундах (0 — доставок нет).</summary>
        float SecondsToNextArrival { get; }

        /// <summary>
        /// Raised when stock, orders or money change, so any open panel can refresh.
        /// </summary>
        event Action Changed;

        bool TryGetProduct(ItemData item, out ShopProduct product);

        /// <summary>
        /// Take one sold unit out of the back room.
        /// </summary>
        bool TryTakeFromStock(ItemData item);

        /// <summary>
        /// Положить товар в корзину. Деньги пока не списываются: это происходит при отправке заказа.
        /// </summary>
        bool TryAddToCart(ItemData item, int quantity, out string error);

        /// <summary>Убрать из корзины указанное количество единиц товара.</summary>
        bool TryRemoveFromCart(ItemData item, int quantity, out string error);

        /// <summary>Очистить корзину целиком.</summary>
        void ClearCart();

        /// <summary>
        /// Оплатить корзину: списать деньги и отправить один заказ (один курьер с коробкой).
        /// </summary>
        bool TrySubmitCart(out string error);
    }
}

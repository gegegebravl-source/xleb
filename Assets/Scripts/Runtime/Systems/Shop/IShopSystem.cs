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
            PurchasePrice = purchasePrice;
        }

        public ItemData Item { get; }

        /// <summary>
        /// What the shopkeeper pays per unit when ordering. Selling price comes from the item.
        /// </summary>
        public int PurchasePrice { get; }

        public int SalePrice => Item ? Item.Cents : 0;

        /// <summary>
        /// Units available for the shelves. Selling decrements it, a delivery replenishes it.
        /// </summary>
        public int Stock { get; internal set; }

        /// <summary>
        /// Units already paid for and currently on the road.
        /// </summary>
        public int InTransit { get; internal set; }

        public bool IsInStock => Stock > 0;
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

        public int Cost => Product.PurchasePrice * Quantity;
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

        public int Cost => Product.PurchasePrice * Quantity;
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
            Lines = lines;
            Cost = cost;
            ArrivalTimeSeconds = arrivalTimeSeconds;
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
                    total += Lines[index].Quantity;
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
                parts.Add($"{UABPetelnia.GGJ2025.Runtime.UI.Views.DeliveryRowView.Prettify(line.Product.Item?.Id)} ×{line.Quantity}");
            }

            return string.Join(", ", parts);
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

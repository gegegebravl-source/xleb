using System;
using System.Collections.Generic;

namespace WarmBread
{
    [Serializable]
    public sealed class ProductStock
    {
        public string ProductId;
        public int Quantity;
        public int Price;
        public int PurchasePrice;
    }

    public sealed class ShopInventoryService
    {
        private readonly Dictionary<string, ProductStock> products = new Dictionary<string, ProductStock>(StringComparer.Ordinal);
        private readonly GameEventBus events;
        public ShopInventoryService(GameEventBus events) { this.events = events ?? throw new ArgumentNullException(nameof(events)); }
        public IEnumerable<ProductStock> Products => products.Values;

        public bool Register(string productId, int quantity, int purchasePrice, int salePrice)
        {
            if (string.IsNullOrWhiteSpace(productId) || quantity < 0 || purchasePrice < 0 || salePrice <= 0 || products.ContainsKey(productId)) return false;
            products.Add(productId, new ProductStock { ProductId = productId, Quantity = quantity, PurchasePrice = purchasePrice, Price = salePrice });
            events.Publish(new StockChanged(productId, quantity));
            return true;
        }

        public bool TryChangeStock(string productId, int delta)
        {
            if (!products.TryGetValue(productId, out var stock)) return false;
            var next = (long)stock.Quantity + delta;
            if (next < 0 || next > int.MaxValue) return false;
            stock.Quantity = (int)next;
            events.Publish(new StockChanged(productId, stock.Quantity));
            return true;
        }

        public bool TrySetPrice(string productId, int price)
        {
            if (!products.TryGetValue(productId, out var stock) || price < 1 || price > 1000000) return false;
            stock.Price = price;
            return true;
        }

        public bool TryGet(string productId, out ProductStock stock) { return products.TryGetValue(productId, out stock); }
    }
}

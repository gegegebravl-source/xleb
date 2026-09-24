using System;
using System.Collections.Generic;

namespace WarmBread
{
    public enum DeliveryStatus { Draft, Ordered, InTransit, Arrived, Cancelled }

    [Serializable]
    public sealed class DeliveryOrder
    {
        public string Id;
        public string ProductId;
        public int Quantity;
        public int Cost;
        public double ArrivalMinute;
        public DeliveryStatus Status;
    }

    public sealed class DeliveryService
    {
        private readonly List<DeliveryOrder> orders = new List<DeliveryOrder>();
        private readonly GameEventBus eventBus;
        private readonly EconomyLedger ledger;

        public DeliveryService(GameEventBus eventBus, EconomyLedger ledger, int capacity = 100)
        {
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            this.ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            Capacity = Math.Max(1, capacity);
        }

        public int Capacity { get; }
        public IReadOnlyList<DeliveryOrder> Orders => orders;

        public bool TryOrder(string id, string productId, int quantity, int cost, double now, double travelMinutes, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(productId)) { error = "invalid_id"; return false; }
            if (quantity <= 0 || quantity > Capacity || cost <= 0 || travelMinutes < 0) { error = "invalid_delivery"; return false; }
            for (var i = 0; i < orders.Count; i++) if (orders[i].Id == id) { error = "duplicate_delivery"; return false; }
            if (!ledger.TryApply("delivery:" + id, -cost, LedgerReason.Purchase, out error)) return false;
            var order = new DeliveryOrder { Id = id, ProductId = productId, Quantity = quantity, Cost = cost, ArrivalMinute = now + travelMinutes, Status = DeliveryStatus.InTransit };
            orders.Add(order);
            eventBus.Publish(new DeliveryChanged(id, order.Status));
            return true;
        }

        public void Update(double now)
        {
            for (var i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                if (order.Status != DeliveryStatus.InTransit || order.ArrivalMinute > now) continue;
                order.Status = DeliveryStatus.Arrived;
                eventBus.Publish(new DeliveryChanged(order.Id, order.Status));
                eventBus.Publish(new StockChanged(order.ProductId, order.Quantity));
            }
        }

        public bool TryCancel(string id, out string error)
        {
            error = "delivery_not_found";
            for (var i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                if (order.Id != id) continue;
                if (order.Status == DeliveryStatus.Arrived || order.Status == DeliveryStatus.Cancelled) { error = "delivery_not_cancellable"; return false; }
                if (!ledger.TryApply("delivery-refund:" + id, order.Cost, LedgerReason.Refund, out error)) return false;
                order.Status = DeliveryStatus.Cancelled;
                eventBus.Publish(new DeliveryChanged(id, order.Status));
                return true;
            }
            return false;
        }
    }
}

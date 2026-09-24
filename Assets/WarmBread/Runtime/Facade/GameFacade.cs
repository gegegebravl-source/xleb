using System;

namespace WarmBread
{
    public sealed class GameFacade
    {
        private readonly GameEventBus events;
        private readonly WorldClock clock;
        private readonly EconomyLedger ledger;
        private readonly DeliveryService deliveries;
        private int reputation = 50;
        private bool dayOpen;

        public GameFacade(GameEventBus events, WorldClock clock, EconomyLedger ledger, DeliveryService deliveries)
        {
            this.events = events ?? throw new ArgumentNullException(nameof(events));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            this.deliveries = deliveries ?? throw new ArgumentNullException(nameof(deliveries));
        }

        public GameSnapshot Snapshot => new GameSnapshot(clock.Day, clock.Hour, clock.Minute, ledger.Balance, reputation, clock.IsPaused);
        public void Tick(double realSeconds) { clock.Tick(realSeconds); deliveries.Update(clock.AbsoluteMinutes); }

        public void StartDay()
        {
            if (dayOpen) return;
            dayOpen = true;
            events.Publish(new DayStarted(clock.Day, clock.Weekday));
        }

        public void PauseGame() { clock.SetPaused(true); }
        public void ResumeGame() { clock.SetPaused(false); }

        public void CloseDay(int revenue, int expenses)
        {
            if (!dayOpen) return;
            dayOpen = false;
            events.Publish(new DayClosed(clock.Day, Math.Max(0, revenue), Math.Max(0, expenses)));
        }

        public bool RequestDelivery(string productId, int quantity, int unitCost, int travelMinutes, out string error)
        {
            var id = clock.Day + "-" + Guid.NewGuid().ToString("N");
            var total = checked(quantity * unitCost);
            return deliveries.TryOrder(id, productId, quantity, total, clock.AbsoluteMinutes, travelMinutes, out error);
        }

        public bool RecordSale(string saleId, string customerId, int total, int paid, out string error)
        {
            error = null;
            if (!dayOpen) { error = "day_closed"; events.Publish(new SaleRejected(error)); return false; }
            if (total <= 0 || paid < total) { error = "invalid_payment"; events.Publish(new SaleRejected(error)); return false; }
            if (!ledger.TryApply("sale:" + saleId, total, LedgerReason.Sale, out error)) { events.Publish(new SaleRejected(error)); return false; }
            events.Publish(new SaleCompleted(customerId, total, paid - total));
            return true;
        }

        public void ChangeReputation(int delta)
        {
            var old = reputation;
            reputation = Math.Max(0, Math.Min(100, reputation + delta));
            if (old != reputation) events.Publish(new ReputationChanged(old, reputation));
        }
    }
}

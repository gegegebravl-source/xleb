namespace WarmBread
{
    public readonly struct DayStarted : IGameEvent
    {
        public DayStarted(int day, int weekday) { Day = day; Weekday = weekday; }
        public int Day { get; }
        public int Weekday { get; }
    }

    public readonly struct DayClosed : IGameEvent
    {
        public DayClosed(int day, int revenue, int expenses) { Day = day; Revenue = revenue; Expenses = expenses; }
        public int Day { get; }
        public int Revenue { get; }
        public int Expenses { get; }
    }

    public readonly struct SaleCompleted : IGameEvent
    {
        public SaleCompleted(string customerId, int total, int change) { CustomerId = customerId; Total = total; Change = change; }
        public string CustomerId { get; }
        public int Total { get; }
        public int Change { get; }
    }

    public readonly struct SaleRejected : IGameEvent
    {
        public SaleRejected(string reason) { Reason = reason; }
        public string Reason { get; }
    }

    public readonly struct StockChanged : IGameEvent
    {
        public StockChanged(string productId, int quantity) { ProductId = productId; Quantity = quantity; }
        public string ProductId { get; }
        public int Quantity { get; }
    }

    public readonly struct ReputationChanged : IGameEvent
    {
        public ReputationChanged(int oldValue, int newValue) { OldValue = oldValue; NewValue = newValue; }
        public int OldValue { get; }
        public int NewValue { get; }
    }

    public readonly struct WeatherChanged : IGameEvent
    {
        public WeatherChanged(string weatherId) { WeatherId = weatherId; }
        public string WeatherId { get; }
    }

    public readonly struct DialogueRequested : IGameEvent
    {
        public DialogueRequested(string speakerId, string text, float duration) { SpeakerId = speakerId; Text = text; Duration = duration; }
        public string SpeakerId { get; }
        public string Text { get; }
        public float Duration { get; }
    }

    public readonly struct DeliveryChanged : IGameEvent
    {
        public DeliveryChanged(string deliveryId, DeliveryStatus status) { DeliveryId = deliveryId; Status = status; }
        public string DeliveryId { get; }
        public DeliveryStatus Status { get; }
    }
}

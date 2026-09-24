using CHARK.GameManagement.Messaging;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Systems.Shop;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Delivery
{
    /// <summary>
    /// Оплаченная доставка доехала до ларька: на точку спавна выходит курьер с коробкой.
    /// </summary>
    internal readonly struct DeliveryArrivedMessage : IMessage
    {
        public ShopDeliveryOrder Order { get; }

        public DeliveryArrivedMessage(ShopDeliveryOrder order)
        {
            Order = order;
        }
    }

    /// <summary>
    /// Игрок забрал коробку у курьера: коробка перешла в руки, курьер свободен.
    /// </summary>
    internal readonly struct DeliveryBoxTakenMessage : IMessage
    {
        public DeliveryBoxActor Box { get; }

        public DeliveryBoxTakenMessage(DeliveryBoxActor box)
        {
            Box = box;
        }
    }
}

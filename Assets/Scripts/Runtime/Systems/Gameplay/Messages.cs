using CHARK.GameManagement.Messaging;
using UABPetelnia.GGJ2025.Runtime.Settings;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Gameplay
{
    /// <summary>
    /// A shopper paid for the product they asked for.
    /// </summary>
    internal readonly struct SaleCompletedMessage : IMessage
    {
        public ItemData Item { get; }

        public int Cents { get; }

        public SaleCompletedMessage(ItemData item, int cents)
        {
            Item = item;
            Cents = cents;
        }
    }

    /// <summary>
    /// The shopper was handed the wrong product and took a swing at the shopkeeper.
    /// </summary>
    internal readonly struct SaleFailedMessage : IMessage
    {
    }

    /// <summary>
    /// The shopper wanted something the kiosk did not have out on the shelves and left empty
    /// handed. Not the shopkeeper's fault, so it is tracked on its own.
    /// </summary>
    internal readonly struct SaleRefusedMessage : IMessage
    {
    }
}

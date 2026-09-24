using System.Collections.Generic;
using UABPetelnia.GGJ2025.Runtime.Settings;

namespace UABPetelnia.GGJ2025.Runtime.Actors
{
    internal interface IPlayerActor
    {
        public int Health { get; set; }

        public int Cents { get; set; }

        public bool IsCentsGoalReached { get; }

        /// <summary><c>true</c>, когда в слотах рук есть хотя бы один товар.</summary>
        public bool HasCarriedItem { get; }

        /// <summary>Товары в слотах рук: их уже нельзя взять с полки, но можно отдать покупателю.</summary>
        public IReadOnlyList<ItemData> CarriedItems { get; }

        /// <summary><c>true</c>, когда в руках есть свободный слот.</summary>
        public bool HasFreeCarrySlot { get; }

        /// <summary>Положить товар (например, из коробки доставки) в свободный слот рук.</summary>
        public bool TryCarryItem(ItemData item);

        public void ShowPurchase(PurchaseRequest text);

        public void PlayGiveAnimation(ItemData item);

        public void StopGiveAnimation();

        public void HidePurchase();
    }
}

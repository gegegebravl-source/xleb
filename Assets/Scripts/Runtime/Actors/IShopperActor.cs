using UABPetelnia.GGJ2025.Runtime.Settings;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Actors
{
    internal interface IShopperActor
    {
        public bool IsContainsPurchases { get; }

        public bool IsBuying { get; }

        public bool IsPunching { get; }

        public bool IsMoving { get; }

        public string Name { get; }

        public ShopperData Data { get; }

        public void Destroy();

        public void Move(Vector3 position);

        public PurchaseRequest PopPurchaseRequest();

        public void PlayBuyAnimation();

        public void StopBuyAnimation();

        public void PlayPunchAnimation();

        public void StopPunchAnimation();

        public void PlayWalkAnimation();

        public void StopWalkAnimation();

        /// <summary>
        /// Show what the shopper wants on their request card.
        /// </summary>
        public void ShowRequest(PurchaseRequest purchase);

        public void HideRequest();

        public void PlaySpeech();

        public void StopSpeech();
    }
}

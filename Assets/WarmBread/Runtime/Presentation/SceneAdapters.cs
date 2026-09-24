using UnityEngine;

namespace WarmBread
{
    public interface ISceneWorldAdapter
    {
        void SetWeather(string weatherId);
        void SetTimeOfDay(float hour);
        void SetKioskLights(bool enabled);
    }

    public interface ICustomerPresentationAdapter
    {
        void MoveCustomer(string customerId, Vector3 target);
        void PlayCustomerReaction(string customerId, string reactionId);
    }

    public interface ITradePresentationAdapter
    {
        void ShowProduct(string productId, int quantity);
        void AnimateSale(string customerId);
    }
}

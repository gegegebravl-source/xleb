using System.Collections.Generic;
using CHARK.GameManagement;
using CHARK.GameManagement.Systems;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Systems.Scenes;
using UABPetelnia.GGJ2025.Runtime.Systems.Shoppers;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Delivery
{
    /// <summary>
    /// Слушает прибытие оплаченных доставок и выводит на улицу курьера с коробкой.
    /// Один заказ — один курьер и одна коробка со всеми позициями.
    /// </summary>
    internal sealed class CourierSystem : MonoSystem
    {
        [Tooltip("Префаб курьера с коробкой.")]
        [SerializeField]
        private CourierActor courierPrefab;

        private readonly List<CourierActor> couriers = new();

        private IShopperSystem shopperSystem;

        public override void OnInitialized()
        {
            SystemsUtility.TryGetSystem(out shopperSystem);

            GameManager.AddListener<DeliveryArrivedMessage>(OnDeliveryArrived);
            GameManager.AddListener<SceneUnloadEnteredMessage>(OnSceneUnloadEntered);
        }

        public override void OnDisposed()
        {
            GameManager.RemoveListener<DeliveryArrivedMessage>(OnDeliveryArrived);
            GameManager.RemoveListener<SceneUnloadEnteredMessage>(OnSceneUnloadEntered);
        }

        private void OnDeliveryArrived(DeliveryArrivedMessage message)
        {
            if (message.Order == null)
            {
                Debug.LogWarning("[Courier] Получена пустая доставка — курьер не создаётся.");
                return;
            }

            if (courierPrefab == false)
            {
                Debug.LogWarning("[Courier] Префаб курьера не назначен — доставка приехала в пустоту.");

                return;
            }

            var spawnPoint = shopperSystem != null ? shopperSystem.RandomSpawnPoint : Vector3.zero;
            var kioskPoint = shopperSystem != null ? shopperSystem.KioskPoint : Vector3.zero;

            var courier = Instantiate(courierPrefab, spawnPoint, Quaternion.identity);
            courier.Initialize(message.Order.Lines);
            courier.GoTo(kioskPoint);

            couriers.Add(courier);
        }

        private void OnSceneUnloadEntered(SceneUnloadEnteredMessage message)
        {
            couriers.Clear();
        }
    }
}

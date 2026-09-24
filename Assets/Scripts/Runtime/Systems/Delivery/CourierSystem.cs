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
        private readonly List<ShopDeliveryOrder> pendingOrders = new();

        private IShopperSystem shopperSystem;
        private ISceneSystem sceneSystem;

        public override void OnInitialized()
        {
            SystemsUtility.TryGetSystem(out shopperSystem);
            SystemsUtility.TryGetSystem(out sceneSystem);

            GameManager.AddListener<DeliveryArrivedMessage>(OnDeliveryArrived);
            GameManager.AddListener<SceneLoadExitedMessage>(OnSceneLoadExited);
            GameManager.AddListener<SceneUnloadEnteredMessage>(OnSceneUnloadEntered);
        }

        public override void OnDisposed()
        {
            GameManager.RemoveListener<DeliveryArrivedMessage>(OnDeliveryArrived);
            GameManager.RemoveListener<SceneLoadExitedMessage>(OnSceneLoadExited);
            GameManager.RemoveListener<SceneUnloadEnteredMessage>(OnSceneUnloadEntered);
        }

        private void OnDeliveryArrived(DeliveryArrivedMessage message)
        {
            if (message.Order == null)
            {
                Debug.LogWarning("[Courier] Получена пустая доставка — курьер не создаётся.");
                return;
            }

            if (IsGameplayCollectionLoaded() == false)
            {
                // The order has already reached the back room while the menu is open. Keep the
                // physical box pending instead of spawning it at world origin with no kiosk.
                if (pendingOrders.Contains(message.Order) == false)
                {
                    pendingOrders.Add(message.Order);
                }

                return;
            }

            if (SpawnCourier(message.Order) == false && pendingOrders.Contains(message.Order) == false)
            {
                pendingOrders.Add(message.Order);
            }
        }

        private void OnSceneUnloadEntered(SceneUnloadEnteredMessage message)
        {
            couriers.RemoveAll(courier => courier == false);
            couriers.Clear();
        }

        private void OnSceneLoadExited(SceneLoadExitedMessage message)
        {
            if (IsGameplayCollectionLoaded() == false || pendingOrders.Count == 0)
            {
                return;
            }

            for (var index = pendingOrders.Count - 1; index >= 0; index--)
            {
                if (SpawnCourier(pendingOrders[index]))
                {
                    pendingOrders.RemoveAt(index);
                }
            }
        }

        private bool SpawnCourier(ShopDeliveryOrder order)
        {
            if (order == null || courierPrefab == false)
            {
                if (courierPrefab == false)
                {
                    Debug.LogWarning("[Courier] Префаб курьера не назначен — доставка ждёт исправления сцены.");
                }

                return false;
            }

            var spawnPoint = shopperSystem != null ? shopperSystem.RandomSpawnPoint : Vector3.zero;
            var kioskPoint = shopperSystem != null ? shopperSystem.KioskPoint : Vector3.zero;

            var courier = Instantiate(courierPrefab, spawnPoint, Quaternion.identity);
            courier.Initialize(order.Lines);
            courier.GoTo(kioskPoint);

            couriers.Add(courier);
            return true;
        }

        private bool IsGameplayCollectionLoaded()
        {
            if (sceneSystem == null || sceneSystem.TryGetLoadedCollection(out var collection) == false)
            {
                return sceneSystem == null;
            }

            return sceneSystem.IsGameplayScene(collection);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using CHARK.GameManagement;
using CHARK.GameManagement.Systems;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UABPetelnia.GGJ2025.Runtime.Systems.Scenes;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Shoppers
{
    internal sealed class ShopperSystem : MonoSystem, IShopperSystem
    {
        [SerializeField]
        private GameplaySettings gameplaySettings;

        private List<ShopperData> availableShoppers = new();
        private readonly List<IDestinationActor> destinations = new();
        private readonly List<IShopperActor> spawnedShoppers = new();

        private IReadOnlyCollection<ItemData> wantedItems = Array.Empty<ItemData>();

        public bool IsShoppersAvailable
        {
            get
            {
                RemoveDeadActors();
                return availableShoppers.Count > 0;
            }
        }

        public bool IsAwaitingItem => wantedItems != null && wantedItems.Count > 0;

        public Vector3 RandomSpawnPoint
        {
            get
            {
                if (destinations.Count <= 0)
                {
                    return Vector3.zero;
                }

                RemoveDeadActors();
                var spawnPoints = destinations.OfType<ShopperSpawnPointActor>().Where(point => point).ToList();
                if (spawnPoints.Count <= 0)
                {
                    return Vector3.zero;
                }

                var spawnPoint = spawnPoints.GetRandom();

                return spawnPoint.Position;
            }
        }

        public Vector3 KioskPoint
        {
            get
            {
                RemoveDeadActors();
                var kioskDestination = destinations.FirstOrDefault(destination => destination is KioskPointActor && IsAlive(destination));
                if (kioskDestination == default)
                {
                    return Vector3.zero;
                }

                return kioskDestination.Position;
            }
        }

        public IEnumerable<ItemData> AvailableItems => gameplaySettings != false
            ? gameplaySettings.AvailableItems
            : Array.Empty<ItemData>();

        public void AwaitItem(PurchaseRequest request)
        {
            wantedItems = request == null ? Array.Empty<ItemData>() : request.WantedItems;
        }

        public void StopAwaitingItem()
        {
            wantedItems = Array.Empty<ItemData>();
        }

        public bool IsItemWanted(ItemData item)
        {
            return item && wantedItems != null && wantedItems.Contains(item);
        }

        public override void OnInitialized()
        {
            GameManager.AddListener<SceneLoadEnteredMessage>(OnSceneLoadEntered);
            GameManager.AddListener<SceneUnloadEnteredMessage>(OnSceneUnloadEntered);
        }

        public override void OnDisposed()
        {
            GameManager.RemoveListener<SceneLoadEnteredMessage>(OnSceneLoadEntered);
            GameManager.RemoveListener<SceneUnloadEnteredMessage>(OnSceneUnloadEntered);
        }

        public bool TryGetShopper(out IShopperActor shopper)
        {
            RemoveDeadActors();
            shopper = spawnedShoppers.FirstOrDefault();
            return shopper != default;
        }

        public bool TrySpawnRandomShopper(Vector3 position, out IShopperActor shopper)
        {
            if (availableShoppers.Count <= 0)
            {
                shopper = default;
                return false;
            }

            while (availableShoppers.Count > 0)
            {
                var randomShopperData = availableShoppers.GetRandom();
                availableShoppers.Remove(randomShopperData);

                if (randomShopperData == null || randomShopperData.ShopperPrefab == false)
                {
                    continue;
                }

                var shopperActor = Instantiate(randomShopperData.ShopperPrefab, position, Quaternion.identity);
                shopperActor.Initialize(randomShopperData);

                shopper = shopperActor;

                return true;
            }

            shopper = default;
            return false;
        }

        /// <summary>
        /// Делегирует в <see cref="TrySpawnRandomShopper"/>: если уникальные покупатели кончились,
        /// возвращает null вместо исключения из GetRandom на пустом списке.
        /// </summary>
        public IShopperActor SpawnRandomShopper(Vector3 position)
        {
            return TrySpawnRandomShopper(position, out var shopper) ? shopper : null;
        }

        public void AddShopper(IShopperActor shopper)
        {
            if (IsAlive(shopper) == false || spawnedShoppers.Contains(shopper))
            {
                return;
            }

            spawnedShoppers.Add(shopper);
        }

        public void RemoveShopper(IShopperActor shopper)
        {
            spawnedShoppers.Remove(shopper);
        }

        public void AddDestination(IDestinationActor destination)
        {
            if (IsAlive(destination) == false || destinations.Contains(destination))
            {
                return;
            }

            destinations.Add(destination);
        }

        public void RemoveAvailableShopper(IShopperActor shopper)
        {
            if (shopper != null)
            {
                availableShoppers.Remove(shopper.Data);
            }
        }

        public void RemoveDestination(IDestinationActor destination)
        {
            destinations.Remove(destination);
        }

        private void OnSceneUnloadEntered(SceneUnloadEnteredMessage message)
        {
            spawnedShoppers.Clear();
            destinations.Clear();
            StopAwaitingItem();
        }

        private void OnSceneLoadEntered(SceneLoadEnteredMessage message)
        {
            availableShoppers.Clear();
            availableShoppers = gameplaySettings != false && gameplaySettings.AvailableShoppers != null
                ? gameplaySettings.AvailableShoppers
                    .Where(data => data != null)
                    .Select(data => data.Copy())
                    .ToList()
                : new List<ShopperData>();

            StopAwaitingItem();
        }

        private void RemoveDeadActors()
        {
            spawnedShoppers.RemoveAll(shopper => IsAlive(shopper) == false);
            destinations.RemoveAll(destination => IsAlive(destination) == false);
        }

        private static bool IsAlive(object actor)
        {
            if (actor == null)
            {
                return false;
            }

            return actor is not Object unityObject || unityObject != false;
        }
    }
}

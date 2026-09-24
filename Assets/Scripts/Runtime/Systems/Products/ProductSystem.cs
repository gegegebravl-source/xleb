using System.Collections.Generic;
using CHARK.GameManagement;
using CHARK.GameManagement.Systems;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UABPetelnia.GGJ2025.Runtime.Systems.Scenes;
using UABPetelnia.GGJ2025.Runtime.Systems.Shop;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Products
{
    /// <summary>
    /// Fills every registered <see cref="ProductShelfPointActor"/> with a physical product, cycling
    /// through the whole assortment so the kiosk sells everything the shop has in stock. When a
    /// delivery arrives the shelves refill on their own.
    /// </summary>
    internal sealed class ProductSystem : MonoSystem, IProductSystem
    {
        [Header("Prefabs")]
        [SerializeField]
        private ProductActor productPrefab;

        [Header("Data")]
        [SerializeField]
        private GameplaySettings gameplaySettings;

        private readonly List<ProductShelfPointActor> shelfPoints = new();
        private readonly List<ItemData> items = new();

        /// <summary>Все живые товары сцены: и заспавненные системой, и расставленные вручную.</summary>
        private readonly HashSet<ProductActor> registeredProducts = new();

        private IShopSystem shopSystem;
        private int nextItemIndex;

        public bool IsShelfAvailable => shelfPoints.Count > 0;

        public override void OnInitialized()
        {
            SystemsUtility.TryGetSystem(out shopSystem);
            if (shopSystem != null)
            {
                shopSystem.Changed += OnShopChanged;
            }

            GameManager.AddListener<SceneLoadEnteredMessage>(OnSceneLoadEntered);
            GameManager.AddListener<SceneUnloadEnteredMessage>(OnSceneUnloadEntered);
        }

        public override void OnDisposed()
        {
            if (shopSystem != null)
            {
                shopSystem.Changed -= OnShopChanged;
            }

            GameManager.RemoveListener<SceneLoadEnteredMessage>(OnSceneLoadEntered);
            GameManager.RemoveListener<SceneUnloadEnteredMessage>(OnSceneUnloadEntered);
        }

        public void AddShelfPoint(ProductShelfPointActor shelfPoint)
        {
            if (shelfPoint == false || shelfPoints.Contains(shelfPoint))
            {
                return;
            }

            shelfPoints.Add(shelfPoint);

            // Empty shelves by default. The player must manually stock them from the delivery box.
            if (shelfPoints.Count % 5 == 0 || shelfPoints.Count == 1)
            {
                Debug.Log(
                    $"[Products] Полок: {shelfPoints.Count}, на них товаров: {CountSpawnedProducts()}."
                );
            }
        }

        private int CountSpawnedProducts()
        {
            var count = 0;

            foreach (var point in shelfPoints)
            {
                if (point && point.Product)
                {
                    count++;
                }
            }

            return count;
        }

        public void RemoveShelfPoint(ProductShelfPointActor shelfPoint)
        {
            if (shelfPoints.Remove(shelfPoint) == false)
            {
                return;
            }

            if (shelfPoint && shelfPoint.Product)
            {
                Destroy(shelfPoint.Product.gameObject);
            }
        }

        public ProductActor SpawnProduct(ItemData item, ProductShelfPointActor shelfPoint)
        {
            if (productPrefab == false || item == false)
            {
                return default;
            }

            var position = shelfPoint ? shelfPoint.Position : Vector3.zero;
            var rotation = shelfPoint ? shelfPoint.Rotation : Quaternion.identity;

            var product = Instantiate(productPrefab, position, rotation);
            product.Initialize(item, shelfPoint);

            if (shelfPoint)
            {
                shelfPoint.Product = product;
            }

            return product;
        }

        /// <summary>
        /// Создать товар вне полки: он попадает в руки игрока и на полку встанет только
        /// после того, как игрок отпустит его рядом со свободным слотом.
        /// </summary>
        public ProductActor SpawnLooseProduct(ItemData item, Vector3 position)
        {
            if (productPrefab == false || item == false)
            {
                return default;
            }

            var product = Instantiate(productPrefab, position, Quaternion.identity);
            product.Initialize(item, default);

            return product;
        }

        public float ResolveDisplayHeight(float displayHeight)
        {
            if (gameplaySettings == false)
            {
                return displayHeight;
            }

            return gameplaySettings.ResolveProductHeight(displayHeight);
        }

        public int CountOnShelf(ItemData item)
        {
            if (item == false)
            {
                return 0;
            }

            var count = 0;

            foreach (var product in registeredProducts)
            {
                if (product && product.ShelfPoint != false && product.Item == item)
                {
                    count++;
                }
            }

            return count;
        }

        public bool TryFindFreeShelfPoint(
            Vector3 position,
            Vector3 lookDirection,
            float radius,
            out ProductShelfPointActor shelfPoint
        )
        {
            shelfPoint = default;

            var bestScore = float.MinValue;

            foreach (var candidate in shelfPoints)
            {
                if (candidate == false || candidate.IsFree == false)
                {
                    continue;
                }

                var toSlot = candidate.Position - position;
                var distance = toSlot.magnitude;

                if (distance > radius)
                {
                    continue;
                }

                // Чем ближе слот и чем точнее он лежит по направлению взгляда — тем лучше.
                var alignment = lookDirection.sqrMagnitude > 0.001f && distance > 0.001f
                    ? Vector3.Dot(toSlot / distance, lookDirection.normalized)
                    : 0f;

                var score = alignment - distance / Mathf.Max(0.01f, radius);

                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                shelfPoint = candidate;
            }

            return shelfPoint != default;
        }

        public bool IsOnShelf(ItemData item)
        {
            if (item == false)
            {
                return false;
            }

            // Любой живой товар в сцене считается доступным: их может быть несколько
            // на одну полку, если игрок расставил товар вручную.
            foreach (var product in registeredProducts)
            {
                if (product && product.ShelfPoint != false && product.Item == item)
                {
                    return true;
                }
            }

            return false;
        }

        public void Register(ProductActor product)
        {
            if (product == false)
            {
                return;
            }

            registeredProducts.Add(product);
        }

        public void Unregister(ProductActor product)
        {
            if (product == false)
            {
                return;
            }

            registeredProducts.Remove(product);
        }

        public bool TryManualRestockShelf(ProductShelfPointActor shelfPoint)
        {
            if (shelfPoint == false || shelfPoint.Product)
            {
                return false;
            }

            var item = TakeNextAvailableItem();
            if (item == false)
            {
                return false;
            }

            if (shopSystem == null || shopSystem.TryTakeFromStock(item) == false)
            {
                return false;
            }

            var product = SpawnProduct(item, shelfPoint);
            if (product == false)
            {
                // A missing prefab must not look like a successful restock. Restore the unit that
                // was reserved above so a broken scene does not silently eat the delivery.
                if (shopSystem != null && shopSystem.TryGetProduct(item, out var stockProduct))
                {
                    stockProduct.Stock++;
                }

                return false;
            }

            return true;
        }

        public void Consume(ProductActor product)
        {
            if (product == false)
            {
                return;
            }

            // Проданный товар ушёл с полки покупателю: склад тут не при чём, его расходует
            // только выкладка из коробки.
            // Clear the shelf reference and detach before Destroy. Destroy is deferred by Unity;
            // leaving the product parented until the end of the frame lets the shelf discover it
            // again and appear occupied after the item was already sold or picked up.
            product.PlaceOn(default);
            Destroy(product.gameObject);

            // Manual restock only: after the sale the shelf stays empty until the player refills it.
        }

        /// <summary>
        /// Walk the assortment from where the last spawn stopped and return the first item the shop
        /// still has in stock, keeping the shelves evenly spread over the catalogue.
        /// </summary>
        private ItemData TakeNextAvailableItem()
        {
            EnsureItemsLoaded();

            if (items.Count <= 0)
            {
                return default;
            }

            // Without a catalogue there is no stock to consult, so the assortment is spread over
            // the shelves as is: an empty kiosk is worse than a full one.
            if (shopSystem == null || shopSystem.Products.Count == 0)
            {
                return items[nextItemIndex++ % items.Count];
            }

            for (var attempt = 0; attempt < items.Count; attempt++)
            {
                var item = items[nextItemIndex++ % items.Count];

                if (shopSystem.TryGetProduct(item, out var product) && product.IsInStock)
                {
                    return item;
                }
            }

            return default;
        }

        private void OnShopChanged()
        {
            // Manual restock mode: delivery updates the back-room stock, but the shelves stay empty
            // until the player explicitly places the delivery box onto the shelf and refills them.
        }

        /// <summary>
        /// Shelf slots register themselves while a scene is still loading, which can happen before
        /// the scene load message arrives. Loading the assortment on demand keeps the shop filled
        /// no matter the initialization order.
        /// </summary>
        private void EnsureItemsLoaded()
        {
            if (items.Count > 0 || gameplaySettings == false)
            {
                return;
            }

            if (gameplaySettings.AvailableItems == null)
            {
                return;
            }

            foreach (var item in gameplaySettings.AvailableItems)
            {
                if (item)
                {
                    items.Add(item);
                }
            }
        }

        private void OnSceneLoadEntered(SceneLoadEnteredMessage message)
        {
            items.Clear();
            EnsureItemsLoaded();
        }

        private void OnSceneUnloadEntered(SceneUnloadEnteredMessage message)
        {
            shelfPoints.Clear();
            items.Clear();
            registeredProducts.Clear();

            nextItemIndex = 0;
        }
    }
}

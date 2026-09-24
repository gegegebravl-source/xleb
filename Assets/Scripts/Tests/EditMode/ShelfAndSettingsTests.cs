using NUnit.Framework;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Tests
{
    public sealed class ShelfAndSettingsTests
    {
        [Test]
        public void ProductShelfPointDoesNotGetStuckWhenOneOfSeveralAuthoredProductsIsRemoved()
        {
            var shelfObject = new GameObject("ShelfPoint");
            var firstObject = new GameObject("ProductA");
            var secondObject = new GameObject("ProductB");
            firstObject.transform.SetParent(shelfObject.transform);
            secondObject.transform.SetParent(shelfObject.transform);
            firstObject.AddComponent<ProductActor>();
            secondObject.AddComponent<ProductActor>();
            var shelf = shelfObject.AddComponent<ProductShelfPointActor>();

            try
            {
                Assert.That(shelf.IsFree, Is.False);
                Object.DestroyImmediate(firstObject);
                Assert.That(shelf.IsFree, Is.False);
                Assert.That(shelf.Product, Is.EqualTo(secondObject.GetComponent<ProductActor>()));
            }
            finally
            {
                Object.DestroyImmediate(shelfObject);
            }
        }

        [Test]
        public void MovingProductOffShelfClearsSlotAndDetachesProduct()
        {
            var shelfObject = new GameObject("ShelfPoint");
            var productObject = new GameObject("Product");
            var item = ScriptableObject.CreateInstance<ItemData>();
            productObject.transform.SetParent(shelfObject.transform);
            var product = productObject.AddComponent<ProductActor>();
            var shelf = shelfObject.AddComponent<ProductShelfPointActor>();

            try
            {
                product.Initialize(item, shelf, snapToShelfPoint: false);
                shelf.Product = product;

                product.PlaceOn(default);

                Assert.That(shelf.IsFree, Is.True);
                Assert.That(product.ShelfPoint, Is.False);
                Assert.That(product.transform.parent, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(shelfObject);
                Object.DestroyImmediate(item);
            }
        }

        [Test]
        public void ProductHeightIsClampedAtBothEdges()
        {
            var settings = ScriptableObject.CreateInstance<GameplaySettings>();
            try
            {
                Assert.That(settings.ResolveProductHeight(0f), Is.EqualTo(settings.MinProductHeight));
                Assert.That(settings.ResolveProductHeight(100f), Is.EqualTo(settings.MaxProductHeight));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UABPetelnia.GGJ2025.Runtime.Systems.Shop;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Tests
{
    public sealed class DeliveryBoxTests
    {
        private GameObject boxObject;
        private ItemData first;
        private ItemData second;

        [SetUp]
        public void SetUp()
        {
            boxObject = new GameObject("TestDeliveryBox");
            first = ScriptableObject.CreateInstance<ItemData>();
            second = ScriptableObject.CreateInstance<ItemData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(boxObject);
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }

        [Test]
        public void FillExpandsQuantitiesAndIgnoresMalformedLines()
        {
            var box = boxObject.AddComponent<DeliveryBoxActor>();
            var valid = new ShopProduct(first, 10);
            var lines = new List<ShopDeliveryLine>
            {
                new ShopDeliveryLine(valid, 2),
                null,
                new ShopDeliveryLine(new ShopProduct(null, 10), 4),
                new ShopDeliveryLine(new ShopProduct(second, 10), -1),
            };

            box.Fill(lines);

            Assert.That(box.Count, Is.EqualTo(2));
            Assert.That(box.IsEmpty, Is.False);
            Assert.That(box.Contents[0], Is.EqualTo(first));
            Assert.That(box.Contents[1], Is.EqualTo(first));
        }

        [Test]
        public void RemoveAndCollectStacksRemainConsistentForMissingItems()
        {
            var box = boxObject.AddComponent<DeliveryBoxActor>();
            var product = new ShopProduct(first, 10);
            box.Fill(new[] { new ShopDeliveryLine(product, 3) });

            Assert.That(box.TryRemoveItem(second), Is.False);
            Assert.That(box.TryRemoveItem(first), Is.True);

            var items = new List<ItemData>();
            var counts = new List<int>();
            box.CollectStacks(items, counts);

            Assert.That(items, Has.Count.EqualTo(1));
            Assert.That(counts, Is.EqualTo(new[] { 2 }));
            Assert.That(box.TryRemoveItem(null), Is.False);
        }
    }
}

using NUnit.Framework;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Tests
{
    public sealed class PurchaseRequestTests
    {
        private ItemData first;
        private ItemData second;

        [SetUp]
        public void SetUp()
        {
            first = ScriptableObject.CreateInstance<ItemData>();
            second = ScriptableObject.CreateInstance<ItemData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }

        [Test]
        public void NullAndDuplicateItemsAreRemovedAtConstruction()
        {
            var request = new PurchaseRequest("хлеб", new[] { first, null, first, second }, null);

            Assert.That(request.IsEmpty, Is.False);
            Assert.That(request.WantedItems, Has.Count.EqualTo(2));
            Assert.That(request.PrimaryItem, Is.EqualTo(first));
            Assert.That(request.IsWanted(first), Is.True);
            Assert.That(request.IsWanted(null), Is.False);
        }

        [Test]
        public void KeepOnlySupportsShelfAvailabilityFilteringAndNullPredicateIsNoOp()
        {
            var request = new PurchaseRequest("товар", new[] { first, second }, null);
            request.KeepOnly(item => item == first);
            Assert.That(request.WantedItems, Has.Count.EqualTo(1));
            Assert.That(request.IsWanted(first), Is.True);
            Assert.That(request.IsWanted(second), Is.False);

            request.KeepOnly(null);
            Assert.That(request.WantedItems, Has.Count.EqualTo(1));
        }

        [Test]
        public void EmptyRequestHasNoPrimaryItem()
        {
            var request = new PurchaseRequest("", null, null);

            Assert.That(request.IsEmpty, Is.True);
            Assert.That(request.PrimaryItem, Is.Null);
        }
    }
}

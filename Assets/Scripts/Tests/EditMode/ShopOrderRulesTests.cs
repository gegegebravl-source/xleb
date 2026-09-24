using NUnit.Framework;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UABPetelnia.GGJ2025.Runtime.Systems.Shop;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Tests
{
    public sealed class ShopOrderRulesTests
    {
        private ItemData item;

        [SetUp]
        public void SetUp()
        {
            item = ScriptableObject.CreateInstance<ItemData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(item);
        }

        [Test]
        public void AddQuantityAcceptsPositiveValuesAndPreservesTheTotal()
        {
            Assert.That(ShopOrderRules.TryAddQuantity(2, 3, out var result, out var error), Is.True);
            Assert.That(result, Is.EqualTo(5));
            Assert.That(error, Is.Null);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void AddQuantityRejectsNonPositiveRequests(int requested)
        {
            Assert.That(ShopOrderRules.TryAddQuantity(2, requested, out _, out var error), Is.False);
            Assert.That(error, Is.EqualTo("bad_quantity"));
        }

        [Test]
        public void AddQuantityRejectsIntegerOverflow()
        {
            Assert.That(
                ShopOrderRules.TryAddQuantity(int.MaxValue, 1, out _, out var error),
                Is.False
            );
            Assert.That(error, Is.EqualTo("bad_quantity"));
        }

        [Test]
        public void RemoveQuantityClampsAtZeroWithoutGoingNegative()
        {
            Assert.That(ShopOrderRules.TryRemoveQuantity(2, 99, out var result, out _), Is.True);
            Assert.That(result, Is.Zero);
        }

        [Test]
        public void RemoveQuantityRejectsInvalidStateAndRequest()
        {
            Assert.That(ShopOrderRules.TryRemoveQuantity(0, 1, out _, out var emptyError), Is.False);
            Assert.That(emptyError, Is.EqualTo("bad_quantity"));
            Assert.That(ShopOrderRules.TryRemoveQuantity(2, 0, out _, out var requestError), Is.False);
            Assert.That(requestError, Is.EqualTo("bad_quantity"));
        }

        [Test]
        public void CalculateTotalUsesLongArithmeticBeforeReturningCents()
        {
            var product = new ShopProduct(item, 125);
            var lines = new[] { new ShopCartLine(product, 2), new ShopCartLine(product, 3) };

            Assert.That(ShopOrderRules.TryCalculateTotal(lines, out var total, out var error), Is.True);
            Assert.That(total, Is.EqualTo(625));
            Assert.That(error, Is.Null);
        }

        [Test]
        public void CalculateTotalRejectsNullEmptyAndOverflowLines()
        {
            Assert.That(ShopOrderRules.TryCalculateTotal(null, out _, out var nullError), Is.False);
            Assert.That(nullError, Is.EqualTo("empty_cart"));
            Assert.That(ShopOrderRules.TryCalculateTotal(new[] { new ShopCartLine(null, 1) }, out _, out var badError), Is.False);
            Assert.That(badError, Is.EqualTo("bad_cost"));

            var product = new ShopProduct(item, int.MaxValue);
            var lines = new[] { new ShopCartLine(product, 2) };
            Assert.That(ShopOrderRules.TryCalculateTotal(lines, out _, out var overflowError), Is.False);
            Assert.That(overflowError, Is.EqualTo("bad_cost"));
        }
    }
}

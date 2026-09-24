using NUnit.Framework;

namespace WarmBread.Tests
{
    public sealed class ShopAndStoryTests
    {
        [Test]
        public void StockCannotBecomeNegative()
        {
            var inventory = new ShopInventoryService(new GameEventBus());
            Assert.That(inventory.Register("bread", 2, 50, 90), Is.True);
            Assert.That(inventory.TryChangeStock("bread", -3), Is.False);
            Assert.That(inventory.TryGet("bread", out var stock), Is.True);
            Assert.That(stock.Quantity, Is.EqualTo(2));
        }

        [Test]
        public void StoryFlagIsIdempotent()
        {
            var flags = new StoryFlagService();
            Assert.That(flags.Add("first_regular"), Is.True);
            Assert.That(flags.Add("first_regular"), Is.False);
            Assert.That(flags.Snapshot(), Has.Length.EqualTo(1));
        }

        [Test]
        public void AchievementUnlocksOnlyOnce()
        {
            var achievements = new AchievementService();
            var unlocks = 0;
            achievements.Unlocked += _ => unlocks++;
            Assert.That(achievements.AddProgress("hundred_sales", 40, 100), Is.False);
            Assert.That(achievements.AddProgress("hundred_sales", 60, 100), Is.True);
            Assert.That(achievements.AddProgress("hundred_sales", 1, 100), Is.False);
            Assert.That(unlocks, Is.EqualTo(1));
        }
    }
}

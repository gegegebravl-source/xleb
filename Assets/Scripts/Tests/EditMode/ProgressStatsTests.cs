using NUnit.Framework;
using UABPetelnia.GGJ2025.Runtime.Systems.Progress;

namespace UABPetelnia.GGJ2025.Tests
{
    public sealed class ProgressStatsTests
    {
        [Test]
        public void RegisterSaleUpdatesCurrentAndBestCleanStreak()
        {
            var stats = new ProgressStats();
            stats.RegisterSale();
            stats.RegisterSale();
            stats.ResetStreak();
            stats.RegisterSale();

            Assert.That(stats.CleanStreak, Is.EqualTo(1));
            Assert.That(stats.BestCleanStreak, Is.EqualTo(2));
        }

        [Test]
        public void ResetStreakDoesNotEraseBestResult()
        {
            var stats = new ProgressStats { CleanStreak = 4, BestCleanStreak = 4 };
            stats.ResetStreak();

            Assert.That(stats.CleanStreak, Is.Zero);
            Assert.That(stats.BestCleanStreak, Is.EqualTo(4));
        }
    }
}

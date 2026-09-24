using NUnit.Framework;

namespace WarmBread.Tests
{
    public sealed class WorldClockTests
    {
        [Test]
        public void PauseFreezesTime()
        {
            var clock = new WorldClock();
            clock.SetPaused(true);
            clock.Tick(600);
            Assert.That(clock.Hour, Is.EqualTo(6));
        }

        [Test]
        public void CrossedHoursFireExactlyOnce()
        {
            var calls = 0;
            var clock = new WorldClock(1, 6, 59);
            clock.HourCrossed += _ => calls++;
            clock.SetTimeScale(60);
            clock.Tick(2);
            Assert.That(calls, Is.EqualTo(1));
        }
    }
}

using System;
using NUnit.Framework;

namespace WarmBread.Tests
{
    public sealed class EventBusTests
    {
        [Test]
        public void BrokenListenerDoesNotStopNextListener()
        {
            var errors = 0;
            var calls = 0;
            var bus = new GameEventBus(_ => errors++);
            bus.Subscribe<DayStarted>(_ => throw new InvalidOperationException());
            bus.Subscribe<DayStarted>(_ => calls++);
            bus.Publish(new DayStarted(1, 0));
            Assert.That(errors, Is.EqualTo(1));
            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void DisposedSubscriptionIsRemoved()
        {
            var calls = 0;
            var bus = new GameEventBus();
            var subscription = bus.Subscribe<DayStarted>(_ => calls++);
            subscription.Dispose();
            bus.Publish(new DayStarted(1, 0));
            Assert.That(calls, Is.Zero);
        }
    }
}

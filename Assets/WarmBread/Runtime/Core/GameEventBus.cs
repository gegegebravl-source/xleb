using System;
using System.Collections.Generic;

namespace WarmBread
{
    public sealed class GameEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> listeners = new Dictionary<Type, List<Delegate>>();
        private readonly Action<Exception> errorHandler;

        public GameEventBus(Action<Exception> errorHandler = null) { this.errorHandler = errorHandler; }

        public IDisposable Subscribe<T>(Action<T> listener) where T : IGameEvent
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            var type = typeof(T);
            if (!listeners.TryGetValue(type, out var bucket))
            {
                bucket = new List<Delegate>();
                listeners.Add(type, bucket);
            }
            bucket.Add(listener);
            return new EventSubscription(() => Unsubscribe(listener));
        }

        public void Unsubscribe<T>(Action<T> listener) where T : IGameEvent
        {
            if (listener == null) return;
            if (!listeners.TryGetValue(typeof(T), out var bucket)) return;
            bucket.Remove(listener);
            if (bucket.Count == 0) listeners.Remove(typeof(T));
        }

        public void Publish<T>(T gameEvent) where T : IGameEvent
        {
            if (!listeners.TryGetValue(typeof(T), out var bucket)) return;
            var copy = bucket.ToArray();
            for (var i = 0; i < copy.Length; i++)
            {
                try { ((Action<T>)copy[i]).Invoke(gameEvent); }
                catch (Exception exception) { errorHandler?.Invoke(exception); }
            }
        }

        public void Clear() { listeners.Clear(); }

        private sealed class EventSubscription : IDisposable
        {
            private Action dispose;
            public EventSubscription(Action dispose) { this.dispose = dispose; }
            public void Dispose() { var action = dispose; dispose = null; action?.Invoke(); }
        }
    }
}

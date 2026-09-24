using System;
using System.Collections.Generic;

namespace WarmBread
{
    public sealed class ObjectPool<T> where T : class
    {
        private readonly Stack<T> available = new Stack<T>();
        private readonly Func<T> factory;
        private readonly Action<T> onGet;
        private readonly Action<T> onRelease;
        private readonly int maximum;

        public ObjectPool(Func<T> factory, int initial = 0, int maximum = 128, Action<T> onGet = null, Action<T> onRelease = null)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.maximum = Math.Max(1, maximum);
            this.onGet = onGet;
            this.onRelease = onRelease;
            for (var i = 0; i < Math.Min(initial, this.maximum); i++) available.Push(factory());
        }

        public T Get()
        {
            var item = available.Count > 0 ? available.Pop() : factory();
            onGet?.Invoke(item);
            return item;
        }

        public void Release(T item)
        {
            if (item == null || available.Count >= maximum) return;
            onRelease?.Invoke(item);
            available.Push(item);
        }

        public int AvailableCount => available.Count;
    }
}

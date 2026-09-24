using System;
using System.Collections.Generic;

namespace WarmBread
{
    public sealed class RelationshipService
    {
        private readonly Dictionary<string, int> values = new Dictionary<string, int>(StringComparer.Ordinal);
        public event Action<string, int, int> Changed;
        public int Get(string id) { return id != null && values.TryGetValue(id, out var value) ? value : 0; }
        public int Change(string id, int delta)
        {
            if (string.IsNullOrWhiteSpace(id)) return 0;
            var oldValue = Get(id);
            var newValue = Math.Max(-100, Math.Min(100, oldValue + delta));
            values[id] = newValue;
            if (oldValue != newValue) Changed?.Invoke(id, oldValue, newValue);
            return newValue;
        }
    }
}

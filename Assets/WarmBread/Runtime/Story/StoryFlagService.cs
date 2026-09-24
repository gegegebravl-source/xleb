using System;
using System.Collections.Generic;

namespace WarmBread
{
    public sealed class StoryFlagService
    {
        private readonly HashSet<string> flags = new HashSet<string>(StringComparer.Ordinal);
        public event Action<string> FlagAdded;
        public bool Has(string flag) { return !string.IsNullOrWhiteSpace(flag) && flags.Contains(flag); }
        public bool Add(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag) || !flags.Add(flag)) return false;
            FlagAdded?.Invoke(flag);
            return true;
        }
        public string[] Snapshot()
        {
            var result = new string[flags.Count];
            flags.CopyTo(result);
            Array.Sort(result, StringComparer.Ordinal);
            return result;
        }
    }
}

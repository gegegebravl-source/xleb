using System;
using System.Collections.Generic;

namespace WarmBread
{
    public sealed class AchievementService
    {
        private readonly Dictionary<string, int> progress = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<string> unlocked = new HashSet<string>(StringComparer.Ordinal);
        public event Action<string> Unlocked;
        public bool IsUnlocked(string id) { return id != null && unlocked.Contains(id); }
        public int GetProgress(string id) { return id != null && progress.TryGetValue(id, out var value) ? value : 0; }
        public bool AddProgress(string id, int amount, int target)
        {
            if (string.IsNullOrWhiteSpace(id) || amount <= 0 || target <= 0 || unlocked.Contains(id)) return false;
            var next = Math.Min(target, GetProgress(id) + amount);
            progress[id] = next;
            if (next < target || !unlocked.Add(id)) return false;
            Unlocked?.Invoke(id);
            return true;
        }
    }
}

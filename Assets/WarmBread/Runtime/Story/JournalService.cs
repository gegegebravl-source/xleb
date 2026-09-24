using System;
using System.Collections.Generic;

namespace WarmBread
{
    [Serializable]
    public sealed class JournalEntry
    {
        public string Id;
        public string LocalizationKey;
        public int Day;
        public string SourceId;
    }

    public sealed class JournalService
    {
        private readonly List<JournalEntry> entries = new List<JournalEntry>();
        private readonly HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        public IReadOnlyList<JournalEntry> Entries => entries;
        public event Action<JournalEntry> EntryAdded;
        public bool Add(string id, string localizationKey, int day, string sourceId)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(localizationKey) || !ids.Add(id)) return false;
            var entry = new JournalEntry { Id = id, LocalizationKey = localizationKey, Day = Math.Max(1, day), SourceId = sourceId ?? string.Empty };
            entries.Add(entry);
            EntryAdded?.Invoke(entry);
            return true;
        }
    }
}

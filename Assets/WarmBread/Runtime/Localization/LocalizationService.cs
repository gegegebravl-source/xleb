using System;
using System.Collections.Generic;

namespace WarmBread
{
    public sealed class LocalizationService
    {
        private readonly Dictionary<string, Dictionary<string, string>> tables = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        public string Language { get; private set; } = "ru";

        public void Register(string language, IReadOnlyDictionary<string, string> entries)
        {
            if (string.IsNullOrWhiteSpace(language) || entries == null) return;
            if (!tables.TryGetValue(language, out var table)) { table = new Dictionary<string, string>(StringComparer.Ordinal); tables.Add(language, table); }
            foreach (var pair in entries) table[pair.Key] = pair.Value;
        }

        public void SetLanguage(string language) { Language = tables.ContainsKey(language) ? language : "ru"; }

        public string Get(string key, params object[] formatArguments)
        {
            var value = Find(Language, key) ?? Find("ru", key) ?? "[" + key + "]";
            if (formatArguments == null || formatArguments.Length == 0) return value;
            try { return string.Format(value, formatArguments); }
            catch (FormatException) { return value; }
        }

        private string Find(string language, string key)
        {
            return tables.TryGetValue(language, out var table) && table.TryGetValue(key, out var value) ? value : null;
        }
    }
}

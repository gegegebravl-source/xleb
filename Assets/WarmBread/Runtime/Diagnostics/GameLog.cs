using System;
using System.Collections.Generic;
using System.Text;

namespace WarmBread
{
    public enum GameLogLevel { Debug, Info, Warning, Error }
    public enum GameLogCategory { Core, Economy, AI, Save, UI, Audio }

    public sealed class GameLog
    {
        private readonly Queue<LogEntry> entries;
        private readonly int capacity;

        public GameLog(int capacity = 256)
        {
            this.capacity = Math.Max(16, capacity);
            entries = new Queue<LogEntry>(this.capacity);
        }

        public void Write(GameLogLevel level, GameLogCategory category, string message)
        {
            if (entries.Count >= capacity) entries.Dequeue();
            entries.Enqueue(new LogEntry(DateTime.UtcNow, level, category, Sanitize(message)));
        }

        public string Export()
        {
            var builder = new StringBuilder();
            foreach (var entry in entries) builder.Append(entry.TimeUtc.ToString("O")).Append(' ').Append(entry.Level).Append(' ').Append(entry.Category).Append(": ").AppendLine(entry.Message);
            return builder.ToString();
        }

        private static string Sanitize(string message)
        {
            if (string.IsNullOrEmpty(message)) return string.Empty;
            return message.Replace(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "<user>");
        }

        private readonly struct LogEntry
        {
            public LogEntry(DateTime timeUtc, GameLogLevel level, GameLogCategory category, string message)
            { TimeUtc = timeUtc; Level = level; Category = category; Message = message; }
            public DateTime TimeUtc { get; }
            public GameLogLevel Level { get; }
            public GameLogCategory Category { get; }
            public string Message { get; }
        }
    }
}

using System;
using System.Collections.Generic;

namespace WarmBread
{
    public sealed class DialogueService
    {
        private readonly List<QueuedDialogue> queue = new List<QueuedDialogue>();
        private readonly Dictionary<string, double> cooldownUntil = new Dictionary<string, double>(StringComparer.Ordinal);
        private readonly GameEventBus events;
        public DialogueService(GameEventBus events) { this.events = events ?? throw new ArgumentNullException(nameof(events)); }

        public bool Enqueue(string id, string speakerId, string text, float duration, int priority, double now, double cooldown)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(text) || duration <= 0f) return false;
            if (cooldownUntil.TryGetValue(id, out var blockedUntil) && blockedUntil > now) return false;
            cooldownUntil[id] = now + Math.Max(0d, cooldown);
            queue.Add(new QueuedDialogue { Id = id, SpeakerId = speakerId ?? string.Empty, Text = text, Duration = duration, Priority = priority });
            queue.Sort((left, right) => right.Priority.CompareTo(left.Priority));
            return true;
        }

        public bool PlayNext()
        {
            if (queue.Count == 0) return false;
            var next = queue[0];
            queue.RemoveAt(0);
            events.Publish(new DialogueRequested(next.SpeakerId, next.Text, next.Duration));
            return true;
        }

        private sealed class QueuedDialogue
        {
            public string Id;
            public string SpeakerId;
            public string Text;
            public float Duration;
            public int Priority;
        }
    }
}

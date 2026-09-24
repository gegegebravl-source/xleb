using CHARK.GameManagement.Messaging;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Progress
{
    /// <summary>
    /// Raised once for every achievement that was just unlocked, so anything on screen can
    /// celebrate: the journal in the menu refreshes and the in-game toast pops up.
    /// </summary>
    internal readonly struct AchievementUnlockedMessage : IMessage
    {
        public string Title { get; }

        public string Description { get; }

        public AchievementUnlockedMessage(string title, string description)
        {
            Title = title;
            Description = description;
        }
    }
}

using System;
using System.Collections.Generic;
using CHARK.GameManagement.Systems;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Progress
{
    /// <summary>
    /// Everything the journal shows: how the shift is going.
    /// </summary>
    [Serializable]
    internal sealed class ProgressStats
    {
        [SerializeField]
        public int UnitsSold;

        [SerializeField]
        public int ShoppersServed;

        [SerializeField]
        public int CentsEarned;

        [SerializeField]
        public int Mistakes;

        [SerializeField]
        public int Refusals;

        [SerializeField]
        public int Orders;

        [SerializeField]
        public int TamagotchiPlays;

        [SerializeField]
        public int BreadSold;

        [SerializeField]
        public int CleanStreak;

        [SerializeField]
        public int BestCleanStreak;

        public void ResetStreak()
        {
            CleanStreak = 0;
        }

        public void RegisterSale()
        {
            CleanStreak = CleanStreak >= int.MaxValue ? int.MaxValue : Mathf.Max(0, CleanStreak) + 1;
            BestCleanStreak = Mathf.Max(Mathf.Max(0, BestCleanStreak), CleanStreak);
        }
    }

    /// <summary>
    /// One achievement plus whether the player already has it.
    /// </summary>
    internal sealed class AchievementStatus
    {
        public AchievementStatus(string id, string title, string description)
        {
            Id = id;
            Title = title;
            Description = description;
        }

        public string Id { get; }

        public string Title { get; }

        public string Description { get; }

        public bool IsUnlocked { get; internal set; }
    }

    /// <summary>
    /// Counts what happens during the shift, keeps the achievement list and stores both between
    /// sessions, so the journal is worth opening.
    /// </summary>
    internal interface IProgressSystem : ISystem
    {
        ProgressStats Stats { get; }

        IReadOnlyList<AchievementStatus> Achievements { get; }

        int UnlockedCount { get; }

        event Action Changed;

        /// <summary>Called when a delivery is bought in from the counter PC.</summary>
        void NotifyOrderPlaced();

        /// <summary>Called when the shopkeeper plays with the tamagotchi toy.</summary>
        void NotifyTamagotchiPlayed();

        /// <summary>Start the shift from zero, keeping the achievements.</summary>
        void ResetShift();
    }
}

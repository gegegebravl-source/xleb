using System;
using System.Collections.Generic;
using CHARK.GameManagement;
using CHARK.GameManagement.Systems;
using UABPetelnia.GGJ2025.Runtime.Systems.Gameplay;
using UABPetelnia.GGJ2025.Runtime.Systems.Scenes;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Progress
{
    /// <summary>
    /// Watches the shop loop and keeps the journal: units sold, money earned, mistakes and the
    /// achievements that unlock along the way.
    /// </summary>
    /// <remarks>
    /// The counters live in <see cref="ProgressStats"/>, the achievement list is defined in code so
    /// it stays readable, and both are stored in <see cref="PlayerPrefs"/> as one JSON blob.
    /// </remarks>
    internal sealed class ProgressSystem : MonoSystem, IProgressSystem
    {
        private const string SaveKey = "WarmBread.Progress";

        private const string BreadIdPart = "hleb";

        private readonly ProgressStats stats = new();
        private readonly List<AchievementStatus> achievements = new();
        private readonly HashSet<string> unlockedIds = new();

        public ProgressStats Stats => stats;

        public IReadOnlyList<AchievementStatus> Achievements => achievements;

        public int UnlockedCount
        {
            get
            {
                var count = 0;

                foreach (var achievement in achievements)
                {
                    if (achievement.IsUnlocked)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public event Action Changed;

        public override void OnInitialized()
        {
            CreateAchievements();
            Load();

            GameManager.AddListener<SaleCompletedMessage>(OnSaleCompleted);
            GameManager.AddListener<SaleFailedMessage>(OnSaleFailed);
            GameManager.AddListener<SaleRefusedMessage>(OnSaleRefused);
            GameManager.AddListener<SceneUnloadEnteredMessage>(OnSceneUnloadEntered);
        }

        public override void OnDisposed()
        {
            GameManager.RemoveListener<SaleCompletedMessage>(OnSaleCompleted);
            GameManager.RemoveListener<SaleFailedMessage>(OnSaleFailed);
            GameManager.RemoveListener<SaleRefusedMessage>(OnSaleRefused);
            GameManager.RemoveListener<SceneUnloadEnteredMessage>(OnSceneUnloadEntered);
        }

        public void NotifyOrderPlaced()
        {
            stats.Orders++;

            Evaluate();
        }

        public void NotifyTamagotchiPlayed()
        {
            stats.TamagotchiPlays++;

            Evaluate();
        }

        /// <summary>
        /// A new shift starts empty, but the achievements earned so far stay.
        /// </summary>
        public void ResetShift()
        {
            stats.UnitsSold = 0;
            stats.ShoppersServed = 0;
            stats.CentsEarned = 0;
            stats.Mistakes = 0;
            stats.Refusals = 0;
            stats.Orders = 0;
            stats.BreadSold = 0;
            stats.CleanStreak = 0;
            stats.BestCleanStreak = 0;

            Evaluate();
        }

        private void OnSaleCompleted(SaleCompletedMessage message)
        {
            stats.UnitsSold++;
            stats.ShoppersServed++;
            stats.CentsEarned += Mathf.Max(0, message.Cents);
            stats.RegisterSale();

            var id = message.Item ? message.Item.Id : string.Empty;
            if (id.ToLowerInvariant().Contains(BreadIdPart))
            {
                stats.BreadSold++;
            }

            Evaluate();
        }

        private void OnSaleFailed(SaleFailedMessage message)
        {
            stats.Mistakes++;
            stats.ResetStreak();

            Evaluate();
        }

        private void OnSaleRefused(SaleRefusedMessage message)
        {
            stats.Refusals++;

            Evaluate();
        }

        private void OnSceneUnloadEntered(SceneUnloadEnteredMessage message)
        {
            // The gameplay scene is gone, so the shopkeeper left the stall: keep the totals but save
            // whatever the shift has produced so far.
            Save();
        }

        /// <summary>
        /// Check every achievement against the counters, unlock what is earned and save once.
        /// </summary>
        private void Evaluate()
        {
            foreach (var achievement in achievements)
            {
                if (achievement.IsUnlocked || CanUnlock(achievement.Id, stats) == false)
                {
                    continue;
                }

                achievement.IsUnlocked = true;
                unlockedIds.Add(achievement.Id);

                Debug.Log($"[Progress] Достижение получено: {achievement.Title} — {achievement.Description}");

                // One message per achievement, so the toast can show what was earned.
                GameManager.Publish(
                    new AchievementUnlockedMessage(achievement.Title, achievement.Description)
                );
            }

            Save();
            Changed?.Invoke();
        }

        private void CreateAchievements()
        {
            achievements.Clear();

            Add("first-sale", "Первая продажа", "Продай первый товар покупателю");
            Add("ten-shoppers", "Десять покупателей", "Обслужи десять человек за одну смену");
            Add("warm-bread", "Тёплый хлеб", "Продай пять батонов");
            Add("till-fifty", "Полтинник", "Заработай 50 руб. за смену");
            Add("no-mistakes", "Без единой ошибки", "Пять удачных продаж подряд");
            Add("restock", "Завхоз", "Закажи товар в ПК на прилавке");
            Add("tamagotchi-alive", "Тамагочи жив", "Поиграй с тамагочи в ларьке");
            Add("patient", "Никого не прогнал", "Двадцать продаж, ни одного промаха");
        }

        private void Add(string id, string title, string description)
        {
            achievements.Add(new AchievementStatus(id, title, description)
            {
                IsUnlocked = unlockedIds.Contains(id),
            });
        }

        private static bool CanUnlock(string id, ProgressStats progress)
        {
            return id switch
            {
                "first-sale" => progress.ShoppersServed >= 1,
                "ten-shoppers" => progress.ShoppersServed >= 10,
                "warm-bread" => progress.BreadSold >= 5,
                "till-fifty" => progress.CentsEarned >= 5000,
                "no-mistakes" => progress.BestCleanStreak >= 5,
                "restock" => progress.Orders >= 1,
                "tamagotchi-alive" => progress.TamagotchiPlays >= 1,
                "patient" => progress.ShoppersServed >= 20 && progress.Mistakes == 0,
                _ => false,
            };
        }

        private void Save()
        {
            var save = new ProgressSave
            {
                Stats = stats,
                UnlockedIds = new List<string>(unlockedIds),
            };

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save));
            PlayerPrefs.Save();
        }

        private void Load()
        {
            if (PlayerPrefs.HasKey(SaveKey) == false)
            {
                return;
            }

            var json = PlayerPrefs.GetString(SaveKey);
            ProgressSave save;

            try
            {
                save = JsonUtility.FromJson<ProgressSave>(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Progress] Не удалось загрузить сохранение: {exception.Message}");
                return;
            }

            if (save == null)
            {
                return;
            }

            unlockedIds.Clear();

            if (save.UnlockedIds != null)
            {
                foreach (var id in save.UnlockedIds)
                {
                    unlockedIds.Add(id);
                }
            }

            if (save.Stats == null)
            {
                return;
            }

            var loaded = save.Stats;

            stats.UnitsSold = loaded.UnitsSold;
            stats.ShoppersServed = loaded.ShoppersServed;
            stats.CentsEarned = loaded.CentsEarned;
            stats.Mistakes = loaded.Mistakes;
            stats.Refusals = loaded.Refusals;
            stats.Orders = loaded.Orders;
            stats.TamagotchiPlays = loaded.TamagotchiPlays;
            stats.BreadSold = loaded.BreadSold;
            stats.CleanStreak = loaded.CleanStreak;
            stats.BestCleanStreak = loaded.BestCleanStreak;
        }

        [Serializable]
        private sealed class ProgressSave
        {
            public ProgressStats Stats;

            public List<string> UnlockedIds;
        }
    }
}

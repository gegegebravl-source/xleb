using System;
using CHARK.GameManagement;
using CHARK.SimpleUI;
using CHARK.GameManagement.Systems;
using UABPetelnia.GGJ2025.Runtime.Systems.Progress;
using UABPetelnia.GGJ2025.Runtime.Systems.Scenes;
using UABPetelnia.GGJ2025.Runtime.UI.Views;
using UABPetelnia.GGJ2025.Runtime.Utilities;

namespace UABPetelnia.GGJ2025.Runtime.UI.Controllers
{
    internal sealed class GameOverViewController : ViewController<GameOverView>
    {
        private const string ReportFormat =
            "Обслужено покупателей: {0}\n"
            + "Батонов продано: {1}\n"
            + "Промахов: {2}   ·   Ушли ни с чем: {3}\n"
            + "Лучшая серия: {4}\n"
            + "Достижений: {5} из {6}";

        private ISceneSystem sceneSystem;
        private IProgressSystem progressSystem;

        protected override void Awake()
        {
            base.Awake();

            TryGetSystem(out sceneSystem);
        }

        protected override void Start()
        {
            base.Start();

            TryGetSystem(out progressSystem);
            RefreshReport();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            View.OnRestartGameClicked += OnViewRestartGameClicked;
            View.OnExitGameClicked += OnViewExitGameClicked;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            View.OnRestartGameClicked -= OnViewRestartGameClicked;
            View.OnExitGameClicked -= OnViewExitGameClicked;
        }

        /// <summary>
        /// Fill the report with whatever the shift produced. Without the progress system the numbers
        /// are simply left out instead of showing zeroes.
        /// </summary>
        private void RefreshReport()
        {
            var stats = progressSystem?.Stats;

            if (stats == null)
            {
                View.SetStats(string.Empty, string.Empty);

                return;
            }

            View.SetStats(
                string.Format(
                    ReportFormat,
                    stats.ShoppersServed,
                    stats.BreadSold,
                    stats.Mistakes,
                    stats.Refusals,
                    stats.BestCleanStreak,
                    progressSystem.UnlockedCount,
                    progressSystem.Achievements.Count
                ),
                $"Заработано: {stats.CentsEarned / 100f:0.00} руб."
            );
        }

        private void OnViewRestartGameClicked()
        {
            progressSystem?.ResetShift();
            sceneSystem?.LoadGameplayScene();
        }

        private void OnViewExitGameClicked()
        {
            sceneSystem?.LoadMenuScene();
        }

        private static void TryGetSystem<TSystem>(out TSystem system) where TSystem : ISystem
        {
            try
            {
                SystemsUtility.TryGetSystem(out system);
            }
            catch (Exception)
            {
                system = default;
            }
        }
    }
}

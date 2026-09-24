using System;
using CHARK.GameManagement;
using CHARK.GameManagement.Systems;
using CHARK.SimpleUI;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UABPetelnia.GGJ2025.Runtime.Systems.Audio;
using UABPetelnia.GGJ2025.Runtime.Systems.Cursors;
using UABPetelnia.GGJ2025.Runtime.Systems.Input;
using UABPetelnia.GGJ2025.Runtime.Systems.Progress;
using UABPetelnia.GGJ2025.Runtime.Systems.Saves;
using UABPetelnia.GGJ2025.Runtime.Systems.Scenes;
using UABPetelnia.GGJ2025.Runtime.UI.Views;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.UI.Controllers
{
    internal sealed class MainMenuViewController : ViewController<MainMenuView>
    {
        private const string SlotLabelFormat = "Сохранение {0}";
        private const string EmptySlotLabelFormat = "{0}\n<size=70%>Пусто</size>";
        private const string OccupiedSlotLabelFormat = "{0}\n<size=70%>{1} LT · Здоровье {2}</size>";

        private ICursorSystem cursorSystem;
        private ISceneSystem sceneSystem;
        private IInputSystem inputSystem;
        private IAudioSystem audioSystem;
        private ISaveSystem saveSystem;
        private IProgressSystem progressSystem;

        private bool isWarningLogged;

        protected override void Awake()
        {
            base.Awake();

            EnsureSystems();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            EnsureSystems();

            View.OnContinueClicked += OnViewContinueClicked;
            View.OnNewGameClicked += OnViewNewGameClicked;
            View.OnSettingsClicked += OnViewSettingsClicked;
            View.OnJournalClicked += OnViewJournalClicked;
            View.OnAchievementsClicked += OnViewAchievementsClicked;
            View.OnExitClicked += OnViewExitClicked;
            View.OnBackClicked += OnViewBackClicked;
            View.OnSaveSlotClicked += OnViewSaveSlotClicked;
            View.OnLookSensitivityChanged += OnViewLookSensitivityChanged;
            View.OnMasterVolumeChanged += OnViewMasterVolumeChanged;

            cursorSystem?.UnLockCursor();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            View.OnContinueClicked -= OnViewContinueClicked;
            View.OnNewGameClicked -= OnViewNewGameClicked;
            View.OnSettingsClicked -= OnViewSettingsClicked;
            View.OnJournalClicked -= OnViewJournalClicked;
            View.OnAchievementsClicked -= OnViewAchievementsClicked;
            View.OnExitClicked -= OnViewExitClicked;
            View.OnBackClicked -= OnViewBackClicked;
            View.OnSaveSlotClicked -= OnViewSaveSlotClicked;
            View.OnLookSensitivityChanged -= OnViewLookSensitivityChanged;
            View.OnMasterVolumeChanged -= OnViewMasterVolumeChanged;
        }

        protected override void Start()
        {
            base.Start();

            EnsureSystems();
            LogMissingSystemsWarning();

            InitializeLookSensitivityData();
            InitializeMasterVolumeData();

            View.ShowPanel(MainMenuPanel.Buttons);
            RefreshSaveSlots();
        }

        private void OnViewContinueClicked()
        {
            RefreshSaveSlots();
            View.ShowPanel(MainMenuPanel.SaveSlots);
        }

        private void OnViewNewGameClicked()
        {
            // A new game means a new shift: the journal starts from zero, the achievements stay.
            // "Continue" keeps whatever the previous shift produced, which is what the menu shows.
            progressSystem?.ResetShift();

            saveSystem?.StartNewGame();

            sceneSystem?.LoadGameplayScene();
        }

        private void OnViewSettingsClicked()
        {
            // Полный экран настроек живёт на менеджере игры: он же открывается из паузы.
            var settingsPanel = SettingsViewController.Instance;

            if (settingsPanel != null)
            {
                settingsPanel.Toggle();

                return;
            }

            View.ShowPanel(MainMenuPanel.Settings);
        }

        private void OnViewJournalClicked()
        {
            RefreshJournal();
            View.ShowPanel(MainMenuPanel.Journal);
        }

        private void OnViewAchievementsClicked()
        {
            RefreshAchievements();
            View.ShowPanel(MainMenuPanel.Achievements);
        }

        /// <summary>
        /// The shift journal: what the shopkeeper did so far, straight from the progress system.
        /// </summary>
        private void RefreshJournal()
        {
            var stats = progressSystem?.Stats;

            if (stats == null)
            {
                for (var row = 0; row < MainMenuView.JournalRowCount; row++)
                {
                    View.SetJournalRow(row, row == 0 ? "Журнал недоступен" : string.Empty);
                }

                return;
            }

            View.SetJournalRow(0, $"Продано товаров: {stats.UnitsSold}");
            View.SetJournalRow(1, $"Покупателей обслужено: {stats.ShoppersServed}");
            View.SetJournalRow(2, $"Заработано: {stats.CentsEarned / 100f:0.00} руб.");
            View.SetJournalRow(3, $"Батонов продано: {stats.BreadSold}");
            View.SetJournalRow(4, $"Промахов: {stats.Mistakes}");
            View.SetJournalRow(5, $"Ушли ни с чем: {stats.Refusals}");
            View.SetJournalRow(6, $"Заказов товара: {stats.Orders}");
            View.SetJournalRow(7, $"Лучшая серия без промахов: {stats.BestCleanStreak}");
        }

        private void RefreshAchievements()
        {
            var achievements = progressSystem?.Achievements;

            for (var row = 0; row < MainMenuView.AchievementRowCount; row++)
            {
                if (achievements == null || row >= achievements.Count)
                {
                    View.SetAchievementRow(row, string.Empty, string.Empty, isUnlocked: false);

                    continue;
                }

                var achievement = achievements[row];

                View.SetAchievementRow(
                    row,
                    achievement.Title,
                    achievement.Description,
                    achievement.IsUnlocked
                );
            }
        }

        private void OnViewBackClicked()
        {
            View.ShowPanel(MainMenuPanel.Buttons);
        }

        private void OnViewSaveSlotClicked(int slot)
        {
            if (saveSystem == null || saveSystem.ContinueFromSlot(slot) == false)
            {
                RefreshSaveSlots();
                return;
            }

            sceneSystem?.LoadGameplayScene();
        }

        private void OnViewExitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            UnityEngine.Application.Quit();
#endif
        }

        private void OnViewLookSensitivityChanged(float value)
        {
            if (inputSystem == null)
            {
                return;
            }

            inputSystem.LookSensitivity = value;
        }

        private void OnViewMasterVolumeChanged(float value)
        {
            if (audioSystem == null)
            {
                return;
            }

            audioSystem.SetVolume(VolumeType.Master, value);
        }

        private void RefreshSaveSlots()
        {
            for (var slot = 0; slot < MainMenuView.SaveSlotCount; slot++)
            {
                var title = string.Format(SlotLabelFormat, slot + 1);

                if (saveSystem == null || saveSystem.TryGetSlotData(slot, out var data) == false)
                {
                    View.SetSaveSlotData(
                        slot,
                        string.Format(EmptySlotLabelFormat, title),
                        isInteractable: false
                    );

                    continue;
                }

                var money = (data.Cents / 100m).ToString("0.00");

                View.SetSaveSlotData(
                    slot,
                    string.Format(OccupiedSlotLabelFormat, title, money, data.Health),
                    isInteractable: true
                );
            }
        }

        private void InitializeLookSensitivityData()
        {
            if (inputSystem == null)
            {
                return;
            }

            View.SetLookSensitivityData(
                GeneralSettings.MinLookSensitivity,
                GeneralSettings.MaxLookSensitivity,
                inputSystem.LookSensitivity,
                isNotifyListeners: false
            );
        }

        private void InitializeMasterVolumeData()
        {
            if (audioSystem == null)
            {
                return;
            }

            View.SetMasterVolumeData(
                GeneralSettings.MinVolume,
                GeneralSettings.MaxVolume,
                audioSystem.GetVolume(VolumeType.Master),
                isNotifyListeners: false
            );
        }

        /// <summary>
        /// Without a <see cref="CHARK.GameManagement.GameManager"/> none of the buttons can do
        /// anything, so make it loud and clear why the menu is not working.
        /// </summary>
        private void LogMissingSystemsWarning()
        {
            if (isWarningLogged || sceneSystem != null)
            {
                return;
            }

            isWarningLogged = true;

            Debug.LogError(
                "Main menu game systems are missing, buttons will not work."
                + " Start the game from Scene_Init (the first scene in Build Settings)"
                + " or enable automatic GameManager instantiation in GameManagerSettingsProfile.",
                this
            );
        }

        private void EnsureSystems()
        {
            TryGetSystem(out cursorSystem);
            TryGetSystem(out sceneSystem);
            TryGetSystem(out inputSystem);
            TryGetSystem(out audioSystem);
            TryGetSystem(out saveSystem);
            TryGetSystem(out progressSystem);
        }

        private static bool TryGetSystem<TSystem>(out TSystem system) where TSystem : ISystem
        {
            try
            {
                return GameManager.TryGetSystem(out system);
            }
            catch (Exception)
            {
                system = default;
                return false;
            }
        }
    }
}

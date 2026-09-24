using CHARK.GameManagement;
using CHARK.SimpleUI;
using UABPetelnia.GGJ2025.Runtime.Components.Input;
using UABPetelnia.GGJ2025.Runtime.Systems.Cursors;
using UABPetelnia.GGJ2025.Runtime.Systems.Pausing;
using UABPetelnia.GGJ2025.Runtime.Systems.Scenes;
using UABPetelnia.GGJ2025.Runtime.UI.Views;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.UI.Controllers
{
    internal sealed class PauseMenuViewController : ViewController<PauseMenuView>
    {
        [Header("Input Listeners")]
        [SerializeField]
        private ButtonInputActionListener toggleMenuListener;

        private ICursorSystem cursorSystem;
        private IPauseSystem pauseSystem;
        private ISceneSystem sceneSystem;

        protected override void Awake()
        {
            base.Awake();
            EnsureSystems();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (toggleMenuListener != null)
            {
                toggleMenuListener.OnPerformed += OnToggleMenuPerformed;
            }

            if (View != null)
            {
                View.OnResumeClicked += OnViewResumeClicked;
                View.OnExitClicked += OnViewExitClicked;
                View.OnSettingsClicked += OnViewSettingsClicked;
                View.OnShowEntered += OnViewShowEntered;
                View.OnHideEntered += OnViewHideEntered;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (toggleMenuListener != null)
            {
                toggleMenuListener.OnPerformed -= OnToggleMenuPerformed;
            }

            if (View != null)
            {
                View.OnResumeClicked -= OnViewResumeClicked;
                View.OnExitClicked -= OnViewExitClicked;
                View.OnSettingsClicked -= OnViewSettingsClicked;
                View.OnShowEntered -= OnViewShowEntered;
                View.OnHideEntered -= OnViewHideEntered;
            }
        }

        private void OnToggleMenuPerformed(bool value)
        {
            if (View == null)
            {
                return;
            }

            if (View.State is ViewVisibilityState.Hiding or ViewVisibilityState.Hidden)
            {
                View.Show();
                return;
            }

            View.Hide();
        }

        private void OnViewResumeClicked()
        {
            View?.Hide();
        }

        private void OnViewExitClicked()
        {
            sceneSystem?.LoadMenuScene();
        }

        /// <summary>Настройки открываются поверх паузы: игра при этом остаётся на паузе.</summary>
        private void OnViewSettingsClicked()
        {
            SettingsViewController.Instance?.Toggle();
        }

        private void OnViewShowEntered()
        {
            pauseSystem?.PauseGame();
            cursorSystem?.UnLockCursor();
        }

        private void OnViewHideEntered()
        {
            pauseSystem?.ResumeGame();
            cursorSystem?.LockCursor();
        }

        private void EnsureSystems()
        {
            if (SystemsUtility.TryGetSystem(out ICursorSystem cursor))
            {
                cursorSystem = cursor;
            }

            if (SystemsUtility.TryGetSystem(out IPauseSystem pause))
            {
                pauseSystem = pause;
            }

            if (SystemsUtility.TryGetSystem(out ISceneSystem scene))
            {
                sceneSystem = scene;
            }
        }
    }
}

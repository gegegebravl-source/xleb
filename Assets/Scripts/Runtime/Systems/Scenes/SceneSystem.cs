using System;
using CHARK.GameManagement;
using CHARK.GameManagement.Systems;
using CHARK.ScriptableScenes;
using CHARK.ScriptableScenes.Events;
using UABPetelnia.GGJ2025.Runtime.Systems.Pausing;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Scenes
{
    internal sealed class SceneSystem : MonoSystem, ISceneSystem
    {
        [Header("General")]
        [SerializeField]
        private ScriptableSceneController controller;

        [Header("Scenes")]
        [SerializeField]
        private ScriptableSceneCollection menuSceneCollection;

        [SerializeField]
        private ScriptableSceneCollection startingSceneCollection;

        [SerializeField]
        private ScriptableSceneCollection gameVictorySceneCollection;

        [SerializeField]
        private ScriptableSceneCollection gameOverSceneCollection;

        private IPauseSystem pauseSystem;

        public bool IsLoading => controller.IsLoading;

        public override void OnInitialized()
        {
            TryGetSystem(out pauseSystem);

            controller.CollectionEvents.OnLoadEntered += OnLoadEntered;
            controller.CollectionEvents.OnLoadExited += OnLoadExited;

            controller.CollectionEvents.OnUnloadEntered += OnUnloadEntered;
        }

        public override void OnDisposed()
        {
            controller.CollectionEvents.OnLoadEntered -= OnLoadEntered;
            controller.CollectionEvents.OnLoadExited -= OnLoadExited;

            controller.CollectionEvents.OnUnloadEntered -= OnUnloadEntered;
        }

        public bool TryGetLoadedCollection(out ScriptableSceneCollection collection)
        {
            return controller.TryGetLoadedSceneCollection(out collection);
        }

        public bool IsStartingScene(ScriptableSceneCollection collection)
        {
            return startingSceneCollection == collection || menuSceneCollection == collection;
        }

        public void LoadInitialScene()
        {
            PrepareForSceneLoad();
            controller.LoadInitialSceneCollection();
        }

        public void ReloadScene()
        {
            PrepareForSceneLoad();
            controller.ReloadLoadedSceneCollection();
        }

        public void LoadMenuScene()
        {
            PrepareForSceneLoad();
            controller.LoadSceneCollection(menuSceneCollection);
        }

        public void LoadGameplayScene()
        {
            PrepareForSceneLoad();
            controller.LoadSceneCollection(startingSceneCollection);
        }

        public void LoadScene(ScriptableSceneCollection collection)
        {
            PrepareForSceneLoad();
            controller.LoadSceneCollection(collection);
        }

        public void LoadGameVictoryScene()
        {
            PrepareForSceneLoad();
            controller.LoadSceneCollection(gameVictorySceneCollection);
        }

        public void LoadGameOverScene()
        {
            PrepareForSceneLoad();
            controller.LoadSceneCollection(gameOverSceneCollection);
        }

        /// <summary>
        /// Collection loading goes through a fade transition, and the transition delay is awaited
        /// with <c>WaitForSeconds</c>, which stands still while <c>Time.timeScale</c> is zero.
        /// Leaving a paused game (the pause menu does exactly that) would therefore freeze the
        /// transition forever: the screen stays black and the controller keeps reporting
        /// <see cref="IsLoading"/>, so no other scene could ever be loaded again. The clock is always
        /// running again before a collection starts loading.
        /// </summary>
        private void PrepareForSceneLoad()
        {
            if (pauseSystem == null || pauseSystem.IsPaused == false)
            {
                return;
            }

            pauseSystem.ResumeGame();
        }

        private static void OnLoadEntered(CollectionLoadEventArgs args)
        {
            var message = new SceneLoadEnteredMessage(args.Collection);
            GameManager.Publish(message);
        }

        private static void TryGetSystem<TSystem>(out TSystem system) where TSystem : ISystem
        {
            try
            {
                GameManager.TryGetSystem(out system);
            }
            catch (Exception)
            {
                system = default;
            }
        }

        private void OnLoadExited(CollectionLoadEventArgs args)
        {
            pauseSystem?.ResumeGame();

            var message = new SceneLoadExitedMessage(args.Collection);
            GameManager.Publish(message);
        }

        private void OnUnloadEntered(CollectionUnloadEventArgs args)
        {
            var message = new SceneUnloadEnteredMessage(args.Collection);
            GameManager.Publish(message);
        }
    }
}

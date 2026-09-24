using System.Collections.Generic;
using CHARK.GameManagement;
using CHARK.GameManagement.Systems;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UABPetelnia.GGJ2025.Runtime.Systems.Gameplay.States;
using UABPetelnia.GGJ2025.Runtime.Systems.Scenes;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Gameplay
{
    internal sealed class GameplaySystem : MonoSystem, IGameplaySystem
    {
        [SerializeField]
        private GameplaySettings gameplaySettings;

        /// <summary>
        /// Writes one line to the console on every shopper state change. Off by default: a full
        /// shift changes state hundreds of times and the log was the noisiest thing in the build.
        /// </summary>
        [SerializeField]
        private bool isLogStateChanges;

        private readonly GameplayStateContext context = new();

        private GameplayState startingState;
        private GameplayState currentState;
        private readonly List<GameplayState> states = new();

        private GameplayState State
        {
            get => currentState;
            set
            {
                var oldState = currentState;
                var newState = value;

                if (oldState != null && oldState == newState)
                {
                    return;
                }

                oldState?.Exit(context);
                currentState = newState;
                newState?.Enter(context);

                OnStateChanged(oldState, newState);
            }
        }

        public override void OnInitialized()
        {
            GameManager.AddListener<SceneLoadEnteredMessage>(OnSceneLoadEntered);
            GameManager.AddListener<SceneUnloadEnteredMessage>(OnSceneUnloadEntered);
        }

        public override void OnDisposed()
        {
            GameManager.RemoveListener<SceneLoadEnteredMessage>(OnSceneLoadEntered);
            GameManager.RemoveListener<SceneUnloadEnteredMessage>(OnSceneUnloadEntered);
        }

        public void OnUpdated(float deltaTime)
        {
            if (State == default)
            {
                return;
            }

            State = State.Update(context);
        }

        public void StartGameplay()
        {
            InitializeStateMachine();
        }

        private void OnStateChanged(GameplayState oldState, GameplayState newState)
        {
            if (isLogStateChanges == false)
            {
                return;
            }

            Debug.Log($"New state {newState?.Name}");
        }


        private void OnSceneLoadEntered(SceneLoadEnteredMessage message)
        {
            // Gameplay is started explicitly by the player actor once the scene is ready, see
            // DesktopPlayerActor.Start. Loading it here as well would run the state machine twice.
        }

        private void OnSceneUnloadEntered(SceneUnloadEnteredMessage message)
        {
            CleanupStateMachine();
        }

        private void InitializeStateMachine()
        {
            // Старые состояния могли остаться подписанными на события — снимаем подписки
            // ДО входа в новое состояние, иначе отдача товара обрабатывается дважды.
            foreach (var state in states)
            {
                state.Dispose();
            }

            states.Clear();

            var spawnState = new ShopperSpawnState(gameplaySettings);
            var moveToKioskState = new ShopperMoveState(ShopperMoveState.MoveTo.KioskPoint);
            var moveToSpawnPointState = new ShopperMoveState(ShopperMoveState.MoveTo.SpawnPoint);

            var purchasedState = new ShopperPurchasedState();
            var punchingState = new ShopperPunchingState();

            // Refusal uses the walk-back state directly: the shopper leaves without buying and
            // without hitting the player, which is what happens when the kiosk cannot serve them.
            var waitingState = new ShopperWaitingState(
                gameplaySettings: gameplaySettings,
                successState: purchasedState,
                failureState: punchingState,
                refusalState: moveToSpawnPointState
            );
            var destroyState = new ShopperDestroyState();

            var gameOverCheckState = new GameOverCheckState();

            // 1. Spawn player and move to kiosk after spawning
            spawnState.Initialize(moveToKioskState);

            // 2. Reaching the counter, wait for the ordered product to be handed over
            moveToKioskState.Initialize(waitingState);

            // 3. The hand-over decides success or failure (no target state needed)
            waitingState.Initialize(default);

            // 4. On success or failure, move back to spawn
            purchasedState.Initialize(gameOverCheckState);
            punchingState.Initialize(gameOverCheckState);

            // 5. Move to spawn point if passing game over
            gameOverCheckState.Initialize(moveToSpawnPointState);

            // 6. Destroy on reaching the spawn point
            moveToSpawnPointState.Initialize(destroyState);

            // 7. Restart.
            destroyState.Initialize(spawnState);

            State = spawnState;

            // Tracking for cleanup
            states.Add(spawnState);
            states.Add(moveToKioskState);
            states.Add(waitingState);
            states.Add(moveToSpawnPointState);
            states.Add(destroyState);
            states.Add(purchasedState);
            states.Add(punchingState);
            states.Add(gameOverCheckState);
        }

        private void CleanupStateMachine()
        {
            foreach (var state in states)
            {
                state.Dispose();
            }

            states.Clear();

            State = default;
        }
    }
}

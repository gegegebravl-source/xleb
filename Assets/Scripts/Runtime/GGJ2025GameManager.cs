using CHARK.GameManagement;
using CHARK.GameManagement.Systems;
using UABPetelnia.GGJ2025.Runtime.Systems.Audio;
using UABPetelnia.GGJ2025.Runtime.Systems.Cursors;
using UABPetelnia.GGJ2025.Runtime.Systems.Delivery;
using UABPetelnia.GGJ2025.Runtime.Systems.Gameplay;
using UABPetelnia.GGJ2025.Runtime.Systems.Input;
using UABPetelnia.GGJ2025.Runtime.Systems.Interaction;
using UABPetelnia.GGJ2025.Runtime.Systems.Pausing;
using UABPetelnia.GGJ2025.Runtime.Systems.Players;
using UABPetelnia.GGJ2025.Runtime.Systems.Progress;
using UABPetelnia.GGJ2025.Runtime.Systems.Products;
using UABPetelnia.GGJ2025.Runtime.Systems.Saves;
using UABPetelnia.GGJ2025.Runtime.Systems.Scenes;
using UABPetelnia.GGJ2025.Runtime.Systems.Settings;
using UABPetelnia.GGJ2025.Runtime.Systems.Shop;
using UABPetelnia.GGJ2025.Runtime.Systems.Shoppers;
using UABPetelnia.GGJ2025.Runtime.Systems.Clock;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime
{
    // ReSharper disable once InconsistentNaming
    internal sealed class GGJ2025GameManager : GameManager
    {
        [Header("Systems")]
        [SerializeField]
        private AudioSystem audioSystem;

        [SerializeField]
        private CursorSystem cursorSystem;

        [SerializeField]
        private InputSystem inputSystem;

        [SerializeField]
        private PauseSystem pauseSystem;

        [SerializeField]
        private SettingsSystem settingsSystem;

        [SerializeField]
        private SceneSystem sceneSystem;

        [SerializeField]
        private ShopperSystem shopperSystem;

        [SerializeField]
        private ShopSystem shopSystem;

        [SerializeField]
        private ProductSystem productSystem;

        [SerializeField]
        private ProgressSystem progressSystem;

        [SerializeField]
        private GameplaySystem gameplaySystem;

        [SerializeField]
        private ShiftClockSystem shiftClockSystem;

        [SerializeField]
        private CourierSystem courierSystem;

        protected override void OnBeforeInitializeSystems()
        {
            AddSystem(audioSystem);
            AddSystem(cursorSystem);
            AddSystem(inputSystem);
            AddSystem(pauseSystem);
            AddSystem(settingsSystem);
            AddSystem(sceneSystem);

            AddSystem(new SaveSystem());
            AddSystem(new PlayerSystem());
            AddSystem(shopperSystem);

            // Registered before the shelves: the products read their stock from the shop.
            AddSystem(EnsureSystem(shopSystem));
            AddSystem(EnsureSystem(productSystem));
            AddSystem(EnsureSystem(progressSystem));
            AddSystem(gameplaySystem);
            AddSystem(EnsureSystem(shiftClockSystem));
            AddSystem(EnsureSystem(courierSystem));
            AddSystem(new InteractionSystem());
        }

        /// <summary>
        /// Return the assigned system, or add one to this game object when the prefab was not
        /// wired up by the editor tools yet. Without the fallback a manager that was set up before
        /// the tools ran would throw on start-up.
        /// </summary>
        private T EnsureSystem<T>(T system) where T : MonoSystem
        {
            if (system)
            {
                return system;
            }

            Debug.LogWarning($"[GameManager] {typeof(T).Name} is not assigned, adding one at runtime.");

            return gameObject.AddComponent<T>();
        }

        protected override void OnAfterInitializeSystems()
        {
        }

        protected override void OnStarted()
        {
            base.OnStarted();

            audioSystem.LoadBanks();

#if UNITY_WEBGL
            StartCoroutine(LoadGameRoutine());
#else
            sceneSystem.LoadInitialScene();
#endif
        }

#if UNITY_WEBGL
        private System.Collections.IEnumerator LoadGameRoutine()
        {
            if (audioSystem.IsLoading)
            {
                yield return null;
            }

            // TODO: scuffed workaround for WebGL not playing audio in main menu, oh well...
            yield return new WaitForSeconds(1f);
            sceneSystem.LoadInitialScene();
        }
#endif
    }
}

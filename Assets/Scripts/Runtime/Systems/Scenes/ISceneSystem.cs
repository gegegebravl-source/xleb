using CHARK.GameManagement.Systems;
using CHARK.ScriptableScenes;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Scenes
{
    internal interface ISceneSystem : ISystem
    {
        /// <summary>
        /// <c>true</c> if a scene is being loaded or <c>false</c> otherwise.
        /// </summary>
        public bool IsLoading { get; }

        /// <returns>
        /// <c>true</c> if <see cref="collection"/> is retrieved or <c>false</c> otherwise.
        /// </returns>
        public bool TryGetLoadedCollection(out ScriptableSceneCollection collection);

        /// <returns>
        /// <c>true</c> if given <paramref name="collection"/> is one of the starting scenes or
        /// <c>false</c> otherwise.
        /// </returns>
        public bool IsStartingScene(ScriptableSceneCollection collection);

        /// <summary>
        /// Load initial game scene.
        /// </summary>
        public void LoadInitialScene();

        /// <summary>
        /// Reload currently active scene.
        /// </summary>
        public void ReloadScene();

        /// <summary>
        /// Load main menu scene.
        /// </summary>
        public void LoadMenuScene();

        /// <summary>
        /// Load starting game scene.
        /// </summary>
        public void LoadGameplayScene();

        /// <summary>
        /// Load given <paramref name="collection"/>.
        /// </summary>
        public void LoadScene(ScriptableSceneCollection collection);

        public void LoadGameVictoryScene();

        public void LoadGameOverScene();
    }
}

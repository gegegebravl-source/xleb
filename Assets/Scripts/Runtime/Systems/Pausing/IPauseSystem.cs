using CHARK.GameManagement.Systems;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Pausing
{
    internal interface IPauseSystem : ISystem
    {
        /// <summary>
        /// <c>true</c> if game is paused or <c>false</c> otherwise.
        /// </summary>
        public bool IsPaused { get; }

        /// <summary>
        /// Pause game.
        /// </summary>
        public void PauseGame();

        /// <summary>
        /// Unpause game.
        /// </summary>
        public void ResumeGame();
    }
}

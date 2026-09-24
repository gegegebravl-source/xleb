using CHARK.GameManagement;
using CHARK.GameManagement.Systems;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Pausing
{
    internal sealed class PauseSystem : MonoSystem, IPauseSystem
    {
        public bool IsPaused { get; private set; }

        public void PauseGame()
        {
            Time.timeScale = 0f;

            IsPaused = true;
            GameManager.Publish(new GamePausedMessage());
        }

        public void ResumeGame()
        {
            Time.timeScale = 1f;

            IsPaused = false;
            GameManager.Publish(new GameResumedMessage());
        }
    }
}

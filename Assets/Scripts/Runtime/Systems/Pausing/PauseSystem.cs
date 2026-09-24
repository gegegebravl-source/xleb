using CHARK.GameManagement;
using CHARK.GameManagement.Systems;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Pausing
{
    internal sealed class PauseSystem : MonoSystem, IPauseSystem
    {
        public bool IsPaused { get; private set; }

        private float previousTimeScale = 1f;

        public override void OnInitialized()
        {
            IsPaused = false;
            previousTimeScale = Mathf.Max(0f, Time.timeScale);
            if (Time.timeScale == 0f)
            {
                Time.timeScale = 1f;
                previousTimeScale = 1f;
            }
        }

        public void PauseGame()
        {
            if (IsPaused)
            {
                return;
            }

            previousTimeScale = Mathf.Max(0f, Time.timeScale);
            Time.timeScale = 0f;
            IsPaused = true;
            GameManager.Publish(new GamePausedMessage());
        }

        public void ResumeGame()
        {
            if (IsPaused == false)
            {
                return;
            }

            Time.timeScale = previousTimeScale;
            IsPaused = false;
            GameManager.Publish(new GameResumedMessage());
        }

        public override void OnDisposed()
        {
            if (IsPaused)
            {
                Time.timeScale = previousTimeScale;
            }

            IsPaused = false;
            if (Time.timeScale == 0f)
            {
                Time.timeScale = 1f;
            }

            previousTimeScale = Mathf.Max(0f, Time.timeScale);
        }
    }
}

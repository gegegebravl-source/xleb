using CHARK.GameManagement;
using UABPetelnia.GGJ2025.Runtime.Systems.Gameplay;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Components.Triggers
{
    internal sealed class GameplayTrigger : MonoBehaviour
    {
        private IGameplaySystem gameplaySystem;

        private void Awake()
        {
            gameplaySystem = GameManager.GetSystem<IGameplaySystem>();
        }

        public void StartGameplay()
        {
            gameplaySystem.StartGameplay();
        }
    }
}

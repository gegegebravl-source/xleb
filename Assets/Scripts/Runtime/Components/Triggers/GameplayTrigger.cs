using CHARK.GameManagement;
using UABPetelnia.GGJ2025.Runtime.Systems.Gameplay;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Components.Triggers
{
    internal sealed class GameplayTrigger : MonoBehaviour
    {
        private IGameplaySystem gameplaySystem;

        private void Awake()
        {
            SystemsUtility.TryGetSystem(out gameplaySystem);
        }

        public void StartGameplay()
        {
            gameplaySystem?.StartGameplay();
        }
    }
}

using CHARK.GameManagement.Systems;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Gameplay
{
    internal interface IGameplaySystem : ISystem, IUpdateListener
    {
        public void StartGameplay();
    }
}

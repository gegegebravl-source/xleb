using CHARK.GameManagement.Systems;
using UABPetelnia.GGJ2025.Runtime.Actors;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Players
{
    internal interface IPlayerSystem : ISystem
    {
        public IPlayerActor Player { get; }

        public bool TryGetPlayer(out IPlayerActor player);

        public void AddPlayer(IPlayerActor player);

        public void RemovePlayer(IPlayerActor player);
    }
}

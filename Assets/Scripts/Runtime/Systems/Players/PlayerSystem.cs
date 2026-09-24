using System.Collections.Generic;
using System.Linq;
using CHARK.GameManagement.Systems;
using UABPetelnia.GGJ2025.Runtime.Actors;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Players
{
    internal sealed class PlayerSystem : SimpleSystem, IPlayerSystem
    {
        private readonly List<IPlayerActor> players = new();

        public IPlayerActor Player => players.FirstOrDefault();

        public bool TryGetPlayer(out IPlayerActor player)
        {
            player = players.FirstOrDefault();
            return player != default;
        }

        public void AddPlayer(IPlayerActor player)
        {
            if (players.Contains(player))
            {
                return;
            }

            players.Add(player);
        }

        public void RemovePlayer(IPlayerActor player)
        {
            players.Remove(player);
        }
    }
}

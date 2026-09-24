using System.Collections.Generic;
using System.Linq;
using CHARK.GameManagement.Systems;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Players
{
    internal sealed class PlayerSystem : SimpleSystem, IPlayerSystem
    {
        private readonly List<IPlayerActor> players = new();

        public IPlayerActor Player
        {
            get
            {
                RemoveDeadPlayers();
                return players.FirstOrDefault();
            }
        }

        public bool TryGetPlayer(out IPlayerActor player)
        {
            player = Player;
            return player != default;
        }

        public void AddPlayer(IPlayerActor player)
        {
            if (IsAlive(player) == false || players.Contains(player))
            {
                return;
            }

            players.Add(player);
        }

        public void RemovePlayer(IPlayerActor player)
        {
            players.Remove(player);
        }

        private void RemoveDeadPlayers()
        {
            players.RemoveAll(player => IsAlive(player) == false);
        }

        private static bool IsAlive(IPlayerActor player)
        {
            if (player == null)
            {
                return false;
            }

            return player is not Object unityObject || unityObject != false;
        }
    }
}

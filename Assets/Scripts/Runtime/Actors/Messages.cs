﻿using CHARK.GameManagement.Messaging;
using UABPetelnia.GGJ2025.Runtime.Settings;

namespace UABPetelnia.GGJ2025.Runtime.Actors
{
    /// <summary>
    /// Published when the player physically hands a product over to the shopper waiting at the
    /// counter. The gameplay state machine decides whether the product was the right one.
    /// </summary>
    internal sealed class ItemHandedOverMessage : IMessage
    {
        public ItemData Item { get; }

        public ItemHandedOverMessage(ItemData item)
        {
            Item = item;
        }
    }

    internal sealed class PlayerCentsChanged : IMessage
    {
        public IPlayerActor Player { get; }

        public PlayerCentsChanged(IPlayerActor player)
        {
            Player = player;
        }
    }

    internal sealed class PlayerHealthChanged : IMessage
    {
        public IPlayerActor Player { get; }

        public PlayerHealthChanged(IPlayerActor player)
        {
            Player = player;
        }
    }
}

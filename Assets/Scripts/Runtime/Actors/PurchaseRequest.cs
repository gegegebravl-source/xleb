﻿using System;
using System.Collections.Generic;
using System.Linq;
using UABPetelnia.GGJ2025.Runtime.Settings;

namespace UABPetelnia.GGJ2025.Runtime.Actors
{
    /// <summary>
    /// What a shopper asks for. <see cref="Text"/> is what is written in the chat, while
    /// <see cref="WantedItems"/> contains the physical products that satisfy the request.
    /// </summary>
    internal sealed class PurchaseRequest
    {
        private readonly List<ItemData> wantedItems;

        public string Text { get; }

        public IReadOnlyCollection<ItemData> WantedItems => wantedItems;

        public IShopperActor Shopper { get; }

        public bool IsEmpty => wantedItems.Count == 0;

        /// <summary>
        /// One of the wanted items, used as the picture on the shopper's request card.
        /// </summary>
        public ItemData PrimaryItem => wantedItems.Count > 0 ? wantedItems[0] : default;

        public PurchaseRequest(string text, IEnumerable<ItemData> wantedItems, IShopperActor shopper)
        {
            Text = text;
            Shopper = shopper;

            this.wantedItems = wantedItems == null
                ? new List<ItemData>()
                : wantedItems
                    .Where(item => item)
                    .Distinct()
                    .ToList();
        }

        /// <returns>
        /// <c>true</c> when handed over <paramref name="item"/> satisfies this request.
        /// </returns>
        public bool IsWanted(ItemData item)
        {
            return item && wantedItems.Contains(item);
        }

        /// <summary>
        /// Drop every wanted item that <paramref name="isAvailable"/> rejects. The chat text stays
        /// the same, so narrowing a request to what the kiosk actually has on the shelves keeps it
        /// readable and, more importantly, keeps it satisfiable.
        /// </summary>
        public void KeepOnly(Predicate<ItemData> isAvailable)
        {
            if (isAvailable == null)
            {
                return;
            }

            wantedItems.RemoveAll(item => isAvailable(item) == false);
        }
    }
}

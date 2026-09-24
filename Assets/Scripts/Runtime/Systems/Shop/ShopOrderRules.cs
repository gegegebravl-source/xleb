using System;
using System.Collections.Generic;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Shop
{
    /// <summary>
    /// Overflow-safe validation used by the shop UI and by <see cref="ShopSystem"/> before it
    /// mutates the cart or charges the player. Keeping the arithmetic here makes boundary cases
    /// testable without booting the whole GameManager.
    /// </summary>
    internal static class ShopOrderRules
    {
        public static bool TryAddQuantity(
            int currentQuantity,
            int requestedQuantity,
            out int result,
            out string error
        )
        {
            result = currentQuantity;
            error = default;

            if (currentQuantity < 0 || requestedQuantity <= 0)
            {
                error = "bad_quantity";
                return false;
            }

            if (requestedQuantity > int.MaxValue - currentQuantity)
            {
                error = "bad_quantity";
                return false;
            }

            result = currentQuantity + requestedQuantity;
            return true;
        }

        public static bool TryRemoveQuantity(
            int currentQuantity,
            int requestedQuantity,
            out int result,
            out string error
        )
        {
            result = currentQuantity;
            error = default;

            if (currentQuantity <= 0 || requestedQuantity <= 0)
            {
                error = "bad_quantity";
                return false;
            }

            result = Math.Max(0, currentQuantity - requestedQuantity);
            return true;
        }

        public static bool TryCalculateTotal(
            IReadOnlyList<ShopCartLine> lines,
            out int total,
            out string error
        )
        {
            total = 0;
            error = default;

            if (lines == null || lines.Count == 0)
            {
                error = "empty_cart";
                return false;
            }

            long sum = 0;

            for (var index = 0; index < lines.Count; index++)
            {
                var line = lines[index];
                if (line == null || line.Product == null || line.Quantity <= 0 || line.Product.PurchasePrice <= 0)
                {
                    error = "bad_cost";
                    return false;
                }

                sum += (long)line.Product.PurchasePrice * line.Quantity;
                if (sum > int.MaxValue)
                {
                    error = "bad_cost";
                    return false;
                }
            }

            total = (int)sum;
            return total > 0;
        }
    }
}

using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Settings;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Gameplay
{
    internal sealed class GameplayStateContext
    {
        public IShopperActor ActiveShopper { get; set; }

        public ItemData CurrentItem { get; set; }
    }
}

using UABPetelnia.GGJ2025.Runtime.Settings;

namespace UABPetelnia.GGJ2025.Runtime.Actors
{
    /// <summary>
    /// Physical product standing on a kiosk shelf. The player grabs it and hands it over to the
    /// shopper waiting at the counter.
    /// </summary>
    internal interface IProductActor
    {
        public ItemData Item { get; }
    }
}

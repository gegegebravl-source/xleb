using Newtonsoft.Json;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Saves
{
    internal struct SaveData
    {
        /// <summary>
        /// Player money in cents.
        /// </summary>
        public int Cents { get; set; }

        /// <summary>
        /// Player health.
        /// </summary>
        public int Health { get; set; }

        /// <summary>
        /// UTC ticks of when this save was written.
        /// </summary>
        public long SavedAtUtcTicks { get; set; }

        [JsonConstructor]
        public SaveData(int cents, int health, long savedAtUtcTicks)
        {
            Cents = cents;
            Health = health;
            SavedAtUtcTicks = savedAtUtcTicks;
        }
    }
}

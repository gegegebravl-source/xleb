using CHARK.GameManagement.Systems;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Saves
{
    internal interface ISaveSystem : ISystem
    {
        /// <summary>
        /// Total amount of available save slots.
        /// </summary>
        public int SlotCount { get; }

        /// <summary>
        /// Index of the slot the ongoing run is saved into or <c>-1</c> if there is no active run.
        /// </summary>
        public int ActiveSlot { get; }

        /// <summary>
        /// <c>true</c> if there is a run in progress (started or continued) or <c>false</c> otherwise.
        /// </summary>
        public bool HasActiveRun { get; }

        /// <returns>
        /// <c>true</c> if <paramref name="slot"/> contains a save or <c>false</c> otherwise.
        /// </returns>
        public bool IsSlotOccupied(int slot);

        /// <returns>
        /// <c>true</c> if <paramref name="slot"/> contains a save that is retrieved into
        /// <paramref name="data"/> or <c>false</c> otherwise.
        /// </returns>
        public bool TryGetSlotData(int slot, out SaveData data);

        /// <summary>
        /// Start a brand new run, resetting the oldest/free save slot.
        /// </summary>
        public void StartNewGame();

        /// <returns>
        /// <c>true</c> if run from <paramref name="slot"/> is requested to be continued or
        /// <c>false</c> otherwise.
        /// </returns>
        public bool ContinueFromSlot(int slot);

        /// <summary>
        /// Clear given <paramref name="slot"/>.
        /// </summary>
        public void DeleteSlot(int slot);

        /// <summary>
        /// Write current player state into the active slot.
        /// </summary>
        public void SaveActiveRun();

        /// <returns>
        /// <c>true</c> if a continued run is waiting to be applied into <paramref name="data"/> or
        /// <c>false</c> otherwise. Requested data is consumed, meaning this returns <c>true</c>
        /// only once.
        /// </returns>
        public bool TryConsumePendingLoad(out SaveData data);
    }
}

using System;
using CHARK.GameManagement;
using CHARK.GameManagement.Systems;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Systems.Players;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Saves
{
    internal sealed class SaveSystem : SimpleSystem, ISaveSystem
    {
        private const int SlotCountConstant = 3;

        private const string SavePathFormat = "Save_{0}.json";

        private readonly SaveData[] slots = new SaveData[SlotCountConstant];
        private readonly bool[] isSlotOccupied = new bool[SlotCountConstant];

        private bool isPendingLoad;
        private SaveData pendingLoadData;

        public int SlotCount => SlotCountConstant;

        public int ActiveSlot { get; private set; } = -1;

        public bool HasActiveRun => ActiveSlot >= 0;

        public override void OnInitialized()
        {
            for (var slot = 0; slot < SlotCountConstant; slot++)
            {
                ReadSlot(slot);
            }

            GameManager.AddListener<PlayerCentsChanged>(OnPlayerCentsChanged);
            GameManager.AddListener<PlayerHealthChanged>(OnPlayerHealthChanged);
        }

        public override void OnDisposed()
        {
            GameManager.RemoveListener<PlayerCentsChanged>(OnPlayerCentsChanged);
            GameManager.RemoveListener<PlayerHealthChanged>(OnPlayerHealthChanged);

            SaveActiveRun();
        }

        public bool IsSlotOccupied(int slot)
        {
            return IsValidSlot(slot) && isSlotOccupied[slot];
        }

        public bool TryGetSlotData(int slot, out SaveData data)
        {
            if (IsSlotOccupied(slot) == false)
            {
                data = default;
                return false;
            }

            data = slots[slot];
            return true;
        }

        public void StartNewGame()
        {
            var slot = FindFreeSlot();

            DeleteSlot(slot);

            ActiveSlot = slot;
            isPendingLoad = false;
            pendingLoadData = default;
        }

        public bool ContinueFromSlot(int slot)
        {
            if (TryGetSlotData(slot, out var data) == false)
            {
                return false;
            }

            ActiveSlot = slot;
            isPendingLoad = true;
            pendingLoadData = data;

            return true;
        }

        public void DeleteSlot(int slot)
        {
            if (IsValidSlot(slot) == false)
            {
                return;
            }

            isSlotOccupied[slot] = false;
            slots[slot] = default;

            GameManager.DeleteData(GetSlotPath(slot));
        }

        public void SaveActiveRun()
        {
            if (HasActiveRun == false)
            {
                return;
            }

            if (TryGetSystem(out IPlayerSystem playerSystem) &&
                playerSystem.TryGetPlayer(out var player))
            {
                SaveRun(ActiveSlot, player);
            }
        }

        public bool TryConsumePendingLoad(out SaveData data)
        {
            data = pendingLoadData;

            if (isPendingLoad == false)
            {
                return false;
            }

            isPendingLoad = false;

            return true;
        }

        private void OnPlayerCentsChanged(PlayerCentsChanged message)
        {
            SaveRun(ActiveSlot, message.Player);
        }

        private void OnPlayerHealthChanged(PlayerHealthChanged message)
        {
            SaveRun(ActiveSlot, message.Player);
        }

        private void SaveRun(int slot, IPlayerActor player)
        {
            if (IsValidSlot(slot) == false || player == default)
            {
                return;
            }

            if (player is UnityEngine.Object playerObject && playerObject == false)
            {
                // Player (or anything else owning it) is already destroyed, nothing to save.
                return;
            }

            if (player.Health <= 0)
            {
                // Run is over, there is nothing to continue from.
                DeleteSlot(slot);

                ActiveSlot = -1;
                isPendingLoad = false;
                pendingLoadData = default;

                return;
            }

            var data = new SaveData(
                cents: player.Cents,
                health: player.Health,
                savedAtUtcTicks: DateTime.UtcNow.Ticks
            );

            slots[slot] = data;
            isSlotOccupied[slot] = true;

            GameManager.SaveData(GetSlotPath(slot), data);
        }

        private void ReadSlot(int slot)
        {
            if (GameManager.TryReadData(GetSlotPath(slot), out SaveData data))
            {
                slots[slot] = data;
                isSlotOccupied[slot] = true;
                return;
            }

            slots[slot] = default;
            isSlotOccupied[slot] = false;
        }

        private static bool TryGetSystem<TSystem>(out TSystem system) where TSystem : ISystem
        {
            try
            {
                return GameManager.TryGetSystem(out system);
            }
            catch (Exception)
            {
                system = default;
                return false;
            }
        }

        private int FindFreeSlot()
        {
            for (var slot = 0; slot < SlotCountConstant; slot++)
            {
                if (isSlotOccupied[slot] == false)
                {
                    return slot;
                }
            }

            var oldestSlot = 0;
            var oldestTicks = long.MaxValue;

            for (var slot = 0; slot < SlotCountConstant; slot++)
            {
                if (slots[slot].SavedAtUtcTicks >= oldestTicks)
                {
                    continue;
                }

                oldestTicks = slots[slot].SavedAtUtcTicks;
                oldestSlot = slot;
            }

            return oldestSlot;
        }

        private static bool IsValidSlot(int slot)
        {
            return slot >= 0 && slot < SlotCountConstant;
        }

        private static string GetSlotPath(int slot)
        {
            return string.Format(SavePathFormat, slot + 1);
        }
    }
}

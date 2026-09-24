using CHARK.GameManagement.Systems;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Clock
{
    /// <summary>
    /// Игровое время смены: номер дня, текущий час и признак закрытия ларька.
    /// </summary>
    internal interface IShiftClockSystem : ISystem
    {
        /// <summary>Номер текущего дня смены, начиная с 1.</summary>
        int Day { get; }

        /// <summary>Текущий час в формате 0..24 (14.5 — 14:30).</summary>
        float Hour { get; }

        /// <summary><c>true</c>, когда смена закрыта и время остановлено.</summary>
        bool IsShiftOver { get; }

        /// <summary>Время в виде "ЧЧ:ММ" для HUD.</summary>
        string ClockText { get; }

        /// <summary>Прогресс смены от 0 (открытие) до 1 (закрытие) — для полоски в HUD.</summary>
        float ShiftProgress { get; }

        /// <summary>
        /// Закрыть текущий день и начать следующий: часы возвращаются на открытие.
        /// </summary>
        void StartNextDay();
    }
}

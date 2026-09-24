using CHARK.GameManagement.Messaging;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Clock
{
    /// <summary>
    /// Игровые часы сдвинулись: HUD перерисовывает время. Публикуется на каждой новой
    /// игровой минуте, а не каждый кадр.
    /// </summary>
    internal readonly struct ShiftClockChangedMessage : IMessage
    {
        public int Day { get; }

        /// <summary>Текущий час в формате 0..24, например 14.5 — половина третьего.</summary>
        public float Hour { get; }

        public ShiftClockChangedMessage(int day, float hour)
        {
            Day = day;
            Hour = hour;
        }
    }

    /// <summary>
    /// Смена закончилась: ларёк закрывается, игроку показывают итоги дня.
    /// </summary>
    internal readonly struct ShiftClosedMessage : IMessage
    {
        public int Day { get; }

        public ShiftClosedMessage(int day)
        {
            Day = day;
        }
    }

    /// <summary>
    /// Начался новый день: часы сброшены на открытие, счётчики смены обнулены.
    /// </summary>
    internal readonly struct DayStartedMessage : IMessage
    {
        public int Day { get; }

        public DayStartedMessage(int day)
        {
            Day = day;
        }
    }
}

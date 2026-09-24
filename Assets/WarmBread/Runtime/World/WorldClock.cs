using System;

namespace WarmBread
{
    public sealed class WorldClock
    {
        private const double MinutesPerDay = 1440d;
        private double absoluteMinutes;

        public WorldClock(int day = 1, int hour = 6, int minute = 0)
        {
            if (day < 1 || hour < 0 || hour > 23 || minute < 0 || minute > 59) throw new ArgumentOutOfRangeException();
            absoluteMinutes = (day - 1) * MinutesPerDay + hour * 60 + minute;
        }

        public bool IsPaused { get; private set; }
        public float TimeScale { get; private set; } = 1f;
        public int Day => (int)(absoluteMinutes / MinutesPerDay) + 1;
        public int Weekday => (Day - 1) % 7;
        public int Hour => (int)(absoluteMinutes % MinutesPerDay) / 60;
        public int Minute => (int)(absoluteMinutes % 60);
        public double AbsoluteMinutes => absoluteMinutes;
        public event Action<int> HourCrossed;

        public void SetPaused(bool paused) { IsPaused = paused; }
        public void SetTimeScale(float scale) { TimeScale = Math.Max(0f, Math.Min(120f, scale)); }

        public void Tick(double realSeconds)
        {
            if (IsPaused || realSeconds <= 0d || TimeScale <= 0f) return;
            var oldHour = (int)(absoluteMinutes / 60d);
            absoluteMinutes += realSeconds * TimeScale / 60d;
            var newHour = (int)(absoluteMinutes / 60d);
            for (var hour = oldHour + 1; hour <= newHour; hour++) HourCrossed?.Invoke(hour);
        }

        public void FastForwardMinutes(int minutes)
        {
            if (minutes < 0) throw new ArgumentOutOfRangeException(nameof(minutes));
            var oldScale = TimeScale;
            TimeScale = 60f;
            Tick(minutes);
            TimeScale = oldScale;
        }
    }
}

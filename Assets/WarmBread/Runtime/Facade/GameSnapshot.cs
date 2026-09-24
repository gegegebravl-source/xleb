namespace WarmBread
{
    public readonly struct GameSnapshot
    {
        public GameSnapshot(int day, int hour, int minute, int money, int reputation, bool paused)
        { Day = day; Hour = hour; Minute = minute; Money = money; Reputation = reputation; Paused = paused; }
        public int Day { get; }
        public int Hour { get; }
        public int Minute { get; }
        public int Money { get; }
        public int Reputation { get; }
        public bool Paused { get; }
    }
}

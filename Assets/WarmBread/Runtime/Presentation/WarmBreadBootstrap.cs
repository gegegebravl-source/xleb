using UnityEngine;

namespace WarmBread
{
    [DefaultExecutionOrder(-900)]
    public sealed class WarmBreadBootstrap : MonoBehaviour
    {
        [SerializeField, Min(0)] private int openingBalance = 25000;
        [SerializeField, Range(1, 1000)] private int deliveryCapacity = 100;
        [SerializeField, Range(0.1f, 120f)] private float minutesPerRealMinute = 12f;

        public GameEventBus Events { get; private set; }
        public GameFacade Facade { get; private set; }
        public SaveSystem Saves { get; private set; }
        public SettingsService Settings { get; private set; }
        public ShopInventoryService Inventory { get; private set; }
        public DialogueService Dialogue { get; private set; }
        public JournalService Journal { get; private set; }
        public RelationshipService Relationships { get; private set; }
        public AchievementService Achievements { get; private set; }
        public StoryFlagService StoryFlags { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            // Procedural runtime creation is disabled: the project is meant to be assembled manually in the editor.
        }

        private void Awake()
        {
            Events = new GameEventBus(exception => Debug.LogException(exception, this));
            var clock = new WorldClock();
            clock.SetTimeScale(minutesPerRealMinute);
            var ledger = new EconomyLedger(openingBalance);
            var deliveries = new DeliveryService(Events, ledger, deliveryCapacity);
            Facade = new GameFacade(Events, clock, ledger, deliveries);
            Saves = new SaveSystem(Application.persistentDataPath);
            Settings = new SettingsService(Application.persistentDataPath);
            Settings.Load();
            Inventory = new ShopInventoryService(Events);
            Dialogue = new DialogueService(Events);
            Journal = new JournalService();
            Relationships = new RelationshipService();
            Achievements = new AchievementService();
            StoryFlags = new StoryFlagService();
            var settingsApplier = gameObject.AddComponent<RuntimeSettingsApplier>();
            settingsApplier.Initialize(Settings.Current);
            Settings.Changed += settingsApplier.Apply;
            Facade.StartDay();
            DontDestroyOnLoad(gameObject);
        }

        private void Update() { Facade?.Tick(Time.unscaledDeltaTime); }
        private void OnApplicationPause(bool paused) { if (paused) SaveSettings(); }
        private void OnApplicationQuit() { SaveSettings(); }
        private void SaveSettings()
        {
            try { Settings?.Save(); }
            catch (System.Exception exception) { Debug.LogWarning("[WarmBread] Не удалось сохранить настройки: " + exception.Message); }
        }
        private void OnDestroy() { Events?.Clear(); }
    }
}

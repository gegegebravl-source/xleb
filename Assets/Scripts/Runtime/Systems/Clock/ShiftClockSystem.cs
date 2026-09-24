using CHARK.GameManagement;
using CHARK.GameManagement.Systems;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UABPetelnia.GGJ2025.Runtime.Systems.Scenes;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Clock
{
    /// <summary>
    /// Часы смены: идут от открытия ларька до закрытия и останавливаются на закрытии.
    /// Единственное место, которое двигает игровое время.
    /// </summary>
    /// <remarks>
    /// Время двигается вместе с <c>Time.deltaTime</c>, поэтому пауза (Time.timeScale = 0)
    /// останавливает и часы, и покупателей одновременно.
    /// </remarks>
    internal sealed class ShiftClockSystem : MonoSystem, IShiftClockSystem, IUpdateListener
    {
        [SerializeField]
        private GameplaySettings gameplaySettings;

        private int day = 1;
        private float hour;
        private bool isShiftOver;
        private bool isGameplayScene;
        private int lastPublishedMinute = -1;

        public int Day => day;

        public float Hour => hour;

        public bool IsShiftOver => isShiftOver;

        public string ClockText
        {
            get
            {
                var totalMinutes = Mathf.Clamp(Mathf.FloorToInt(hour * 60f), 0, 24 * 60 - 1);
                var hours = totalMinutes / 60;
                var minutes = totalMinutes % 60;

                return $"{hours:00}:{minutes:00}";
            }
        }

        public float ShiftProgress
        {
            get
            {
                var start = GetStartHour();
                var end = gameplaySettings != false ? gameplaySettings.ShiftEndHour : 20f;
                var length = Mathf.Max(0.01f, end - start);

                return Mathf.Clamp01((hour - start) / length);
            }
        }

        public override void OnInitialized()
        {
            hour = GetStartHour();
            lastPublishedMinute = Mathf.FloorToInt(hour * 60f);

            GameManager.AddListener<SceneLoadEnteredMessage>(OnSceneLoadEntered);
        }

        public override void OnDisposed()
        {
            GameManager.RemoveListener<SceneLoadEnteredMessage>(OnSceneLoadEntered);
        }

        private void OnSceneLoadEntered(SceneLoadEnteredMessage message)
        {
            // The clock system survives collection changes. A finished shift must therefore be
            // reset when the player starts the next gameplay collection, otherwise IsShiftOver
            // remains true and the second run is frozen at the previous closing time.
            if (SystemsUtility.TryGetSystem<ISceneSystem>(out var sceneSystem) == false)
            {
                return;
            }

            isGameplayScene = sceneSystem.IsGameplayScene(message.Collection);
            if (isGameplayScene == false)
            {
                return;
            }

            if (isShiftOver)
            {
                StartNextDay();
                return;
            }

            hour = GetStartHour();
            lastPublishedMinute = Mathf.FloorToInt(hour * 60f);
            GameManager.Publish(new ShiftClockChangedMessage(day, hour));
        }

        public void OnUpdated(float deltaTime)
        {
            if (isShiftOver || isGameplayScene == false || gameplaySettings == false)
            {
                return;
            }

            hour += deltaTime * gameplaySettings.GameMinutesPerRealSecond / 60f;

            var endHour = gameplaySettings.ShiftEndHour;
            if (hour >= endHour)
            {
                hour = endHour;
                isShiftOver = true;

                PublishClock();
                GameManager.Publish(new ShiftClosedMessage(day));

                return;
            }

            var minute = Mathf.FloorToInt(hour * 60f);
            if (minute != lastPublishedMinute)
            {
                PublishClock();
            }
        }

        public void StartNextDay()
        {
            day = day >= int.MaxValue ? int.MaxValue : day + 1;
            hour = GetStartHour();
            isShiftOver = false;
            isGameplayScene = true;

            GameManager.Publish(new DayStartedMessage(day));
            PublishClock();
        }

        private float GetStartHour()
        {
            return gameplaySettings != false ? gameplaySettings.ShiftStartHour : 9f;
        }

        private void PublishClock()
        {
            lastPublishedMinute = Mathf.FloorToInt(hour * 60f);

            GameManager.Publish(new ShiftClockChangedMessage(day, hour));
        }
    }
}

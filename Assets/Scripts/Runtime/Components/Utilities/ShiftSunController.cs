using UABPetelnia.GGJ2025.Runtime.Settings;
using UABPetelnia.GGJ2025.Runtime.Systems.Clock;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Components.Utilities
{
    /// <summary>
    /// Двигает солнце по игровым часам смены: утро — низкое тёплое солнце, полдень — высокое и
    /// светлое, вечер — снова низкое и оранжевое. Источник времени — <see cref="ShiftClockSystem"/>,
    /// поэтому пауза (Time.timeScale = 0) останавливает и небо тоже.
    /// </summary>
    /// <remarks>
    /// Вешается на направленный свет сцены. Часы публикуют <see cref="ShiftClockChangedMessage"/>
    /// раз в игровую минуту, так что пересчёт дешёвый.
    /// </remarks>
    [DisallowMultipleComponent]
    internal sealed class ShiftSunController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField]
        private GameplaySettings gameplaySettings;

        [Header("Arc")]
        [Tooltip("Высота солнца в градусах на открытии смены.")]
        [Min(0f)]
        [SerializeField]
        private float morningElevation = 16f;

        [Tooltip("Высота солнца в градусах в середине смены.")]
        [Min(5f)]
        [SerializeField]
        private float noonElevation = 52f;

        [Tooltip("Высота солнца в градусах на закрытии смены.")]
        [Min(0f)]
        [SerializeField]
        private float eveningElevation = 12f;

        [Tooltip("На сколько градусов солнце уходит по азимуту за смену (от -половины до +половины).")]
        [Range(0f, 180f)]
        [SerializeField]
        private float azimuthSpan = 130f;

        [Header("Light")]
        [SerializeField]
        private float morningIntensity = 1.9f;

        [SerializeField]
        private float noonIntensity = 2.3f;

        [SerializeField]
        private float eveningIntensity = 1.6f;

        [SerializeField]
        private Color morningColor = new(1f, 0.80f, 0.60f);

        [SerializeField]
        private Color noonColor = new(1f, 0.97f, 0.90f);

        [SerializeField]
        private Color eveningColor = new(1f, 0.60f, 0.36f);

        private Light sun;
        private float baseYaw;
        private bool isSubscribed;
        private float appliedHour = float.MinValue;

        private void OnEnable()
        {
            sun = GetComponent<Light>();
            baseYaw = transform.eulerAngles.y;

            isSubscribed = SystemsUtility.TryAddListener<ShiftClockChangedMessage>(OnClockChanged);

            // Часы могли публиковать время до нашей подписки: применяем текущий час сразу.
            if (SystemsUtility.TryGetSystem(out IShiftClockSystem clock))
            {
                Apply(clock.Hour);
            }
            else
            {
                Apply(gameplaySettings != false ? gameplaySettings.ShiftStartHour : 9f);
            }
        }

        private void OnDisable()
        {
            if (isSubscribed == false)
            {
                return;
            }

            SystemsUtility.TryRemoveListener<ShiftClockChangedMessage>(OnClockChanged);
            isSubscribed = false;
        }

        private void OnClockChanged(ShiftClockChangedMessage message)
        {
            Apply(message.Hour);
        }

        private void Apply(float hour)
        {
            if (Mathf.Approximately(hour, appliedHour))
            {
                return;
            }

            appliedHour = hour;

            var start = gameplaySettings != false ? gameplaySettings.ShiftStartHour : 9f;
            var end = gameplaySettings != false ? gameplaySettings.ShiftEndHour : 20f;
            var t = Mathf.Clamp01((hour - start) / Mathf.Max(0.01f, end - start));

            // Первая половина смены: утро -> полдень, вторая: полдень -> вечер.
            var first = Mathf.Clamp01(t * 2f);
            var second = Mathf.Clamp01(t * 2f - 1f);

            var elevation = Mathf.Lerp(
                Mathf.Lerp(morningElevation, noonElevation, first),
                eveningElevation,
                second
            );

            var intensity = Mathf.Lerp(
                Mathf.Lerp(morningIntensity, noonIntensity, first),
                eveningIntensity,
                second
            );

            var color = Color.Lerp(
                Color.Lerp(morningColor, noonColor, first),
                eveningColor,
                second
            );

            var yaw = baseYaw - azimuthSpan * 0.5f + azimuthSpan * t;

            transform.rotation = Quaternion.Euler(elevation, yaw, 0f);

            if (sun != false)
            {
                sun.intensity = intensity;
                sun.color = color;
            }
        }
    }
}

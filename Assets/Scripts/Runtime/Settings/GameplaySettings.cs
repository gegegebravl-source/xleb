using System.Collections.Generic;
using UABPetelnia.GGJ2025.Runtime.Constants;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Settings
{
    [CreateAssetMenu(
        fileName = CreateAssetMenuConstants.BaseFileName + nameof(GameplaySettings),
        menuName = CreateAssetMenuConstants.BaseMenuName + "/Gameplay Settings",
        order = CreateAssetMenuConstants.BaseOrder
    )]
    internal sealed class GameplaySettings : ScriptableObject
    {
        [Header("Entities")]
        [SerializeField]
        private List<ShopperData> availableShoppers;

        [SerializeField]
        private List<ItemData> availableItems;

        [Header("Durations")]
        [Min(0f)]
        [SerializeField]
        private Vector2 spawnDelayRange = new(1f, 3f);

        [Min(0f)]
        [SerializeField]
        private Vector2 rantDurationRange = new(1.5f, 3f);

        [Tooltip("How long a shopper waits at the counter before losing their temper.")]
        [Min(1f)]
        [SerializeField]
        private float patienceDurationSeconds = 20f;

        [Header("Shift clock")]
        [Tooltip("Час открытия ларька.")]
        [Range(0f, 23f)]
        [SerializeField]
        private float shiftStartHour = 9f;

        [Tooltip("Час закрытия: на нём смена заканчивается и показываются итоги дня.")]
        [Range(1f, 24f)]
        [SerializeField]
        private float shiftEndHour = 20f;

        [Tooltip("Сколько игровых минут проходит за одну реальную секунду. При 2.0 смена 09:00–20:00 длится 5.5 минут.")]
        [Min(0.1f)]
        [SerializeField]
        private float gameMinutesPerRealSecond = 2f;

        [Header("Products on shelves")]
        [Tooltip("Общий множитель размера товара на полке: 1 — как в ассете, 1.6 — крупнее и заметнее.")]
        [Range(0.2f, 4f)]
        [SerializeField]
        private float productSizeMultiplier = 1.7f;

        [Tooltip("Минимальная высота товара в метрах: мелочь вроде жвачки иначе почти не видна.")]
        [Min(0.05f)]
        [SerializeField]
        private float minProductHeight = 0.26f;

        [Tooltip("Максимальная высота товара в метрах: крупные не должны заслонять всю полку.")]
        [Min(0.05f)]
        [SerializeField]
        private float maxProductHeight = 0.52f;

        public IReadOnlyCollection<ShopperData> AvailableShoppers => availableShoppers;

        public IReadOnlyCollection<ItemData> AvailableItems => availableItems;

        public float SpawnDelaySeconds => Random.Range(spawnDelayRange.x, spawnDelayRange.y);

        public float RantDurationSeconds => Random.Range(rantDurationRange.x, rantDurationRange.y);

        /// <summary>
        /// How long the shopper stands at the counter waiting for the ordered product before
        /// giving up. Without this a sale that cannot be completed would stall the whole loop.
        /// </summary>
        public float PatienceDurationSeconds => patienceDurationSeconds;

        /// <summary>Час открытия ларька.</summary>
        public float ShiftStartHour => shiftStartHour;

        /// <summary>Час закрытия: на нём смена заканчивается.</summary>
        public float ShiftEndHour => shiftEndHour;

        /// <summary>Сколько игровых минут проходит за одну реальную секунду.</summary>
        public float GameMinutesPerRealSecond => gameMinutesPerRealSecond;

        /// <summary>Общий множитель размера товара на полке.</summary>
        public float ProductSizeMultiplier => productSizeMultiplier;

        /// <summary>Минимальная высота товара в метрах.</summary>
        public float MinProductHeight => minProductHeight;

        /// <summary>Максимальная высота товара в метрах.</summary>
        public float MaxProductHeight => maxProductHeight;

        /// <summary>
        /// Высота товара на полке: размер из ассета, умноженный на общий множитель и
        /// зажатый в границы, чтобы ни мелочь, ни гигант не ломали вид полки.
        /// </summary>
        public float ResolveProductHeight(float displayHeight)
        {
            var scaled = displayHeight * productSizeMultiplier;

            return Mathf.Clamp(scaled, minProductHeight, Mathf.Max(minProductHeight, maxProductHeight));
        }
    }
}

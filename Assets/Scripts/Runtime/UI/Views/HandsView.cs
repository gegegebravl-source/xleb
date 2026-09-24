using System.Collections.Generic;
using CHARK.SimpleUI;
using TMPro;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace UABPetelnia.GGJ2025.Runtime.UI.Views
{
    /// <summary>
    /// Обе руки игрока: два квадратных слота внизу экрана. Показывают, что игрок несёт,
    /// чтобы товар можно было отдать покупателю, не глядя в инвентарь.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class HandsView : View
    {
        public const int DefaultSlotCount = 2;

        /// <summary>
        /// Один квадратный слот: рамка, иконка предмета и подпись с кнопкой.
        /// </summary>
        [System.Serializable]
        internal sealed class Slot
        {
            [Tooltip("Квадратная рамка слота. Перекрашивается, когда рука занята.")]
            public Image Frame;

            [Tooltip("Иконка предмета на слоте. Скрывается, когда рука пуста.")]
            public Image Icon;

            [Tooltip("Подпись с кнопкой («1», «2»). Может быть пустой.")]
            public TMP_Text KeyLabel;
        }

        #region Serialized

        [Header("Slots")]
        [Tooltip("Слоты рук по порядку: сначала первая рука, потом вторая.")]
        [SerializeField]
        private Slot[] slots = new Slot[DefaultSlotCount];

        [Header("Style")]
        [SerializeField]
        private Color emptyFrameColor = new(0.28f, 0.16f, 0.09f, 0.55f);

        [SerializeField]
        private Color filledFrameColor = new(0.99f, 0.80f, 0.36f, 1f);

        [Tooltip("Рамка активного слота (переключается клавишами 1/2) — перекрывает остальные цвета.")]
        [SerializeField]
        private Color selectedFrameColor = new(1f, 1f, 1f, 1f);

        [SerializeField]
        private Color emptyKeyColor = new(0.96f, 0.90f, 0.78f, 0.45f);

        [SerializeField]
        private Color filledKeyColor = new(1f, 1f, 1f, 1f);

        [Header("Hint")]
        [Tooltip("Подпись под слотами: что делает кнопка. Может быть пустой.")]
        [SerializeField]
        private TMP_Text hintLabel;

        [SerializeField]
        private string emptyHint = string.Empty;

        [SerializeField]
        private string holdingHint = "1/2 — достать или убрать · E — отдать или поставить";

        #endregion

        #region Cached state

        // Спрайты создаются в рантайме из текстур предметов (ItemData хранит Texture2D),
        // поэтому их надо освобождать вместе с вью.
        private readonly Dictionary<Texture2D, Sprite> runtimeSprites = new();
        private readonly List<Texture2D> runtimeSpriteKeys = new();

        private readonly ItemData[] lastItems = new ItemData[DefaultSlotCount];
        private int selectedSlot;

        #endregion

        public int SlotCount => slots?.Length ?? 0;

        /// <summary>Индекс подсвеченного слота (0..количество слотов).</summary>
        public int SelectedSlot => selectedSlot;

        /// <summary>
        /// Подсветить слот как активный (клавиши 1/2). Из активного слота товар
        /// первым уходит покупателю.
        /// </summary>
        public void SetSelectedSlot(int index)
        {
            var clamped = Mathf.Clamp(index, 0, SlotCount - 1);
            if (clamped == selectedSlot)
            {
                return;
            }

            selectedSlot = clamped;
            RefreshSlots();
        }

        /// <summary>
        /// Показать содержимое рук. Всё, что не влезло в слоты, игнорируется.
        /// </summary>
        public void SetItems(IReadOnlyList<ItemData> items)
        {
            var count = items?.Count ?? 0;
            var isHoldingSomething = count > 0;

            for (var index = 0; index < SlotCount; index++)
            {
                var item = index < count ? items[index] : default;
                lastItems[index] = item;
                ApplySlot(index, item);
            }

            if (hintLabel != null)
            {
                hintLabel.text = isHoldingSomething ? holdingHint : emptyHint;
            }
        }

        /// <summary>Перекрасить слоты по последнему состоянию.</summary>
        private void RefreshSlots()
        {
            for (var index = 0; index < SlotCount; index++)
            {
                ApplySlot(index, index < lastItems.Length ? lastItems[index] : default);
            }
        }

        /// <summary>
        /// Очистить все руки.
        /// </summary>
        public void Clear() => SetItems(default);

        private void ApplySlot(int index, ItemData item)
        {
            var slot = slots[index];
            if (slot == null)
            {
                return;
            }

            var isFilled = item != false;
            var isSelected = index == selectedSlot;

            if (slot.Frame != null)
            {
                slot.Frame.color = isSelected
                    ? selectedFrameColor
                    : isFilled ? filledFrameColor : emptyFrameColor;
            }

            if (slot.Icon != null)
            {
                slot.Icon.sprite = isFilled ? GetSprite(item.Image) : default;
                slot.Icon.color = isFilled ? Color.white : new Color(1f, 1f, 1f, 0f);
                slot.Icon.enabled = isFilled;
            }

            if (slot.KeyLabel != null)
            {
                slot.KeyLabel.color = isSelected
                    ? selectedFrameColor
                    : isFilled ? filledKeyColor : emptyKeyColor;
            }
        }

        /// <summary>
        /// ItemData хранит текстуру, а слоту нужен спрайт: оборачиваем её один раз и
        /// переиспользуем, чтобы смена рук не аллоцировала память каждый кадр.
        /// </summary>
        private Sprite GetSprite(Texture2D texture)
        {
            if (texture == false)
            {
                return default;
            }

            if (runtimeSprites.TryGetValue(texture, out var sprite) && sprite != false)
            {
                return sprite;
            }

            sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );

            runtimeSprites[texture] = sprite;
            runtimeSpriteKeys.Add(texture);

            return sprite;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            foreach (var texture in runtimeSpriteKeys)
            {
                if (runtimeSprites.TryGetValue(texture, out var sprite) && sprite != false)
                {
                    Destroy(sprite);
                }
            }

            runtimeSprites.Clear();
            runtimeSpriteKeys.Clear();
        }
    }
}

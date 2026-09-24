using System;
using TMPro;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace UABPetelnia.GGJ2025.Runtime.UI.Views
{
    /// <summary>
    /// Одна строка меню коробки: иконка товара, название, остаток и кнопка «Взять».
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class DeliveryBoxRowView : MonoBehaviour
    {
        [SerializeField] private RawImage icon;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text count;
        [SerializeField] private Button takeButton;
        [SerializeField] private TMP_Text takeLabel;

        private ItemData item;
        private Action<ItemData> takeClicked;

        public ItemData Item => item;

        /// <summary>Привязать строку к товару и обработчику кнопки «Взять».</summary>
        public void Initialize(ItemData data, int quantity, Action<ItemData> onTakeClicked)
        {
            item = data;
            takeClicked = onTakeClicked;

            if (icon != null)
            {
                icon.texture = data != null ? data.Image : null;
            }

            if (title != null)
            {
                title.text = data != null ? DeliveryRowView.Prettify(data.Id) : "—";
            }

            if (count != null)
            {
                count.text = "×" + quantity;
            }

            if (takeLabel != null)
            {
                takeLabel.text = "Взять";
            }

            if (takeButton != null)
            {
                takeButton.onClick.RemoveListener(OnTakeClicked);
                takeButton.onClick.AddListener(OnTakeClicked);
            }
        }

        /// <summary>Серым, когда в руках нет свободного слота.</summary>
        public void SetTakeAvailable(bool isAvailable)
        {
            if (takeButton != null)
            {
                takeButton.interactable = isAvailable;
            }
        }

        private void OnDisable()
        {
            if (takeButton != null)
            {
                takeButton.onClick.RemoveListener(OnTakeClicked);
            }
        }

        private void OnTakeClicked()
        {
            takeClicked?.Invoke(item);
        }
    }
}

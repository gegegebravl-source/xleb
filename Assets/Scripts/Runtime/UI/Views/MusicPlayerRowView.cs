using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UABPetelnia.GGJ2025.Runtime.UI.Views
{
    /// <summary>
    /// Строка списка песен в меню плеера: кнопка с названием трека и подсветкой выбранного.
    /// ВАЖНО: класс обязан жить в отдельном файле — в подчинённом классе внутри чужого файла
    /// Unity не записывает m_Script в ассет, и ссылка на компонент теряется.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class MusicPlayerRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        private int index;
        private Action<int> onClicked;
        private bool isSelected;

        private static readonly Color32 NormalColor = new(247, 237, 209, 255);   // Plank
        private static readonly Color32 SelectedColor = new(252, 204, 92, 255);  // Butter

        public void Initialize(int songIndex, string title, Action<int> callback)
        {
            index = songIndex;
            onClicked = callback;

            if (label != null)
            {
                label.text = title;
            }

            if (TryGetComponent<Button>(out var button) == false)
            {
                return;
            }

            button.onClick.RemoveListener(OnClick);
            button.onClick.AddListener(OnClick);
        }

        public void SetSelected(bool selected)
        {
            if (isSelected == selected)
            {
                return;
            }

            isSelected = selected;

            if (TryGetComponent<Button>(out var button) == false)
            {
                return;
            }

            var colors = button.colors;
            colors.normalColor = selected ? SelectedColor : NormalColor;
            colors.selectedColor = colors.normalColor;
            colors.highlightedColor = selected ? SelectedColor : NormalColor;
            button.colors = colors;

            if (button.targetGraphic != null)
            {
                button.targetGraphic.color = Color.white;
            }

            if (label != null)
            {
                label.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        private void OnClick()
        {
            onClicked?.Invoke(index);
        }
    }
}

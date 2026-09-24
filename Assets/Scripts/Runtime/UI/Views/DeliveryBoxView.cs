using System;
using System.Collections.Generic;
using CHARK.SimpleUI;
using TMPro;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace UABPetelnia.GGJ2025.Runtime.UI.Views
{
    /// <summary>
    /// Меню коробки с приехавшим товаром: список позиций и кнопки «Взять», которые кладут
    /// товар сразу в свободный слот рук.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class DeliveryBoxView : View
    {
        #region Serialized

        [Header("Content")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private RectTransform rowsContent;
        [SerializeField] private DeliveryBoxRowView rowTemplate;
        [SerializeField] private TMP_Text emptyText;

        [Header("Footer")]
        [SerializeField] private Button closeButton;

        #endregion

        #region Events

        public event Action OnCloseClicked;

        public event Action<ItemData> OnTakeClicked;

        #endregion

        #region Cached state

        private readonly List<DeliveryBoxRowView> rows = new();

        #endregion

        #region Public API

        /// <summary>
        /// Показать содержимое коробки. <paramref name="hasFreeSlot"/> гасит кнопки,
        /// когда обе руки заняты.
        /// </summary>
        public void SetContents(IReadOnlyList<ItemData> items, IReadOnlyList<int> counts, bool hasFreeSlot)
        {
            ClearRows();

            if (rowsContent == null || rowTemplate == null || items == null)
            {
                return;
            }

            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(items.Count <= 0);
            }

            rowTemplate.gameObject.SetActive(false);

            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                if (item == false)
                {
                    continue;
                }

                var row = Instantiate(rowTemplate, rowsContent);
                row.gameObject.SetActive(true);
                row.Initialize(item, index < counts.Count ? counts[index] : 0, OnTakeClickedInternal);
                row.SetTakeAvailable(hasFreeSlot);

                rows.Add(row);
            }
        }

        public void SetStatus(string status)
        {
            if (hintText != null && string.IsNullOrEmpty(status) == false)
            {
                hintText.text = status;
            }
        }

        public void SetHint(string hint)
        {
            if (hintText != null)
            {
                hintText.text = hint;
            }
        }

        #endregion

        #region Unity lifecycle

        protected override void OnEnable()
        {
            base.OnEnable();

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(OnCloseClickedInternal);
                closeButton.onClick.AddListener(OnCloseClickedInternal);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(OnCloseClickedInternal);
            }
        }

        #endregion

        #region Helpers

        private void ClearRows()
        {
            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                if (row != null)
                {
                    Destroy(row.gameObject);
                }
            }

            rows.Clear();
        }

        private void OnCloseClickedInternal()
        {
            OnCloseClicked?.Invoke();
        }

        private void OnTakeClickedInternal(ItemData item)
        {
            OnTakeClicked?.Invoke(item);
        }

        #endregion
    }
}

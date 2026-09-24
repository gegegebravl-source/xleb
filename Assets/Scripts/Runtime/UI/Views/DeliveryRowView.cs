using System;
using TMPro;
using UABPetelnia.GGJ2025.Runtime.Systems.Shop;
using UnityEngine;
using UnityEngine.UI;

namespace UABPetelnia.GGJ2025.Runtime.UI.Views
{
    /// <summary>
    /// Одна строка каталога в панели заказов: иконка, название, остаток на складе,
    /// цена закупки и кнопка «+1». Инстанцируется из шаблона <see cref="DeliveryView"/>.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class DeliveryRowView : MonoBehaviour
    {
        #region Serialized — Content

        [SerializeField] private RawImage icon;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text details;

        #endregion

        #region Serialized — Order button

        [SerializeField] private Button   orderButton;
        [SerializeField] private TMP_Text orderButtonLabel;

        #endregion

        #region Serialized — Behaviour

        [Header("Behaviour")]
        [Tooltip("Применить Warm Bread тему при первом включении строки. " +
                 "Если строки инстанцируются из уже темизированного шаблона — можно выключить.")]
        [SerializeField] private bool applyRuntimeTheme = true;

        [Tooltip("Добавить кнопке заказа hover/нажатие масштаб (UI juice).")]
        [SerializeField] private bool addButtonJuice = true;

        #endregion

        #region Cached state

        private ShopProduct product;
        private Action<ShopProduct> orderClicked;
        private bool themeApplied;
        private bool polishApplied;
        private bool listenersRegistered;

        /// <summary>Название + id в нижнем регистре — для поиска по каталогу.</summary>
        private string searchKey = string.Empty;

        #endregion

        #region Public API

        /// <summary>
        /// Товар, который сейчас отображает эта строка. Может быть <c>null</c>,
        /// если <see cref="Initialize"/> ещё не вызывали.
        /// </summary>
        public ShopProduct Product => product;

        /// <summary>
        /// Строка для поиска (название и id в нижнем регистре). Наполняется в <see cref="Refresh"/>.
        /// </summary>
        public string SearchKey => searchKey;

        #endregion

        #region Unity lifecycle

        private void OnEnable()
        {
            TryApplyTheme();
            TryApplyPolish();
            RegisterListeners();
        }

        private void OnDisable()
        {
            UnregisterListeners();
        }

        private void OnDestroy()
        {
            UnregisterListeners();
            product = null;
            orderClicked = null;
        }

        #endregion

        #region Public API — Initialize & Refresh

        /// <summary>
        /// Привязать строку к товару и обработчику клика. Безопасно вызывать повторно
        /// (старый обработчик снимается автоматически).
        /// </summary>
        public void Initialize(ShopProduct shopProduct, Action<ShopProduct> onOrderClicked)
        {
            product = shopProduct;
            orderClicked = onOrderClicked;

            // Если строка уже была активна и подписка не снялась — снимем и подпишемся заново.
            // Это нужно, потому что orderButton — тот же объект, а listener — уже новый.
            if (listenersRegistered)
            {
                UnregisterListeners();
                RegisterListeners();
            }

            Refresh();
        }

        /// <summary>
        /// Перечитать данные из <see cref="Product"/> и обновить текст/иконку.
        /// Безопасно вызывать каждый кадр.
        /// </summary>
        public void Refresh()
        {
            if (product == null)
            {
                ClearVisuals();
                return;
            }

            var item = product.Item;
            var displayName = item != null
                ? (item.HasDisplayName ? item.DisplayName : Prettify(item.Id))
                : "—";

            searchKey = item != null
                ? $"{displayName} {item.Id}".ToLowerInvariant()
                : string.Empty;

            if (icon != null)
            {
                icon.texture = item != null ? item.Image : null;
            }

            if (title != null)
            {
                title.text = displayName;
            }

            if (details != null)
            {
                var transit = product.InTransit > 0 ? $" (+{product.InTransit} в пути)" : string.Empty;
                details.text = $"остаток {product.Stock}{transit} · закуп {FormatRub(product.PurchasePrice)}";
            }

            if (orderButtonLabel != null)
            {
                // Кнопка кладёт товар в корзину, а не заказывает отдельной доставкой.
                orderButtonLabel.text = "В корзину";
            }
        }

        /// <summary>
        /// Единый формат цен во всём UI: копейки ассетов делятся на 100 и округляются
        /// до целых рублей ("10 руб.") — копейки в ларьке 2000-х не считали.
        /// </summary>
        public static string FormatRub(int cents)
        {
            return Mathf.RoundToInt(cents / 100f) + " руб.";
        }

        private void ClearVisuals()
        {
            if (icon  != null) icon.texture = null;
            if (title != null) title.text   = "—";
            if (details != null) details.text = string.Empty;
            if (orderButtonLabel != null) orderButtonLabel.text = string.Empty;
        }

        #endregion

        #region Prettify

        /// <summary>
        /// Asset id вида <c>Item_Alus</c> или <c>hleb_01</c> превращается в читаемую
        /// подпись вида <c>Alus</c> / <c>Hleb 01</c>.
        /// </summary>
        public static string Prettify(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return "—";
            }

            var text = id
                .Replace("Item_", string.Empty)
                .Replace('_', ' ')
                .Trim();

            if (text.Length == 0)
            {
                return "—";
            }

            return char.ToUpperInvariant(text[0]) + text[1..];
        }

        #endregion

        #region Theme & polish

        private void TryApplyTheme()
        {
            if (themeApplied || !applyRuntimeTheme) return;
            themeApplied = true;

            try
            {
                // Тот же Warm Bread стиль, что и у остальных панелей.
                WarmBreadRuntimeTheme.ApplyMainMenuTheme(this);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[DeliveryRowView] Theme application failed: {exception.Message}", this);
            }
        }

        private void TryApplyPolish()
        {
            if (polishApplied || !addButtonJuice) return;
            polishApplied = true;

            if (orderButton != null && !orderButton.TryGetComponent<MenuButtonAnimator>(out _))
            {
                orderButton.gameObject.AddComponent<MenuButtonAnimator>();
            }
        }

        #endregion

        #region Listener registration

        private void RegisterListeners()
        {
            if (orderButton == null) return;

            // Защита от двойной подписки при повторных OnEnable.
            orderButton.onClick.RemoveListener(OnOrderButtonClicked);
            orderButton.onClick.AddListener(OnOrderButtonClicked);

            listenersRegistered = true;
        }

        private void UnregisterListeners()
        {
            if (!listenersRegistered || orderButton == null)
            {
                listenersRegistered = false;
                return;
            }

            orderButton.onClick.RemoveListener(OnOrderButtonClicked);
            listenersRegistered = false;
        }

        private void OnOrderButtonClicked()
        {
            orderClicked?.Invoke(product);
        }

        #endregion
    }
}
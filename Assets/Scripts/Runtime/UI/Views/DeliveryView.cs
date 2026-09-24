using System;
using System.Collections.Generic;
using CHARK.SimpleUI;
using TMPro;
using UABPetelnia.GGJ2025.Runtime.Systems.Shop;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UABPetelnia.GGJ2025.Runtime.UI.Views
{
    /// <summary>
    /// Экран ПК на прилавке: закупка товара, список доставок в пути и остаток в кассе.
    /// Стилизуется Warm Bread темой и получает ту же UI-полировку, что главное меню.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class DeliveryView : View, ICancelHandler
    {
        #region Serialized — Header

        [Header("Header icons")]
        [SerializeField] private RawImage balanceIcon;
        [SerializeField] private RawImage ordersIcon;

        [Header("Header text")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text balanceText;
        [SerializeField] private TMP_Text ordersText;

        #endregion

        #region Serialized — Catalogue

        [Header("Catalogue")]
        [SerializeField] private RectTransform content;
        [SerializeField] private DeliveryRowView rowTemplate;

        [Tooltip("Поле поиска над списком: фильтрует строки по названию. Может быть пустым.")]
        [SerializeField] private TMP_InputField searchField;

        #endregion

        #region Serialized — Deliveries

        [Header("Deliveries on the road")]
        [SerializeField] private RectTransform orderLinesContent;
        [SerializeField] private TMP_Text orderLineTemplate;
        [SerializeField] private TMP_Text emptyOrdersText;
        [SerializeField] private Button orderEverythingButton;

        #endregion

        #region Serialized — Footer

        [Header("Footer")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private Button clearCartButton;
        [SerializeField] private Button closeButton;

        #endregion

        #region Serialized — Content strings

        [Header("Content")]
        [SerializeField] private string titleTextValue  = "ПК — заказы и доставка";
        [SerializeField] private string hintTextValue   = "Tab — закрыть · кнопки справа — заказать товар";
        [SerializeField] private string emptyOrdersValue = "Доставок в пути нет";

        #endregion

        #region Serialized — Behaviour

        [Header("Behaviour")]
        [Tooltip("Применять Warm Bread тему при первом включении view.")]
        [SerializeField] private bool applyRuntimeTheme = true;

        [Tooltip("Плавное появление панели (fade + slide).")]
        [SerializeField] private bool animatePanel = true;

        [Tooltip("Добавить кнопкам hover/нажатие масштаб.")]
        [SerializeField] private bool addButtonJuice = true;

        [Tooltip("На какую кнопку ставить фокус при открытии.")]
        [SerializeField] private bool focusCloseOnOpen = true;

        #endregion

        #region Events

        public event Action OnCloseClicked;
        public event Action<ShopProduct> OnOrderClicked;
        public event Action OnOrderEverythingClicked;
        public event Action OnClearCartClicked;

        #endregion

        #region Public state

        /// <summary>
        /// <c>true</c>, когда панель на экране.
        /// </summary>
        public bool IsOpen => State is ViewVisibilityState.Showing or ViewVisibilityState.Shown;

        #endregion

        #region Cached state

        private readonly Dictionary<ShopProduct, DeliveryRowView> rows = new();
        private readonly List<TMP_Text> orderLines = new();
        private readonly List<ShopProduct> staleProductsBuffer = new();

        /// <summary>Текущий поисковый фильтр (как ввёл игрок, без нормализации).</summary>
        private string currentFilter = string.Empty;

        private bool themeApplied;
        private bool polishApplied;

        #endregion

        #region Unity lifecycle

        protected override void Awake()
        {
            base.Awake();

            if (titleText != null) titleText.text = titleTextValue;
            if (hintText  != null) hintText.text  = hintTextValue;

            if (rowTemplate       != null) rowTemplate.gameObject.SetActive(false);
            if (orderLineTemplate != null) orderLineTemplate.gameObject.SetActive(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            TryApplyTheme();
            TryApplyPolish();
            RegisterListeners();
            RegisterSearch();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnregisterListeners();
            UnregisterSearch();
        }

        protected override void OnViewShowEntered()
        {
            base.OnViewShowEntered();

            // Каждое открытие — чистый поиск: игрок снова видит весь каталог.
            // SetTextWithoutNotify не зажигает onValueChanged, поэтому сбрасываем
            // и сам фильтр вручную.
            currentFilter = string.Empty;
            if (searchField != null)
            {
                searchField.SetTextWithoutNotify(string.Empty);
            }

            ApplyFilter(string.Empty);

            if (focusCloseOnOpen)
            {
                Select(closeButton);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            rows.Clear();
            orderLines.Clear();
            staleProductsBuffer.Clear();
        }

        #endregion

        #region Cancel handling

        void ICancelHandler.OnCancel(BaseEventData eventData)
        {
            // Esc/Cancel = закрыть панель, если она открыта.
            if (!IsOpen) return;
            OnCloseClicked?.Invoke();
        }

        #endregion

        #region Public API — Header & status

        public void SetHeader(int balanceCents, int orderCount, float secondsToNextArrival)
        {
            SetHeader(balanceCents, orderCount, secondsToNextArrival, 0, 0);
        }

        /// <summary>
        /// Шапка панели: баланс, корзина и доставки в пути.
        /// </summary>
        public void SetHeader(
            int balanceCents,
            int orderCount,
            float secondsToNextArrival,
            int cartQuantity,
            int cartCost
        )
        {
            if (balanceText != null)
            {
                // Единый формат цен: целые рубли без копеек (см. DeliveryRowView.FormatRub).
                balanceText.text = DeliveryRowView.FormatRub(balanceCents);
            }

            if (ordersText != null)
            {
                var cart = cartQuantity > 0
                    ? $"Корзина: {cartQuantity} шт · {DeliveryRowView.FormatRub(cartCost)} · "
                    : string.Empty;

                ordersText.text = orderCount <= 0
                    ? cart + emptyOrdersValue
                    : cart + $"В пути: {orderCount} · ближайшая через {Mathf.CeilToInt(secondsToNextArrival)} с";
            }

            if (clearCartButton != null)
            {
                clearCartButton.interactable = cartQuantity > 0;
            }
        }

        public void SetStatus(string status)
        {
            if (statusText != null && status != null)
            {
                statusText.text = status;
            }
        }

        #endregion

        #region Public API — Catalogue

        /// <summary>
        /// Показать каталог. Первый раз создаёт строку для товара, повторные вызовы
        /// только обновляют существующие и удаляют те, что пропали из каталога.
        /// </summary>
        public void SetProducts(IReadOnlyList<ShopProduct> products)
        {
            if (content == null || rowTemplate == null || products == null)
            {
                return;
            }

            // 1) Собрать список уже отрисованных товаров, которых больше нет в каталоге.
            staleProductsBuffer.Clear();
            foreach (var kvp in rows)
            {
                if (!ContainsProduct(products, kvp.Key))
                {
                    staleProductsBuffer.Add(kvp.Key);
                }
            }

            // 2) Удалить призраков — иначе они висят до конца смены.
            foreach (var product in staleProductsBuffer)
            {
                if (rows.TryGetValue(product, out var staleRow) && staleRow != null)
                {
                    Destroy(staleRow.gameObject);
                }
                rows.Remove(product);
            }

            // 3) Обновить существующие + создать новые.
            for (var i = 0; i < products.Count; i++)
            {
                var product = products[i];
                if (product == null) continue;

                if (rows.TryGetValue(product, out var existing) && existing != null)
                {
                    existing.Refresh();
                    continue;
                }

                var row = Instantiate(rowTemplate, content);
                row.name = $"Row_{(product.Item != null ? product.Item.Id : "Item")}";
                row.gameObject.SetActive(true);
                row.Initialize(product, OnRowOrderClicked);

                rows[product] = row;
            }

            // Новые строки тоже должны respect текущий поиск.
            ApplyFilter(currentFilter);
        }

        private static bool ContainsProduct(IReadOnlyList<ShopProduct> products, ShopProduct target)
        {
            for (var i = 0; i < products.Count; i++)
            {
                if (ReferenceEquals(products[i], target))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Search

        private void RegisterSearch()
        {
            if (searchField == null)
            {
                return;
            }

            searchField.onValueChanged.RemoveListener(ApplyFilter);
            searchField.onValueChanged.AddListener(ApplyFilter);
        }

        private void UnregisterSearch()
        {
            if (searchField == null)
            {
                return;
            }

            searchField.onValueChanged.RemoveListener(ApplyFilter);
        }

        /// <summary>
        /// Показать только те строки, чьё название содержит подстроку поиска.
        /// Пустой запрос показывает весь каталог.
        /// </summary>
        private void ApplyFilter(string query)
        {
            currentFilter = query ?? string.Empty;

            var normalized = currentFilter.Trim().ToLowerInvariant();

            foreach (var kvp in rows)
            {
                var row = kvp.Value;
                if (row == null)
                {
                    continue;
                }

                var matches = normalized.Length == 0
                    || row.SearchKey.Contains(normalized);

                if (row.gameObject.activeSelf != matches)
                {
                    row.gameObject.SetActive(matches);
                }
            }
        }

        #endregion

        #region Public API — Orders on the road

        /// <summary>
        /// Пересобрать список "в пути": одна строка на каждую оплаченную доставку
        /// с живым счётчиком времени.
        /// </summary>
        public void SetOrders(IReadOnlyList<ShopDeliveryOrder> orders)
        {
            if (orderLinesContent == null || orderLineTemplate == null || orders == null)
            {
                return;
            }

            foreach (var line in orderLines)
            {
                if (line != null)
                {
                    Destroy(line.gameObject);
                }
            }
            orderLines.Clear();

            if (emptyOrdersText != null)
            {
                emptyOrdersText.gameObject.SetActive(orders.Count <= 0);
            }

            orderLineTemplate.gameObject.SetActive(false);

            for (var i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                if (order == null) continue;

                var line = Instantiate(orderLineTemplate, orderLinesContent);
                line.gameObject.SetActive(true);
                line.text =
                    order.Describe()
                    + $" — через {Mathf.CeilToInt(order.SecondsLeft)} с";

                orderLines.Add(line);
            }
        }

        #endregion

        #region Listener registration

        private void RegisterListeners()
        {
            AddListener(closeButton,           HandleCloseClicked);
            AddListener(orderEverythingButton, HandleOrderEverythingClicked);
            AddListener(clearCartButton,       HandleClearCartClicked);
        }

        private void UnregisterListeners()
        {
            RemoveListener(closeButton,           HandleCloseClicked);
            RemoveListener(orderEverythingButton, HandleOrderEverythingClicked);
            RemoveListener(clearCartButton,       HandleClearCartClicked);
        }

        #endregion

        #region Theme & polish

        private void TryApplyTheme()
        {
            if (themeApplied || !applyRuntimeTheme) return;
            themeApplied = true;

            try
            {
                // Тот же Warm Bread стиль, что и у главного меню.
                WarmBreadRuntimeTheme.ApplyMainMenuTheme(this);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[DeliveryView] Theme application failed: {exception.Message}", this);
            }
        }

        private void TryApplyPolish()
        {
            if (polishApplied) return;
            polishApplied = true;

            if (animatePanel && !TryGetComponent<MenuPanelFader>(out _))
            {
                gameObject.AddComponent<MenuPanelFader>();
            }

            if (addButtonJuice)
            {
                EnsureButtonAnimator(closeButton);
                EnsureButtonAnimator(orderEverythingButton);
            }
        }

        private static void EnsureButtonAnimator(Button button)
        {
            if (button == null) return;
            if (!button.TryGetComponent<MenuButtonAnimator>(out _))
                button.gameObject.AddComponent<MenuButtonAnimator>();
        }

        #endregion

        #region Helpers

        private static void AddListener(Button button, UnityEngine.Events.UnityAction listener)
        {
            if (button != null) button.onClick.AddListener(listener);
        }

        private static void RemoveListener(Button button, UnityEngine.Events.UnityAction listener)
        {
            if (button != null) button.onClick.RemoveListener(listener);
        }

        private static void Select(Selectable selectable)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null || selectable == null || !selectable.interactable) return;

            eventSystem.SetSelectedGameObject(selectable.gameObject);
        }

        #endregion

        #region Event handlers

        private void HandleCloseClicked()           => OnCloseClicked?.Invoke();
        private void HandleOrderEverythingClicked() => OnOrderEverythingClicked?.Invoke();
        private void HandleClearCartClicked()       => OnClearCartClicked?.Invoke();
        private void OnRowOrderClicked(ShopProduct product) => OnOrderClicked?.Invoke(product);

        #endregion
    }
}
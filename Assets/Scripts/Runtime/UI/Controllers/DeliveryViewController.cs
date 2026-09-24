using System.Collections.Generic;
using CHARK.GameManagement;
using CHARK.SimpleUI;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Systems.Cursors;
using UABPetelnia.GGJ2025.Runtime.Systems.Input;
using UABPetelnia.GGJ2025.Runtime.Systems.Pausing;
using UABPetelnia.GGJ2025.Runtime.Systems.Players;
using UABPetelnia.GGJ2025.Runtime.Systems.Products;
using UABPetelnia.GGJ2025.Runtime.Systems.Progress;
using UABPetelnia.GGJ2025.Runtime.Systems.Shop;
using UABPetelnia.GGJ2025.Runtime.UI.Views;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.UI.Controllers
{
    /// <summary>
    /// Управляет панелью заказов на ПК киоска: открывает её по кнопке заказов
    /// у прилавка, оформляет покупки и показывает доставки в пути.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class DeliveryViewController : ViewController<DeliveryView>
    {
        #region Serialized

        [Header("Orders")]
        [Min(1)]
        [SerializeField] private int orderQuantity = 1;

        [Header("Behaviour")]
        [Tooltip("Логировать пропущенные заказы (нет view / нет систем / null product).")]
        [SerializeField] private bool logSkips = true;

        #endregion

        #region Cached state — systems

        private IInputSystem   inputSystem;
        private IShopSystem    shopSystem;
        private IPlayerSystem  playerSystem;
        private IProductSystem productSystem;
        private ICursorSystem  cursorSystem;
        private IPauseSystem   pauseSystem;
        private IProgressSystem progressSystem;

        #endregion

        #region Cached state — collections

        // Переиспользуем один буфер, чтобы не аллоцировать List на каждый Refresh.
        private readonly List<ShopProduct> catalogueBuffer = new();
        private bool subscribed;

        #endregion

        #region Public state

        /// <summary>
        /// <c>true</c>, пока панель заказов на экране. Используется для заморозки игрока.
        /// </summary>
        public bool IsOpen =>
            View != null && ViewState is ViewVisibilityState.Showing or ViewVisibilityState.Shown;

        /// <summary>
        /// Сколько единиц товара заказывает одна кнопка «+1». По умолчанию — 1.
        /// </summary>
        public int OrderQuantity
        {
            get => orderQuantity;
            set => orderQuantity = Mathf.Max(1, value);
        }

        #endregion

        #region Unity lifecycle

        protected override void Awake()
        {
            base.Awake();

            SystemsUtility.TryGetSystem(out inputSystem);
            SystemsUtility.TryGetSystem(out shopSystem);
            SystemsUtility.TryGetSystem(out playerSystem);
            SystemsUtility.TryGetSystem(out productSystem);
            SystemsUtility.TryGetSystem(out cursorSystem);
            SystemsUtility.TryGetSystem(out pauseSystem);
            SystemsUtility.TryGetSystem(out progressSystem);

            if (shopSystem != null)
            {
                shopSystem.Changed += OnShopChanged;
            }

            // Меню паузы забирает курсор — панель заказов должна закрыться,
            // иначе она останется за паузой с мёртвым указателем.
            SystemsUtility.TryAddListener<GamePausedMessage>(OnGamePaused);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            SubscribeToView();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            UnsubscribeFromView();
        }

        private void OnDestroy()
        {
            if (shopSystem != null)
            {
                shopSystem.Changed -= OnShopChanged;
            }

            SystemsUtility.TryRemoveListener<GamePausedMessage>(OnGamePaused);

            catalogueBuffer.Clear();
        }

        private void Update()
        {
            if (inputSystem == null)
            {
                return;
            }

            // Меню паузы владеет экраном и курсором, пока открыто.
            if (pauseSystem != null && pauseSystem.IsPaused)
            {
                return;
            }

            if (inputSystem.IsOrdersPressedThisFrame)
            {
                TogglePanel();

                return;
            }

            // Клавиша взаимодействия (E) открывает панель, когда игрок навёл камеру на ПК
            // прилавка. Ждать её закрытия не нужно: закрыть можно той же клавишей через кнопку
            // или Escape на панели.
            if (IsOpen == false
                && inputSystem.IsInteractPressedThisFrame
                && DeliveryPcActor.TryFindAimed(Camera.main, out _))
            {
                OpenPanel();
            }

            // Пока панель открыта — обновляем шапку каждый кадр: таймер доставки
            // должен тикать, а не замирать до следующего события магазина.
            if (IsOpen)
            {
                RefreshHeader();
            }
        }

        #endregion

        #region Public API

        public void TogglePanel()
        {
            if (IsOpen)
            {
                ClosePanel();
                return;
            }

            OpenPanel();
        }

        /// <summary>
        /// Открыть панель, только если игрок стоит у прилавка.
        /// </summary>
        public void OpenPanel()
        {
            if (View == null || !TryGetPlayerPosition(out var playerPosition))
            {
                return;
            }

            // Открыть можно двумя способами: стоять у прилавка или навести камеру на сам ПК.
            var isNear = DeliveryPcActor.TryFindNearest(playerPosition, out _);
            var isAimed = DeliveryPcActor.TryFindAimed(Camera.main, out _);

            if (!isNear && !isAimed)
            {
                return;
            }

            View.Show();
        }

        public void ClosePanel()
        {
            if (!IsOpen)
            {
                return;
            }

            View.Hide();
        }

        /// <summary>
        /// Принудительно перечитать данные из системы магазина. Полезно, если
        /// система не шлёт <c>Changed</c>, а данные поменялись снаружи.
        /// </summary>
        public void RefreshIfOpen()
        {
            if (IsOpen)
            {
                Refresh();
            }
        }

        #endregion

        #region View subscription

        private void SubscribeToView()
        {
            if (subscribed) return;

            var view = View;
            if (view == null)
            {
                // View может появиться позже — при следующем OnEnable попробуем снова.
                return;
            }

            view.OnCloseClicked           += HandleCloseClicked;
            view.OnOrderClicked           += HandleOrderClicked;
            view.OnOrderEverythingClicked += HandleOrderEverythingClicked;
            view.OnClearCartClicked        += HandleClearCartClicked;

            view.OnShowEntered += HandleViewShowEntered;
            view.OnHideEntered += HandleViewHideEntered;

            subscribed = true;
        }

        private void UnsubscribeFromView()
        {
            if (!subscribed) return;

            var view = View;
            if (view != null)
            {
                view.OnCloseClicked           -= HandleCloseClicked;
                view.OnOrderClicked           -= HandleOrderClicked;
                view.OnOrderEverythingClicked -= HandleOrderEverythingClicked;
                view.OnClearCartClicked        -= HandleClearCartClicked;

                view.OnShowEntered -= HandleViewShowEntered;
                view.OnHideEntered -= HandleViewHideEntered;
            }

            subscribed = false;
        }

        #endregion

        #region Event handlers

        private void HandleCloseClicked() => ClosePanel();

        private void HandleClearCartClicked()
        {
            shopSystem?.ClearCart();
            View.SetStatus("Корзина очищена.");
            Refresh();
        }

        private void HandleViewShowEntered()
        {
            cursorSystem?.UnLockCursor();

            View.SetStatus(string.Empty);
            Refresh();
        }

        private void HandleViewHideEntered()
        {
            cursorSystem?.LockCursor();
        }

        private void OnGamePaused(GamePausedMessage message) => ClosePanel();

        private void OnShopChanged() => RefreshIfOpen();

        private void HandleOrderClicked(ShopProduct product)
        {
            if (product == null)
            {
                LogSkip("Попытка заказать null-товар.");
                return;
            }

            if (shopSystem == null)
            {
                LogSkip("ShopSystem недоступна, заказ невозможен.");
                return;
            }

            // Товар идёт в корзину, а не отдельной доставкой: один заказ — один курьер
            // и одна коробка со всеми позициями.
            if (shopSystem.TryAddToCart(product.Item, orderQuantity, out var error))
            {
                var label = DeliveryRowView.Prettify(product.Item?.Id);
                View.SetStatus($"В корзине: {label} — всего {shopSystem.CartTotalQuantity} шт.");
            }
            else
            {
                View.SetStatus(GetErrorMessage(error));
            }

            Refresh();
        }

        /// <summary>
        /// Оплатить корзину и вызвать курьера: один заказ — один курьер с коробкой.
        /// </summary>
        private void HandleOrderEverythingClicked()
        {
            if (shopSystem == null)
            {
                LogSkip("ShopSystem недоступна, заказ невозможен.");
                return;
            }

            // The footer action is deliberately not a second "submit" button: its Russian label
            // promises to order every item that is absent from the physical shelves. Existing cart
            // lines are kept, then all missing catalogue entries get one unit before payment.
            AddMissingProductsToCart();

            if (shopSystem.TrySubmitCart(out var error))
            {
                progressSystem?.NotifyOrderPlaced();
                View.SetStatus("Заказ оплачен: курьер уже в пути.");
            }
            else
            {
                View.SetStatus(GetErrorMessage(error));
            }

            Refresh();
        }

        private int AddMissingProductsToCart()
        {
            if (productSystem == null)
            {
                View.SetStatus("Состояние полок недоступно: заказ не сформирован.");
                return 0;
            }

            var added = 0;
            var products = shopSystem.Products;

            if (products == null)
            {
                return 0;
            }

            for (var index = 0; index < products.Count; index++)
            {
                var product = products[index];
                if (product == null || product.Item == false || productSystem.IsOnShelf(product.Item))
                {
                    continue;
                }

                var alreadyInCart = false;
                var cart = shopSystem.Cart;
                for (var lineIndex = 0; cart != null && lineIndex < cart.Count; lineIndex++)
                {
                    if (cart[lineIndex] != null && cart[lineIndex].Product == product)
                    {
                        alreadyInCart = true;
                        break;
                    }
                }

                if (alreadyInCart)
                {
                    continue;
                }

                if (shopSystem.TryAddToCart(product.Item, 1, out _))
                {
                    added++;
                }
            }

            return added;
        }

        #endregion

        #region Refresh

        private void Refresh()
        {
            if (shopSystem == null) return;

            var view = View;
            if (view == null) return;

            var products = shopSystem.Products;
            var orders   = shopSystem.Orders;

            catalogueBuffer.Clear();
            if (products != null)
            {
                catalogueBuffer.AddRange(products);
            }

            view.SetProducts(catalogueBuffer);
            view.SetOrders(orders);
            RefreshHeader();
        }

        private void RefreshHeader()
        {
            var view = View;

            if (view == null || shopSystem == null)
            {
                return;
            }

            view.SetHeader(
                balanceCents: shopSystem.Balance,
                orderCount: shopSystem.Orders?.Count ?? 0,
                secondsToNextArrival: shopSystem.SecondsToNextArrival,
                cartQuantity: shopSystem.CartTotalQuantity,
                cartCost: shopSystem.CartTotalCost
            );
        }

        private static float GetSecondsToNextArrival(IReadOnlyList<ShopDeliveryOrder> orders)
        {
            if (orders == null || orders.Count == 0)
            {
                return 0f;
            }

            var seconds = float.MaxValue;

            for (var i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                if (order == null) continue;

                if (order.SecondsLeft < seconds)
                {
                    seconds = order.SecondsLeft;
                }
            }

            return seconds == float.MaxValue ? 0f : seconds;
        }

        #endregion

        #region Helpers

        private static string GetErrorMessage(string error)
        {
            return error switch
            {
                "not_enough_money" => "В кассе недостаточно денег.",
                "unknown_product"  => "Такого товара нет в каталоге.",
                "no_player"        => "Игрок не найден.",
                "empty_cart"       => "Корзина пуста: сначала добавь товар.",
                "not_in_cart"      => "Этого товара нет в корзине.",
                "bad_quantity"     => "Неверное количество.",
                "bad_cost"         => "Стоимость заказа некорректна.",
                _                  => "Не удалось оформить заказ.",
            };
        }

        private bool TryGetPlayerPosition(out Vector3 position)
        {
            position = default;

            if (playerSystem == null)
            {
                LogSkip("PlayerSystem недоступна.");
                return false;
            }

            if (!playerSystem.TryGetPlayer(out var player))
            {
                LogSkip("Игрок не найден.");
                return false;
            }

            if (player is not Component component)
            {
                LogSkip("Player не является Component — позиция недоступна.");
                return false;
            }

            position = component.transform.position;
            return true;
        }

        private void LogSkip(string message)
        {
            if (!logSkips) return;
            Debug.LogWarning($"[DeliveryViewController] {message}", this);
        }

        #endregion
    }
}
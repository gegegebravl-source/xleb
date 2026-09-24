using System.Collections.Generic;
using CHARK.GameManagement;
using CHARK.SimpleUI;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UABPetelnia.GGJ2025.Runtime.Systems.Cursors;
using UABPetelnia.GGJ2025.Runtime.Systems.Pausing;
using UABPetelnia.GGJ2025.Runtime.Systems.Players;
using UABPetelnia.GGJ2025.Runtime.Systems.Shop;
using UABPetelnia.GGJ2025.Runtime.UI.Views;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.UI.Controllers
{
    /// <summary>
    /// Меню коробки с доставкой: открывается, когда игрок берёт коробку у курьера, и
    /// раскладывает товар из неё по слотам рук.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class DeliveryBoxViewController : ViewController<DeliveryBoxView>
    {
        private const string TakeHint =
            "Взять — предмет сразу в руку · 1/2 — достать или убрать · X — положить коробку";

        private IPlayerSystem playerSystem;
        private IShopSystem shopSystem;
        private ICursorSystem cursorSystem;
        private IPauseSystem pauseSystem;

        private DeliveryBoxActor box;
        private bool subscribed;

        private readonly List<ItemData> stackItems = new();
        private readonly List<int> stackCounts = new();

        /// <summary><c>true</c>, пока меню коробки на экране.</summary>
        public bool IsOpen => View != null && ViewState is ViewVisibilityState.Showing or ViewVisibilityState.Shown;

        protected override void Awake()
        {
            base.Awake();

            SystemsUtility.TryGetSystem(out playerSystem);
            SystemsUtility.TryGetSystem(out shopSystem);
            SystemsUtility.TryGetSystem(out cursorSystem);
            SystemsUtility.TryGetSystem(out pauseSystem);

            SystemsUtility.TryAddListener<GamePausedMessage>(OnGamePaused);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Subscribe();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            Unsubscribe();
        }

        private void OnDestroy()
        {
            SystemsUtility.TryRemoveListener<GamePausedMessage>(OnGamePaused);
        }

        #region Public API

        /// <summary>Открыть меню коробки.</summary>
        public void Open(DeliveryBoxActor target)
        {
            if (target == false)
            {
                return;
            }

            if (View == null)
            {
                return;
            }

            box = target;
            View.Show();
        }

        /// <summary>Закрыть меню коробки.</summary>
        public void Close()
        {
            if (IsOpen)
            {
                View.Hide();
            }
        }

        /// <summary>Открыть, если закрыто, и наоборот.</summary>
        public void Toggle(DeliveryBoxActor target)
        {
            if (IsOpen)
            {
                Close();

                return;
            }

            Open(target);
        }

        #endregion

        #region View subscription

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            var view = View;

            if (view == null)
            {
                return;
            }

            view.OnCloseClicked += Close;
            view.OnTakeClicked += OnTakeClicked;

            view.OnShowEntered += OnViewShowEntered;
            view.OnHideEntered += OnViewHideEntered;

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (subscribed == false)
            {
                return;
            }

            var view = View;

            if (view != null)
            {
                view.OnCloseClicked -= Close;
                view.OnTakeClicked -= OnTakeClicked;

                view.OnShowEntered -= OnViewShowEntered;
                view.OnHideEntered -= OnViewHideEntered;
            }

            subscribed = false;
        }

        private void OnViewShowEntered()
        {
            cursorSystem?.UnLockCursor();
            Refresh();
        }

        private void OnViewHideEntered()
        {
            cursorSystem?.LockCursor();
        }

        private void OnGamePaused(GamePausedMessage message)
        {
            Close();
        }

        #endregion

        #region Taking items

        private void OnTakeClicked(ItemData item)
        {
            var view = View;

            if (view == null || box == false || item == false)
            {
                return;
            }

            if (TryGetPlayer(out var player) == false)
            {
                return;
            }

            if (player.HasFreeCarrySlot == false)
            {
                view.SetStatus("Обе руки заняты: сначала выложи товар на полку.");

                return;
            }

            if (box.ContainsItem(item) == false)
            {
                view.SetStatus("Эта позиция уже забрана из коробки.");
                return;
            }

            // The box and the back-room ledger must move together. A malformed or stale box must
            // not let the player duplicate a delivery unit by taking it when the ledger is empty.
            if (shopSystem == null
                || shopSystem.TryGetProduct(item, out var stockProduct) == false
                || stockProduct.Stock <= 0)
            {
                view.SetStatus("Склад недоступен или этой позиции уже нет в учёте.");

                return;
            }

            // Reserve the ledger and the box before creating the physical hand item. If the
            // player prefab cannot spawn products, both reservations are rolled back.
            if (shopSystem.TryTakeFromStock(item) == false)
            {
                view.SetStatus("Этой позиции уже нет в учёте склада.");
                return;
            }

            if (box.TryRemoveItem(item) == false)
            {
                stockProduct.Stock++;
                view.SetStatus("Позиция уже была забрана.");
                return;
            }

            if (player.TryCarryItem(item) == false)
            {
                box.RestoreItem(item);
                stockProduct.Stock++;
                view.SetStatus("Не удалось создать товар в руках.");
                return;
            }

            if (box.IsEmpty)
            {
                var courier = box.GetComponentInParent<CourierActor>();
                if (courier != false)
                {
                    courier.OnBoxTaken();
                }

                box.Discard();
                box = default;

                Close();

                return;
            }

            Refresh();
        }

        #endregion

        #region Refresh

        private void Refresh()
        {
            var view = View;

            if (view == null)
            {
                return;
            }

            if (box == false)
            {
                view.SetContents(stackItems, stackCounts, hasFreeSlot: false);

                return;
            }

            box.CollectStacks(stackItems, stackCounts);

            var hasFreeSlot = TryGetPlayer(out var player) && player.HasFreeCarrySlot;

            view.SetContents(stackItems, stackCounts, hasFreeSlot);
            view.SetHint(TakeHint);
        }

        private bool TryGetPlayer(out IPlayerActor player)
        {
            player = default;

            if (playerSystem == null)
            {
                return false;
            }

            if (playerSystem.TryGetPlayer(out player) == false)
            {
                return false;
            }

            return player is Object unityPlayer && unityPlayer != false;
        }

        #endregion
    }
}

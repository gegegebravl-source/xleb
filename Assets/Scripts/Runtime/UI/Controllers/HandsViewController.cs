using System.Collections.Generic;
using CHARK.SimpleUI;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UABPetelnia.GGJ2025.Runtime.UI.Views;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.UI.Controllers
{
    /// <summary>
    /// Держит HUD рук в актуальном состоянии: игрок сообщает сюда, что он несёт, а вью
    /// раскладывает это по квадратным слотам.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class HandsViewController : ViewController<HandsView>
    {
        /// <summary>
        /// Показать текущее содержимое рук. Пустой список оставляет слоты пустыми.
        /// </summary>
        public void SetItems(IReadOnlyList<ItemData> items)
        {
            var view = View;

            if (view == null)
            {
                return;
            }

            view.SetItems(items);
        }

        public void Clear() => SetItems(default);

        /// <summary>
        /// Подсветить активный слот (клавиши 1/2). Из него товар первым уходит покупателю.
        /// </summary>
        public void SetSelectedSlot(int index)
        {
            var view = View;

            if (view == null)
            {
                return;
            }

            view.SetSelectedSlot(index);
        }

        /// <summary>Показать или спрятать HUD рук: в меню паузы и на экране ПК слоты лишние.</summary>
        public void SetVisible(bool isVisible)
        {
            var view = View;

            if (view == null)
            {
                return;
            }

            if (isVisible)
            {
                if (view.State is ViewVisibilityState.Hidden or ViewVisibilityState.Hiding)
                {
                    view.Show(isAnimate: false);
                }

                return;
            }

            if (view.State is ViewVisibilityState.Shown or ViewVisibilityState.Showing)
            {
                view.Hide(isAnimate: false);
            }
        }

        /// <summary>
        /// Показать HUD, если он ещё скрыт (например, после смены сцены).
        /// </summary>
        public void EnsureVisible()
        {
            var view = View;

            if (view == null)
            {
                return;
            }

            if (view.State is ViewVisibilityState.Showing or ViewVisibilityState.Shown)
            {
                return;
            }

            view.Show(isAnimate: false);
        }

        protected override void Awake()
        {
            base.Awake();

            // Подписываемся до первого Show, чтобы HUD сразу показывал руки.
            EnsureVisible();
        }
    }
}

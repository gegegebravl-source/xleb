using CHARK.GameManagement;
using CHARK.SimpleUI;
using UABPetelnia.GGJ2025.Runtime.Systems.Interaction;
using UABPetelnia.GGJ2025.Runtime.UI.Views;
using UABPetelnia.GGJ2025.Runtime.Utilities;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.UI.Controllers
{
    /// <summary>
    /// Держит прицел по центру экрана и подсвечивает его, когда под точкой есть
    /// интерактивный объект: товар на полке, коробка курьера или покупатель.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class CrosshairViewController : ViewController<CrosshairView>
    {
        protected override void OnEnable()
        {
            base.OnEnable();

            SystemsUtility.TryAddListener<InteractorHoveredEnteredMessage>(OnHoverEntered);
            SystemsUtility.TryAddListener<InteractorHoveredExitedMessage>(OnHoverExited);

            EnsureVisible();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SystemsUtility.TryRemoveListener<InteractorHoveredEnteredMessage>(OnHoverEntered);
            SystemsUtility.TryRemoveListener<InteractorHoveredExitedMessage>(OnHoverExited);
        }

        protected override void Start()
        {
            base.Start();

            EnsureVisible();
        }

        /// <summary>Показать прицел, если он ещё скрыт (например, после смены сцены).</summary>
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

        private void OnHoverEntered(InteractorHoveredEnteredMessage message)
        {
            var view = View;

            if (view != null)
            {
                view.SetHover(true);
            }
        }

        private void OnHoverExited(InteractorHoveredExitedMessage message)
        {
            var view = View;

            if (view != null)
            {
                view.SetHover(false);
            }
        }
    }
}

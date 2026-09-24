using System;
using CHARK.SimpleUI;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.UI.Views;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.UI.Controllers
{
    /// <summary>
    /// Управляет окном реплики NPC: показывает текст покупки, транслирует наружу
    /// события начала и конца «речи» (печатной машинки TMPEffects).
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class ChatViewController : ViewController<ChatView>
    {
        #region Serialized — Behaviour

        [Header("Behaviour")]
        [Tooltip("Показывать view через анимацию ViewController'а. По умолчанию — мгновенно, " +
                 "чтобы реплика появлялась вместе с текстом без задержки.")]
        [SerializeField] private bool animateShow = false;

        [Tooltip("Логировать пропущенные реплики (View == null, purchase == null).")]
        [SerializeField] private bool logSkips = true;

        #endregion

        #region Events

        public event Action OnSpeechEntered;
        public event Action OnSpeechExited;

        #endregion

        #region Cached state

        private bool subscribed;

        #endregion

        #region Unity lifecycle

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

        #endregion

        #region Public API

        /// <summary>
        /// Показать реплику по запросу покупки. Идемпотентно: повторный вызов
        /// с новым purchase перезапускает печать с начала.
        /// </summary>
        public void ShowPurchase(PurchaseRequest purchase)
        {
            var view = View;

            if (view == null)
            {
                if (logSkips)
                {
                    Debug.LogWarning(
                        "[ChatViewController] View недоступна, реплика пропущена.",
                        this);
                }
                return;
            }

            if (purchase == null)
            {
                if (logSkips)
                {
                    Debug.LogWarning(
                        "[ChatViewController] PurchaseRequest == null, реплика пропущена.",
                        this);
                }
                return;
            }

            // Пустой текст — валидный сценарий: NPC молчит, но view всё равно показываем,
            // чтобы отыграть анимацию. Если тебе такое не нужно — добавь early return.
            view.ChatText = purchase.Text ?? string.Empty;
            view.Show(isAnimate: animateShow);
        }

        /// <summary>
        /// Принудительно скрыть текущую реплику. Полезно при прерывании диалога
        /// (игрок закрыл меню, NPC ушёл со сцены).
        /// </summary>
        public void HideSpeech()
        {
            View?.Hide();
        }

        /// <summary>
        /// <c>true</c>, пока идёт печать реплики.
        /// </summary>
        public bool IsSpeaking => View != null && View.IsSpeaking;

        #endregion

        #region View subscription

        private void SubscribeToView()
        {
            if (subscribed) return;

            var view = View;
            if (view == null)
            {
                // View может стать доступной позже — пробуем ещё раз на следующем кадре.
                // Это редкий кейс, но защищает от гонки при горячей перезагрузке.
                return;
            }

            view.OnSpeechEntered += HandleViewSpeechEntered;
            view.OnSpeechExited  += HandleViewSpeechExited;
            subscribed = true;
        }

        private void UnsubscribeFromView()
        {
            if (!subscribed) return;

            var view = View;
            if (view != null)
            {
                view.OnSpeechEntered -= HandleViewSpeechEntered;
                view.OnSpeechExited  -= HandleViewSpeechExited;
            }

            subscribed = false;
        }

        #endregion

        #region Event forwarding

        private void HandleViewSpeechEntered() => OnSpeechEntered?.Invoke();
        private void HandleViewSpeechExited()  => OnSpeechExited?.Invoke();

        #endregion
    }
}
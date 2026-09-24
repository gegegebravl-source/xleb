using System;
using CHARK.SimpleUI;
using TMPEffects.Components;
using TMPro;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.UI.Views
{
    /// <summary>
    /// Строка диалога / подпись к речи NPC. Текст печатается через <see cref="TMPWriter"/>
    /// (эффект печатной машинки), наружу транслируются события «начал говорить» /
    /// «закончил говорить» — ровно одна пара на одну реплику.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class ChatView : View
    {
        #region Serialized — Text

        [Header("Text")]
        [Tooltip("Текстовое поле, куда пишет TMPWriter.")]
        [SerializeField] private TMP_Text chatText;

        [Tooltip("Компонент эффекта печати. Если null — событий речи не будет, но текст всё равно пишется.")]
        [SerializeField] private TMPWriter tmpWriter;

        #endregion

        #region Serialized — Behaviour

        [Header("Behaviour")]
        [Tooltip("Применить Warm Bread тему (цвет текста, обводка) при первом включении.")]
        [SerializeField] private bool applyRuntimeTheme = true;

        [Tooltip("Обнулять текст при показе view — полезно, если view переиспользуется между репликами.")]
        [SerializeField] private bool clearOnShow = false;

        [Tooltip("Считать, что речь завершилась при скрытии view. " +
                 "Если выключено — OnSpeechExited придёт только от TMPWriter.")]
        [SerializeField] private bool notifyExitOnHide = true;

        #endregion

        #region Events

        public event Action OnSpeechEntered;
        public event Action OnSpeechExited;

        #endregion

        #region Public API

        /// <summary>
        /// Текст текущей реплики. Get возвращает то, что сейчас в <see cref="chatText"/>
        /// (может быть частично напечатанным), set перезаписывает поле.
        /// </summary>
        public string ChatText
        {
            get => chatText != null ? chatText.text : string.Empty;
            set
            {
                if (chatText != null && value != null)
                {
                    chatText.text = value;
                }
            }
        }

        /// <summary>
        /// <c>true</c>, пока TMPWriter печатает и мы ещё не отправили OnSpeechExited.
        /// </summary>
        public bool IsSpeaking => isSpeaking;

        #endregion

        #region Cached state

        private bool themeApplied;
        private bool isSpeaking;

        #endregion

        #region Unity lifecycle

        protected override void OnEnable()
        {
            base.OnEnable();

            TryApplyTheme();
            RegisterWriterListeners();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            UnregisterWriterListeners();

            // Если view гаснет посреди речи — сообщаем наружу ровно один раз.
            // Флаг isSpeaking гарантирует, что следующий OnStopWriter от TMPWriter
            // уже не выстрелит повторно.
            if (notifyExitOnHide)
            {
                NotifySpeechExited();
            }
        }

        protected override void OnViewShowEntered()
        {
            base.OnViewShowEntered();

            if (clearOnShow && chatText != null)
            {
                chatText.text = string.Empty;
            }
        }

        protected override void OnViewHideExited()
        {
            base.OnViewHideExited();

            if (notifyExitOnHide)
            {
                NotifySpeechExited();
            }
        }

        #endregion

        #region Theme

        private void TryApplyTheme()
        {
            if (themeApplied || !applyRuntimeTheme) return;
            themeApplied = true;

            try
            {
                // ChatView — часть игрового UI, стилизуем тем же Warm Bread стилем.
                // Если в префабе есть кнопка «продолжить» — она автоматически
                // получит MenuButtonAnimator, если её имя не содержит "Exit".
                WarmBreadRuntimeTheme.ApplyMainMenuTheme(this);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[ChatView] Theme application failed: {exception.Message}", this);
            }
        }

        #endregion

        #region TMPWriter listeners

        private void RegisterWriterListeners()
        {
            if (tmpWriter == null) return;

            // Remove-before-Add — защита от двойной подписки при повторных OnEnable.
            tmpWriter.OnStartWriter.RemoveListener(HandleStartWriter);
            tmpWriter.OnStartWriter.AddListener(HandleStartWriter);

            tmpWriter.OnStopWriter.RemoveListener(HandleStopWriter);
            tmpWriter.OnStopWriter.AddListener(HandleStopWriter);

            // OnFinishWriter = естественное завершение, OnStopWriter = прерывание.
            // Оба должны закрыть реплику — поэтому оба ведут в один обработчик.
            tmpWriter.OnFinishWriter.RemoveListener(HandleStopWriter);
            tmpWriter.OnFinishWriter.AddListener(HandleStopWriter);
        }

        private void UnregisterWriterListeners()
        {
            if (tmpWriter == null) return;

            tmpWriter.OnStartWriter.RemoveListener(HandleStartWriter);
            tmpWriter.OnStopWriter.RemoveListener(HandleStopWriter);
            tmpWriter.OnFinishWriter.RemoveListener(HandleStopWriter);
        }

        private void HandleStartWriter(TMPWriter writer)
        {
            if (isSpeaking) return;

            isSpeaking = true;
            OnSpeechEntered?.Invoke();
        }

        private void HandleStopWriter(TMPWriter writer)
        {
            // Не важно, Stop это или Finish — реплика закончилась.
            NotifySpeechExited();
        }

        /// <summary>
        /// Идемпотентно сообщает наружу о завершении речи. Повторные вызовы — no-op.
        /// </summary>
        private void NotifySpeechExited()
        {
            if (!isSpeaking) return;

            isSpeaking = false;
            OnSpeechExited?.Invoke();
        }

        #endregion
    }
}
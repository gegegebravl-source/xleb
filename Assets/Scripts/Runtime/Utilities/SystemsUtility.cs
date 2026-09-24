using System;
using CHARK.GameManagement;
using CHARK.GameManagement.Messaging;
using CHARK.GameManagement.Systems;

namespace UABPetelnia.GGJ2025.Runtime.Utilities
{
    /// <summary>
    /// Safe way to reach the game systems.
    /// </summary>
    /// <remarks>
    /// <see cref="GameManager.TryGetSystem{TSystem}"/> throws when there is no <c>GameManager</c> at
    /// all, which happens when a scene is played without one or when a script is recompiled in the
    /// middle of Play mode. Actors and states want to ask "is it there?" without the answer taking
    /// the whole frame down, so they use this instead.
    /// </remarks>
    internal static class SystemsUtility
    {
        /// <summary>
        /// Try to get a system. Returns <c>false</c> and a default value instead of throwing when the
        /// systems are not available.
        /// </summary>
        public static bool TryGetSystem<TSystem>(out TSystem system) where TSystem : ISystem
        {
            try
            {
                return GameManager.TryGetSystem(out system);
            }
            catch (Exception)
            {
                system = default;

                return false;
            }
        }

        /// <summary>
        /// Подписаться на сообщение, не падая, если менеджер игры ещё не создан
        /// (например, при запуске сцены геймплея напрямую, без Scene_Init).
        /// </summary>
        public static bool TryAddListener<TMessage>(Action<TMessage> listener) where TMessage : IMessage
        {
            try
            {
                GameManager.AddListener(listener);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Снять подписку так же безопасно.</summary>
        public static bool TryRemoveListener<TMessage>(Action<TMessage> listener) where TMessage : IMessage
        {
            try
            {
                GameManager.RemoveListener(listener);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Опубликовать сообщение без падения у изолированного prefab.</summary>
        public static bool TryPublish<TMessage>(TMessage message) where TMessage : IMessage
        {
            try
            {
                GameManager.Publish(message);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

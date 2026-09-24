using CHARK.GameManagement.Messaging;
using CHARK.ScriptableScenes;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Scenes
{
    internal readonly struct SceneLoadEnteredMessage : IMessage
    {
        public ScriptableSceneCollection Collection { get; }

        public SceneLoadEnteredMessage(ScriptableSceneCollection collection)
        {
            Collection = collection;
        }
    }

    internal readonly struct SceneLoadExitedMessage : IMessage
    {
        public ScriptableSceneCollection Collection { get; }

        public SceneLoadExitedMessage(ScriptableSceneCollection collection)
        {
            Collection = collection;
        }
    }

    internal readonly struct SceneUnloadEnteredMessage : IMessage
    {
        public ScriptableSceneCollection Collection { get; }

        public SceneUnloadEnteredMessage(ScriptableSceneCollection collection)
        {
            Collection = collection;
        }
    }
}

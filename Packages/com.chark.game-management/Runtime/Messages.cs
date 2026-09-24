using CHARK.GameManagement.Messaging;

namespace CHARK.GameManagement
{
    public readonly struct DebuggingChangedMessage : IMessage
    {
        public bool IsDebuggingEnabled { get; }

        public DebuggingChangedMessage(bool isDebuggingEnabled)
        {
            IsDebuggingEnabled = isDebuggingEnabled;
        }
    }
}

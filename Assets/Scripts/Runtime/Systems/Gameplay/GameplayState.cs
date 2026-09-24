using System;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Gameplay
{
    internal abstract class GameplayState
    {
        protected enum Status
        {
            Completed,
            Working,
        }

        private GameplayState targetNextState;

        public string Name => GetType().Name;

        protected GameplayState NextState
        {
            get => targetNextState;
            set => targetNextState = value;
        }

        protected GameplayStateContext Context
        {
            get;
            private set;
        }

        public void Initialize(GameplayState nextState)
        {
            OnInitialized();
            targetNextState = nextState;
        }

        public void Dispose()
        {
            OnDisposed();
        }

        public void Enter(GameplayStateContext context)
        {
            Context = context;
            OnEntered(context);
        }

        public void Exit(GameplayStateContext context)
        {
            OnExited(context);
        }

        public GameplayState Update(GameplayStateContext context)
        {
            var status = OnUpdated(context);
            return status switch
            {
                Status.Working => this,
                Status.Completed => targetNextState,
                _ => throw new ArgumentOutOfRangeException($"Unsuported status: {status}")
            };
        }

        protected abstract void OnInitialized();

        protected abstract void OnDisposed();

        protected abstract void OnEntered(GameplayStateContext context);

        protected abstract void OnExited(GameplayStateContext context);

        protected abstract Status OnUpdated(GameplayStateContext context);
    }
}

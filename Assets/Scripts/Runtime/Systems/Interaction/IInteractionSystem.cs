using System.Collections.Generic;
using CHARK.GameManagement.Systems;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Interaction
{
    internal interface IInteractionSystem : ISystem
    {
        public IReadOnlyList<IInteractor> Interactors { get; }

        public void AddInteractor(IInteractor interactor);

        public void RemoveInteractor(IInteractor interactor);
    }
}

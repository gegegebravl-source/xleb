using CHARK.GameManagement;
using UABPetelnia.GGJ2025.Runtime.Systems.Cursors;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Components.Triggers
{
    internal sealed class CursorTrigger : MonoBehaviour
    {
        [SerializeField]
        private bool isLockOnStart;

        private ICursorSystem cursorSystem;

        private void Awake()
        {
            cursorSystem = GameManager.GetSystem<ICursorSystem>();
        }

        private void Start()
        {
            if (isLockOnStart)
            {
                cursorSystem.LockCursor();
            }
        }
    }
}

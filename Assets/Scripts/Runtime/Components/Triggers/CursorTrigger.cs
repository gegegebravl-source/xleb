using CHARK.GameManagement;
using UABPetelnia.GGJ2025.Runtime.Systems.Cursors;
using UABPetelnia.GGJ2025.Runtime.Utilities;
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
            SystemsUtility.TryGetSystem(out cursorSystem);
        }

        private void Start()
        {
            if (isLockOnStart)
            {
                cursorSystem?.LockCursor();
            }
        }
    }
}

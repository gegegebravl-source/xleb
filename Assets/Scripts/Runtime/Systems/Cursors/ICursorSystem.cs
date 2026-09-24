using CHARK.GameManagement.Systems;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Cursors
{
    internal interface ICursorSystem : ISystem
    {
        public bool IsCursorLocked { get; }

        /// <summary>
        /// Lock and hide game cursor.
        /// </summary>
        public void LockCursor();

        /// <summary>
        /// Unlock and show game cursor.
        /// </summary>
        public void UnLockCursor();
    }
}

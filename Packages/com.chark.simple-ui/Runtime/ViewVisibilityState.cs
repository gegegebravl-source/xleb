namespace CHARK.SimpleUI
{
    public enum ViewVisibilityState
    {
        /// <summary>
        /// View state is unknown, possibly the view was not initialized.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The View is playing "show animation" and is about to be fully shown.
        /// </summary>
        Showing = 1,

        /// <summary>
        /// The View is fully shown.
        /// </summary>
        Shown = 2,

        /// <summary>
        /// The View is playing "hide animation" and about to be fully hidden.
        /// </summary>
        Hiding = 3,

        /// <summary>
        /// The View is fully hidden.
        /// </summary>
        Hidden = 4,
    }
}

using UnityEngine;

namespace CHARK.SimpleUI.Constants
{
    /// <summary>
    /// Constants using in <see cref="UnityEngine.CreateAssetMenuAttribute"/>.
    /// </summary>
    internal static class CreateAssetMenuConstants
    {
        /// <summary>
        /// Prefix for <see cref="CreateAssetMenuAttribute.fileName"/>
        /// </summary>
        public const string BaseFileName = "New ";

        /// <summary>
        /// Prefix for <see cref="CreateAssetMenuAttribute.menuName"/>
        /// </summary>
        public const string BaseMenuName = "CHARK/Simple UI";

        /// <summary>
        /// Prefix for <see cref="CreateAssetMenuAttribute.order"/>
        /// </summary>
        public const int BaseOrder = -2000;
    }
}

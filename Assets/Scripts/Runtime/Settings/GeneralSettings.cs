using UABPetelnia.GGJ2025.Runtime.Constants;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.Settings
{
    [CreateAssetMenu(
        fileName = CreateAssetMenuConstants.BaseFileName + nameof(GeneralSettings),
        menuName = CreateAssetMenuConstants.BaseMenuName + "/General Settings",
        order = CreateAssetMenuConstants.BaseOrder
    )]
    internal sealed class GeneralSettings : ScriptableObject
    {
        [Header("Defaults")]
        [SerializeField]
        private float defaultLookSensitivity = 5f;

        [Range(0f, 1f)]
        [SerializeField]
        private float defaultMasterVolume = 1f;

        [Range(0f, 1f)]
        [SerializeField]
        private float defaultMusicVolume = 1f;

        [Range(0f, 1f)]
        [SerializeField]
        private float defaultSfxVolume = 1f;

        public const float MinLookSensitivity = 0f;

        public const float MaxLookSensitivity = 20f;

        public const float MinVolume = 0f;

        public const float MaxVolume = 1f;

        public float DefaultLookSensitivity => defaultLookSensitivity;

        public float DefaultMasterVolume => defaultMasterVolume;

        public float DefaultMusicVolume => defaultMusicVolume;

        public float DefaultSfxVolume => defaultSfxVolume;
    }
}

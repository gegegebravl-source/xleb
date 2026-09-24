using CHARK.GameManagement.Systems;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Audio
{
    internal interface IAudioSystem : ISystem
    {
        /// <summary>
        /// <c>true</c> if any audio data is being loaded or <c>false</c> otherwise.
        /// </summary>
        public bool IsLoading { get; }

        /// <summary>
        /// Load audio banks.
        /// </summary>
        public void LoadBanks();

        /// <summary>
        /// Un-load audio banks.
        /// </summary>
        public void UnLoadBanks();

        /// <returns>
        /// Audio volume for given <paramref name="type"/> in [0, 1] range.
        /// </returns>
        public float GetVolume(VolumeType type);

        /// <summary>
        /// Change audio volume of given <paramref name="type"/>. The provided
        /// <paramref name="volume"/> must be within [0, 1] range.
        /// </summary>
        public void SetVolume(VolumeType type, float volume);
    }
}

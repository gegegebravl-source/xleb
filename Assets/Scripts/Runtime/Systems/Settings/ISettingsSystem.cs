using CHARK.GameManagement.Systems;

namespace UABPetelnia.GGJ2025.Runtime.Systems.Settings
{
    internal interface ISettingsSystem : ISystem
    {
        /// <summary>
        /// Current settings.
        /// </summary>
        public SettingsData Settings { get; set; }

        /// <summary>Записать настройки на диск сейчас, не дожидаясь выхода из игры.</summary>
        public void Save();

        /// <summary>Применить графические/экранные настройки из Settings прямо сейчас.</summary>
        public void ApplyGraphics();
    }
}

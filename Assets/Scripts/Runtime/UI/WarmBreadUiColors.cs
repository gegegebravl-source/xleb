using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.UI
{
    /// <summary>
    /// Runtime half of the warm bread look. Mirrors the editor palette in
    /// <c>WarmBreadTheme</c>, because code that runs during play cannot reach editor-only code.
    /// </summary>
    internal static class WarmBreadUiColors
    {
        /// <summary>Parchment: normal text on the dark crust.</summary>
        public static readonly Color Cream = new(0.96f, 0.90f, 0.78f, 1f);

        /// <summary>Second level parchment: captions and hints.</summary>
        public static readonly Color CreamSoft = new(0.85f, 0.77f, 0.62f, 1f);

        /// <summary>Crust: text on parchment.</summary>
        public static readonly Color Crust = new(0.29f, 0.16f, 0.09f, 1f);

        /// <summary>Butter: buttons and highlights.</summary>
        public static readonly Color Butter = new(0.99f, 0.80f, 0.36f, 1f);

        /// <summary>Тёплый кремовый тон для деревянных кнопок-досок (как в логотипе).
        /// Не насыщенный: зелёное дерево спрайта тонируется им, а не перекрашивается.</summary>
        public static readonly Color Plank = new(0.97f, 0.93f, 0.82f, 1f);

        public static readonly Color PlankHover = new(1.00f, 0.97f, 0.88f, 1f);

        public static readonly Color PlankSelected = new(0.99f, 0.93f, 0.78f, 1f);

        public static readonly Color PlankPressed = new(0.89f, 0.80f, 0.62f, 1f);

        /// <summary>Доска кнопки выхода — с тёплым blush-оттенком, акцент уходит в цвет текста.</summary>
        public static readonly Color PlankJam = new(0.96f, 0.87f, 0.82f, 1f);

        public static readonly Color PlankJamHover = new(0.99f, 0.91f, 0.87f, 1f);

        public static readonly Color PlankJamPressed = new(0.90f, 0.72f, 0.64f, 1f);

        /// <summary>Leaf: earned, done, unlocked.</summary>
        public static readonly Color Leaf = new(0.62f, 0.72f, 0.31f, 1f);

        /// <summary>Jam: the exit button and mistakes.</summary>
        public static readonly Color Jam = new(0.79f, 0.29f, 0.20f, 1f);

        /// <summary>Тёмный джем для текста на светлой доске кнопки выхода.</summary>
        public static readonly Color JamDark = new(0.55f, 0.17f, 0.11f, 1f);

        public static readonly Color JournalValue = Cream;

        public static readonly Color AchievementUnlocked = Leaf;

        public static readonly Color AchievementLocked = CreamSoft;
    }
}

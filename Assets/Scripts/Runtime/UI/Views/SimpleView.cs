using CHARK.SimpleUI;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Runtime.UI.Views
{
    /// <summary>
    /// Универсальный контейнер для простых панелей без собственной логики:
    /// экран загрузки, заглушка «нажмите любую клавишу», туториал с одной картинкой.
    /// Всё поведение — из <see cref="View"/>.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class SimpleView : View
    {
    }
}
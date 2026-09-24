#if UNITY_EDITOR
using UABPetelnia.GGJ2025.Runtime.Constants;
using UABPetelnia.GGJ2025.Runtime.EditorTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Editor
{
    /// <summary>
    /// One button that runs the whole warm bread setup: art pack, menu look, kiosk shop, ordering
    /// PC, scene decor and the studio look that puts real textures into the editor.
    /// </summary>
    /// <remarks>
    /// Every step is idempotent, so this can be pressed as often as needed. The art import step
    /// touches a few hundred images and takes a while, the editor stays busy until it is done.
    /// </remarks>
    internal static class WarmBreadSetup
    {
        [MenuItem(
            MenuItemConstants.BaseToolsItemName + "/Setup Everything (Warm Bread)",
            priority = MenuItemConstants.BaseToolsItemPriority - 1)]
        public static void SetupEverything()
        {
            if (Application.isBatchMode == false &&
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
            {
                Debug.LogWarning("[WarmBreadSetup] Отменено: сцены не сохранены.");

                return;
            }

            RunStep("1/9 Кириллица интерфейса", CyrillicFontFixer.Ensure);
            RunStep("2/9 Арт-пак Кирилла", UserArtPackImporter.ImportArtPack);
            RunStep("3/9 Главное меню", MainMenuPrefabBuilder.Rebuild);
            RunStep("4/9 Стиль интерфейса", WarmBreadUiThemer.ApplyTheme);
            RunStep("5/9 Киоск, товар и полки", KioskShopSetup.BuildShop);
            RunStep("6/9 Меню заказов на ПК", DeliveryUiBuilder.Build);
            RunStep("7/9 Декор киоска", KioskDecorBuilder.BuildDecor);
            RunStep("8/9 Объёмный задний двор 2000-х", BackyardRealismBuilder.Build);

            // Last on purpose: the steps above create materials and prefabs, and this one is what
            // gives them their surface detail, normal maps and scene lighting.
            RunStep("9/9 Вид сцены: свет, отражения и PBR-текстуры", WarmBreadStudioLookBaker.BakeFromSetup);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[WarmBreadSetup] ГОТОВО. Открой Assets/Scenes/Scene_Gameplay.unity и нажми Play: "
                + "хлеб лежит на полках, Tab у прилавка открывает ПК заказов. "
                + "Сцена в редакторе теперь с текстурами; если Scene view плоский — включи в его "
                + "шапке Post Processing."
            );
        }

        private static void RunStep(string title, System.Action step)
        {
            Debug.Log($"[WarmBreadSetup] {title}…");

            try
            {
                step();
            }
            catch (System.Exception exception)
            {
                // Returns here on purpose: printing "готово" after a failed step is how a broken
                // pipeline looks like a working one.
                Debug.LogError($"[WarmBreadSetup] {title} упал с ошибкой: {exception}");

                return;
            }

            Debug.Log($"[WarmBreadSetup] {title} — готово.");
        }
    }
}
#endif

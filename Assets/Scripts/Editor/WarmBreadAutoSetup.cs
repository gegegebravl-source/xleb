#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UABPetelnia.GGJ2025.Runtime.Constants;
using UnityEditor;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Editor
{
    /// <summary>
    /// Builds the warm bread content on its own the first time the project is opened, and bakes the
    /// studio look into the project so the editor shows textures instead of a flat grey street.
    /// </summary>
    /// <remarks>
    /// The code in this repository is only the logic: prefabs, shelves, decor, icons, menus and shop
    /// data are produced by the tools under <c>Tools/UAB Petelnia</c>. Until those run, the project
    /// looks exactly like it did before, which is why this hook exists. It waits for the first
    /// import to finish, runs the pipeline once and never again, because the check is the content
    /// itself: as soon as the artefacts are there, nothing happens.
    /// </remarks>
    [InitializeOnLoad]
    internal static class WarmBreadAutoSetup
    {
        private const string MissingContentWarning =
            "Тёплый хлеб: контент всё ещё не собран, поэтому в игре видны старые спрайты";

        /// <summary>
        /// The folder the look bake creates. It is the one artefact that is cheap to produce on its
        /// own, so it gets its own fast path below.
        /// </summary>
        private const string LookArtifactPath = "Assets/Visuals/Objects/Materials/Characters";

        /// <summary>
        /// Files that only exist after the matching tool has been run at least once. The pipeline is
        /// considered done when every one of them is on disk. A folder counts as an artefact too,
        /// which is how the look bake is tracked.
        /// </summary>
        private static readonly (string Path, string Tool)[] ExpectedArtifacts =
        {
            ("Assets/Prefabs/Actors/Actor_Product.prefab", "Shop/Build Warm Bread Kiosk Shop"),
            ("Assets/Prefabs/UI/View_Delivery.prefab", "Shop/Build Delivery PC Menu"),
            ("Assets/Prefabs/UI/View_AchievementToast.prefab", "UI/Apply Warm Bread Theme"),
            ("Assets/Visuals/UI/Icons/UI_Icon_Journal.png", "Art/Import Kirill Art Pack"),
            ("Assets/Visuals/Fonts/Font_Cyrillic_Fallback_SDF.asset", "Art/Fix Russian UI Font"),
            ("Assets/Visuals/Materials/Backyard/Backyard_Asphalt.mat", "Art/Build 3D 2000s Backyard"),
            (LookArtifactPath, "Art/Bake Studio Look into Project"),
        };

        private static bool hasRunThisSession;

        static WarmBreadAutoSetup()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            EditorApplication.delayCall += TrySetUp;
        }

        private static void TrySetUp()
        {
            if (hasRunThisSession || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            // Wait until the editor is idle: running the tools in the middle of an import would
            // build half of the content and then fail on the missing half.
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TrySetUp;

                return;
            }

            var missing = FindMissing();

            if (missing.Count == 0)
            {
                return;
            }

            hasRunThisSession = true;

            // Everything else in the pipeline reads the art pack and takes a while. The look bake
            // only reads what is already there, so when it is the only thing missing there is no
            // reason to import a few hundred images again.
            if (missing.Count == 1 && missing[0].Path == LookArtifactPath)
            {
                BakeLookOnly();

                return;
            }

            Debug.Log(
                "[WarmBreadAutoSetup] Контент ещё не собран — запускаю пайплайн один раз. "
                + "Редактор на это время сам переключит сцену, не трогай его до сообщения ГОТОВО."
            );

            WarmBreadSetup.SetupEverything();

            ReportWhatIsLeft(FindMissing());
        }

        private static void BakeLookOnly()
        {
            Debug.Log(
                "[WarmBreadAutoSetup] Не хватает только запечённого вида сцены (свет, отражения, "
                + "текстуры персонажей). Запускаю только его, полный пайплайн тут не нужен."
            );

            try
            {
                WarmBreadStudioLookBaker.BakeFromSetup();
            }
            catch (System.Exception exception)
            {
                Debug.LogError("[WarmBreadAutoSetup] Запекание вида сцены упало: " + exception);

                return;
            }

            ReportWhatIsLeft(FindMissing());
        }

        private static void ReportWhatIsLeft(List<(string Path, string Tool)> missing)
        {
            if (missing.Count > 0)
            {
                LogWarning(missing);
            }
        }

        private static List<(string Path, string Tool)> FindMissing()
        {
            var missing = new List<(string Path, string Tool)>();

            foreach (var (path, tool) in ExpectedArtifacts)
            {
                var onDisk = AssetDatabase.LoadAssetAtPath<Object>(path) != false
                    || AssetDatabase.IsValidFolder(path);

                if (onDisk == false)
                {
                    missing.Add((path, tool));
                }
            }

            return missing;
        }

        private static void LogWarning(List<(string Path, string Tool)> missing)
        {
            Debug.LogWarning(
                $"[WarmBreadAutoSetup] {MissingContentWarning}.\n"
                + "Запусти ОДИН пункт меню, он соберёт всё остальное по порядку:\n"
                + $"  →  {MenuItemConstants.BaseToolsItemName}/Setup Everything (Warm Bread)\n"
                + "Чего сейчас не хватает:\n"
                + string.Join("\n", missing.Select(entry => $"  • {entry.Path}  →  {entry.Tool}"))
            );
        }
    }
}
#endif

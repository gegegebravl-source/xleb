using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace WarmBread.Editor
{
    public static class ProjectValidator
    {
        private static readonly string[] RequiredScenes =
        {
            "Assets/Scenes/Scene_Init.unity",
            "Assets/Scenes/Scene_Menu.unity",
            "Assets/Scenes/Scene_Gameplay.unity",
            "Assets/Scenes/Scene_GameVictory.unity",
            "Assets/Scenes/Scene_GameOver.unity"
        };

        private static readonly string[] RequiredFiles =
        {
            "FMODAssets/Builds/Desktop/Master.bank",
            "FMODAssets/Builds/Desktop/Master.strings.bank",
            "FMODAssets/Builds/Desktop/Ambience.bank",
            "FMODAssets/Builds/Desktop/SFX.bank",
            "FMODAssets/Builds/Desktop/VoiceOver.bank",
            "Assets/Settings/Rendering/URP_Desktop.asset",
            "Assets/Settings/Rendering/ForwardRenderer_Desktop.asset",
            "Assets/WarmBread/Resources/PBR/GreenKioskPaint/GreenKioskPaint_BaseColor.png",
            "Assets/WarmBread/Resources/PBR/ConcretePavement/ConcretePavement_BaseColor.png",
            "Assets/WarmBread/Resources/PBR/WornPlaster/WornPlaster_BaseColor.png",
            "Assets/WarmBread/Resources/PBR/RustedMetal/RustedMetal_BaseColor.png"
        };

        [MenuItem("Warm Bread/Release/Validate Project")]
        public static void Validate()
        {
            var issues = CollectIssues();
            if (issues.Count == 0) Debug.Log("[WarmBread] Release-проверка пройдена: критических проблем не найдено.");
            else Debug.LogError("[WarmBread] Release-проверка не пройдена (" + issues.Count + "):\n- " + string.Join("\n- ", issues));
        }

        public static List<string> CollectIssues()
        {
            var issues = new List<string>();
            ValidateBuildScenes(issues);
            ValidateRequiredFiles(issues);
            ValidatePlayerSettings(issues);
            ValidateMaterials(issues);
            ValidatePackages(issues);
            ValidateResources(issues);
            ValidateBrokenAssets(issues);
            return issues;
        }

        private static void ValidateBuildScenes(List<string> issues)
        {
            var enabled = new List<string>();
            for (var i = 0; i < EditorBuildSettings.scenes.Length; i++)
                if (EditorBuildSettings.scenes[i].enabled) enabled.Add(EditorBuildSettings.scenes[i].path);

            if (enabled.Count != RequiredScenes.Length)
                issues.Add("Build Settings должны содержать ровно " + RequiredScenes.Length + " включённых сцен.");

            for (var i = 0; i < RequiredScenes.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RequiredScenes[i]) == null)
                    issues.Add("Не найдена обязательная сцена: " + RequiredScenes[i]);
                else if (i >= enabled.Count || enabled[i] != RequiredScenes[i])
                    issues.Add("Неверный порядок сцены в Build Settings: " + RequiredScenes[i]);
            }
        }

        private static void ValidateRequiredFiles(List<string> issues)
        {
            for (var i = 0; i < RequiredFiles.Length; i++)
                if (!File.Exists(RequiredFiles[i])) issues.Add("Отсутствует обязательный файл: " + RequiredFiles[i]);
        }

        private const string MusicResourcesFolder = "Assets/Resources/Music";

        /// <summary>
        /// Lobby music is loaded through <see cref="Resources"/> at runtime, so the clips have to
        /// live inside a Resources folder. Files sitting anywhere else are silently dropped from the
        /// player build, which is how the released game used to end up with a silent main menu.
        /// </summary>
        private static void ValidateResources(List<string> issues)
        {
            if (!Directory.Exists(MusicResourcesFolder))
            {
                issues.Add(
                    "Нет папки " + MusicResourcesFolder + ", музыка главного меню не попадёт в сборку."
                );

                return;
            }

            var clips = Directory.GetFiles(MusicResourcesFolder, "*.mp3");
            var wav = Directory.GetFiles(MusicResourcesFolder, "*.wav");
            var ogg = Directory.GetFiles(MusicResourcesFolder, "*.ogg");
            if (clips.Length + wav.Length + ogg.Length == 0)
            {
                issues.Add(
                    "В " + MusicResourcesFolder + " нет ни одного аудиофайла, музыка меню играть не будет."
                );
            }
        }

        /// <summary>
        /// A volume profile that still holds a component without a script is loaded with every
        /// build, so it is caught here instead of showing up as a "Missing (Mono Script)" row
        /// nobody notices.
        /// </summary>
        private static void ValidateBrokenAssets(List<string> issues)
        {
            var broken = BrokenAssetFixer.CountVolumeProfilesWithMissingComponents();
            if (broken <= 0)
            {
                return;
            }

            issues.Add(
                $"В volume-профилях {broken} шт. содержат компоненты без скрипта."
                + " Выполни Warm Bread > Release > Fix Broken Assets."
            );
        }

        private static void ValidatePlayerSettings(List<string> issues)
        {
            if (string.IsNullOrWhiteSpace(PlayerSettings.productName)) issues.Add("Не задано имя продукта.");
            if (PlayerSettings.bundleVersion != "2.0.0") issues.Add("Release Candidate должен иметь версию 2.0.0.");
            var identifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone);
            if (string.IsNullOrWhiteSpace(identifier) || identifier.Contains("Unity-Technologies") || identifier.Contains("template"))
                issues.Add("Application Identifier всё ещё содержит шаблонное значение.");
        }

        private static void ValidateMaterials(List<string> issues)
        {
            var guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
                    issues.Add("Материал без рабочего shader: " + path);
            }
        }

        /// <summary>
        /// Required packages and the version family they belong to. The exact patch version is
        /// read from the manifest instead of being hard-coded: pinning it here made the check
        /// report three false failures the moment the packages were updated.
        /// </summary>
        private static readonly (string Package, string MinimumVersion)[] RequiredPackages =
        {
            ("com.unity.render-pipelines.universal", "17.0.0"),
            ("com.unity.visualeffectgraph", "17.0.0"),
            ("com.unity.cinemachine", "3.0.0"),
            ("com.unity.inputsystem", "1.0.0"),
            ("com.unity.splines", "2.0.0"),
            ("com.unity.nuget.newtonsoft-json", "3.0.0"),
        };

        private static void ValidatePackages(List<string> issues)
        {
            var manifest = File.ReadAllText("Packages/manifest.json");

            foreach (var (package, minimumVersion) in RequiredPackages)
            {
                var key = "\"" + package + "\": \"";
                var start = manifest.IndexOf(key, System.StringComparison.Ordinal);
                if (start < 0)
                {
                    issues.Add("В Packages/manifest.json нет пакета " + package + ".");
                    continue;
                }

                start += key.Length;
                var end = manifest.IndexOf('"', start);
                if (end < 0)
                {
                    issues.Add("Не удалось прочитать версию пакета " + package + ".");
                    continue;
                }

                var version = manifest.Substring(start, end - start);
                if (IsAtLeast(version, minimumVersion) == false)
                {
                    issues.Add(
                        $"Пакет {package} устарел: {version}, требуется не ниже {minimumVersion}."
                    );
                }
            }

            // URP and VFX Graph are released together and break parity when one of them drifts.
            if (TryReadVersion(manifest, "com.unity.render-pipelines.universal", out var urp) &&
                TryReadVersion(manifest, "com.unity.visualeffectgraph", out var vfx) &&
                urp != vfx)
            {
                issues.Add($"URP ({urp}) и VFX Graph ({vfx}) должны быть одной версии.");
            }
        }

        private static bool TryReadVersion(string manifest, string package, out string version)
        {
            version = string.Empty;

            var key = "\"" + package + "\": \"";
            var start = manifest.IndexOf(key, System.StringComparison.Ordinal);
            if (start < 0)
            {
                return false;
            }

            start += key.Length;
            var end = manifest.IndexOf('"', start);
            if (end < 0)
            {
                return false;
            }

            version = manifest.Substring(start, end - start);
            return true;
        }

        private static bool IsAtLeast(string version, string minimumVersion)
        {
            return System.Version.TryParse(Normalize(version), out var parsed) &&
                   System.Version.TryParse(Normalize(minimumVersion), out var minimum) &&
                   parsed >= minimum;
        }

        /// <summary>
        /// Package versions may carry a preview suffix ("1.0.0-pre.2"), which <c>System.Version</c>
        /// cannot parse.
        /// </summary>
        private static string Normalize(string version)
        {
            var dashIndex = version.IndexOf('-');
            return dashIndex < 0 ? version : version.Substring(0, dashIndex);
        }
    }
}

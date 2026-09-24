using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace WarmBread.Editor
{
    public static class ReleaseBuilder
    {
        private const string OutputDirectory = "Builds/Windows";
        private const string ExecutableName = "WarmBread.exe";

        [MenuItem("Warm Bread/Release/Prepare Project")]
        public static void PrepareProject()
        {
            PlayerSettings.productName = "Тёплый хлеб";
            PlayerSettings.bundleVersion = "2.0.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.warmbread.game");
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.forceSingleInstance = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Unity_4_8);
            PlayerSettings.stripEngineCode = true;
            PBRMaterialBuilder.Rebuild();
            WriteReleaseManifest();
            AssetDatabase.SaveAssets();
            Debug.Log("[WarmBread] Production-настройки применены.");
        }

        [MenuItem("Warm Bread/Release/Build Windows x64")]
        public static void BuildWindows()
        {
            PrepareProject();
            var issues = ProjectValidator.CollectIssues();
            if (issues.Count > 0) throw new BuildFailedException("Release-проверка не пройдена:\n- " + string.Join("\n- ", issues));

            Directory.CreateDirectory(OutputDirectory);
            var scenes = Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled);
            var scenePaths = Array.ConvertAll(scenes, scene => scene.path);
            var options = new BuildPlayerOptions
            {
                scenes = scenePaths,
                locationPathName = Path.Combine(OutputDirectory, ExecutableName),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                subtarget = (int)StandaloneBuildSubtarget.Player,
                options = BuildOptions.CleanBuildCache | BuildOptions.CompressWithLz4HC | BuildOptions.StrictMode
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Windows-сборка завершилась со статусом " + report.summary.result + ". Ошибок: " + report.summary.totalErrors);

            Debug.Log("[WarmBread] Windows x64 release готов: " + options.locationPathName + " (" + report.summary.totalSize + " bytes)");
        }

        private static void WriteReleaseManifest()
        {
            Directory.CreateDirectory("Assets/StreamingAssets");
            var data = new ReleaseManifest
            {
                product = "Тёплый хлеб",
                version = PlayerSettings.bundleVersion,
                unity = Application.unityVersion,
                buildUtc = DateTime.UtcNow.ToString("O"),
                saveFormat = SaveSystem.CurrentVersion
            };
            File.WriteAllText("Assets/StreamingAssets/warm-bread-release.json", JsonUtility.ToJson(data, true));
            AssetDatabase.ImportAsset("Assets/StreamingAssets/warm-bread-release.json", ImportAssetOptions.ForceUpdate);
        }

        [Serializable]
        private sealed class ReleaseManifest
        {
            public string product;
            public string version;
            public string unity;
            public string buildUtc;
            public int saveFormat;
        }
    }
}

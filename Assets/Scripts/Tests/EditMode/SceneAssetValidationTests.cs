using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Tests
{
    public sealed class SceneAssetValidationTests
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/Scene_Init.unity",
            "Assets/Scenes/Scene_Menu.unity",
            "Assets/Scenes/Scene_Gameplay.unity",
            "Assets/Scenes/Scene_GameOver.unity",
            "Assets/Scenes/Scene_GameVictory.unity",
        };

        [Test]
        public void AllRuntimeScenesExistAsImportableAssets()
        {
            foreach (var path in ScenePaths)
            {
                var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                Assert.That(scene, Is.Not.Null, $"Missing scene asset: {path}");
            }
        }

        [Test]
        public void CriticalPrefabsExist()
        {
            var manager = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GGJ2025GameManager.prefab");
            var product = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Actors/Actor_Product.prefab");

            Assert.That(manager, Is.Not.Null);
            Assert.That(product, Is.Not.Null);
        }
    }
}

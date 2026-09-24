using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UABPetelnia.GGJ2025.Tests
{
    public sealed class ScenePlayModeSmokeTests
    {
        [UnityTest]
        public IEnumerator BootstrapSceneActivatesThePersistentGameManager()
        {
            yield return SceneManager.LoadSceneAsync("Scene_Init", LoadSceneMode.Single);
            yield return null;
            yield return null;

            // Scene_Init is the only scene that owns the persistent manager. The prefab instance
            // must be active or no systems can load Menu/Gameplay collections in a player build.
            Assert.That(GameObject.Find("GGJ2025GameManager"), Is.Not.Null);
            Assert.That(SceneManager.GetActiveScene().IsValid(), Is.True);
        }
    }
}

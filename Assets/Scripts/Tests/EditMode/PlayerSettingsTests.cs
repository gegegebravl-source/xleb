using NUnit.Framework;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Tests
{
    public sealed class PlayerSettingsTests
    {
        [Test]
        public void MissingHealthTexturesAreSafe()
        {
            var settings = ScriptableObject.CreateInstance<PlayerSettings>();
            try
            {
                Assert.That(settings.MaxHealth, Is.Zero);
                Assert.That(settings.GetHealthTexture(1), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void MissingGameplayListsReadAsEmptyCollections()
        {
            var settings = ScriptableObject.CreateInstance<GameplaySettings>();
            try
            {
                Assert.That(settings.AvailableItems, Is.Empty);
                Assert.That(settings.AvailableShoppers, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }
    }
}

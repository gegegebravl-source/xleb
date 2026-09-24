#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace UABPetelnia.GGJ2025.Editor
{
    /// <summary>
    /// Keeps the Tiny5 pixel look for Latin text but adds a real Cyrillic fallback. The old Tiny5
    /// asset contains no Russian glyphs, so TextMesh Pro silently substituted unrelated Latin
    /// characters in every Russian label. A fallback preserves the visual identity without making
    /// the interface unreadable.
    /// </summary>
    internal static class CyrillicFontFixer
    {
        private const string PrimaryFontPath = "Assets/Visuals/Fonts/Font_Tiny5_Regular_SDF.asset";
        private const string SourceFontPath = "Assets/Plugins/TextMesh Pro/Fonts/LiberationSans.ttf";
        private const string FallbackFontPath = "Assets/Visuals/Fonts/Font_Cyrillic_Fallback_SDF.asset";
        private const string RussianCharacters =
            "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ" +
            "абвгдеёжзийклмнопрстуфхцчшщъыьэюя" +
            "№«»„“”–—";

        [MenuItem(
            "Tools/UAB Petelnia/Art/Fix Russian UI Font",
            priority = 35)]
        public static void Ensure()
        {
            var primary = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PrimaryFontPath);
            var source = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);

            if (primary == false || source == false)
            {
                Debug.LogWarning(
                    "[CyrillicFontFixer] Не найден Tiny5 или исходный Liberation Sans. " +
                    "Русский fallback не создан."
                );

                return;
            }

            var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontPath);

            if (fallback == false)
            {
                fallback = TMP_FontAsset.CreateFontAsset(
                    source,
                    90,
                    9,
                    GlyphRenderMode.SDFAA,
                    1024,
                    1024,
                    AtlasPopulationMode.Dynamic,
                    true
                );

                if (fallback == false)
                {
                    Debug.LogError("[CyrillicFontFixer] TextMesh Pro не смог создать Cyrillic fallback.");

                    return;
                }

                fallback.name = "Font_Cyrillic_Fallback_SDF";
                AssetDatabase.CreateAsset(fallback, FallbackFontPath);

                if (fallback.material != null)
                {
                    AssetDatabase.AddObjectToAsset(fallback.material, fallback);
                }

                foreach (var atlas in fallback.atlasTextures)
                {
                    if (atlas != null)
                    {
                        AssetDatabase.AddObjectToAsset(atlas, fallback);
                    }
                }
            }

            // Dynamic population means new Russian dialogue lines remain safe too, not just the
            // current UI strings. TryAddCharacters also warms the atlas for the first frame.
            fallback.TryAddCharacters(RussianCharacters, out _);

            var fallbacks = primary.fallbackFontAssetTable;
            if (fallbacks == null)
            {
                fallbacks = new List<TMP_FontAsset>();
                primary.fallbackFontAssetTable = fallbacks;
            }

            if (fallbacks.Contains(fallback) == false)
            {
                fallbacks.Add(fallback);
            }

            EditorUtility.SetDirty(fallback);
            EditorUtility.SetDirty(primary);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[CyrillicFontFixer] Русская кириллица подключена к Tiny5: " +
                FallbackFontPath
            );
        }
    }
}
#endif

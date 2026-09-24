using UnityEditor;
using UnityEngine;

namespace WarmBread.Editor
{
    /// <summary>
    /// Removes the broken leftovers Unity occasionally leaves behind inside assets — volume profile
    /// components whose script cannot be resolved.
    /// </summary>
    /// <remarks>
    /// A broken copy/paste inside the volume inspector (a known Unity bug, it pastes a component
    /// from the render pipeline's own test suite) leaves entries in a <c>VolumeProfile</c> that
    /// point at a class which does not exist in the project. They show up as "Missing (Mono
    /// Script)" rows, Unity cannot recreate them, and they are loaded with every build because the
    /// global profile is part of the render pipeline settings.
    /// </remarks>
    public static class BrokenAssetFixer
    {
        private const string VolumeProfileTypeFilter = "t:VolumeProfile";
        private const string PostFxFolder = "Assets/Settings/PostFX";
        private const string ComponentsProperty = "components";

        [MenuItem("Warm Bread/Release/Fix Broken Assets")]
        public static void Fix()
        {
            var fixedProfiles = FixVolumeProfiles();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (fixedProfiles <= 0)
            {
                Debug.Log("[WarmBread] Битых ассетов не найдено: все volume-профили целые.");

                return;
            }

            Debug.Log(
                $"[WarmBread] Починено volume-профилей: {fixedProfiles}. "
                + "Компоненты без скрипта удалены."
            );
        }

        /// <summary>
        /// Drop every <c>VolumeProfile</c> component that has no script behind it.
        /// </summary>
        /// <returns>How many profile assets were changed.</returns>
        public static int FixVolumeProfiles()
        {
            var guids = FindProfileGuids();
            var fixedProfiles = 0;

            for (var index = 0; index < guids.Length; index++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[index]);

                if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) is not { } profile)
                {
                    continue;
                }

                using var serialized = new SerializedObject(profile);

                var components = serialized.FindProperty(ComponentsProperty);
                if (components == null || components.isArray == false)
                {
                    continue;
                }

                var removed = 0;

                for (var elementIndex = components.arraySize - 1; elementIndex >= 0; elementIndex--)
                {
                    if (components.GetArrayElementAtIndex(elementIndex).objectReferenceValue != null)
                    {
                        continue;
                    }

                    // Deleting an object reference only clears it; the entry has to be deleted a
                    // second time for the array to actually shrink.
                    components.DeleteArrayElementAtIndex(elementIndex);

                    if (components.GetArrayElementAtIndex(elementIndex).objectReferenceValue == null)
                    {
                        components.DeleteArrayElementAtIndex(elementIndex);
                    }

                    removed++;
                }

                if (removed <= 0)
                {
                    continue;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(profile);

                Debug.Log($"[WarmBread] {path}: убрано компонентов без скрипта — {removed}.");
                fixedProfiles++;
            }

            return fixedProfiles;
        }

        /// <summary>
        /// How many volume profiles still hold a component without a script. Used by the release
        /// check, so a broken profile cannot slip into a build unnoticed.
        /// </summary>
        public static int CountVolumeProfilesWithMissingComponents()
        {
            var guids = FindProfileGuids();
            var broken = 0;

            for (var index = 0; index < guids.Length; index++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[index]);

                if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) is not { } profile)
                {
                    continue;
                }

                using var serialized = new SerializedObject(profile);

                var components = serialized.FindProperty(ComponentsProperty);
                if (components == null || components.isArray == false)
                {
                    continue;
                }

                for (var elementIndex = 0; elementIndex < components.arraySize; elementIndex++)
                {
                    if (components.GetArrayElementAtIndex(elementIndex).objectReferenceValue != null)
                    {
                        continue;
                    }

                    broken++;

                    break;
                }
            }

            return broken;
        }

        /// <summary>
        /// The type filter is the fast path; the folder scan is the fallback for the day the search
        /// index does not know about <c>VolumeProfile</c>.
        /// </summary>
        private static string[] FindProfileGuids()
        {
            var guids = AssetDatabase.FindAssets(VolumeProfileTypeFilter);

            return guids.Length > 0
                ? guids
                : AssetDatabase.FindAssets("t:ScriptableObject", new[] { PostFxFolder });
        }
    }
}

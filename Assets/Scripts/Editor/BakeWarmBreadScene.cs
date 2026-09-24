#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UABPetelnia.GGJ2025.Runtime.Actors;
using UABPetelnia.GGJ2025.Runtime.Constants;
using UABPetelnia.GGJ2025.Runtime.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UABPetelnia.GGJ2025.Editor
{
    /// <summary>
    /// Puts a real product on every shelf slot of the gameplay scene and saves it, so the whole
    /// shop is visible and editable in the editor instead of being spawned when the game launches.
    /// </summary>
    /// <remarks>
    /// Idempotent and additive: slots that already hold a baked product are left alone. The runtime
    /// adopts baked products too, so no duplicates appear during play.
    /// </remarks>
    public static class BakeWarmBreadScene
    {
        private const string GameplayScenePath = "Assets/Scenes/Scene_Gameplay.unity";
        private const string ProductPrefabPath = "Assets/Prefabs/Actors/Actor_Product.prefab";
        private const string GameplaySettingsPath = "Assets/Settings/Game/Settings_Gameplay.asset";
        private const string ProductMaterialPath = "Assets/Visuals/Objects/Materials/Product_Image.mat";
        private const string ProductsMaterialFolder = "Assets/Visuals/Objects/Materials/Products";
        private const string TextureProperty = "_BaseMap";

        private const string AutoBakePrefKey = "UABPetelnia.GGJ2025.SceneBaked";

        private static bool hasAutoBakedThisSession;

        [InitializeOnLoadMethod]
        private static void AutoBakeOnStartup()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            EditorApplication.delayCall += TryAutoBake;
        }

        private static void TryAutoBake()
        {
            if (hasAutoBakedThisSession || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryAutoBake;

                return;
            }

            // Never overwrite unsaved work in another scene; retries on the next editor session.
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                return;
            }

            // Baked only once on purpose: products removed by hand must not come back.
            if (EditorPrefs.GetBool(AutoBakePrefKey, false))
            {
                return;
            }

            var settings = AssetDatabase.LoadAssetAtPath<GameplaySettings>(GameplaySettingsPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProductPrefabPath);
            if (settings == false || prefab == false)
            {
                return;
            }

            if (SceneAlreadyContainsBakedProducts())
            {
                EditorPrefs.SetBool(AutoBakePrefKey, true);

                return;
            }

            hasAutoBakedThisSession = true;
            EditorPrefs.SetBool(AutoBakePrefKey, true);

            try
            {
                BakeInternal();
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[BakeScene] Автозапекание упало: {exception}");
            }
        }

        private static bool SceneAlreadyContainsBakedProducts()
        {
            if (File.Exists(GameplayScenePath) == false)
            {
                return true;
            }

            foreach (var line in File.ReadLines(GameplayScenePath))
            {
                if (line.IndexOf("m_Name: Actor_Product_", System.StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        [MenuItem(
            MenuItemConstants.BaseToolsItemName + "/Bake Scene Fully into Editor",
            priority = MenuItemConstants.BaseToolsItemPriority - 20)]
        public static void Bake()
        {
            if (Application.isBatchMode == false &&
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
            {
                Debug.LogWarning("[BakeScene] Отменено, сцены не сохранены.");
                return;
            }

            BakeInternal();
        }

        /// <summary>
        /// Batchmode entry point, so the whole bake can run without opening the editor.
        /// </summary>
        public static void BakeBatch()
        {
            BakeInternal();
        }

        private static void BakeInternal()
        {
            var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProductPrefabPath);
            if (prefab == false)
            {
                Debug.LogError($"[BakeScene] Не найден {ProductPrefabPath}; сперва запусти «Setup Everything».");
                return;
            }

            var settings = AssetDatabase.LoadAssetAtPath<GameplaySettings>(GameplaySettingsPath);
            var items = settings != null
                ? settings.AvailableItems.Where(item => item).ToList()
                : new List<ItemData>();

            if (items.Count <= 0)
            {
                Debug.LogError("[BakeScene] Пустой ассортимент, запекать нечего.");
                return;
            }

            // Stable order, so the assortment spreads over the shelves the same way every re-bake.
            var shelfPoints = Object.FindObjectsByType<ProductShelfPointActor>(FindObjectsInactive.Include)
                .OrderBy(point => TransformPath(point.transform))
                .ToArray();

            var placed = 0;
            var skipped = 0;
            var nextItem = 0;

            foreach (var point in shelfPoints)
            {
                if (HasBakedProduct(point))
                {
                    skipped++;
                    continue;
                }

                var item = items[nextItem++ % items.Count];
                BakeProduct(point, prefab, item);
                placed++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorPrefs.SetBool(AutoBakePrefKey, true);

            Debug.Log(
                $"[BakeScene] Товар запечён: {placed}, на полках уже был: {skipped}. "
                + "Открой Scene_Gameplay — хлеб лежит на полках и его можно двигать руками."
            );
        }

        private static bool HasBakedProduct(ProductShelfPointActor point)
        {
            for (var i = 0; i < point.transform.childCount; i++)
            {
                if (point.transform.GetChild(i).GetComponent<ProductActor>())
                {
                    return true;
                }
            }

            return false;
        }

        private static void BakeProduct(ProductShelfPointActor point, GameObject prefab, ItemData item)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, point.transform);
            instance.name = $"Actor_Product_{item.Id}";

            var productTransform = instance.transform;
            productTransform.localPosition = Vector3.zero;
            productTransform.localRotation = Quaternion.identity;
            productTransform.localScale = Vector3.one;

            var product = instance.GetComponent<ProductActor>();
            if (product == false)
            {
                Object.DestroyImmediate(instance);
                return;
            }

            // Persist which item this product is, so the runtime adopts it instead of respawning.
            var serialized = new SerializedObject(product);
            serialized.FindProperty("item").objectReferenceValue = item;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // MaterialPropertyBlock is runtime-only, so bake the picture into a material per item
            // to keep the product visible and editable after reloading the scene.
            var imageRenderer = instance.GetComponentInChildren<MeshRenderer>();
            if (imageRenderer)
            {
                imageRenderer.sharedMaterial = EnsureProductMaterial(item);
            }
        }

        private static Material EnsureProductMaterial(ItemData item)
        {
            if (AssetDatabase.IsValidFolder(ProductsMaterialFolder) == false)
            {
                AssetDatabase.CreateFolder("Assets/Visuals/Objects/Materials", "Products");
            }

            var materialPath = $"{ProductsMaterialFolder}/Product_{Sanitize(item.Id)}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (material == false)
            {
                var source = AssetDatabase.LoadAssetAtPath<Material>(ProductMaterialPath);
                var shader = source ? source.shader : Shader.Find("Universal Render Pipeline/Unlit");

                material = new Material(shader) { name = $"Product_{item.Id}" };
                if (item.Image)
                {
                    material.SetTexture(TextureProperty, item.Image);
                }

                // Спрайты нарисованы с прозрачным фоном: без отсечения он рисуется чёрным квадратом.
                ApplyAlphaClip(material);

                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                if (item.Image)
                {
                    material.SetTexture(TextureProperty, item.Image);
                }

                ApplyAlphaClip(material);
            }

            return material;
        }

        private static void ApplyAlphaClip(Material material)
        {
            if (material.HasProperty("_AlphaClip"))
            {
                material.SetFloat("_AlphaClip", 1f);
            }

            if (material.HasProperty("_Cutoff"))
            {
                material.SetFloat("_Cutoff", 0.5f);
            }

            material.EnableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", "TransparentCutout");

            if (material.renderQueue < (int)RenderQueue.AlphaTest)
            {
                material.renderQueue = (int)RenderQueue.AlphaTest;
            }

            EditorUtility.SetDirty(material);
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "Item";
            }

            var forbidden = Path.GetInvalidFileNameChars();
            var clean = new string(value.Select(character => forbidden.Contains(character) ? '_' : character).ToArray());

            return clean.Length > 0 ? clean : "Item";
        }

        private static string TransformPath(Transform transform)
        {
            var indices = new List<int>();
            var current = transform;

            while (current != null)
            {
                indices.Add(current.GetSiblingIndex());
                current = current.parent;
            }

            indices.Reverse();

            return string.Join("/", indices.Select(index => index.ToString("D4")));
        }
    }
}
#endif
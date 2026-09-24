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
    /// Раскладывает весь ассортимент магазина по полкам ларька и держит спрайтовые материалы
    /// с включённым альфа-отсечением.
    /// </summary>
    /// <remarks>
    /// Полки ларька трёхуровневые: прилавок и две доски. Сетка 15×3 закрывает все 45 товаров
    /// каталога, поэтому в игре видно каждую картинку, а не только первую дюжину.
    /// Альфа-отсечение обязательно: у спрайтов прозрачный фон записан чёрным цветом, и в
    /// непрозрачном режиме он рисовался чёрным квадратом вокруг товара.
    /// </remarks>
    public static class KioskShelfGridBuilder
    {
        private const string GameplayScenePath = "Assets/Scenes/Scene_Gameplay.unity";
        private const string ShelfRootName = "Shop_Shelves";
        private const string ProductPrefabPath = "Assets/Prefabs/Actors/Actor_Product.prefab";
        private const string GameplaySettingsPath = "Assets/Settings/Game/Settings_Gameplay.asset";
        private const string ProductMaterialPath = "Assets/Visuals/Objects/Materials/Product_Image.mat";
        private const string ProductsMaterialFolder = "Assets/Visuals/Objects/Materials/Products";
        private const string TextureProperty = "_BaseMap";

        private const int Columns = 15;
        private const int Rows = 3;

        private const float FirstColumnX = -1.6f;
        private const float LastColumnX = 1.85f;

        /// <summary>Высоты поверхностей: прилавок и две доски полки.</summary>
        private static readonly float[] RowSurfaceHeight = { 0.94f, 1.68f, 2.13f };

        /// <summary>Глубина ряда: на прилавке товар стоит чуть ближе к покупателю.</summary>
        private static readonly float[] RowDepth = { 2.55f, 2.49f, 2.49f };

        private static readonly string[] SpriteMaterialFolders =
        {
            "Assets/Visuals/Objects/Materials",
            "Assets/Visuals/Products/Materials",
            "Assets/Visuals/Items/Materials",
        };

        [MenuItem(MenuItemConstants.BaseToolsItemName + "/Полки: разложить весь ассортимент", priority = 100)]
        public static void BuildFullAssortment()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[ShelfGrid] Сначала выйди из Play Mode — правки сцены в игре не сохраняются.");
                return;
            }

            var scene = EnsureGameplayScene();
            if (scene.IsValid() == false)
            {
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProductPrefabPath);
            if (prefab == false)
            {
                Debug.LogError($"[ShelfGrid] Не найден {ProductPrefabPath}.");
                return;
            }

            var settings = AssetDatabase.LoadAssetAtPath<GameplaySettings>(GameplaySettingsPath);
            var items = settings != null
                ? settings.AvailableItems.Where(item => item).ToList()
                : new List<ItemData>();

            if (items.Count <= 0)
            {
                Debug.LogError("[ShelfGrid] Пустой ассортимент, раскладывать нечего.");
                return;
            }

            var root = GameObject.Find(ShelfRootName);
            if (root == false)
            {
                root = new GameObject(ShelfRootName);
                Undo.RegisterCreatedObjectUndo(root, "Create shelf root");
            }

            var points = Object.FindObjectsByType<ProductShelfPointActor>(
                    FindObjectsInactive.Include
                )
                .OrderBy(point => TransformPath(point.transform))
                .ToArray();

            var slotCount = Columns * Rows;
            var created = 0;
            var reused = 0;
            var baked = 0;

            for (var index = 0; index < slotCount; index++)
            {
                var row = index / Columns;
                var column = index % Columns;
                var item = items[index % items.Count];

                ProductShelfPointActor point;

                if (index < points.Length)
                {
                    point = points[index];
                    reused++;
                }
                else
                {
                    var slot = new GameObject($"ShelfPoint_{row}_{column}");
                    slot.transform.SetParent(root.transform, worldPositionStays: true);
                    point = slot.AddComponent<ProductShelfPointActor>();
                    created++;
                }

                point.name = $"ShelfPoint_{row}_{column}";
                point.transform.SetPositionAndRotation(
                    new Vector3(
                        Mathf.Lerp(FirstColumnX, LastColumnX, column / (float)(Columns - 1)),
                        RowSurfaceHeight[row] + (item.DisplayHeight * 0.5f),
                        RowDepth[row]
                    ),
                    Quaternion.identity
                );

                var product = point.GetComponentInChildren<ProductActor>(true);

                if (product == false)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, point.transform);
                    product = instance.GetComponent<ProductActor>();
                    baked++;
                }

                if (product == false)
                {
                    continue;
                }

                product.name = $"Actor_Product_{item.Id}";

                var productTransform = product.transform;
                productTransform.localPosition = Vector3.zero;
                productTransform.localRotation = Quaternion.identity;
                productTransform.localScale = Vector3.one * item.DisplayHeight;

                var serialized = new SerializedObject(product);
                serialized.FindProperty("item").objectReferenceValue = item;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var renderer = product.GetComponentInChildren<MeshRenderer>();
                if (renderer)
                {
                    renderer.sharedMaterial = EnsureProductMaterial(item);
                }
            }

            // Полок больше, чем слотов сетки — выключаем лишние, но не удаляем.
            for (var index = slotCount; index < points.Length; index++)
            {
                points[index].gameObject.SetActive(false);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log(
                $"[ShelfGrid] Слотов: {slotCount} (переиспользовано {reused}, создано {created}). "
                + $"Товаров запечено заново: {baked}. Ассортимент: {items.Count}."
            );
        }

        [MenuItem(MenuItemConstants.BaseToolsItemName + "/Спрайты: включить альфа-отсечение", priority = 101)]
        public static void EnableAlphaClipOnSpriteMaterials()
        {
            var guids = AssetDatabase.FindAssets("t:Material", SpriteMaterialFolders);
            var fixedCount = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (material == false || material.HasProperty("_AlphaClip") == false)
                {
                    continue;
                }

                // Прозрачные материалы (контуры, лужи, дым) настраиваются отдельно.
                if (material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.5f)
                {
                    continue;
                }

                if (material.GetFloat("_AlphaClip") > 0.5f && material.renderQueue == (int)RenderQueue.AlphaTest)
                {
                    continue;
                }

                ApplyAlphaClip(material);
                fixedCount++;
            }

            AssetDatabase.SaveAssets();

            Debug.Log($"[ShelfGrid] Альфа-отсечение включено у {fixedCount} материалов.");
        }

        /// <summary>
        /// Включить отсечение прозрачных пикселей: спрайты нарисованы с прозрачным фоном, который
        /// в непрозрачном режиме выводится чёрным.
        /// </summary>
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

                ApplyAlphaClip(material);
                AssetDatabase.CreateAsset(material, materialPath);

                return material;
            }

            if (item.Image)
            {
                material.SetTexture(TextureProperty, item.Image);
            }

            ApplyAlphaClip(material);

            return material;
        }

        private static Scene EnsureGameplayScene()
        {
            var active = SceneManager.GetActiveScene();

            if (active.path == GameplayScenePath)
            {
                return active;
            }

            if (active.isDirty)
            {
                Debug.LogError($"[ShelfGrid] Сцена {active.path} не сохранена — сначала сохрани её.");
                return default;
            }

            return EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
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

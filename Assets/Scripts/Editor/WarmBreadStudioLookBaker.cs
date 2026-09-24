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
using UnityEngine.SceneManagement;

// UnityEditor.PlayerSettings is the editor's own project settings, this is the game's asset.
using GamePlayerSettings = UABPetelnia.GGJ2025.Runtime.Settings.PlayerSettings;

// Тела отключенных инструментов (после return-гвардов) оставлены намеренно — глушим CS0162.
#pragma warning disable 0162

namespace UABPetelnia.GGJ2025.Editor
{
    /// <summary>
    /// Bakes the look that used to be built only while the game was running back into the project,
    /// so the editor shows the same warm kiosk the player sees instead of a flat grey street.
    /// </summary>
    /// <remarks>
    /// Four things are written out:
    /// <list type="bullet">
    /// <item>
    /// the kiosk atmosphere (fog, ambient light, warm practical lamp, local reflections) into the
    /// gameplay scene;
    /// </item>
    /// <item>
    /// a body texture onto the player that is already placed in that scene, because a scene
    /// instance can carry a material override the prefab fix would never reach;
    /// </item>
    /// <item>
    /// the surface detail (normal maps, metallic, smoothness) into the material assets;
    /// </item>
    /// <item>
    /// a preview texture into the player and shopper prefabs, so a character is not a blank
    /// surface while a scene is being built.
    /// </item>
    /// </list>
    /// Then it reports every renderer that is still without a texture, so "the scene looks flat"
    /// is a question the log can answer.
    /// Idempotent: a second run changes nothing. Runtime code still overrides the character
    /// textures through a <c>MaterialPropertyBlock</c>, which is what makes every shopper look
    /// different and the player look hurt. The baked materials are the editor preview.
    /// </remarks>
    public static class WarmBreadStudioLookBaker
    {
        private const int ReportLimit = 25;

        private const string GameplayScenePath = "Assets/Scenes/Scene_Gameplay.unity";
        private const string GameplaySettingsPath = "Assets/Settings/Game/Settings_Gameplay.asset";
        private const string PlayerSettingsPath = "Assets/Settings/Game/Settings_Player.asset";
        private const string PlayerPrefabPath = "Assets/Prefabs/Actors/Actor_Player.prefab";
        private const string CharacterMaterialsParent = "Assets/Visuals/Objects/Materials";
        private const string CharacterMaterialsFolder = CharacterMaterialsParent + "/Characters";

        private const string BodyRendererField = "bodyRenderer";
        private const string TextureProperty = "_BaseMap";
        private const string UnlitShaderName = "Universal Render Pipeline/Unlit";

        [MenuItem(
            MenuItemConstants.BaseToolsItemName + "/Art/Bake Studio Look into Project",
            priority = MenuItemConstants.BaseToolsItemPriority - 25)]
        public static void Bake()
        {
            Debug.Log("[StudioLook] Процедурная подготовка вида сцены отключена: подгоняйте свет и материалы вручную в редакторе.");
            return;

            Bake(interactive: true);
        }

        /// <summary>
        /// Batchmode entry point, so the look can be baked without opening the editor.
        /// </summary>
        public static void BakeBatch()
        {
            Debug.Log("[StudioLook] Batch-bake отключён: всё делается вручную.");
            return;

            Bake(interactive: false);
        }

        /// <summary>
        /// Called by the setup pipeline, which has already asked the user about unsaved scenes.
        /// Asking twice in the middle of the pipeline would be confusing.
        /// </summary>
        internal static void BakeFromSetup()
        {
            Debug.Log("[StudioLook] Setup-bake отключён: всё делается вручную.");
            return;

            Bake(interactive: false);
        }

        private static void Bake(bool interactive)
        {
            if (interactive &&
                Application.isBatchMode == false &&
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
            {
                Debug.LogWarning("[StudioLook] Отменено: сцены не сохранены.");

                return;
            }

            // Created up front so this run always leaves the marker the auto-setup looks for.
            EnsureCharacterMaterialsFolder();

            BakeScene();
            BakeMaterialDetail();
            BakeCharacterPreviewTextures();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[StudioLook] Готово: сцена, материалы и префабы персонажей теперь выглядят "
                + "в редакторе так же, как в игре."
            );
        }

        private static void BakeScene()
        {
            var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);

            // Same code path the game runs, so the editor and the build can never drift apart.
            WarmBread.KioskAtmosphereController.ApplyToScene(scene);

            // A scene instance can carry its own material override, in which case fixing the prefab
            // alone would change nothing on screen.
            var playerBodies = BakeScenePlayerBodies(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log(
                $"[StudioLook] Сцена {GameplayScenePath}: туман, тёплое окружение, лампа и локальные "
                + $"отражения записаны, телам игроков отдана текстура ({playerBodies})."
            );

            ReportUntexturedRenderers(scene);
        }

        private static int BakeScenePlayerBodies(Scene scene)
        {
            var texture = LoadPlayerBodyTexture();
            if (texture == false)
            {
                return 0;
            }

            Material material = default;
            var baked = 0;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var player in root.GetComponentsInChildren<DesktopPlayerActor>(true))
                {
                    if (TryReadBodyRenderer(player, out var renderer) == false || renderer == false)
                    {
                        continue;
                    }

                    // Nothing to do for a player that already shows the healthy body, which is the
                    // normal case because the artist material is already wired up.
                    if (ShowsTexture(renderer.sharedMaterial, texture))
                    {
                        continue;
                    }

                    // Created once and only when a player actually needs it.
                    if (material == false)
                    {
                        material = EnsureCharacterMaterial(renderer.sharedMaterial, texture);
                    }

                    if (ApplyBodyMaterial(renderer, material))
                    {
                        baked++;
                    }
                }
            }

            return baked;
        }

        /// <summary>
        /// Says out loud what is still flat after the bake, so "the scene looks grey" can be
        /// answered from the log instead of from guesswork.
        /// </summary>
        private static void ReportUntexturedRenderers(Scene scene)
        {
            var textureless = new List<string>();

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (IsIgnoredRenderer(renderer) || HasTexture(renderer))
                    {
                        continue;
                    }

                    textureless.Add(
                        $"  • {DescribePath(renderer.transform)}  ({DescribeMaterial(renderer.sharedMaterial)})"
                    );
                }
            }

            if (textureless.Count == 0)
            {
                Debug.Log(
                    "[StudioLook] В сцене не осталось поверхностей без текстуры — редактор показывает "
                    + "то же, что и игра."
                );

                return;
            }

            var shown = textureless.Count > ReportLimit
                ? textureless.GetRange(0, ReportLimit)
                : textureless;

            Debug.LogWarning(
                $"[StudioLook] Без текстуры осталось рендереров: {textureless.Count}. Ниже список — "
                + "если тут есть что-то, что видно глазами, значит у него нет картинки в арт-паке:\n"
                + string.Join("\n", shown)
                + (textureless.Count > ReportLimit
                    ? $"\n  …и ещё {textureless.Count - ReportLimit}."
                    : string.Empty)
            );
        }

        private static bool IsIgnoredRenderer(Renderer renderer)
        {
            // Sprites carry their picture on the Sprite asset, and particles/lines/trails draw
            // through their own texture sheets, so a missing _BaseMap means nothing for them.
            if (renderer is SpriteRenderer
                or ParticleSystemRenderer
                or TrailRenderer
                or LineRenderer)
            {
                return true;
            }

            var material = renderer.sharedMaterial;
            if (material == false)
            {
                return false;
            }

            var shaderName = material.shader == false ? string.Empty : material.shader.name;

            return shaderName.Contains("TextMeshPro")
                || shaderName.StartsWith("UI/")
                || shaderName.StartsWith("Sprites/")
                || shaderName.StartsWith("Hidden/");
        }

        /// <summary>
        /// True when the material already shows exactly this texture, which is the case for the
        /// player, whose artist material is already the healthy body. Duplicating it into a second
        /// material would only give the project two materials that must be kept in sync.
        /// </summary>
        private static bool ShowsTexture(Material material, Texture2D texture)
        {
            if (material == false || texture == false)
            {
                return false;
            }

            if (material.mainTexture == texture)
            {
                return true;
            }

            return material.HasProperty(TextureProperty) && material.GetTexture(TextureProperty) == texture;
        }

        private static bool HasTexture(Renderer renderer)
        {
            var material = renderer.sharedMaterial;
            if (material == false)
            {
                return false;
            }

            if (material.mainTexture)
            {
                return true;
            }

            if (material.HasProperty(TextureProperty) && material.GetTexture(TextureProperty))
            {
                return true;
            }

            // A character shows its texture through a MaterialPropertyBlock, which is never part of
            // the material asset, so a renderer that has one is not "untextured".
            return renderer.HasPropertyBlock();
        }

        private static string DescribePath(Transform transform)
        {
            var path = transform.name;

            for (var parent = transform.parent; parent != null; parent = parent.parent)
            {
                path = parent.name + "/" + path;
            }

            return path;
        }

        private static string DescribeMaterial(Material material)
        {
            return material == false ? "нет материала" : material.name;
        }

        private static Texture2D LoadPlayerBodyTexture()
        {
            var playerSettings = AssetDatabase.LoadAssetAtPath<GamePlayerSettings>(PlayerSettingsPath);
            if (playerSettings == false)
            {
                Debug.LogWarning(
                    "[StudioLook] Нет " + PlayerSettingsPath + ", тело игрока останется пустым."
                );

                return default;
            }

            return playerSettings.GetHealthTexture(playerSettings.MaxHealth);
        }

        private static void BakeMaterialDetail()
        {
            var guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
            var changed = 0;

            for (var index = 0; index < guids.Length; index++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[index]);

                if (AssetDatabase.LoadAssetAtPath<Material>(path) is not { } material)
                {
                    continue;
                }

                if (WarmBread.MaterialAtmosphereEnhancer.TryEnhance(material) == false)
                {
                    continue;
                }

                EditorUtility.SetDirty(material);
                changed++;
            }

            AssetDatabase.SaveAssets();

            Debug.Log($"[StudioLook] Нормалмапы и PBR-параметры записаны в материалы: {changed}.");
        }

        private static void BakeCharacterPreviewTextures()
        {
            var baked = 0;

            if (BakeBodyTexture<DesktopPlayerActor>(PlayerPrefabPath, LoadPlayerBodyTexture()))
            {
                baked++;
            }

            baked += BakeShopperPreviewTextures();

            AssetDatabase.SaveAssets();

            Debug.Log($"[StudioLook] Превью-текстуры проставлены в префабы персонажей: {baked}.");
        }

        private static int BakeShopperPreviewTextures()
        {
            var gameplaySettings = AssetDatabase.LoadAssetAtPath<GameplaySettings>(GameplaySettingsPath);
            if (gameplaySettings == false)
            {
                Debug.LogWarning("[StudioLook] Нет " + GameplaySettingsPath + ", покупатели останутся пустыми.");

                return 0;
            }

            // Several shoppers can point at one prefab, and a prefab can only show one texture, so
            // the first one wins instead of the last one overwriting it.
            var alreadyBaked = new HashSet<string>();
            var baked = 0;

            foreach (var shopper in gameplaySettings.AvailableShoppers)
            {
                if (shopper == false || shopper.ShopperPrefab == false)
                {
                    continue;
                }

                var prefabPath = AssetDatabase.GetAssetPath(shopper.ShopperPrefab);
                if (string.IsNullOrEmpty(prefabPath) || alreadyBaked.Add(prefabPath) == false)
                {
                    continue;
                }

                if (BakeBodyTexture<ShopperActor>(prefabPath, shopper.Image))
                {
                    baked++;
                }
            }

            return baked;
        }

        private static bool BakeBodyTexture<TComponent>(string prefabPath, Texture2D texture)
            where TComponent : Component
        {
            if (texture == false)
            {
                return false;
            }

            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == false)
            {
                Debug.LogWarning($"[StudioLook] Нет префаба {prefabPath}.");

                return false;
            }

            try
            {
                var component = root.GetComponentInChildren<TComponent>(true);

                // `is null` on purpose: a generic type parameter has no Unity `==` overload in
                // scope, and prefab contents are never destroyed anyway.
                if (component is null)
                {
                    Debug.LogWarning($"[StudioLook] В {prefabPath} нет {typeof(TComponent).Name}.");

                    return false;
                }

                if (TryReadBodyRenderer(component, out var renderer) == false)
                {
                    Debug.LogWarning($"[StudioLook] В {prefabPath} нет поля {BodyRendererField}.");

                    return false;
                }

                var slots = renderer.sharedMaterials;
                var template = slots.Length > 0 ? slots[0] : default;

                if (ShowsTexture(template, texture))
                {
                    return true;
                }

                var material = EnsureCharacterMaterial(template, texture);
                if (material == false)
                {
                    return false;
                }

                if (ApplyBodyMaterial(renderer, material))
                {
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }

                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// The renderer field is private, so it is read through a SerializedObject. The snapshot is
        /// released before the renderer is touched, leaving no stale state behind. The returned
        /// renderer is a normal scene or prefab reference and outlives the snapshot.
        /// </summary>
        private static bool TryReadBodyRenderer(Component component, out Renderer renderer)
        {
            renderer = default;

            if (component == false)
            {
                return false;
            }

            using var serialized = new SerializedObject(component);

            if (serialized.FindProperty(BodyRendererField)?.objectReferenceValue is not Renderer found)
            {
                return false;
            }

            renderer = found;

            return true;
        }

        /// <summary>
        /// Only the body slot is replaced: a character with extra material slots keeps them exactly
        /// as they were. Returns false when there was nothing to change.
        /// </summary>
        private static bool ApplyBodyMaterial(Renderer renderer, Material material)
        {
            if (renderer == false || material == false)
            {
                return false;
            }

            var slots = renderer.sharedMaterials;

            if (slots.Length > 0 && slots[0] == material)
            {
                return false;
            }

            if (slots.Length <= 0)
            {
                renderer.sharedMaterial = material;
            }
            else
            {
                slots[0] = material;
                renderer.sharedMaterials = slots;
            }

            EditorUtility.SetDirty(renderer);

            return true;
        }

        private static void EnsureCharacterMaterialsFolder()
        {
            if (AssetDatabase.IsValidFolder(CharacterMaterialsParent) == false)
            {
                AssetDatabase.CreateFolder("Assets/Visuals/Objects", "Materials");
            }

            if (AssetDatabase.IsValidFolder(CharacterMaterialsFolder) == false)
            {
                AssetDatabase.CreateFolder(CharacterMaterialsParent, "Characters");
            }
        }

        /// <summary>
        /// A material per texture, cloned from whatever the renderer already uses so the shader and
        /// its settings stay exactly as the artist left them.
        /// </summary>
        private static Material EnsureCharacterMaterial(Material template, Texture2D texture)
        {
            EnsureCharacterMaterialsFolder();

            var materialName = "Character_" + Sanitize(texture.name);
            var path = $"{CharacterMaterialsFolder}/{materialName}.mat";

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == false)
            {
                if (template == false)
                {
                    var shader = Shader.Find(UnlitShaderName);
                    if (shader == false)
                    {
                        Debug.LogWarning("[StudioLook] Не найден шейдер " + UnlitShaderName + ".");

                        return default;
                    }

                    material = new Material(shader);
                }
                else
                {
                    material = new Material(template);
                }

                material.name = materialName;

                AssetDatabase.CreateAsset(material, path);
            }

            material.SetTexture(TextureProperty, texture);
            EditorUtility.SetDirty(material);

            return material;
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "Item";
            }

            var forbidden = Path.GetInvalidFileNameChars();
            var clean = new string(
                value.Select(character => forbidden.Contains(character) ? '_' : character).ToArray()
            );

            return clean.Length > 0 ? clean : "Item";
        }
    }
}
#endif

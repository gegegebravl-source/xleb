using UnityEngine;
using UnityEngine.SceneManagement;

namespace WarmBread
{
    /// <summary>
    /// Adds surface detail (normal maps, metallic, smoothness) to the street and kiosk materials,
    /// which have no baked-in detail of their own.
    /// </summary>
    /// <remarks>
    /// <see cref="TryEnhance"/> is public on purpose: the editor bake tool calls it on the material
    /// assets directly, so the detail is visible while the scene is being built instead of only
    /// appearing once the game is running.
    /// </remarks>
    public sealed class MaterialAtmosphereEnhancer : MonoBehaviour
    {
        private const string UniversalPipelineNamePart = "Universal Render Pipeline";
        private const string NormalMapProperty = "_BumpMap";
        private const string NormalScaleProperty = "_BumpScale";
        private const string NormalMapKeyword = "_NORMALMAP";
        private const string MetallicProperty = "_Metallic";
        private const string SmoothnessProperty = "_Smoothness";

        private const string KioskNormalPath = "PBR/GreenKioskPaint/GreenKioskPaint_Height";
        private const string ConcreteNormalPath = "PBR/ConcretePavement/ConcretePavement_Height";
        private const string PlasterNormalPath = "PBR/WornPlaster/WornPlaster_Height";
        private const string RustNormalPath = "PBR/RustedMetal/RustedMetal_Height";

        private static Texture2D kioskNormal;
        private static Texture2D concreteNormal;
        private static Texture2D plasterNormal;
        private static Texture2D rustNormal;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Enhance(SceneManager.GetActiveScene());
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Enhance(SceneManager.GetActiveScene());
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Enhance(scene);
        }

        private static void Enhance(Scene scene)
        {
            if (scene.IsValid() == false || scene.isLoaded == false)
            {
                return;
            }

            var roots = scene.GetRootGameObjects();

            for (var rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                var renderers = roots[rootIndex].GetComponentsInChildren<Renderer>(true);

                for (var index = 0; index < renderers.Length; index++)
                {
                    var materials = renderers[index].sharedMaterials;

                    for (var materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                    {
                        TryEnhance(materials[materialIndex]);
                    }
                }
            }
        }

        /// <summary>
        /// Add the surface detail that belongs to <paramref name="material"/>, if any rule applies.
        /// </summary>
        /// <returns>
        /// <c>true</c> when the material was changed, so a caller can tell whether it has to be
        /// saved back to disk.
        /// </returns>
        public static bool TryEnhance(Material material)
        {
            if (material == false || material.shader == false)
            {
                return false;
            }

            if (material.shader.name.Contains(UniversalPipelineNamePart) == false)
            {
                return false;
            }

            var name = material.name.ToLowerInvariant();

            if (name.Contains("kioskas"))
            {
                return Apply(
                    material,
                    LoadDetailMap(ref kioskNormal, KioskNormalPath),
                    0.7f,
                    0.08f,
                    0.34f
                );
            }

            if (name.Contains("saligatvis") || name.Contains("asfalt"))
            {
                return Apply(
                    material,
                    LoadDetailMap(ref concreteNormal, ConcreteNormalPath),
                    0.55f,
                    0f,
                    0.16f
                );
            }

            if (name.Contains("building"))
            {
                return Apply(
                    material,
                    LoadDetailMap(ref plasterNormal, PlasterNormalPath),
                    0.38f,
                    0f,
                    0.2f
                );
            }

            if (name.Contains("bin") || name.Contains("busstop") || name.Contains("mirror_frame"))
            {
                return Apply(
                    material,
                    LoadDetailMap(ref rustNormal, RustNormalPath),
                    0.48f,
                    0.55f,
                    0.18f
                );
            }

            return false;
        }

        /// <summary>
        /// The detail maps live in <c>Assets/WarmBread/Resources</c> and are read on demand, so the
        /// editor bake tool can use them without the runtime scene pass having run first.
        /// </summary>
        private static Texture2D LoadDetailMap(ref Texture2D cache, string resourcesPath)
        {
            if (cache == false)
            {
                cache = Resources.Load<Texture2D>(resourcesPath);
            }

            return cache;
        }

        private static bool Apply(
            Material material,
            Texture2D normal,
            float strength,
            float metallic,
            float smoothness
        )
        {
            var isChanged = false;

            if (normal != false && material.HasProperty(NormalMapProperty))
            {
                if (material.GetTexture(NormalMapProperty) != normal)
                {
                    material.SetTexture(NormalMapProperty, normal);
                    isChanged = true;
                }

                isChanged |= SetFloat(material, NormalScaleProperty, strength);

                if (material.IsKeywordEnabled(NormalMapKeyword) == false)
                {
                    material.EnableKeyword(NormalMapKeyword);
                    isChanged = true;
                }
            }

            isChanged |= SetFloat(material, MetallicProperty, metallic);
            isChanged |= SetFloat(material, SmoothnessProperty, smoothness);

            if (material.enableInstancing == false)
            {
                // The same product or piece of street furniture is drawn many times per frame.
                material.enableInstancing = true;
                isChanged = true;
            }

            return isChanged;
        }

        /// <summary>
        /// Set a float property, but only when the shader actually has it. Writing to a missing
        /// property would fill the console with "material doesn't have a float property".
        /// </summary>
        private static bool SetFloat(Material material, string property, float value)
        {
            if (material.HasProperty(property) == false)
            {
                return false;
            }

            if (Mathf.Approximately(material.GetFloat(property), value))
            {
                return false;
            }

            material.SetFloat(property, value);

            return true;
        }
    }
}

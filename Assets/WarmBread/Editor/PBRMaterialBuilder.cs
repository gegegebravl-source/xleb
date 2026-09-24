using System.IO;
using UnityEditor;
using UnityEngine;

namespace WarmBread.Editor
{
    public static class PBRMaterialBuilder
    {
        private const string Root = "Assets/WarmBread/Resources/PBR";

        [MenuItem("Warm Bread/Art/Rebuild PBR Materials")]
        public static void Rebuild()
        {
            Build("ConcretePavement", 0f, 0.22f, new Vector2(2f, 2f));
            Build("WornPlaster", 0f, 0.18f, new Vector2(1.4f, 1.4f));
            Build("GreenKioskPaint", 0.08f, 0.42f, new Vector2(1f, 1f));
            Build("RustedMetal", 0.72f, 0.2f, new Vector2(1.3f, 1.3f));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[WarmBread] Созданы четыре URP PBR-материала в Assets/WarmBread/Resources/PBR.");
        }

        private static void Build(string name, float metallic, float smoothness, Vector2 tiling)
        {
            var folder = Root + "/" + name;
            var baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/" + name + "_BaseColor.png");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/" + name + "_Height.png");
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null || baseMap == null) return;
            var materialPath = folder + "/MAT_" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "MAT_" + name };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.shader = shader;
            material.SetTexture("_BaseMap", baseMap);
            material.SetTextureScale("_BaseMap", tiling);
            material.SetTexture("_BumpMap", normal);
            material.SetTextureScale("_BumpMap", tiling);
            material.SetFloat("_BumpScale", 0.65f);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            material.EnableKeyword("_NORMALMAP");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
        }
    }
}

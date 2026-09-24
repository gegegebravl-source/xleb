using UnityEditor;

namespace WarmBread.Editor
{
    public sealed class WarmBreadTextureImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/WarmBread/Resources/PBR/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.wrapMode = UnityEngine.TextureWrapMode.Repeat;
            importer.filterMode = UnityEngine.FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.mipmapEnabled = true;
            importer.streamingMipmaps = true;
            importer.maxTextureSize = 2048;
            if (assetPath.EndsWith("_Height.png"))
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.convertToNormalmap = true;
                importer.heightmapScale = 0.12f;
                importer.sRGBTexture = false;
            }
            else if (assetPath.EndsWith("_Roughness.png"))
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = false;
            }
            else importer.sRGBTexture = true;
        }
    }
}

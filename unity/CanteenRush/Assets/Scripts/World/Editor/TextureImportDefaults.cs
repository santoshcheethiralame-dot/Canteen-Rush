using UnityEditor;

namespace CanteenRush.World.Editor
{
    // Spec §8/§4: nothing over 1024 px, mipmapped, no CPU copy. Applied to every texture under Assets/Art so .meta files never drift.
    public class TextureImportDefaults : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Art/")) return;
            var t = (TextureImporter)assetImporter;
            t.maxTextureSize = 1024;
            t.mipmapEnabled = true;
            t.isReadable = false;
            t.textureCompression = TextureImporterCompression.Compressed;
            t.wrapMode = UnityEngine.TextureWrapMode.Repeat;
            t.anisoLevel = 4;
        }
    }
}

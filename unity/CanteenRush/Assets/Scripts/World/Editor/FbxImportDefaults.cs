using UnityEditor;

namespace CanteenRush.World.Editor
{
    // Every FBX under Assets/Art is produced by tools/blender/build_world.py in metres, Y-up, scale already applied.
    // These settings make the import deterministic for everyone so the .meta files never drift.
    public class FbxImportDefaults : AssetPostprocessor
    {
        const string ArtRoot = "Assets/Art/";

        // Backdrops sit beyond lightmap range and are lit by probes, so they get no second UV set.
        static bool IsBackdrop(string path) { return path.EndsWith("Env_PodsBackdrop.fbx") || path.EndsWith("Env_Terrace.fbx") || path.EndsWith("Env_Skyline.fbx"); }

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(ArtRoot) || !assetPath.EndsWith(".fbx")) return;

            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.addCollider = false;
            // Environment meshes stay readable: the NavMesh bake, MeshColliders and chair-facing detection read them.
            importer.isReadable = assetPath.Contains("/Environment/") || assetPath.EndsWith("Prop_ChairBlue.fbx");
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;

            // Lightmapped architecture needs a second UV set; small movable props do not.
            importer.generateSecondaryUV = assetPath.Contains("/Environment/") && !IsBackdrop(assetPath);
        }
    }
}

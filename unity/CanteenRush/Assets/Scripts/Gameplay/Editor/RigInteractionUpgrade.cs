using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
namespace CanteenRush.Gameplay.Editor
{
    public static class RigInteractionUpgrade
    {
        const string Path = "Assets/Prefabs/Systems/Rig.prefab";
        [InitializeOnLoadMethod]
        static void Schedule() { EditorApplication.delayCall += Repair; }
        [MenuItem("Canteen Rush/Gameplay/Repair rig interaction masks")]
        public static void Repair()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Path); if (asset == null) return;
            int targets = LayerMask.GetMask("Items", "Tray", "Stations", "WorldUI", "UI");
            bool repair = false;
            foreach (var caster in asset.GetComponentsInChildren<SphereInteractionCaster>(true))
                repair |= (caster.physicsLayerMask.value & targets) != targets;
            foreach (var caster in asset.GetComponentsInChildren<CurveInteractionCaster>(true))
                repair |= (caster.raycastMask.value & targets) != targets;
            foreach (var ray in asset.GetComponentsInChildren<XRRayInteractor>(true))
                repair |= (ray.raycastMask.value & targets) != targets;
            if (!repair) return;
            var root = PrefabUtility.LoadPrefabContents(Path);
            try
            {
                root.GetComponent<ComfortSettings>().ConfigureInteractionMasks();
                PrefabUtility.SaveAsPrefabAsset(root, Path);
                Debug.Log("P2 rig repaired: hand/ray detection now includes gameplay physics layers.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}

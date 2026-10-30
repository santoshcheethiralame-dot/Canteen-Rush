using Unity.AI.Navigation;
using UnityEngine;

namespace CanteenRush.World
{
    // Safety net: if the baked NavMesh data did not travel with the scene, bake it once when the scene starts.
    // Needs readable meshes on the Environment layer (FbxImportDefaults keeps Environment FBX readable for this).
    [RequireComponent(typeof(NavMeshSurface))]
    public class NavMeshBootstrap : MonoBehaviour
    {
        void Start()
        {
            var surface = GetComponent<NavMeshSurface>();
            if (surface.navMeshData == null) surface.BuildNavMesh();
        }
    }
}

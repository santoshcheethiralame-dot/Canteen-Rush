using Unity.AI.Navigation;
using UnityEngine;
namespace CanteenRush.Gameplay
{
    public sealed class SandboxBootstrap : MonoBehaviour
    {
        [SerializeField] NavMeshSurface _surface;
        [SerializeField] GameManager _game;
        void Start()
        {
            WorldGameplayInstaller.AddTeleportSurfaces();
            if (_surface != null) _surface.BuildNavMesh();
            if (_game != null) _game.Play();
        }
    }
}

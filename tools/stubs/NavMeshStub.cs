namespace Unity.AI.Navigation {
  public enum CollectObjects { All = 0, Volume = 1, Children = 2, MarkedWithModifier = 3 }
  public class NavMeshSurface : UnityEngine.MonoBehaviour {
    public CollectObjects collectObjects { get; set; }
    public UnityEngine.LayerMask layerMask { get; set; }
    public UnityEngine.AI.NavMeshData navMeshData { get; set; }
    public void BuildNavMesh() {}
  }
}

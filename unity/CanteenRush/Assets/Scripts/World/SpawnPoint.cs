using UnityEngine;

namespace CanteenRush.World
{
    // Marks where an item should sit: one per donut plate, cola can, shake glass, cake and the cup dispenser.
    // ItemKey is the item name without the "Spawn_" prefix and index: Donut, Cola, Shake, Cake or Cup.
    // Gameplay code (P2) finds these with FindObjectsByType and places its ItemSpawner prefab at each one.
    [DisallowMultipleComponent]
    public class SpawnPoint : MonoBehaviour
    {
        public string ItemKey;

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.7f, 0.1f, 0.9f);
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.08f);
        }
    }
}

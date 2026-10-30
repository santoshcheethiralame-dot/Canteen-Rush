using UnityEngine;

namespace CanteenRush.World
{
    // One of the 8 gameplay seats on the patio. Index matches contracts section 7 (seat 2t and 2t+1 belong to table t).
    // Placed by MainSceneBuilder; transform.forward points at the table (the way a seated customer faces).
    [DisallowMultipleComponent]
    public class SeatPoint : MonoBehaviour
    {
        public int Index;

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.9f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f, 0.2f);
            Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, transform.forward * 0.6f);
        }
    }
}

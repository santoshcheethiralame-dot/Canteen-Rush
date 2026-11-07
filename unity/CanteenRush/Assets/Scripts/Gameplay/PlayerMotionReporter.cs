using CanteenRush.Core;
using UnityEngine;
namespace CanteenRush.Gameplay
{
    public sealed class PlayerMotionReporter : MonoBehaviour
    {
        [SerializeField] Transform _head;
        [SerializeField] LayerMask _floorMask;
        [SerializeField] GameConfig _config;
        Vector3 _previous;
        float _distance;
        void OnEnable() { _previous = transform.position; _distance = 0; }
        void Update()
        {
            Vector3 delta = transform.position - _previous; delta.y = 0; _previous = transform.position;
            float distance = delta.magnitude; _distance += distance;
            GameEvents.PlayerMoveSpeed(Time.deltaTime > 0 ? distance / Time.deltaTime : 0);
            float stride = _config != null ? _config.FootstepDistance : .5f;
            if (_distance < stride) return;
            _distance %= stride; var origin = _head != null ? _head.position : transform.position + Vector3.up;
            Surface surface = Surface.Tile; Vector3 position = transform.position;
            if (Physics.Raycast(origin, Vector3.down, out var hit, 3f, _floorMask))
            { position = hit.point; if (hit.collider.CompareTag("SurfaceStone")) surface = Surface.Stone; }
            GameEvents.Footstep(surface, position);
        }
    }
}

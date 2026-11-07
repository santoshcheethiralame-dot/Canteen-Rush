using CanteenRush.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
namespace CanteenRush.Gameplay
{
    [RequireComponent(typeof(HingeJoint))]
    public sealed class FridgeDoor : MonoBehaviour
    {
        [SerializeField] int _doorIndex;
        [SerializeField] GameConfig _config;
        [SerializeField] Behaviour[] _protectedGrabs;
        [SerializeField] Rigidbody[] _protectedBodies;
        HingeJoint _hinge;
        Rigidbody _body;
        Vector3 _closedPosition;
        Quaternion _closedRotation;
        float _previous;
        bool _openLimit, _closedLimit;
        public int DoorIndex => _doorIndex;
        public bool IsOpen => _hinge != null && Mathf.Abs(_hinge.angle) >= (_config != null ? _config.FridgeLatchDeg : 8f);
        public void UseSourceGeometry(Transform source, Vector3 playerPosition)
        {
            var placeholder = transform.Find("Visual"); if (placeholder != null) { placeholder.gameObject.SetActive(false); Destroy(placeholder.gameObject); }
            var visual = Instantiate(source.gameObject, transform); visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero; visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = source.lossyScale; visual.SetActive(true);
            foreach (var child in visual.GetComponentsInChildren<Transform>(true)) child.gameObject.isStatic = false;
            foreach (var collider in visual.GetComponentsInChildren<Collider>()) collider.enabled = false;
            bool first = true; Bounds bounds = default;
            foreach (var filter in visual.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = mesh.center + Vector3.Scale(mesh.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var point = transform.InverseTransformPoint(filter.transform.TransformPoint(corner));
                    if (first) { bounds = new Bounds(point, Vector3.zero); first = false; } else bounds.Encapsulate(point);
                }
            }
            var box = GetComponent<BoxCollider>(); box.center = bounds.center; box.size = bounds.size;
            var grab = GetComponent<XRGrabInteractable>(); var handle = grab.attachTransform;
            bool widthX = bounds.size.x > bounds.size.z;
            var toward = transform.InverseTransformPoint(playerPosition);
            Vector3 position = bounds.center;
            if (widthX)
            { position.x = Mathf.Abs(bounds.min.x) > Mathf.Abs(bounds.max.x) ? bounds.min.x + .06f : bounds.max.x - .06f; position.z += Mathf.Sign(toward.z - bounds.center.z) * (bounds.extents.z + .04f); }
            else
            { position.z = Mathf.Abs(bounds.min.z) > Mathf.Abs(bounds.max.z) ? bounds.min.z + .06f : bounds.max.z - .06f; position.x += Mathf.Sign(toward.x - bounds.center.x) * (bounds.extents.x + .04f); }
            handle.localPosition = position;
        }
        void Awake()
        {
            _hinge = GetComponent<HingeJoint>(); _body = GetComponent<Rigidbody>();
            _closedPosition = transform.position; _closedRotation = transform.rotation;
        }
        void Update()
        {
            float angle = Mathf.Abs(_hinge.angle), latch = _config != null ? _config.FridgeLatchDeg : 8f;
            bool detent = (_previous < latch && angle >= latch) || (_previous >= latch && angle < latch);
            if (Mathf.Abs(angle - _previous) >= 1 || detent)
            {
                GameEvents.FridgeDoorMoved(_doorIndex, Mathf.Clamp01(angle / (_config != null ? _config.FridgeMaxDeg : 110f)), detent);
                _previous = angle;
            }
            bool open = angle >= (_config != null ? _config.FridgeStopDeg : 100f), closed = angle <= 1;
            if (open && !_openLimit) GameEvents.FridgeDoorLimit(_doorIndex, true);
            if (closed && !_closedLimit) GameEvents.FridgeDoorLimit(_doorIndex, false);
            _openLimit = open; _closedLimit = closed;
            if (_protectedGrabs != null) foreach (var grab in _protectedGrabs) if (grab != null) grab.enabled = IsOpen;
            if (_protectedBodies != null) foreach (var body in _protectedBodies) if (body != null) body.isKinematic = !IsOpen;
        }
        public void ResetDoor()
        {
            if (_hinge == null) Awake();
            var grab = GetComponent<XRGrabInteractable>();
            if (grab != null && grab.interactionManager != null) grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
            _body.linearVelocity = Vector3.zero; _body.angularVelocity = Vector3.zero;
            _body.position = _closedPosition; _body.rotation = _closedRotation;
            transform.SetPositionAndRotation(_closedPosition, _closedRotation);
            _previous = 0; _openLimit = _closedLimit = false;
        }
    }
}

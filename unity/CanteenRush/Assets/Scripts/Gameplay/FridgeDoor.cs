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
        void Start() { Invoke(nameof(IgnoreStaticOverlaps), .1f); }
        /// The closed door's collision box overlaps the fridge body and the other door, which locks the hinge
        /// (found by the desktop-mode tests). Ignore collisions with anything it already overlaps when closed.
        void IgnoreStaticOverlaps()
        {
            var box = GetComponent<BoxCollider>(); if (box == null) return;
            foreach (var other in Physics.OverlapBox(box.bounds.center, box.bounds.extents, transform.rotation, ~0, QueryTriggerInteraction.Ignore))
            {
                if (other == box || other.transform.IsChildOf(transform)) continue;
                var body = other.attachedRigidbody;
                if (body == null || body.isKinematic || other.GetComponentInParent<FridgeDoor>() != null) Physics.IgnoreCollision(box, other, true);
            }
        }
        float _swingUntil, _swingDir;
        const float SwingDegPerSecond = 150f;
        /// Desktop mode: swing the door open or shut. The body goes kinematic for the swing and is turned about
        /// its hinge by hand, then handed back to physics, so no collider or joint quirk can stall it.
        public void Toggle()
        {
            if (_hinge == null) Awake();
            _swingDir = IsOpen ? -1f : 1f;      // +1 opens, -1 closes
            _swingUntil = Time.time + 1.6f;
        }
        void FixedUpdate()
        {
            if (_swingUntil <= 0) return;
            float angle = Mathf.Abs(_hinge.angle), stop = (_config != null ? _config.FridgeStopDeg : 100f) - 3f;
            bool done = Time.time >= _swingUntil || (_swingDir > 0 && angle >= stop) || (_swingDir < 0 && angle <= 1.5f);
            if (done) { _swingUntil = 0; _body.isKinematic = false; _body.linearVelocity = Vector3.zero; _body.angularVelocity = Vector3.zero; return; }
            _body.isKinematic = true;
            float openSign = Mathf.Abs(_hinge.limits.max) >= Mathf.Abs(_hinge.limits.min) ? 1f : -1f;
            // Rotating about +axis raises the hinge angle (checked by DesktopModeTests: the door must stay open afterwards).
            Vector3 axis = transform.TransformDirection(_hinge.axis).normalized;
            var turn = Quaternion.AngleAxis(openSign * _swingDir * SwingDegPerSecond * Time.fixedDeltaTime, axis);
            Vector3 pivot = transform.TransformPoint(_hinge.anchor);
            _body.MovePosition(pivot + turn * (_body.position - pivot));
            _body.MoveRotation(turn * _body.rotation);
        }
        public void ResetDoor()
        {
            if (_hinge == null) Awake();
            _swingUntil = 0; _body.isKinematic = false;
            var grab = GetComponent<XRGrabInteractable>();
            if (grab != null && grab.interactionManager != null) grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
            _body.linearVelocity = Vector3.zero; _body.angularVelocity = Vector3.zero;
            _body.position = _closedPosition; _body.rotation = _closedRotation;
            transform.SetPositionAndRotation(_closedPosition, _closedRotation);
            _previous = 0; _openLimit = _closedLimit = false;
        }
    }
}

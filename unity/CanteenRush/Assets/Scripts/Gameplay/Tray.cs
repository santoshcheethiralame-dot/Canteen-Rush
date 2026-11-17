using System.Collections.Generic;
using CanteenRush.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
namespace CanteenRush.Gameplay
{
    [RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody))]
    public sealed class Tray : MonoBehaviour
    {
        [SerializeField] GameConfig _config;
        [SerializeField] XRSocketInteractor[] _sockets;
        readonly List<ItemId> _contents = new List<ItemId>(4);
        XRGrabInteractable _grab;
        Rigidbody _body;
        Vector3 _homePosition;
        Quaternion _homeRotation;
        float _spillTimer, _returnTimer = -1;
        public bool Returning => _returnTimer >= 0;
        /// Desktop mode: where the next item should be set down (the first socket with nothing in it).
        public bool TryGetFreeSocket(out XRSocketInteractor free)
        {
            foreach (var socket in _sockets)
                if (socket != null && !socket.hasSelection) { free = socket; return true; }
            free = null; return false;
        }
        public IReadOnlyList<ItemId> Contents
        {
            get
            {
                _contents.Clear();
                foreach (var socket in _sockets)
                    for (int i = 0; i < socket.interactablesSelected.Count; i++)
                    {
                        var item = socket.interactablesSelected[i].transform.GetComponent<ItemBehaviour>();
                        if (item != null && item.Id != ItemId.None) _contents.Add(item.Id);
                    }
                return _contents;
            }
        }
        Collider[] _trayColliders;
        void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>(); _body = GetComponent<Rigidbody>(); SetHome(transform.position, transform.rotation);
            _trayColliders = GetComponents<Collider>();
            foreach (var socket in _sockets)
            {
                if (socket == null) continue;
                socket.selectEntered.AddListener(Socketed);
                socket.selectExited.AddListener(Unsocketed);
            }
        }
        // Food is wider than its slot and rides kinematically, so left alone it pushes against the tray and its
        // neighbours and shoves the tray off the rack. Socketed food never collides with the tray or other food on it.
        void Socketed(SelectEnterEventArgs args)
        {
            var item = args.interactableObject.transform;
            SeatOnSurface((XRSocketInteractor)args.interactorObject, item);
            IgnoreWithTray(item, true);
            if (!_grab.isSelected && !_body.isKinematic) { _body.linearVelocity = Vector3.zero; _body.angularVelocity = Vector3.zero; }
        }
        // Turning collisions back on while the food still overlaps the tray would fire both apart, so wait until it is clear.
        void Unsocketed(SelectExitEventArgs args)
        {
            var item = args.interactableObject.transform;
            if (isActiveAndEnabled && gameObject.activeInHierarchy) StartCoroutine(RestoreWhenClear(item)); else IgnoreWithTray(item, false);
        }
        System.Collections.IEnumerator RestoreWhenClear(Transform item)
        {
            for (float t = 0; t < 3f; t += Time.deltaTime)
            {
                yield return null;
                if (item == null) yield break;
                if (Held(item)) yield break;   // back in a slot: stays ignored
                if (!Overlaps(item)) break;
            }
            if (item != null && !Held(item)) IgnoreWithTray(item, false);
        }
        bool Held(Transform item)
        {
            foreach (var socket in _sockets)
                if (socket != null) foreach (var s in socket.interactablesSelected) if (s.transform == item) return true;
            return false;
        }
        bool Overlaps(Transform item)
        {
            foreach (var a in item.GetComponentsInChildren<Collider>())
                foreach (var t in _trayColliders)
                    if (a != null && t != null && !a.isTrigger && !t.isTrigger && a.bounds.Intersects(t.bounds)) return true;
            return false;
        }
        void IgnoreWithTray(Transform item, bool ignore)
        {
            var mine = item.GetComponentsInChildren<Collider>(true);
            foreach (var a in mine)
            {
                if (a == null || a.isTrigger) continue;
                foreach (var t in _trayColliders) if (t != null && !t.isTrigger) Physics.IgnoreCollision(a, t, ignore);
                foreach (var socket in _sockets)
                {
                    if (socket == null) continue;
                    foreach (var other in socket.interactablesSelected)
                    {
                        if (other.transform == item) continue;
                        foreach (var b in other.transform.GetComponentsInChildren<Collider>(true))
                            if (b != null && !b.isTrigger) Physics.IgnoreCollision(a, b, ignore);
                    }
                }
            }
        }
        // Rest the item's lowest point on the tray's top face instead of burying its pivot in the slot.
        void SeatOnSurface(XRSocketInteractor socket, Transform item)
        {
            if (socket.attachTransform == null) return;
            float top = float.NegativeInfinity;
            foreach (var t in _trayColliders) if (t is BoxCollider box && !box.isTrigger) top = Mathf.Max(top, box.center.y + box.size.y * .5f);
            if (float.IsNegativeInfinity(top)) return;
            float bottom = float.PositiveInfinity;
            foreach (var c in item.GetComponentsInChildren<Collider>(true))
            {
                if (c == null || c.isTrigger) continue;
                float local;
                if (c is BoxCollider b) local = b.center.y - b.size.y * .5f;
                else if (c is CapsuleCollider k) local = k.center.y - Mathf.Max(k.height * .5f, k.radius);
                else if (c is SphereCollider sp) local = sp.center.y - sp.radius;
                else continue;
                bottom = Mathf.Min(bottom, item.InverseTransformPoint(c.transform.TransformPoint(new Vector3(0, local, 0))).y);
            }
            if (float.IsPositiveInfinity(bottom)) bottom = 0;
            var attach = socket.attachTransform; var p = attach.localPosition;
            // attach is a child of the slot; slot sits at socket.transform.localPosition in tray space
            p.y = (top - socket.transform.localPosition.y) - bottom + .002f;
            attach.localPosition = p;
        }
        public void SetHome(Vector3 position, Quaternion rotation) { _homePosition = position; _homeRotation = rotation; }
        void Update()
        {
            if (_returnTimer >= 0)
            {
                _returnTimer -= Time.deltaTime;
                if (_returnTimer <= 0) ResetTray();
                return;
            }
            if (_spillTimer > 0)
            {
                _spillTimer -= Time.deltaTime;
                if (_spillTimer <= 0) foreach (var socket in _sockets) socket.socketActive = true;
            }
            if (!_grab.isSelected || _grab.firstInteractorSelecting is XRSocketInteractor || _config == null) return;
            float angle = Vector3.Angle(transform.up, Vector3.up);
            if (angle >= _config.TrayTiltWarnDeg) GameEvents.TrayTilt(angle, HandResolver.Resolve(_grab.firstInteractorSelecting));
            if (angle > _config.TrayTiltSpillDeg && _spillTimer <= 0)
            {
                _spillTimer = _config.SocketCooldown;
                foreach (var socket in _sockets)
                { socket.socketActive = false; socket.interactionManager.CancelInteractorSelection((IXRSelectInteractor)socket); }
            }
        }
        public void BeginReturn() { _returnTimer = _config.TrayReturnSeconds; }
        public void ClearItems()
        {
            foreach (var socket in _sockets)
                while (socket.interactablesSelected.Count > 0)
                {
                    var selected = socket.interactablesSelected[0];
                    var item = selected.transform.GetComponent<ItemBehaviour>();
                    socket.interactionManager.SelectExit((IXRSelectInteractor)socket, selected);
                    if (item != null) item.Recycle(); else selected.transform.gameObject.SetActive(false);
                }
        }
        public void ResetTray()
        {
            if (_grab == null) Awake();
            _returnTimer = -1; _spillTimer = 0;
            if (_grab.interactionManager != null) _grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)_grab);
            ClearItems();
            foreach (var socket in _sockets) socket.socketActive = true;
            _body.linearVelocity = Vector3.zero; _body.angularVelocity = Vector3.zero;
            _body.position = _homePosition; _body.rotation = _homeRotation;
            transform.SetPositionAndRotation(_homePosition, _homeRotation);
        }
    }
}

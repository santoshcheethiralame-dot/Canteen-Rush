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
        void Awake() { _grab = GetComponent<XRGrabInteractable>(); _body = GetComponent<Rigidbody>(); SetHome(transform.position, transform.rotation); }
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

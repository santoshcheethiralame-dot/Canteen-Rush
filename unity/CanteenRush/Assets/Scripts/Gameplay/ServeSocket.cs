using CanteenRush.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
namespace CanteenRush.Gameplay
{
    [RequireComponent(typeof(XRSocketInteractor))]
    public sealed class ServeSocket : MonoBehaviour
    {
        [SerializeField] int _seatIndex;
        [SerializeField] OrderService _orders;
        [SerializeField] GameConfig _config;
        [SerializeField] Transform _trayReturn;
        XRSocketInteractor _socket;
        public int SeatIndex => _seatIndex;
        public void Configure(int seat, OrderService orders) { _seatIndex = seat; _orders = orders; }
        void Awake() { _socket = GetComponent<XRSocketInteractor>(); }
        void OnEnable() { _socket.selectEntered.AddListener(Placed); }
        void OnDisable() { _socket.selectEntered.RemoveListener(Placed); }
        void Placed(SelectEnterEventArgs args)
        {
            var tray = args.interactableObject.transform.GetComponent<Tray>();
            if (tray == null) return;
            bool active = _orders != null && _orders.HasOrder(_seatIndex) && !tray.Returning;
            GameEvents.TrayServedPlaced(transform.position, active);
            if (active && _orders.Serve(_seatIndex, tray.Contents)) tray.BeginReturn();
        }
        public void ResetSocket()
        {
            if (_socket == null) _socket = GetComponent<XRSocketInteractor>();
            if (_socket.interactionManager != null) _socket.interactionManager.CancelInteractorSelection((IXRSelectInteractor)_socket);
        }
    }
}

using CanteenRush.Core;
using UnityEngine;
namespace CanteenRush.UI.Gameplay
{
    public sealed class CustomerBubble : MonoBehaviour
    {
        [SerializeField] int _seatIndex = -1;
        [SerializeField] OrderTicket _ticket;
        [SerializeField] Transform _viewer;
        void OnEnable()
        {
            _seatIndex = -1;
            if (_ticket != null) _ticket.Hide();
            if (_viewer == null && Camera.main != null) _viewer = Camera.main.transform;
            GameEvents.OnOrderPlaced += Placed; GameEvents.OnOrderPatience += Patience;
            GameEvents.OnOrderExpired += Expired; GameEvents.OnOrderServed += Served;
            GameEvents.OnCustomerState += StateChanged;
        }
        void OnDisable()
        {
            GameEvents.OnOrderPlaced -= Placed; GameEvents.OnOrderPatience -= Patience;
            GameEvents.OnOrderExpired -= Expired; GameEvents.OnOrderServed -= Served;
            GameEvents.OnCustomerState -= StateChanged;
        }
        public void SetSeat(int seat) { _seatIndex = seat; }
        void Placed(Order order)
        {
            // Order anchors are the seated customer's root, so a pooled bubble binds without calling gameplay.
            if (_seatIndex < 0 && (order.SeatPosition - transform.parent.position).sqrMagnitude < .25f) _seatIndex = order.SeatIndex;
            if (order.SeatIndex == _seatIndex && _ticket != null) _ticket.Show(order);
        }
        void Patience(Order order, float value) { if (order.SeatIndex == _seatIndex && _ticket != null) _ticket.SetPatience(value); }
        void Expired(Order order) { if (order.SeatIndex == _seatIndex && _ticket != null) _ticket.ShowAngry(); }
        void Served(ServeResult result) { if (_ticket != null && _ticket.OrderId == result.OrderId) _ticket.Hide(); }
        void StateChanged(int seat, CustomerState state) { if (seat == _seatIndex && state == CustomerState.Leaving && _ticket != null) _ticket.Hide(); }
        void LateUpdate()
        {
            if (_viewer == null) return;
            var direction = transform.position - _viewer.position; direction.y = 0;
            if (direction.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}

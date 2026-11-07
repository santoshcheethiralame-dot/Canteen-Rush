using CanteenRush.Core;
using UnityEngine;
namespace CanteenRush.UI.Gameplay
{
    public sealed class OrderRail : MonoBehaviour
    {
        [SerializeField] OrderTicket[] _tickets;
        void OnEnable() { GameEvents.OnOrderPlaced += Placed; GameEvents.OnOrderPatience += Patience; GameEvents.OnOrderServed += Served; GameEvents.OnOrderExpired += Expired; GameEvents.OnRoundStateChanged += StateChanged; }
        void OnDisable() { GameEvents.OnOrderPlaced -= Placed; GameEvents.OnOrderPatience -= Patience; GameEvents.OnOrderServed -= Served; GameEvents.OnOrderExpired -= Expired; GameEvents.OnRoundStateChanged -= StateChanged; }
        void Placed(Order order) { foreach (var ticket in _tickets) if (ticket.OrderId == 0) { ticket.Show(order); return; } }
        void Patience(Order order, float value) { foreach (var ticket in _tickets) if (ticket.OrderId == order.Id) ticket.SetPatience(value); }
        void Served(ServeResult result) { Remove(result.OrderId); }
        void Expired(Order order) { Remove(order.Id); }
        void Remove(int id) { foreach (var ticket in _tickets) if (ticket.OrderId == id) ticket.Hide(); }
        void StateChanged(RoundState state) { if (state == RoundState.Menu || state == RoundState.Results) foreach (var ticket in _tickets) ticket.Hide(); }
    }
}

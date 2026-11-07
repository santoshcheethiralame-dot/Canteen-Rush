using System.Collections.Generic;
using CanteenRush.Core;
using UnityEngine;
namespace CanteenRush.Gameplay
{
    public sealed class OrderService : MonoBehaviour
    {
        [SerializeField] GameConfig _config;
        [SerializeField] ScoreService _score;
        readonly Dictionary<int, Order> _orders = new Dictionary<int, Order>();
        readonly List<Order> _snapshot = new List<Order>(8);
        int _nextId;
        public int LastCompletedSeat { get; private set; } = -1;
        float _reportClock;
        public void ResetOrders() { _orders.Clear(); _nextId = 0; _reportClock = 0; LastCompletedSeat = -1; }
        public Order Place(int seat, Vector3 position, OrderTemplate template)
        {
            if (_orders.ContainsKey(seat)) return null;
            float patience = _config.PatienceBase + _config.PatiencePerItem * template.Items.Length;
            var order = new Order { Id = ++_nextId, SeatIndex = seat, SeatPosition = position, Items = (ItemId[])template.Items.Clone(), PatienceMax = patience, PatienceRemaining = patience };
            _orders.Add(seat, order); GameEvents.OrderPlaced(order); return order;
        }
        public bool HasOrder(int seat) => _orders.ContainsKey(seat);
        public Order GetOrder(int seat) => _orders.TryGetValue(seat, out var order) ? order : null;
        public Order PlaceTutorial(int seat, Vector3 position)
        {
            var template = ScriptableObject.CreateInstance<OrderTemplate>();
            template.Items = new[] { ItemId.Donut };
            var order = Place(seat, position, template);
            if (Application.isPlaying) Destroy(template); else DestroyImmediate(template);
            return order;
        }
        public bool Serve(int seat, IReadOnlyList<ItemId> contents)
        {
            if (!_orders.TryGetValue(seat, out var order)) return false;
            _orders.Remove(seat);
            LastCompletedSeat = seat;
            var result = ScoreCalculator.Score(order, contents, order.Patience01, _config);
            _score.Apply(result.Points); GameEvents.OrderServed(result); return true;
        }
        public void Tick(float dt)
        {
            if (dt <= 0) return;
            _reportClock += dt;
            bool report = _reportClock >= .25f;
            if (report) _reportClock %= .25f;
            _snapshot.Clear();
            foreach (var pair in _orders) _snapshot.Add(pair.Value);
            foreach (var order in _snapshot)
            {
                // Listeners may synchronously reset or replace orders while events are raised.
                if (!_orders.TryGetValue(order.SeatIndex, out var current) || !ReferenceEquals(current, order)) continue;
                order.PatienceRemaining = Mathf.Max(0, order.PatienceRemaining - dt);
                if (order.PatienceRemaining <= 0)
                {
                    _orders.Remove(order.SeatIndex);
                    _score.Apply(ScoreCalculator.Timeout(_config)); GameEvents.OrderExpired(order);
                }
                else if (report) GameEvents.OrderPatience(order, order.Patience01);
            }
        }
    }
}

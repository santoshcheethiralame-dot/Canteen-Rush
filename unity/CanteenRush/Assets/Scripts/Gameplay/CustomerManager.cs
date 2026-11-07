using CanteenRush.Core;
using CanteenRush.World;
using UnityEngine;
namespace CanteenRush.Gameplay
{
    public sealed class CustomerManager : MonoBehaviour
    {
        [SerializeField] GameConfig _config;
        [SerializeField] OrderService _orders;
        [SerializeField] Customer _customerPrefab;
        [SerializeField] Transform _entrance, _player;
        [SerializeField] SeatPoint[] _seats;
        readonly Customer[] _occupied = new Customer[8];
        readonly Customer[] _pool = new Customer[4];
        WaveDirector _wave;
        System.Random _random;
        bool _tutorial;
        int _count, _variant;
        void OnEnable() { GameEvents.OnOrderServed += Served; GameEvents.OnOrderExpired += Expired; }
        void OnDisable() { GameEvents.OnOrderServed -= Served; GameEvents.OnOrderExpired -= Expired; }
        void Discover()
        {
            if (_seats == null || _seats.Length == 0) _seats = FindObjectsByType<SeatPoint>(FindObjectsSortMode.None);
            if (_entrance == null) { var marker = GameObject.Find("Marker_CustomerEntrance"); if (marker != null) _entrance = marker.transform; }
            if (_player == null && Camera.main != null) _player = Camera.main.transform;
            if (!Application.isPlaying) return;
            for (int i = 0; i < _pool.Length; i++) if (_pool[i] == null)
            { _pool[i] = Instantiate(_customerPrefab, transform); _pool[i].gameObject.SetActive(false); }
        }
        public void BeginRound(WaveDirector wave, System.Random random)
        { ResetCustomers(); Discover(); _wave = wave; _random = random; _tutorial = false; }
        public void BeginTutorial()
        {
            ResetCustomers(); Discover(); _tutorial = true;
            SeatPoint first = null;
            foreach (var seat in _seats) if (seat != null && (first == null || seat.Index < first.Index)) first = seat;
            if (first != null && Application.isPlaying) Spawn(first, true);
            else _orders.PlaceTutorial(first != null ? first.Index : 0, first != null ? first.transform.position : Vector3.zero);
        }
        public void Tick()
        {
            if (_wave == null || _customerPrefab == null || _entrance == null) return;
            int free = 0;
            foreach (var seat in _seats) if (seat != null && seat.Index >= 0 && seat.Index < _occupied.Length && _occupied[seat.Index] == null) free++;
            if (!_wave.ShouldSpawnCustomer(_count, free)) return;
            if (!Application.isPlaying) return;
            int pick = _random.Next(free);
            foreach (var seat in _seats)
            {
                if (seat == null || seat.Index < 0 || seat.Index >= _occupied.Length || _occupied[seat.Index] != null) continue;
                if (pick-- != 0) continue;
                Spawn(seat, false); break;
            }
        }
        void Spawn(SeatPoint seat, bool seated)
        {
            if (_entrance == null || seat.Index < 0 || seat.Index >= _occupied.Length) return;
            Customer customer = null;
            foreach (var pooled in _pool) if (pooled != null && !pooled.gameObject.activeSelf) { customer = pooled; break; }
            if (customer == null) return;
            customer.gameObject.SetActive(true);
            _occupied[seat.Index] = customer; _count++;
            if (!customer.Begin(this, seat.Index, seat.transform, _entrance, _player, _variant++, seated))
            { _occupied[seat.Index] = null; _count--; customer.gameObject.SetActive(false); }
            GameEvents.CustomerCount(_count);
        }
        public void PlaceOrder(Customer customer)
        {
            var order = _tutorial ? _orders.PlaceTutorial(customer.SeatIndex, customer.transform.position)
                : _orders.Place(customer.SeatIndex, customer.transform.position, _wave.NextTemplate());
            customer.OrderId = order != null ? order.Id : 0;
        }
        void Served(ServeResult result)
        {
            foreach (var customer in _occupied)
                if (customer != null && customer.OrderId == result.OrderId) { customer.Served(_config.EatSeconds); break; }
        }
        void Expired(Order order)
        { if (order.SeatIndex >= 0 && order.SeatIndex < _occupied.Length && _occupied[order.SeatIndex] != null) _occupied[order.SeatIndex].Expired(); }
        public void CustomerLeft(Customer customer)
        {
            if (_occupied[customer.SeatIndex] != customer) return;
            _occupied[customer.SeatIndex] = null; _count--; customer.gameObject.SetActive(false); GameEvents.CustomerCount(_count);
        }
        public void ResetCustomers()
        {
            for (int i = 0; i < _occupied.Length; i++) { if (_occupied[i] != null) _occupied[i].gameObject.SetActive(false); _occupied[i] = null; }
            _count = 0; _wave = null; _tutorial = false; GameEvents.CustomerCount(0);
        }
    }
}

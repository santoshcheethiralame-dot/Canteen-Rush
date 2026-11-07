using CanteenRush.Core;
using UnityEngine;
using UnityEngine.AI;
namespace CanteenRush.Gameplay
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Customer : MonoBehaviour
    {
        [SerializeField] Transform _visual;
        [SerializeField] Transform _head;
        [SerializeField] Renderer[] _tintedRenderers;
        [SerializeField] GameConfig _config;
        NavMeshAgent _agent;
        CustomerManager _manager;
        Transform _seat, _entrance, _player;
        Vector3 _visualOrigin;
        Quaternion _headOrigin;
        MaterialPropertyBlock _colour;
        float _stateTimer;
        public int SeatIndex { get; private set; }
        public int OrderId { get; set; }
        public CustomerState State { get; private set; }
        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_visual != null) _visualOrigin = _visual.localPosition;
            if (_head != null) _headOrigin = _head.localRotation;
            _colour = new MaterialPropertyBlock();
        }
        public bool Begin(CustomerManager manager, int seatIndex, Transform seat, Transform entrance, Transform player, int variant, bool seated = false)
        {
            _manager = manager; SeatIndex = seatIndex; _seat = seat; _entrance = entrance; _player = player; OrderId = 0;
            if (_visual != null) _visual.localPosition = _visualOrigin;
            if (_head != null) _head.localRotation = _headOrigin;
            _colour.SetColor("_BaseColor", Color.HSVToRGB((variant * .17f) % 1, .55f, .85f));
            if (_tintedRenderers != null) foreach (var renderer in _tintedRenderers) if (renderer != null) renderer.SetPropertyBlock(_colour);
            if (_config != null) _agent.speed = _config.CustomerSpeed;
            Vector3 start = seated ? seat.position : entrance.position;
            if (!NavMesh.SamplePosition(start, out var hit, 2, _agent.areaMask) || !_agent.Warp(hit.position)) return false;
            _agent.isStopped = false; SetState(CustomerState.Arriving);
            if (seated) Arrive(); else if (!_agent.SetDestination(seat.position)) return false;
            return true;
        }
        void SetState(CustomerState state) { State = state; GameEvents.CustomerState(SeatIndex, state); }
        void Arrive()
        {
            _agent.isStopped = true; transform.rotation = _seat.rotation;
            SetState(CustomerState.Ordering); SetState(CustomerState.Waiting);
            _manager.PlaceOrder(this);
        }
        public void Served(float seconds) { if (State != CustomerState.Waiting) return; SetState(CustomerState.Eating); _stateTimer = seconds; }
        public void Expired() { if (State != CustomerState.Waiting) return; SetState(CustomerState.Angry); _stateTimer = _config != null ? _config.CustomerAngrySeconds : 1; }
        bool AtDestination => _agent.isOnNavMesh && !_agent.pathPending && _agent.pathStatus == NavMeshPathStatus.PathComplete && _agent.remainingDistance <= _agent.stoppingDistance + .1f;
        void Update()
        {
            if (_manager == null) return;
            if (State == CustomerState.Arriving && AtDestination) Arrive();
            else if (State == CustomerState.Eating || State == CustomerState.Angry)
            {
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0)
                {
                    SetState(CustomerState.Leaving);
                    if (!_agent.isOnNavMesh) { _manager.CustomerLeft(this); return; }
                    _agent.isStopped = false; _agent.SetDestination(_entrance.position);
                }
            }
            else if (State == CustomerState.Leaving && AtDestination) { _manager.CustomerLeft(this); return; }
            if (_head != null && _player != null && (State == CustomerState.Waiting || State == CustomerState.Eating))
            {
                float distance = _config != null ? _config.CustomerLookDistance : 6;
                var direction = _player.position - _head.position; direction.y = 0;
                if (direction.sqrMagnitude > .001f && direction.sqrMagnitude < distance * distance) _head.rotation = Quaternion.LookRotation(direction);
            }
            if (_visual != null)
            {
                float bob = _agent.isOnNavMesh && _agent.velocity.sqrMagnitude > .01f
                    ? Mathf.Abs(Mathf.Sin(Time.time * (_config != null ? _config.CustomerBobSpeed : 8))) * (_config != null ? _config.CustomerBobHeight : .025f) : 0;
                _visual.localPosition = _visualOrigin + Vector3.up * bob;
            }
        }
    }
}

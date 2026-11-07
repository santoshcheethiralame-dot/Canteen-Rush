using System.Collections.Generic;
using CanteenRush.Core;
using UnityEngine;
namespace CanteenRush.Gameplay
{
    public sealed class ItemSpawner : MonoBehaviour
    {
        [SerializeField] ItemBehaviour _prefab;
        [SerializeField] GameConfig _config;
        [SerializeField] float _respawnSeconds = 5f;
        [SerializeField] float _slotRadius = .15f;
        [SerializeField] FridgeDoor _door;
        readonly List<ItemBehaviour> _pool = new List<ItemBehaviour>(4);
        ItemBehaviour _current;
        float _timer;
        public ItemBehaviour Current => _current;
        public void Configure(ItemBehaviour prefab, GameConfig config, float respawn, FridgeDoor door = null)
        { _prefab = prefab; _config = config; _respawnSeconds = respawn; _door = door; Prewarm(); }
        void Awake() { Prewarm(); }
        void Prewarm()
        {
            if (_prefab == null || _pool.Count != 0) return;
            int capacity = _config != null ? _config.ItemsPerSpawner : 4;
            for (int i = 0; i < capacity; i++)
            {
                var item = Instantiate(_prefab); item.gameObject.SetActive(false); _pool.Add(item);
            }
        }
        void OnEnable() { _timer = 0; }
        void Update()
        {
            if (_current != null && _current.gameObject.activeSelf && (_current.transform.position - transform.position).sqrMagnitude <= _slotRadius * _slotRadius)
            {
                _current.SetAccessible(_door == null || _door.IsOpen);
                return;
            }
            if (_current != null) { _current.SetAccessible(true); _current = null; _timer = _respawnSeconds; }
            _timer -= Time.deltaTime;
            if (_timer <= 0 && _prefab != null) Spawn();
        }
        void Spawn()
        {
            if (_config != null && ItemBehaviour.ActiveCount >= _config.MaxActiveItems) return;
            for (int i = 0; i < _pool.Count; i++) if (!_pool[i].gameObject.activeSelf) { _current = _pool[i]; break; }
            if (_current == null) return;
            _current.transform.SetPositionAndRotation(transform.position, transform.rotation);
            var body = _current.GetComponent<Rigidbody>(); body.isKinematic = false; body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            _current.gameObject.SetActive(true); _current.SetAccessible(_door == null || _door.IsOpen);
            GameEvents.ItemSpawned(_current.Id, transform.position);
        }
        public void ResetSpawner()
        {
            for (int i = 0; i < _pool.Count; i++) if (_pool[i] != null) _pool[i].Recycle();
            _current = null; _timer = 0;
        }
        void OnDestroy()
        {
            for (int i = 0; i < _pool.Count; i++)
                if (_pool[i] != null) { if (Application.isPlaying) Destroy(_pool[i].gameObject); else DestroyImmediate(_pool[i].gameObject); }
        }
    }
}

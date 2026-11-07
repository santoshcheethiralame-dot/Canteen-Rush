using CanteenRush.Core;
using CanteenRush.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
namespace CanteenRush.Gameplay
{
    // P2 supplies the wiring at runtime; P1 retains exclusive ownership of the saved Main scene.
    public sealed class WorldGameplayInstaller : MonoBehaviour
    {
        [SerializeField] GameConfig _config;
        [SerializeField] GameObject _rig, _simulator, _rack, _cooler, _rail, _cupDispenser;
        [SerializeField] ItemSpawner _spawner;
        [SerializeField] ServeSocket _serveSocket;
        [SerializeField] FridgeDoor[] _doorPrefabs;
        [SerializeField] ItemBehaviour[] _items;
        GameManager _game;
        ComfortSettings _comfort;
        bool _seated, _teleport;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            SceneManager.sceneLoaded -= Loaded;
            SceneManager.sceneLoaded += Loaded;
        }
        static void Loaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Main" || FindFirstObjectByType<GameManager>() != null) return;
            var prefab = Resources.Load<GameObject>("P2Runtime");
            if (prefab != null) Instantiate(prefab);
        }
        void Awake()
        {
            _game = GetComponent<GameManager>();
            if (gameObject.scene.name == "Main") InstallWorld();
        }
        void Start() { _comfort = FindFirstObjectByType<ComfortSettings>(); }
        void Update()
        {
            var keyboard = Keyboard.current; if (keyboard == null) return;
            if (keyboard.enterKey.wasPressedThisFrame && (_game.State == RoundState.Menu || _game.State == RoundState.Results)) _game.Play();
            if (keyboard.f8Key.wasPressedThisFrame && _game.State != RoundState.Menu) _game.Menu();
            if (keyboard.f6Key.wasPressedThisFrame && _comfort != null) { _seated = !_seated; _comfort.SetSeated(_seated); }
            if (keyboard.f7Key.wasPressedThisFrame && _comfort != null) { _teleport = !_teleport; _comfort.SetTeleport(_teleport); }
        }
        void InstallWorld()
        {
            PrepareStationColliders();
            AddTeleportSurfaces();
            var startObject = GameObject.Find("PlayerStart");
            Vector3 start = startObject != null ? startObject.transform.position : Vector3.zero;
            Quaternion facing = startObject != null ? startObject.transform.rotation : Quaternion.identity;
            if (FindFirstObjectByType<ComfortSettings>() == null)
            {
                foreach (var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                {
                    camera.gameObject.SetActive(false);
                }
                Instantiate(_rig, start, facing);
            }
            if (_simulator != null) Instantiate(_simulator);
            var doors = FindObjectsByType<FridgeDoor>(FindObjectsSortMode.None);
            string[] doorNames = { "Door_Upper", "Door_Lower" };
            for (int i = 0; i < doorNames.Length; i++)
            {
                Transform original = null;
                foreach (var candidate in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (candidate.name == doorNames[i]) { original = candidate; break; }
                if (original == null) continue;
                FridgeDoor replacement = null;
                foreach (var door in doors) if (door.DoorIndex == i) replacement = door;
                if (replacement == null) replacement = Instantiate(_doorPrefabs[i], original.position, original.rotation);
                replacement.UseSourceGeometry(original, start);
                original.gameObject.SetActive(false);
            }
            doors = FindObjectsByType<FridgeDoor>(FindObjectsSortMode.None);
            FridgeDoor upper = null;
            foreach (var door in doors) if (door.DoorIndex == 0) upper = door;
            var markers = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
            foreach (var marker in markers)
            {
                int index = marker.ItemKey == "Donut" ? 0 : marker.ItemKey == "Cola" ? 1 : marker.ItemKey == "Shake" ? 2 : marker.ItemKey == "Cake" ? 3 : 4;
                float respawn = index == 0 ? _config.DonutRespawn : index == 1 ? _config.ColaRespawn : index == 2 ? _config.ShakeRespawn : index == 3 ? _config.CakeRespawn : _config.DonutRespawn;
                var spawner = Instantiate(index == 4 ? _cupDispenser.GetComponent<ItemSpawner>() : _spawner, marker.transform.position, marker.transform.rotation, transform);
                spawner.Configure(_items[index], _config, respawn, index == 1 ? upper : null);
            }
            Instantiate(_rack, start + facing * new Vector3(.7f, 0, -.6f), facing, transform);
            var sourceCooler = GameObject.Find("WaterCooler");
            if (sourceCooler != null)
            {
                var renderer = sourceCooler.GetComponentInChildren<Renderer>();
                Vector3 position = renderer != null ? renderer.bounds.center : sourceCooler.transform.position;
                // Pump/spout overlays are in front of the source bottle, toward the working aisle.
                position.y = renderer != null ? renderer.bounds.min.y + .25f : position.y;
                var overlay = Instantiate(_cooler, position + facing * Vector3.forward * .3f, facing, transform);
                var placeholder = overlay.transform.Find("Visual"); if (placeholder != null) placeholder.gameObject.SetActive(false);
            }
            var orders = GetComponent<OrderService>();
            foreach (var seat in FindObjectsByType<SeatPoint>(FindObjectsSortMode.None))
            {
                Vector3 position = seat.transform.position + seat.transform.forward * .65f + Vector3.up * .8f;
                if (Physics.Raycast(position + Vector3.up * 2, Vector3.down, out var hit, 3, 1 << LayerMask.NameToLayer("Stations")))
                    position.y = hit.point.y + .06f;
                var socket = Instantiate(_serveSocket, position, seat.transform.rotation, transform);
                socket.Configure(seat.Index, orders);
            }
            Instantiate(_rail, start + facing * new Vector3(0, 1.9f, 1.6f), facing, transform);
        }
        public static void AddTeleportSurfaces()
        {
            foreach (var collider in FindObjectsByType<Collider>(FindObjectsSortMode.None))
                if ((collider.CompareTag("SurfaceTile") || collider.CompareTag("SurfaceStone")) && collider.GetComponent<TeleportationArea>() == null)
                    collider.gameObject.AddComponent<TeleportationArea>();
        }
        static void PrepareStationColliders()
        {
            // A bounding box closes the openings in a display case or fridge. Keep the source geometry's openings.
            foreach (string name in new[] { "DisplayCase_A", "DisplayCase_B", "FridgeBody" })
            {
                var station = GameObject.Find(name); if (station == null) continue;
                foreach (var filter in station.GetComponentsInChildren<MeshFilter>())
                {
                    foreach (var box in filter.GetComponents<BoxCollider>()) box.enabled = false;
                    if (filter.GetComponent<MeshCollider>() == null)
                    { var collider = filter.gameObject.AddComponent<MeshCollider>(); collider.sharedMesh = filter.sharedMesh; }
                }
            }
        }
    }
}

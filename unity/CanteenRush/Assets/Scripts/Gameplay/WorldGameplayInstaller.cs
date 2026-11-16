using System.Collections.Generic;
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
        [SerializeField] GameObject _canteenModel;
        [SerializeField] GameObject _rig, _simulator, _rack, _cooler, _rail, _cupDispenser;
        [SerializeField] ItemSpawner _spawner;
        [SerializeField] ServeSocket _serveSocket;
        [SerializeField] FridgeDoor[] _doorPrefabs;
        [SerializeField] ItemBehaviour[] _items;
        GameManager _game;
        ComfortSettings _comfort;
        Mesh _entranceShellMesh;
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
        void OnDestroy() { if (_entranceShellMesh != null) Destroy(_entranceShellMesh); }
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
            if (startObject != null) OpenPatioEntrance(start, facing);
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
        void OpenPatioEntrance(Vector3 start, Quaternion facing)
        {
            var counter = GameObject.Find("Counter_A");
            var source = _canteenModel != null ? _canteenModel.transform.Find("Counter_A") : null;
            if (counter == null || source == null) return;

            // This counter closes the gap between the displays and the right-hand wall.
            // Keep it as a prep surface inside. Use a new renderer so the baked static
            // batch in Main cannot leave a visible counter across the new opening.
            var prepCounter = Instantiate(source.gameObject, counter.transform.position - facing * Vector3.forward * 4.25f,
                counter.transform.rotation, counter.transform.parent);
            prepCounter.name = "PatioEntrance_PrepCounter";
            prepCounter.layer = counter.layer;
            prepCounter.transform.localScale = counter.transform.localScale;
            prepCounter.AddComponent<BoxCollider>();
            counter.SetActive(false);
            OpenRaisedPlatform(start, facing);
        }

        void OpenRaisedPlatform(Vector3 start, Quaternion facing)
        {
            var shell = GameObject.Find("Shell");
            var source = _canteenModel.transform.Find("Shell");
            if (shell == null || source == null || !source.TryGetComponent<MeshFilter>(out var filter)) return;
            var mesh = filter.sharedMesh;
            if (mesh == null || !mesh.isReadable) return;

            // The imported shell includes a 31 cm plinth between the aisle and patio.
            // Replace its three faces with two solid sections that leave a level doorway.
            var vertices = mesh.vertices;
            var inverseFacing = Quaternion.Inverse(facing);
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = inverseFacing * (shell.transform.TransformPoint(vertices[i]) - start);
            var platform = new Bounds();
            var kept = new List<int>[mesh.subMeshCount];
            int platformMaterial = -1;
            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                var indices = mesh.GetTriangles(submesh);
                kept[submesh] = new List<int>(indices.Length);
                for (int i = 0; i < indices.Length; i += 3)
                {
                    Vector3 a = vertices[indices[i]], b = vertices[indices[i + 1]], c = vertices[indices[i + 2]];
                    Vector3 min = Vector3.Min(a, Vector3.Min(b, c)), max = Vector3.Max(a, Vector3.Max(b, c));
                    if (min.z >= .62f && max.z <= 2.02f && max.y >= .28f && max.y <= .34f)
                    {
                        if (platformMaterial < 0) platform = new Bounds(a, Vector3.zero);
                        platform.Encapsulate(min); platform.Encapsulate(max);
                        if (platformMaterial < 0 || min.y >= .28f) platformMaterial = submesh;
                        continue;
                    }
                    kept[submesh].Add(indices[i]); kept[submesh].Add(indices[i + 1]); kept[submesh].Add(indices[i + 2]);
                }
            }
            const float openingLeft = 1.47f, openingRight = 2.33f;
            if (platformMaterial < 0 || platform.min.x >= openingLeft || platform.max.x <= openingRight) return;

            _entranceShellMesh = Instantiate(mesh);
            _entranceShellMesh.name = "Canteen shell with front passage";
            for (int i = 0; i < kept.Length; i++) _entranceShellMesh.SetTriangles(kept[i], i);
            var openShell = Instantiate(source.gameObject, shell.transform.position, shell.transform.rotation, shell.transform.parent);
            openShell.name = "Shell_FrontPassage";
            openShell.transform.localScale = shell.transform.localScale;
            openShell.layer = shell.layer;
            openShell.GetComponent<MeshFilter>().sharedMesh = _entranceShellMesh;
            openShell.AddComponent<MeshCollider>().sharedMesh = _entranceShellMesh;
            var renderer = openShell.GetComponent<MeshRenderer>();
            var originalRenderer = shell.GetComponent<MeshRenderer>();
            renderer.lightmapIndex = originalRenderer.lightmapIndex;
            renderer.lightmapScaleOffset = originalRenderer.lightmapScaleOffset;
            var material = renderer.sharedMaterials[platformMaterial];
            AddPlatformSection("DisplayPlinth", start, facing, platform, platform.min.x, openingLeft, shell.layer, material);
            AddPlatformSection("SidePlinth", start, facing, platform, openingRight, platform.max.x, shell.layer, material);
            shell.SetActive(false);
        }

        void AddPlatformSection(string name, Vector3 start, Quaternion facing, Bounds platform,
            float left, float right, int layer, Material material)
        {
            var section = GameObject.CreatePrimitive(PrimitiveType.Cube);
            section.name = name;
            section.layer = layer;
            section.transform.SetParent(transform);
            section.transform.SetPositionAndRotation(start + facing * new Vector3((left + right) * .5f,
                platform.max.y * .5f, platform.center.z), facing);
            section.transform.localScale = new Vector3(right - left, platform.max.y, platform.size.z);
            section.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        public static void AddTeleportSurfaces()
        {
            foreach (var collider in FindObjectsByType<Collider>(FindObjectsSortMode.None))
                if ((collider.CompareTag("SurfaceTile") || collider.CompareTag("SurfaceStone")) && collider.GetComponent<TeleportationArea>() == null)
                    collider.gameObject.AddComponent<TeleportationArea>();
        }
        void PrepareStationColliders()
        {
            if (_canteenModel == null) return;
            // A bounding box closes the openings in a display case or fridge. Keep the source geometry's openings.
            // Scene MeshFilters can point to a combined static batch instead of the fixture's own mesh.
            foreach (string name in new[] { "DisplayCase_A", "DisplayCase_B", "FridgeBody" })
            {
                var station = GameObject.Find(name);
                var source = _canteenModel.transform.Find(name);
                if (station == null || source == null || !source.TryGetComponent<MeshFilter>(out var filter)
                    || filter.sharedMesh == null) continue;
                foreach (var box in station.GetComponents<BoxCollider>()) box.enabled = false;
                if (!station.TryGetComponent<MeshCollider>(out var collider))
                    collider = station.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
            }
        }
    }
}

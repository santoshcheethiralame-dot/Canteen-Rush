using System.IO;
using CanteenRush.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.UI;

namespace CanteenRush.Gameplay.Editor
{
    public static class GameplayPrefabBuilder
    {
        const string Folder = "Assets/Prefabs/Gameplay";
        static GameConfig Config => AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Data/GameConfig.asset");

        static void Reference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        static void Number(Object target, string field, int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        static GameObject Visual(GameObject root, PrimitiveType shape, Vector3 scale)
        {
            var visual = GameObject.CreatePrimitive(shape); visual.name = "Visual";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(root.transform, false); visual.transform.localScale = scale; return visual;
        }
        static GameObject ArtVisual(GameObject root, string path, PrimitiveType fallback, Vector3 size)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) return Visual(root, fallback, size);
            var visual = new GameObject("Visual"); visual.transform.SetParent(root.transform, false);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            model.transform.SetParent(visual.transform, false);
            return visual;
        }
        static void Save(GameObject root, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(root, Folder + "/" + name + ".prefab"); Object.DestroyImmediate(root);
        }
        static void Item(string name, ItemId id, Vector3 scale, float mass, bool cup = false)
        {
            var root = new GameObject("Item_" + name); root.layer = LayerMask.NameToLayer("Items");
            var visual = ArtVisual(root, "Assets/Art/Items/Item_" + name + ".fbx", PrimitiveType.Cylinder, scale);
            var renderers = visual.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            if (id == ItemId.Donut || id == ItemId.Cake)
            {
                var collider = root.AddComponent<BoxCollider>(); collider.center = bounds.center; collider.size = bounds.size;
            }
            else
            {
                var collider = root.AddComponent<CapsuleCollider>(); collider.center = bounds.center;
                collider.radius = Mathf.Max(bounds.size.x, bounds.size.z) / 2; collider.height = bounds.size.y;
            }
            var body = root.AddComponent<Rigidbody>(); body.mass = mass; body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var grab = root.AddComponent<XRGrabInteractable>(); grab.interactionLayers = 3;
            var item = root.AddComponent<ItemBehaviour>(); Number(item, "_id", (int)id); Reference(item, "_config", Config);
            if (cup)
            {
                var fill = root.AddComponent<CupFill>(); Reference(fill, "_config", Config);
                foreach (var child in visual.GetComponentsInChildren<Transform>(true)) if (child.name == "Cup_Water") { Reference(fill, "_waterVisual", child.gameObject); break; }
            }
            Save(root, root.name);
            var def = AssetDatabase.LoadAssetAtPath<ItemDef>("Assets/Data/Items/Item_" + (cup ? "Water" : name) + ".asset");
            if (def != null) { def.Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Item_" + name + ".prefab"); EditorUtility.SetDirty(def); }
        }
        static void TrayPrefab()
        {
            var root = new GameObject("Prop_Tray"); root.layer = LayerMask.NameToLayer("Tray"); root.tag = "Tray";
            ArtVisual(root, "Assets/Art/Props/Prop_Tray.fbx", PrimitiveType.Cube, new Vector3(.45f, .03f, .3f));
            root.AddComponent<BoxCollider>().size = new Vector3(.45f, .03f, .3f);
            var body = root.AddComponent<Rigidbody>(); body.mass = .4f; body.interpolation = RigidbodyInterpolation.Interpolate;
            var grab = root.AddComponent<XRGrabInteractable>(); grab.interactionLayers = 5;
            var tray = root.AddComponent<Tray>(); Reference(tray, "_config", Config);
            var serialized = new SerializedObject(tray); var slots = serialized.FindProperty("_sockets"); slots.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                var slot = new GameObject("Slot_" + i); slot.transform.SetParent(root.transform, false);
                slot.transform.localPosition = new Vector3((i % 2 == 0 ? -1 : 1) * .1f, .06f, (i / 2 == 0 ? -1 : 1) * .07f);
                var trigger = slot.AddComponent<SphereCollider>(); trigger.isTrigger = true; trigger.radius = .075f;
                var socket = slot.AddComponent<XRSocketInteractor>(); socket.interactionLayers = 2; socket.showInteractableHoverMeshes = false;
                slots.GetArrayElementAtIndex(i).objectReferenceValue = socket;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo(); Save(root, root.name);
        }
        static void CustomerPrefab()
        {
            var root = new GameObject("Customer"); root.layer = LayerMask.NameToLayer("Customers");
            var agent = root.AddComponent<NavMeshAgent>(); agent.radius = .2f; agent.height = 1.7f; agent.stoppingDistance = .1f;
            var customer = root.AddComponent<Customer>(); Reference(customer, "_config", Config);
            var visual = Visual(root, PrimitiveType.Capsule, new Vector3(.35f, .5f, .35f)); visual.transform.localPosition = Vector3.up * .75f;
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere); head.name = "Head"; Object.DestroyImmediate(head.GetComponent<Collider>());
            head.transform.SetParent(visual.transform, false); head.transform.localPosition = Vector3.up * 1.1f; head.transform.localScale = Vector3.one * .8f;
            Reference(customer, "_visual", visual.transform); Reference(customer, "_head", head.transform);
            var data = new SerializedObject(customer); var renderers = data.FindProperty("_tintedRenderers"); renderers.arraySize = 1;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = visual.GetComponent<Renderer>(); data.ApplyModifiedPropertiesWithoutUndo();
            var bubble = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UIGameplay/UI_CustomerBubble.prefab"));
            bubble.transform.SetParent(root.transform, false); bubble.transform.localPosition = Vector3.up * 1.9f;
            Save(root, root.name);
        }
        static void FridgePrefab(string name, int index, Vector3 size)
        {
            var root = new GameObject(name); root.layer = LayerMask.NameToLayer("Stations");
            var visual = Visual(root, PrimitiveType.Cube, size); visual.transform.localPosition = Vector3.right * size.x / 2;
            var box = root.AddComponent<BoxCollider>(); box.size = size; box.center = Vector3.right * size.x / 2;
            var body = root.AddComponent<Rigidbody>(); body.useGravity = false;
            var hinge = root.AddComponent<HingeJoint>(); hinge.axis = Vector3.up; hinge.useLimits = true;
            var limits = hinge.limits; limits.min = 0; limits.max = 110; hinge.limits = limits;
            var grab = root.AddComponent<XRGrabInteractable>(); grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grab.trackRotation = false; grab.throwOnDetach = false;
            var handle = new GameObject("Handle"); handle.transform.SetParent(root.transform, false);
            handle.transform.localPosition = new Vector3(size.x * .85f, 0, -.08f);
            var handleCollider = handle.AddComponent<BoxCollider>(); handleCollider.size = new Vector3(.08f, size.y * .5f, .1f);
            grab.colliders.Clear(); grab.colliders.Add(handleCollider); grab.attachTransform = handle.transform;
            var door = root.AddComponent<FridgeDoor>(); Number(door, "_doorIndex", index); Reference(door, "_config", Config);
            Save(root, root.name);
        }
        static void CoolerPrefab()
        {
            var root = new GameObject("Station_WaterCooler"); root.layer = LayerMask.NameToLayer("Triggers"); root.tag = "FillZone";
            var visual = Visual(root, PrimitiveType.Cylinder, new Vector3(.4f, .4f, .4f)); visual.transform.localPosition = new Vector3(0, -.25f, .3f);
            var zone = root.AddComponent<BoxCollider>(); zone.isTrigger = true; zone.size = new Vector3(.2f, .25f, .2f);
            var pump = new GameObject("Pump"); pump.layer = LayerMask.NameToLayer("Stations"); pump.transform.SetParent(root.transform, false); pump.transform.localPosition = Vector3.up * .25f;
            var button = pump.AddComponent<BoxCollider>(); button.isTrigger = true; button.size = Vector3.one * .12f;
            var interactable = pump.AddComponent<XRSimpleInteractable>();
            Visual(pump, PrimitiveType.Cube, Vector3.one * .1f);
            var cooler = root.AddComponent<WaterCooler>(); Reference(cooler, "_pump", interactable);
            Save(root, root.name);
        }
        static void ServeSocketPrefab()
        {
            var root = new GameObject("Table_ServeSocket"); root.layer = LayerMask.NameToLayer("Triggers"); root.tag = "ServeSocket";
            var trigger = root.AddComponent<BoxCollider>(); trigger.isTrigger = true; trigger.size = new Vector3(.5f, .12f, .35f);
            var socket = root.AddComponent<XRSocketInteractor>(); socket.interactionLayers = 4; socket.showInteractableHoverMeshes = false;
            var serve = root.AddComponent<ServeSocket>(); Reference(serve, "_config", Config);
            Save(root, root.name);
        }
        static void SystemsPrefab()
        {
            Directory.CreateDirectory("Assets/Prefabs/Systems");
            var root = new GameObject("Systems");
            root.AddComponent<XRInteractionManager>();
            var score = root.AddComponent<ScoreService>(); Reference(score, "_config", Config);
            var orders = root.AddComponent<OrderService>(); Reference(orders, "_config", Config); Reference(orders, "_score", score);
            var customers = root.AddComponent<CustomerManager>(); Reference(customers, "_config", Config); Reference(customers, "_orders", orders);
            var customerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Customer.prefab");
            Reference(customers, "_customerPrefab", customerPrefab.GetComponent<Customer>());
            var manager = root.AddComponent<GameManager>(); Reference(manager, "_config", Config); Reference(manager, "_score", score);
            Reference(manager, "_orders", orders); Reference(manager, "_customers", customers);
            var installer = root.AddComponent<WorldGameplayInstaller>(); Reference(installer, "_config", Config);
            foreach (var pair in new[] {
                new[] { "_rig", "Assets/Prefabs/Systems/Rig.prefab" },
                new[] { "_simulator", Folder + "/DeviceSimulator.prefab" },
                new[] { "_rack", Folder + "/Station_TrayRack.prefab" },
                new[] { "_cooler", Folder + "/Station_WaterCooler.prefab" },
                new[] { "_rail", "Assets/Prefabs/UIGameplay/UI_OrderRail.prefab" },
                new[] { "_cupDispenser", Folder + "/Station_CupDispenser.prefab" } })
                Reference(installer, pair[0], AssetDatabase.LoadAssetAtPath<GameObject>(pair[1]));
            Reference(installer, "_spawner", AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Station_ItemSpawner.prefab").GetComponent<ItemSpawner>());
            Reference(installer, "_serveSocket", AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Table_ServeSocket.prefab").GetComponent<ServeSocket>());
            var settings = new SerializedObject(installer); var items = settings.FindProperty("_items"); items.arraySize = 5;
            var names = new[] { "Donut", "Cola", "Shake", "Cake", "Cup" };
            for (int i = 0; i < 5; i++) items.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Item_" + names[i] + ".prefab").GetComponent<ItemBehaviour>();
            var doors = settings.FindProperty("_doorPrefabs"); doors.arraySize = 2;
            doors.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Station_FridgeDoorUpper.prefab").GetComponent<FridgeDoor>();
            doors.GetArrayElementAtIndex(1).objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Station_FridgeDoorLower.prefab").GetComponent<FridgeDoor>();
            settings.ApplyModifiedPropertiesWithoutUndo();
            Directory.CreateDirectory("Assets/Data/Resources");
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Data/Resources/P2Runtime.prefab");
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Systems/Systems.prefab"); Object.DestroyImmediate(root);
        }
        static void RigPrefab()
        {
            var sample = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Data/XRI/Starter Assets/Prefabs/XR Origin (XR Rig).prefab");
            if (sample == null) { Debug.LogWarning("XRI Starter Assets sample is required for Rig.prefab."); return; }
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(sample); rig.name = "Rig";
            foreach (var move in rig.GetComponentsInChildren<ContinuousMoveProvider>(true)) move.moveSpeed = Config.MoveSpeed;
            foreach (var turn in rig.GetComponentsInChildren<SnapTurnProvider>(true)) turn.turnAmount = Config.SnapTurnDeg;
            var teleport = rig.GetComponentInChildren<TeleportationProvider>(true); if (teleport != null) teleport.enabled = false;
            var reporter = rig.AddComponent<PlayerMotionReporter>();
            Reference(reporter, "_config", Config);
            var camera = rig.GetComponentInChildren<Camera>(true); if (camera != null) Reference(reporter, "_head", camera.transform);
            Number(reporter, "_floorMask", 1 << LayerMask.NameToLayer("Environment"));
            var comfort = rig.AddComponent<ComfortSettings>();
            Reference(comfort, "_config", Config);
            comfort.ConfigureInteractionMasks();
            if (teleport != null) Reference(comfort, "_teleportProvider", teleport);
            var offset = rig.transform.Find("Camera Offset"); if (offset != null) Reference(comfort, "_cameraOffset", offset);
            if (camera != null)
            {
                var shader = Shader.Find("CanteenRush/ComfortVignette");
                if (shader != null)
                {
                    var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Data/ComfortVignette.mat");
                    if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, "Assets/Data/ComfortVignette.mat"); }
                    var overlay = new GameObject("ComfortVignette", typeof(RectTransform)); overlay.transform.SetParent(camera.transform, false);
                    var canvas = overlay.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .3f;
                    var group = overlay.AddComponent<CanvasGroup>(); group.alpha = 0; group.blocksRaycasts = false; Reference(comfort, "_vignette", group);
                    var imageObject = new GameObject("Edges", typeof(RectTransform)); imageObject.transform.SetParent(overlay.transform, false);
                    var imageRect = imageObject.GetComponent<RectTransform>(); imageRect.anchorMin = Vector2.zero; imageRect.anchorMax = Vector2.one; imageRect.offsetMin = imageRect.offsetMax = Vector2.zero;
                    var image = imageObject.AddComponent<Image>(); image.material = material; image.raycastTarget = false;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(rig, "Assets/Prefabs/Systems/Rig.prefab"); Object.DestroyImmediate(rig);
        }
        [MenuItem("Canteen Rush/Gameplay/Build basic prefabs")]
        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            Item("Donut", ItemId.Donut, new Vector3(.32f, .035f, .32f), .2f);
            Item("Cola", ItemId.Cola, new Vector3(.14f, .16f, .14f), .3f);
            Item("Shake", ItemId.Shake, new Vector3(.22f, .31f, .22f), .4f);
            Item("Cake", ItemId.Cake, new Vector3(.5f, .23f, .5f), .4f);
            Item("Cup", ItemId.None, new Vector3(.16f, .1f, .16f), .2f, true);
            GameplayUIBuilder.Build();
            TrayPrefab(); CustomerPrefab(); RackPrefab();
            FridgePrefab("Station_FridgeDoorUpper", 0, new Vector3(.86f, 1.28f, .12f));
            FridgePrefab("Station_FridgeDoorLower", 1, new Vector3(.89f, .85f, .12f));
            CoolerPrefab(); ServeSocketPrefab();
            var spawner = new GameObject("Station_ItemSpawner"); var spawn = spawner.AddComponent<ItemSpawner>(); Reference(spawn, "_config", Config); Save(spawner, spawner.name);
            var dispenser = new GameObject("Station_CupDispenser"); dispenser.layer = LayerMask.NameToLayer("Stations");
            var cups = dispenser.AddComponent<ItemSpawner>(); Reference(cups, "_config", Config);
            Reference(cups, "_prefab", AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Item_Cup.prefab").GetComponent<ItemBehaviour>());
            var cupVisual = ArtVisual(dispenser, "Assets/Art/Props/Prop_CupDispenser.fbx", PrimitiveType.Cylinder, new Vector3(.2f, .1f, .2f));
            var cupBounds = cupVisual.GetComponentInChildren<Renderer>().bounds;
            cupVisual.transform.localPosition -= Vector3.up * cupBounds.max.y;
            var cupSupport = dispenser.AddComponent<BoxCollider>(); cupSupport.size = cupBounds.size;
            cupSupport.center = cupBounds.center - Vector3.up * cupBounds.max.y;
            Save(dispenser, dispenser.name);
            var simulator = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Data/XRI/XR Device Simulator/XRDeviceSimulator/XR Device Simulator.prefab"));
            simulator.AddComponent<SimulatorSupport>(); Save(simulator, "DeviceSimulator");
            RigPrefab(); SystemsPrefab(); AssetDatabase.SaveAssets();
        }
        static void RackPrefab()
        {
            var root = new GameObject("Station_TrayRack");
            ArtVisual(root, "Assets/Art/Props/Prop_TrayRack.fbx", PrimitiveType.Cube, new Vector3(1.1f, .75f, .5f));
            root.layer = LayerMask.NameToLayer("Stations");
            var support = root.AddComponent<BoxCollider>(); support.size = new Vector3(1.1f, .75f, .5f); support.center = Vector3.up * .375f;
            var rack = root.AddComponent<TrayRack>(); Reference(rack, "_trayPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Prop_Tray.prefab").GetComponent<Tray>());
            var data = new SerializedObject(rack); var slots = data.FindProperty("_slots"); slots.arraySize = 2;
            for (int i = 0; i < 2; i++)
            {
                var slot = new GameObject("TrayHome_" + i); slot.transform.SetParent(root.transform, false);
                slot.transform.localPosition = new Vector3(i == 0 ? -.28f : .28f, .8f, 0);
                slots.GetArrayElementAtIndex(i).objectReferenceValue = slot.transform;
            }
            data.ApplyModifiedPropertiesWithoutUndo(); Save(root, root.name);
        }
    }
}

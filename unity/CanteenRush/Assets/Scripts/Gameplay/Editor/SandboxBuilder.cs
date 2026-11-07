using System.Collections.Generic;
using CanteenRush.World;
using CanteenRush.UI.Gameplay;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace CanteenRush.Gameplay.Editor
{
    public static class SandboxBuilder
    {
        const string Gameplay = "Assets/Prefabs/Gameplay/";
        static GameObject Instance(string path, Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new System.InvalidOperationException("Missing prefab " + path);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab); instance.transform.position = position; return instance;
        }
        static void Ref(Object component, string field, Object value)
        {
            var data = new SerializedObject(component); data.FindProperty(field).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo();
        }
        static void Int(Object component, string field, int value)
        {
            var data = new SerializedObject(component); data.FindProperty(field).intValue = value; data.ApplyModifiedPropertiesWithoutUndo();
        }
        static void Float(Object component, string field, float value)
        {
            var data = new SerializedObject(component); data.FindProperty(field).floatValue = value; data.ApplyModifiedPropertiesWithoutUndo();
        }
        static GameObject Box(string name, Vector3 position, Vector3 size, string layer)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube); box.name = name; box.transform.position = position;
            box.transform.localScale = size; box.layer = LayerMask.NameToLayer(layer); return box;
        }
        static Text Label(Transform parent, string name, Vector2 position, Vector2 size, int fontSize)
        {
            var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.sizeDelta = size; rect.anchoredPosition = position;
            var text = obj.AddComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize; text.color = Color.white; text.alignment = TextAnchor.MiddleCenter; return text;
        }
        static void OrderBoard()
        {
            Instance("Assets/Prefabs/UIGameplay/UI_OrderRail.prefab", new Vector3(0, 1.8f, -.62f));
        }
        [MenuItem("Canteen Rush/Gameplay/Build P2 sandbox")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Box("Floor", new Vector3(0, -.1f, 2), new Vector3(14, .2f, 16), "Environment").tag = "SurfaceTile";
            var light = new GameObject("Sun").AddComponent<Light>(); light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(50, -30, 0);
            var surface = new GameObject("Navigation").AddComponent<NavMeshSurface>();
            surface.layerMask = 1 << LayerMask.NameToLayer("Environment");
            var rig = Instance("Assets/Prefabs/Systems/Rig.prefab", new Vector3(0, 0, -3));
            Instance(Gameplay + "DeviceSimulator.prefab", Vector3.zero);
            var systems = Instance("Assets/Prefabs/Systems/Systems.prefab", Vector3.zero);
            var manager = systems.GetComponent<GameManager>(); var orders = systems.GetComponent<OrderService>();
            var customers = systems.GetComponent<CustomerManager>();
            var entrance = new GameObject("Marker_CustomerEntrance"); entrance.transform.position = new Vector3(0, 0, 7);
            Ref(customers, "_entrance", entrance.transform);
            var camera = rig.GetComponentInChildren<Camera>(true); if (camera != null) Ref(customers, "_player", camera.transform);
            var spawners = new List<ItemSpawner>();
            string[] names = { "Donut", "Cola", "Shake", "Cake", "Cup" };
            float[] respawns = { 5, 6, 8, 10, 5 };
            Box("Counter", new Vector3(0, .45f, -1), new Vector3(4, .9f, .7f), "Stations");
            for (int i = 0; i < names.Length; i++)
            {
                var obj = Instance(Gameplay + (i == 4 ? "Station_CupDispenser.prefab" : "Station_ItemSpawner.prefab"), new Vector3(-1.6f + .8f * i, 1.1f, -1));
                var spawner = obj.GetComponent<ItemSpawner>();
                var item = AssetDatabase.LoadAssetAtPath<GameObject>(Gameplay + "Item_" + names[i] + ".prefab");
                Ref(spawner, "_prefab", item.GetComponent<ItemBehaviour>()); Float(spawner, "_respawnSeconds", respawns[i]); spawners.Add(spawner);
            }
            var fields = new SerializedObject(manager); var slots = fields.FindProperty("_spawners"); slots.arraySize = spawners.Count;
            for (int i = 0; i < spawners.Count; i++) slots.GetArrayElementAtIndex(i).objectReferenceValue = spawners[i]; fields.ApplyModifiedPropertiesWithoutUndo();
            var rack = Instance(Gameplay + "Station_TrayRack.prefab", new Vector3(-2.8f, 0, -1));
            Instance(Gameplay + "Station_WaterCooler.prefab", new Vector3(2.5f, 1, -1));
            var upper = Instance(Gameplay + "Station_FridgeDoorUpper.prefab", new Vector3(3.5f, 1.49f, -1)).GetComponent<FridgeDoor>();
            Box("FridgeShelf", new Vector3(3.9f, .98f, -.6f), new Vector3(1, .06f, .6f), "Stations");
            spawners[1].transform.position = new Vector3(3.9f, 1.2f, -.7f);
            Ref(spawners[1], "_door", upper);
            Instance(Gameplay + "Station_FridgeDoorLower.prefab", new Vector3(3.5f, .425f, -1));
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -2 : 2;
                Box("Table_" + i, new Vector3(x, .45f, 3), new Vector3(1.2f, .9f, 1), "Stations");
                var seat = new GameObject("Seat_" + i).AddComponent<SeatPoint>(); seat.Index = i; seat.transform.position = new Vector3(x, 0, 4.5f);
                var socket = Instance(Gameplay + "Table_ServeSocket.prefab", new Vector3(x, .98f, 3)).GetComponent<ServeSocket>();
                Int(socket, "_seatIndex", i); Ref(socket, "_orders", orders); Ref(socket, "_trayReturn", rack.transform);
            }
            var boot = new GameObject("SandboxBootstrap").AddComponent<SandboxBootstrap>(); Ref(boot, "_surface", surface); Ref(boot, "_game", manager);
            OrderBoard();
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            if (!EditorSceneManager.SaveScene(scene, "Assets/Scenes/Sandbox_P2.unity")) throw new System.InvalidOperationException("Could not save Sandbox_P2.unity");
        }
        public static void BuildAll() { GameplayPrefabBuilder.Build(); Build(); }
    }
}

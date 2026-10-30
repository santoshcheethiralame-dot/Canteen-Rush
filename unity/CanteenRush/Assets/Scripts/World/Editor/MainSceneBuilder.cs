using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CanteenRush.World.Editor
{
    // Generates Assets/Scenes/Main.unity from the imported FBX modules so nobody hand-edits scene YAML.
    // Run from the menu (Canteen Rush > World > Build Main Scene) or in batch mode:
    //   Unity.exe -batchmode -nographics -quit -projectPath <path> -executeMethod CanteenRush.World.Editor.MainSceneBuilder.BuildBatch
    // Until M2 Main.unity is a generated file: rerun this instead of editing the scene by hand.
    // Frames, offsets and the reasoning are in docs/LAYOUT.md.
    public static class MainSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string NavMeshPath = "Assets/Scenes/Main_NavMesh.asset";
        const float PatioDistance = 16f;      // shell centre -> patio ground centre along the open side
        const float TableRadius = 1.9f;       // chairs within this of a table belong to it

        [MenuItem("Canteen Rush/World/Build Main Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var systems = Root("_Systems"); var rigRoot = Root("_Rig"); var audio = Root("_Audio"); var haptics = Root("_Haptics");
            var world = Root("World"); var canteenRoot = Child(world, "Canteen"); var patioRoot = Child(world, "Patio");
            Child(world, "Backdrop");
            var stations = Root("Stations"); var spawnRoot = Child(stations, "SpawnPoints");
            Root("Customers"); Root("UI"); var lighting = Root("Lighting");

            int env = Layer("Environment"), furniture = Layer("Stations"), triggers = Layer("Triggers");

            // ---- canteen (nodes keep their authored positions: instance at the origin) ----
            var canteen = Instantiate("Assets/Art/Environment/Env_Canteen.fbx", canteenRoot.transform);
            Transform shell = canteen ? Find(canteen.transform, "Shell") : null;
            Transform caseA = canteen ? Find(canteen.transform, "DisplayCase_A") : null;
            Transform fridge = canteen ? Find(canteen.transform, "FridgeBody") : null;
            if (canteen == null || shell == null || caseA == null || fridge == null)
            {
                Debug.LogError("MainSceneBuilder: Env_Canteen.fbx missing or its nodes were renamed. Run tools/blender/build_world.py and let Unity import first.");
                return;
            }
            foreach (var n in new[] { "Shell", "DisplayCase_A", "DisplayCase_B", "Counter_A", "Counter_B", "FridgeBody", "WaterCooler" })
            {
                var t = Find(canteen.transform, n);
                if (t == null) continue;
                SetLayer(t.gameObject, env);
                if (n == "Shell") t.gameObject.AddComponent<MeshCollider>(); else AddBoxColliders(t.gameObject);
                StaticFlags(t.gameObject);
            }
            foreach (var n in new[] { "Plates", "Fixtures" })
            {
                var t = Find(canteen.transform, n);
                if (t != null) StaticFlags(t.gameObject);
            }

            Bounds shellB = Rb(shell.gameObject);
            Vector3 dir = Flat(Rb(caseA.gameObject).center - shellB.center).normalized;   // the open (customer) side
            dir = Mathf.Abs(dir.x) >= Mathf.Abs(dir.z) ? new Vector3(Mathf.Sign(dir.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(dir.z));
            Vector3 side = Vector3.Cross(Vector3.up, dir);

            // ---- patio, shifted rigidly onto the open side ----
            var patio = Instantiate("Assets/Art/Environment/Env_Patio.fbx", patioRoot.transform);
            if (patio == null) { Debug.LogError("MainSceneBuilder: Env_Patio.fbx missing."); return; }
            Transform ground = Find(patio.transform, "Ground");
            Vector3 target = Flat(shellB.center) + dir * PatioDistance;
            Vector3 delta = Flat(target) - Flat(Rb(ground.gameObject).center);
            patio.transform.position += delta;
            foreach (var n in new[] { "Ground", "Fence", "Building", "Planter", "Frames", "Doors", "Panels" })
            {
                var t = Find(patio.transform, n);
                if (t == null) continue;
                SetLayer(t.gameObject, env); StaticFlags(t.gameObject);
                if (n == "Fence") AddBoxColliders(t.gameObject); else t.gameObject.AddComponent<MeshCollider>();
            }
            Ground(ground.gameObject, "SurfaceStone");
            Bounds groundB = Rb(ground.gameObject);

            // ---- floors: the source had only placeholder planes ----
            var greyTile = GreyMaterial("M_Greybox_Tile", new Color(0.62f, 0.60f, 0.56f));
            var greyStone = GreyMaterial("M_Greybox_Stone", new Color(0.52f, 0.52f, 0.50f));
            var greyGrass = GreyMaterial("M_Greybox_Grass", new Color(0.30f, 0.45f, 0.25f));
            float shellExtent = Mathf.Abs(Vector3.Dot(shellB.size, dir)) * 0.5f;
            Vector3 frontEdge = Flat(shellB.center) + dir * shellExtent;
            Vector3 nearEdge = Flat(groundB.center) - dir * (Mathf.Abs(Vector3.Dot(groundB.size, dir)) * 0.5f);
            Slab("Floor_Canteen", canteenRoot.transform, Flat(shellB.center), Along(shellB.size, dir, side), greyTile, "SurfaceTile", env, dir);
            Vector3 apronMid = (frontEdge + nearEdge) * 0.5f;
            float apronLen = Vector3.Distance(frontEdge, nearEdge);
            if (apronLen > 0.1f)
                Slab("Apron", patioRoot.transform, apronMid, new Vector3(apronLen, 0.1f, Mathf.Max(Mathf.Abs(Vector3.Dot(groundB.size, side)), 6f)), greyStone, "SurfaceStone", env, dir);
            Slab("Ground_Outer", world.transform, Flat(shellB.center) + dir * (PatioDistance * 0.5f) + Vector3.down * 0.07f,
                 new Vector3(160f, 0.1f, 160f), greyGrass, null, 0, dir, true);

            // ---- furniture from markers ----
            var furnRoot = Child(patioRoot, "Furniture");
            var tables = Markers(patio.transform, "Table_").OrderBy(t => Vector3.Distance(Flat(t.position), Flat(shellB.center))).ToList();
            var chairs = Markers(patio.transform, "Chair_").ToList();
            var tablePrefab = Load("Assets/Art/Props/Prop_TablePatio.fbx");
            var chairPrefab = Load("Assets/Art/Props/Prop_ChairBlue.fbx");
            Vector3 chairFront = ChairFront(chairPrefab);
            var tableCentres = new List<Vector3>();
            foreach (var t in tables)
            {
                tableCentres.Add(t.position);
                if (tablePrefab == null) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(tablePrefab, furnRoot.transform);
                go.name = t.name; go.transform.position = t.position;
                SetLayer(go, furniture); AddBoxColliders(go); StaticFlags(go);
            }
            var chairObjs = new Dictionary<Transform, Vector3>();
            foreach (var c in chairs)
            {
                var nearest = tableCentres.Count > 0 ? tableCentres.OrderBy(p => Vector3.Distance(Flat(p), Flat(c.position))).First() : c.position + dir;
                Vector3 toTable = Flat(nearest - c.position); if (toTable.sqrMagnitude < 1e-4f) toTable = dir; toTable.Normalize();
                chairObjs[c] = toTable;
                if (chairPrefab == null) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(chairPrefab, furnRoot.transform);
                go.name = c.name; go.transform.position = c.position;
                go.transform.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(chairFront, toTable, Vector3.up), Vector3.up);
                SetLayer(go, furniture); AddBoxColliders(go); StaticFlags(go);
            }

            // ---- 8 seat points: per table, the 2 chairs nearest the canteen ----
            var seatRoot = Child(patioRoot, "SeatPoints");
            for (int ti = 0; ti < tables.Count && ti < 4; ti++)
            {
                var mine = chairs.Where(c => Vector3.Distance(Flat(c.position), Flat(tables[ti].position)) < TableRadius)
                                 .OrderBy(c => Vector3.Distance(Flat(c.position), Flat(shellB.center))).Take(2)
                                 .OrderBy(c => Vector3.Dot(c.position, side)).ToList();
                for (int k = 0; k < mine.Count; k++)
                {
                    var sp = new GameObject("SeatPoint_" + (ti * 2 + k));
                    sp.transform.SetParent(seatRoot.transform, false);
                    sp.transform.position = mine[k].position;
                    sp.transform.rotation = Quaternion.LookRotation(chairObjs[mine[k]], Vector3.up);
                    sp.tag = "SeatPoint";
                    sp.AddComponent<SeatPoint>().Index = ti * 2 + k;
                }
            }
            var entrance = new GameObject("Marker_CustomerEntrance");
            entrance.transform.SetParent(patioRoot.transform, false);
            entrance.transform.position = Flat(groundB.center) + dir * (Mathf.Abs(Vector3.Dot(groundB.size, dir)) * 0.5f - 0.6f);
            entrance.transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);

            // ---- stations: spawn points, optional gameplay prefabs from P2 ----
            foreach (var m in Markers(canteen.transform, "Spawn_"))
            {
                var go = new GameObject(m.name);
                go.transform.SetParent(spawnRoot.transform, false);
                go.transform.position = m.position;
                go.AddComponent<SpawnPoint>().ItemKey = m.name.Split('_')[1];
            }
            ReplaceWithPrefab("Assets/Prefabs/Gameplay/Station_FridgeDoorUpper.prefab", Find(canteen.transform, "Door_Upper"), stations.transform);
            ReplaceWithPrefab("Assets/Prefabs/Gameplay/Station_FridgeDoorLower.prefab", Find(canteen.transform, "Door_Lower"), stations.transform);

            // ---- player start, rig or fallback camera ----
            Vector3 start = (Flat(Rb(caseA.gameObject).center) + Flat(Rb(fridge.gameObject).center)) * 0.5f;
            var startGo = new GameObject("PlayerStart");
            startGo.transform.SetParent(rigRoot.transform, false);
            startGo.transform.SetPositionAndRotation(start, Quaternion.LookRotation(dir, Vector3.up));
            var rig = Load("Assets/Prefabs/Systems/Rig.prefab");
            if (rig != null)
            {
                var r = (GameObject)PrefabUtility.InstantiatePrefab(rig, rigRoot.transform);
                r.transform.SetPositionAndRotation(start, startGo.transform.rotation);
            }
            else
            {
                var cam = new GameObject("Main Camera"); cam.tag = "MainCamera";
                cam.transform.SetParent(rigRoot.transform, false);
                cam.transform.SetPositionAndRotation(start + Vector3.up * 1.6f, startGo.transform.rotation);
                cam.AddComponent<Camera>(); cam.AddComponent<AudioListener>();
            }
            AddIfExists("Assets/Prefabs/Systems/Systems.prefab", systems.transform);
            AddIfExists("Assets/Prefabs/Systems/Audio.prefab", audio.transform);
            AddIfExists("Assets/Prefabs/Systems/Haptics.prefab", haptics.transform);

            // ---- audio zones (P3 attaches its component to these) ----
            var zones = Child(world, "AudioZones");
            Zone("AudioZone_Indoor", zones.transform, shellB.center, shellB.size + new Vector3(0.5f, 0.5f, 0.5f), triggers);
            Zone("AudioZone_Outdoor", zones.transform, Flat(groundB.center) + Vector3.up * 2f, new Vector3(120f, 8f, 120f), triggers);

            // ---- lighting basics (baking is a later step) ----
            var sun = new GameObject("Sun"); sun.transform.SetParent(lighting.transform, false);
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = sun.AddComponent<Light>(); light.type = LightType.Directional; light.shadows = LightShadows.Hard; light.intensity = 1.1f;
            RenderSettings.sun = light;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.60f, 0.70f);

            // ---- NavMesh over everything on the Environment layer ----
            var surface = world.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.layerMask = 1 << env;
            surface.BuildNavMesh();
            if (surface.navMeshData != null) AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);
            world.AddComponent<NavMeshBootstrap>();

            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("MainSceneBuilder: wrote " + ScenePath + " (open side direction " + dir + ", " + tables.Count + " tables, " + chairs.Count + " chairs).");
        }

        public static void BuildBatch() { Build(); }

        // ---------- helpers ----------
        static GameObject Root(string name) { return new GameObject(name); }
        static GameObject Child(GameObject parent, string name) { var g = new GameObject(name); g.transform.SetParent(parent.transform, false); return g; }
        static int Layer(string name) { int l = LayerMask.NameToLayer(name); if (l < 0) Debug.LogWarning("Layer missing: " + name); return Mathf.Max(l, 0); }
        static Vector3 Flat(Vector3 v) { return new Vector3(v.x, 0f, v.z); }

        static GameObject Load(string path) { return AssetDatabase.LoadAssetAtPath<GameObject>(path); }

        static GameObject Instantiate(string path, Transform parent)
        {
            var asset = Load(path);
            if (asset == null) { Debug.LogWarning("Missing asset " + path); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity;
            return go;
        }

        static void AddIfExists(string path, Transform parent)
        {
            var p = Load(path);
            if (p != null) PrefabUtility.InstantiatePrefab(p, parent);
        }

        static void ReplaceWithPrefab(string path, Transform node, Transform parent)
        {
            var p = Load(path);
            if (p == null || node == null) return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(p, parent);
            go.transform.SetPositionAndRotation(node.position, node.rotation);
            node.gameObject.SetActive(false);
        }

        static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform c in root) { var r = Find(c, name); if (r != null) return r; }
            return null;
        }

        static IEnumerable<Transform> Markers(Transform root, string prefix)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            return all.Where(t => t.name.StartsWith(prefix) && t.GetComponent<Renderer>() == null).OrderBy(t => t.name);
        }

        static Bounds Rb(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        static void SetLayer(GameObject go, int layer)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        static void AddBoxColliders(GameObject go)
        {
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                if (mf.GetComponent<Collider>() == null) mf.gameObject.AddComponent<BoxCollider>();
        }

        static void StaticFlags(GameObject go, bool recurse = true)
        {
            var flags = StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;
            GameObjectUtility.SetStaticEditorFlags(go, flags);
            if (recurse) foreach (Transform c in go.transform) StaticFlags(c.gameObject, true);
        }

        static void Ground(GameObject go, string tag)
        {
            go.tag = tag;
            foreach (var c in go.GetComponentsInChildren<Collider>()) c.gameObject.tag = tag;
        }

        static Vector3 Along(Vector3 size, Vector3 dir, Vector3 side)
        {
            return new Vector3(Mathf.Abs(Vector3.Dot(size, dir)), 0.1f, Mathf.Abs(Vector3.Dot(size, side)));
        }

        // Slab box whose local X runs along dir; top surface at y = 0 unless 'raw' (then centre.y is used as is).
        static void Slab(string name, Transform parent, Vector3 centre, Vector3 size, Material mat, string tag, int layer, Vector3 dir, bool raw = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.LookRotation(Vector3.Cross(dir, Vector3.up), Vector3.up);   // local X runs along dir
            go.transform.localScale = size;
            go.transform.position = raw ? centre : new Vector3(centre.x, -size.y * 0.5f, centre.z);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!string.IsNullOrEmpty(tag)) go.tag = tag;
            SetLayer(go, layer);
            StaticFlags(go, false);
        }

        static void Zone(string name, Transform parent, Vector3 centre, Vector3 size, int layer)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.transform.position = centre; go.layer = layer;
            var bc = go.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = size;
        }

        static Material GreyMaterial(string name, Color colour)
        {
            string path = "Assets/Art/Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            m = new Material(shader != null ? shader : Shader.Find("Standard"));
            m.SetColor("_BaseColor", colour); m.color = colour;
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        // Which way the chair mesh faces, in its own XZ plane: opposite the backrest.
        static Vector3 ChairFront(GameObject chairPrefab)
        {
            Vector3 fallback = Vector3.forward;
            if (chairPrefab == null) return fallback;
            var mf = chairPrefab.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return fallback;
            try
            {
                var v = mf.sharedMesh.vertices;
                Vector3 hi = Vector3.zero, mid = Vector3.zero; int nh = 0, nm = 0;
                float top = mf.sharedMesh.bounds.max.y;
                foreach (var p in v)
                {
                    if (p.y > top * 0.70f) { hi += p; nh++; }
                    else if (p.y > top * 0.40f && p.y < top * 0.55f) { mid += p; nm++; }
                }
                if (nh == 0 || nm == 0) return fallback;
                Vector3 back = Flat(hi / nh - mid / nm);
                if (back.sqrMagnitude < 1e-6f) return fallback;
                return -back.normalized;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("ChairFront: mesh not readable (" + e.Message + "); chairs keep default facing.");
                return fallback;
            }
        }
    }
}

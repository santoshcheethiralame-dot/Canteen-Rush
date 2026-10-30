using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

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
        const float PatioDistance = 14f;      // shell centre -> patio ground centre along the open side
        const float TableRadius = 1.9f;       // chairs within this of a table belong to it

        [MenuItem("Canteen Rush/World/Build Main Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var systems = Root("_Systems"); var rigRoot = Root("_Rig"); var audio = Root("_Audio"); var haptics = Root("_Haptics");
            var world = Root("World"); var canteenRoot = Child(world, "Canteen"); var patioRoot = Child(world, "Patio");
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
                if (n != "Ground")
                {
                    // roofs and ledges must not become walkable islands (the entrance once snapped onto the building roof)
                    var mod = t.gameObject.AddComponent<NavMeshModifier>();
                    mod.overrideArea = true; mod.area = 1;   // area 1 = Not Walkable
                }
            }
            Ground(ground.gameObject, "SurfaceStone");
            Bounds groundB = Rb(ground.gameObject);

            // ---- floors: the source had only placeholder planes ----
            // Ground(go, tag) above only tags; the flat-colour material the FBX arrived with is replaced by a tiled supplied texture.
            var groundMesh = ground.GetComponent<MeshFilter>();
            var groundMat = TiledMaterial("M_Ground_Patio", "T_Granite", new Color(1f, 1f, 1f), 3f, 3f);
            if (groundMesh != null && groundMesh.sharedMesh != null) FitTiling(groundMat, groundMesh.sharedMesh, groundB.size, 3f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMat;
            float shellExtent = Mathf.Abs(Vector3.Dot(shellB.size, dir)) * 0.5f;
            Vector3 frontEdge = Flat(shellB.center) + dir * shellExtent;
            Vector3 nearEdge = Flat(groundB.center) - dir * (Mathf.Abs(Vector3.Dot(groundB.size, dir)) * 0.5f);
            var greyTile = TiledMaterial("M_Floor_Canteen", "T_FloorBrick", new Color(0.80f, 0.76f, 0.70f), Along(shellB.size, dir, side).x / 2f, Along(shellB.size, dir, side).z / 1f);
            Slab("Floor_Canteen", canteenRoot.transform, Flat(shellB.center), Along(shellB.size, dir, side), greyTile, "SurfaceTile", env, dir);
            Vector3 apronMid = (frontEdge + nearEdge) * 0.5f;
            float apronLen = Vector3.Distance(frontEdge, nearEdge);
            var greyStone = TiledMaterial("M_Floor_Apron", "T_Granite", new Color(0.9f, 0.9f, 0.88f), apronLen / 3f, Mathf.Max(Mathf.Abs(Vector3.Dot(groundB.size, side)), 6f) / 3f);
            if (apronLen > 0.1f)
                Slab("Apron", patioRoot.transform, apronMid, new Vector3(apronLen, 0.1f, Mathf.Max(Mathf.Abs(Vector3.Dot(groundB.size, side)), 6f)), greyStone, "SurfaceStone", env, dir);

            // ---- furniture from markers, re-staggered for the roof ----
            var furnRoot = Child(patioRoot, "Furniture");
            var tables = Markers(patio.transform, "Table_").OrderBy(t => Vector3.Distance(Flat(t.position), Flat(shellB.center))).ToList();
            var chairs = Markers(patio.transform, "Chair_").ToList();
            var tablePrefab = Load("Assets/Art/Props/Prop_TablePatio.fbx");
            var chairPrefab = Load("Assets/Art/Props/Prop_ChairBlue.fbx");
            var chairLod = Load("Assets/Art/Props/Prop_ChairBlue_LOD1.fbx");
            Vector3 chairFront = ChairFront(chairPrefab);

            // The source's 2x2 grid becomes a zig-zag: walks of roughly 9 to 18 m (the spec's tension), alternating sides so customers
            // do not queue in one lane, the last pair next to the stair core. Chairs keep their offset from their own table.
            float[] rowAlong = { 9.5f, 12.3f, 15.2f, 18.0f };
            float[] rowSide = { -2.4f, 2.4f, -2.4f, 2.4f };
            var tableCentres = new List<Vector3>();
            for (int i = 0; i < tables.Count; i++)
            {
                int k = Mathf.Min(i, rowAlong.Length - 1);
                var pos = Flat(shellB.center) + dir * rowAlong[k] + side * rowSide[k]; pos.y = tables[i].position.y;
                tableCentres.Add(pos);
            }
            var chairPos = new Dictionary<Transform, Vector3>();
            foreach (var c in chairs)
            {
                int k = 0; float best = float.MaxValue;
                for (int i = 0; i < tables.Count; i++) { float d = Vector3.Distance(Flat(tables[i].position), Flat(c.position)); if (d < best) { best = d; k = i; } }
                chairPos[c] = tableCentres[k] + (c.position - tables[k].position);
            }
            for (int i = 0; i < tables.Count; i++)
            {
                if (tablePrefab == null) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(tablePrefab, furnRoot.transform);
                go.name = tables[i].name; go.transform.position = tableCentres[i];
                SetLayer(go, furniture); AddBoxColliders(go); StaticFlags(go);
            }
            var chairObjs = new Dictionary<Transform, Vector3>();
            foreach (var c in chairs)
            {
                var near = tableCentres.Count > 0 ? tableCentres.OrderBy(p => Vector3.Distance(Flat(p), Flat(chairPos[c]))).First() : chairPos[c] + dir;
                Vector3 toTable = Flat(near - chairPos[c]); if (toTable.sqrMagnitude < 1e-4f) toTable = dir; toTable.Normalize();
                chairObjs[c] = toTable;
                if (chairPrefab == null) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(chairPrefab, furnRoot.transform);
                go.name = c.name; go.transform.position = chairPos[c];
                go.transform.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(chairFront, toTable, Vector3.up), Vector3.up);
                SetLayer(go, furniture); AddBoxColliders(go); AddLod(go, chairLod, 0.054f, 0.02f); StaticFlags(go);
            }

            // ---- 8 seat points: per table, the 2 chairs nearest the canteen ----
            var seatRoot = Child(patioRoot, "SeatPoints");
            for (int ti = 0; ti < tables.Count && ti < 4; ti++)
            {
                var mine = chairs.Where(c => Vector3.Distance(Flat(chairPos[c]), Flat(tableCentres[ti])) < TableRadius)
                                 .OrderBy(c => Vector3.Distance(Flat(chairPos[c]), Flat(shellB.center))).Take(2)
                                 .OrderBy(c => Vector3.Dot(chairPos[c], side)).ToList();
                for (int k = 0; k < mine.Count; k++)
                {
                    var sp = new GameObject("SeatPoint_" + (ti * 2 + k));
                    sp.transform.SetParent(seatRoot.transform, false);
                    sp.transform.position = chairPos[mine[k]];
                    sp.transform.rotation = Quaternion.LookRotation(chairObjs[mine[k]], Vector3.up);
                    sp.tag = "SeatPoint";
                    sp.AddComponent<SeatPoint>().Index = ti * 2 + k;
                }
            }
            Vector3 entranceWanted = Flat(groundB.center) + dir * (Mathf.Abs(Vector3.Dot(groundB.size, dir)) * 0.5f - 0.6f);

            Vector3 start = (Flat(Rb(caseA.gameObject).center) + Flat(Rb(fridge.gameObject).center)) * 0.5f;

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

            // ---- player start, rig or fallback camera (start was computed above) ----
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

            // ---- everything decorative that is not gameplay ----
            Bounds fenceB = Rb(Find(patio.transform, "Fence").gameObject);
            float fenceSide = Mathf.Sign(Vector3.Dot(fenceB.center - groundB.center, side));
            var backdrops = PlaceBackdrops(Child(world, "Backdrop").transform, groundB, dir, side, fenceSide);
            backdrops.AddRange(BuildCanopy(Child(world, "Canopy").transform, shellB, groundB, dir, side, env));
            BuildRoof(Child(world, "Roof").transform, shellB, groundB, backdrops, dir, side, env);
            PlacePlants(patioRoot.transform, groundB, frontEdge, dir, side, fenceSide, env);
            Boundary(Child(world, "Boundary").transform, shellB, groundB, dir, side, env);
            LightingBuilder.Build(lighting.transform, canteen.transform, shellB, groundB, Rb(fridge.gameObject), dir, side, frontEdge, start);
            PerfMarkers(Child(lighting, "PerfProbe").transform, start, frontEdge, groundB, dir, side);

            // ---- NavMesh over everything on the Environment layer ----
            var surface = world.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.layerMask = 1 << env;
            surface.BuildNavMesh();
            if (surface.navMeshData != null) AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);
            world.AddComponent<NavMeshBootstrap>();

            // entrance: the far end of the patio, snapped to the nearest walkable point (the service building may occupy the end)
            NavMeshHit eh;
            if (NavMesh.SamplePosition(entranceWanted, out eh, 8f, NavMesh.AllAreas)) entranceWanted = eh.position;
            var entrance = new GameObject("Marker_CustomerEntrance");
            entrance.transform.SetParent(patioRoot.transform, false);
            entrance.transform.SetPositionAndRotation(entranceWanted, Quaternion.LookRotation(-dir, Vector3.up));

            SavePrefabs(canteenRoot, patioRoot, world.transform.Find("Backdrop").gameObject);

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
        static GameObject Slab(string name, Transform parent, Vector3 centre, Vector3 size, Material mat, string tag, int layer, Vector3 dir, bool raw = false)
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
            return go;
        }

        static void Zone(string name, Transform parent, Vector3 centre, Vector3 size, int layer)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.transform.position = centre; go.layer = layer;
            var bc = go.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = size;
        }

        // Shared URP/Lit asset with a supplied texture tiled across a primitive's 0..1 UVs.
        static Material TiledMaterial(string name, string texture, Color tint, float tilesU, float tilesV)
        {
            string path = "Assets/Art/Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/" + texture + ".jpg");
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", tint);
            m.SetTextureScale("_BaseMap", new Vector2(Mathf.Max(tilesU, 1f), Mathf.Max(tilesV, 1f)));
            m.SetFloat("_Smoothness", 0.25f);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material FlatMaterial(string name, Color colour)
        {
            string path = "Assets/Art/Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", colour); m.SetFloat("_Smoothness", 0.15f); m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        // The patio ground plane came with its own UVs; scale the tiling so one texture repeat is 'tile' metres whatever span they cover.
        static void FitTiling(Material m, Mesh mesh, Vector3 size, float tile)
        {
            var uv = mesh.uv;
            if (uv == null || uv.Length == 0) return;
            Vector2 lo = uv[0], hi = uv[0];
            foreach (var u in uv) { lo = Vector2.Min(lo, u); hi = Vector2.Max(hi, u); }
            float su = Mathf.Max(hi.x - lo.x, 1e-3f), sv = Mathf.Max(hi.y - lo.y, 1e-3f);
            float a = Mathf.Max(size.x, size.z), b = Mathf.Min(size.x, size.z);
            m.SetTextureScale("_BaseMap", new Vector2(a / tile / su, b / tile / sv));
        }

        // LOD0 = the FBX itself, LOD1 = a low-poly child; heights are screen-relative (chair: LOD1 at about 15 m, culled at about 40 m).
        static void AddLod(GameObject go, GameObject lodPrefab, float lod1Height, float cullHeight)
        {
            var r0 = go.GetComponentInChildren<MeshRenderer>();
            var lodMesh = lodPrefab != null ? lodPrefab.GetComponentInChildren<MeshFilter>() : null;
            if (r0 == null || lodMesh == null || lodMesh.sharedMesh == null) return;
            var child = new GameObject("LOD1"); child.transform.SetParent(go.transform, false);
            child.transform.localPosition = r0.transform.localPosition; child.transform.localRotation = r0.transform.localRotation; child.transform.localScale = r0.transform.localScale;
            child.AddComponent<MeshFilter>().sharedMesh = lodMesh.sharedMesh;
            var r1 = child.AddComponent<MeshRenderer>();
            var src = r0.sharedMaterials; var mats = new Material[lodMesh.sharedMesh.subMeshCount];
            for (int i = 0; i < mats.Length; i++) mats[i] = src[Mathf.Min(i, src.Length - 1)];
            r1.sharedMaterials = mats;
            var lg = go.AddComponent<LODGroup>();
            lg.SetLODs(new[] { new LOD(lod1Height, new Renderer[] { r0 }), new LOD(cullHeight, new Renderer[] { r1 }) });
            lg.fadeMode = LODFadeMode.None;
            lg.RecalculateBounds();
        }

        // Backdrop FBX resting on y = 0, no collider, no shadow casting (beyond 20 m nobody sees the difference).
        // Its nearest face is put exactly 'clearance' metres from 'anchor' along 'axis'; 'lateral' slides it sideways.
        static GameObject Backdrop(string path, Transform parent, Vector3 anchor, Vector3 axis, float clearance, Vector3 lateral, float yaw)
        {
            var go = Instantiate(path, parent);
            if (go == null) return null;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var b = Rb(go);
            Vector3 centre = anchor + axis * (clearance + Mathf.Abs(Vector3.Dot(b.extents, axis))) + lateral;
            go.transform.position += new Vector3(centre.x - b.center.x, -b.min.y, centre.z - b.center.z);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            return go;
        }

        const float BackdropClearance = 6f;    // backdrops are other corners of the same roof; the boundary walls keep the player out
        const float RoofHeight = 42f;          // 13th floor: about 13 storeys of 3.2 m

        static List<GameObject> PlaceBackdrops(Transform root, Bounds groundB, Vector3 dir, Vector3 side, float fenceSide)
        {
            Vector3 g = Flat(groundB.center);
            float along = Mathf.Abs(Vector3.Dot(groundB.size, dir)) * 0.5f, across = Mathf.Abs(Vector3.Dot(groundB.size, side)) * 0.5f;
            float yaw = Mathf.Abs(dir.x) >= Mathf.Abs(dir.z) ? 0f : 90f;
            Vector3 open = side * -fenceSide;   // the long edge without the fence is the one the player sees
            var all = new List<GameObject>();
            all.Add(Backdrop("Assets/Art/Environment/Env_PodsBackdrop.fbx", root, g + open * across, open, BackdropClearance, dir * -2f, yaw));
            all.Add(Backdrop("Assets/Art/Environment/Env_Terrace.fbx", root, g - open * across, -open, BackdropClearance, dir * 3f, yaw));
            return all;
        }

        const float CanopyHalfWidth = 8.8f, GrillOffset = 8.3f, TreeOffset = 5.6f;

        // The roof over the canteen and patio. The branching "trees" of Part 1 are its columns (two rows, one per long side, each 3 columns
        // at 5 m spacing); the canopy slab rests on their tips, and a lattice grill closes both long sides from the deck to the slab.
        static List<GameObject> BuildCanopy(Transform root, Bounds shellB, Bounds groundB, Vector3 dir, Vector3 side, int env)
        {
            var made = new List<GameObject>();
            Vector3 origin = Flat(shellB.center);
            float yaw = Mathf.Abs(dir.x) >= Mathf.Abs(dir.z) ? 90f : 0f;   // the module's row of columns runs along world Z unrotated
            float aMin = -(Mathf.Abs(Vector3.Dot(shellB.size, dir)) * 0.5f + 1.5f);
            var bark = FlatMaterial("M_Column", new Color(0.62f, 0.60f, 0.56f));
            float rowEnd = aMin, top = 0f, aMax = aMin;
            foreach (int rowIndex in new[] { 0, 1 })
            {
                float start = rowIndex == 0 ? aMin : rowEnd + 0.8f;
                foreach (float sgn in new[] { 1f, -1f })
                {
                    var go = Instantiate("Assets/Art/Environment/Env_Skyline.fbx", root);
                    if (go == null) return made;
                    go.name = "Columns_" + rowIndex + (sgn > 0 ? "A" : "B");
                    go.transform.rotation = Quaternion.Euler(0f, yaw + (sgn > 0 ? 0f : 180f), 0f);
                    var trees = Find(go.transform, "Skyline_Trees");
                    var pot = Find(go.transform, "Skyline_Pot");
                    if (pot != null) pot.gameObject.SetActive(false);
                    var tb = Rb(trees.gameObject);
                    // put the row's first column at 'start' along dir, its trunk line at +-TreeOffset across, feet on the deck
                    float a0 = Vector3.Dot(Flat(tb.min) - origin, dir), a1 = Vector3.Dot(Flat(tb.max) - origin, dir);
                    float aLo = Mathf.Min(a0, a1), aHi = Mathf.Max(a0, a1);
                    float lat = Vector3.Dot(Flat(tb.center) - origin, side);
                    Vector3 move = dir * (start - aLo) + side * (sgn * TreeOffset - lat);
                    go.transform.position += new Vector3(move.x, -tb.min.y, move.z);
                    foreach (var r in trees.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterials = new[] { bark };
                    SetLayer(go, env); StaticFlags(go); NotWalkable(trees.gameObject);
                    foreach (var mf in trees.GetComponentsInChildren<MeshFilter>()) { var mc = mf.gameObject.AddComponent<MeshCollider>(); }
                    made.Add(go);
                    top = Mathf.Max(top, Rb(trees.gameObject).max.y);
                    if (sgn > 0) { rowEnd = start + (aHi - aLo); aMax = Mathf.Max(aMax, rowEnd); }
                }
            }

            // the roof, resting on the column tips
            float aLen = aMax - aMin + 3f, aMid = (aMin + aMax) * 0.5f;
            var roofMat = FlatMaterial("M_Canopy", new Color(0.80f, 0.79f, 0.76f));
            var slab = Slab("Canopy_Roof", root, new Vector3(origin.x, 0f, origin.z) + dir * aMid + Vector3.up * (top + 0.1f),
                            new Vector3(aLen, 0.3f, CanopyHalfWidth * 2f), roofMat, null, env, dir, true);
            NotWalkable(slab); made.Add(slab);

            // grill walls: floor to the underside of the roof
            float underside = top + 0.1f - 0.15f;
            var grillMat = FlatMaterial("M_Grill", new Color(0.20f, 0.22f, 0.25f));
            grillMat.SetFloat("_Metallic", 0.6f); grillMat.SetFloat("_Smoothness", 0.5f);
            foreach (float sgn in new[] { 1f, -1f })
            {
                var g = new GameObject(sgn > 0 ? "Grill_A" : "Grill_B"); g.transform.SetParent(root, false);
                g.transform.rotation = Quaternion.LookRotation(side, Vector3.up);   // local X along dir, as in Slab
                g.transform.position = origin + dir * aMid + side * (sgn * GrillOffset);
                var gg = g.AddComponent<GridGrill>(); gg.Length = aLen; gg.Height = underside; gg.Pitch = 0.5f;
                g.GetComponent<MeshRenderer>().sharedMaterial = grillMat;
                SetLayer(g, env);
                var bc = g.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, underside * 0.5f, 0f); bc.size = new Vector3(aLen, underside, 0.1f);
                NotWalkable(g);
                GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.OccludeeStatic);
                made.Add(g);
            }
            return made;
        }

        // The deck every other module stands on, its parapet (not walkable), and the campus 42 m below.
        static void BuildRoof(Transform root, Bounds shellB, Bounds groundB, List<GameObject> backdrops, Vector3 dir, Vector3 side, int env)
        {
            var area = new Bounds(shellB.center, Vector3.zero);
            area.Encapsulate(shellB); area.Encapsulate(groundB);
            foreach (var b in backdrops) if (b != null) area.Encapsulate(Rb(b));
            area.Expand(new Vector3(8f, 0f, 8f));   // 4 m of deck beyond the outermost module on every side
            Vector3 c = Flat(area.center);
            Vector3 size = Along(area.size, dir, side);   // x along dir, z along side

            var deckMat = TiledMaterial("M_Roof_Deck", "T_Granite", new Color(0.70f, 0.70f, 0.68f), size.x / 4f, size.z / 4f);
            var deck = Slab("Roof_Deck", root, new Vector3(c.x, -0.33f, c.z), new Vector3(size.x, 0.6f, size.z), deckMat, "SurfaceStone", env, dir, true);
            // Walkable on purpose: the patio floor sits 1-3 cm above the deck, and an overlapping Not Walkable deck swallowed it in the bake.

            var wallMat = FlatMaterial("M_Parapet", new Color(0.72f, 0.71f, 0.68f));
            var capMat = FlatMaterial("M_ParapetCap", new Color(0.86f, 0.85f, 0.82f));
            const float t = 0.35f, h = 1.15f;
            var specs = new[]
            {
                (n: "Parapet_Far",   centre: c + dir * (size.x * 0.5f - t * 0.5f),   len: new Vector3(t, h, size.z)),
                (n: "Parapet_Back",  centre: c - dir * (size.x * 0.5f - t * 0.5f),   len: new Vector3(t, h, size.z)),
                (n: "Parapet_SideA", centre: c + side * (size.z * 0.5f - t * 0.5f),  len: new Vector3(size.x, h, t)),
                (n: "Parapet_SideB", centre: c - side * (size.z * 0.5f - t * 0.5f),  len: new Vector3(size.x, h, t)),
            };
            foreach (var sp in specs)
            {
                Block(sp.n, root, sp.centre + Vector3.up * (h * 0.5f - 0.03f), sp.len, wallMat, dir, side, env);
                Block(sp.n + "_Cap", root, sp.centre + Vector3.up * (h + 0.04f - 0.03f), sp.len + new Vector3(sp.len.x < sp.len.z ? 0.12f : 0f, 0.08f - sp.len.y, sp.len.x < sp.len.z ? 0f : 0.12f), capMat, dir, side, env);
            }

            // The campus the player sees over the parapet: a ground plane and a ring of plain blocks, far enough to read as silhouettes.
            var campus = Child(root.gameObject, "Campus");
            var ground = Slab("Campus_Ground", campus.transform, new Vector3(c.x, -RoofHeight - 0.5f, c.z), new Vector3(900f, 1f, 900f),
                              TiledMaterial("M_Campus_Ground", "T_Grass", new Color(0.62f, 0.72f, 0.52f), 900f / 8f, 900f / 8f), null, 0, dir, true);
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            ground.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            var palette = new[] { FlatMaterial("M_Campus_A", new Color(0.78f, 0.74f, 0.68f)), FlatMaterial("M_Campus_B", new Color(0.62f, 0.45f, 0.38f)), FlatMaterial("M_Campus_C", new Color(0.70f, 0.74f, 0.78f)) };
            var rng = new System.Random(13);
            float reach = Mathf.Max(size.x, size.z) * 0.5f;
            for (int i = 0; i < 46; i++)
            {
                double ang = rng.NextDouble() * System.Math.PI * 2.0;
                float radius = reach + 45f + (float)rng.NextDouble() * 160f;
                float w = 14f + (float)rng.NextDouble() * 26f, d = 14f + (float)rng.NextDouble() * 26f, bh = 14f + (float)rng.NextDouble() * 56f;
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                b.name = "Campus_Block_" + i; b.transform.SetParent(campus.transform, false);
                b.transform.position = new Vector3(c.x + (float)System.Math.Cos(ang) * radius, -RoofHeight + bh * 0.5f, c.z + (float)System.Math.Sin(ang) * radius);
                b.transform.localScale = new Vector3(w, bh, d);
                b.GetComponent<Renderer>().sharedMaterial = palette[i % palette.Length];
                b.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Object.DestroyImmediate(b.GetComponent<Collider>());
                GameObjectUtility.SetStaticEditorFlags(b, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            }
        }

        static void NotWalkable(GameObject go)
        {
            var mod = go.AddComponent<NavMeshModifier>();
            mod.overrideArea = true; mod.area = 1;   // area 1 = Not Walkable
        }

        // Box with local X along dir and Z along side: size = (along dir, height, along side).
        static GameObject Block(string name, Transform parent, Vector3 centre, Vector3 size, Material mat, Vector3 dir, Vector3 side, int env)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.LookRotation(side, Vector3.up);
            go.transform.localScale = size; go.transform.position = centre;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            SetLayer(go, env); StaticFlags(go, false); NotWalkable(go);
            return go;
        }

        static void PlacePlants(Transform root, Bounds groundB, Vector3 frontEdge, Vector3 dir, Vector3 side, float fenceSide, int env)
        {
            var pot = Load("Assets/Art/Props/Prop_PlantPot.fbx");
            var lod = Load("Assets/Art/Props/Prop_PlantPot_LOD1.fbx");
            if (pot == null) return;
            var plants = Child(root.gameObject, "Plants");
            Vector3 g = Flat(groundB.center);
            float across = Mathf.Abs(Vector3.Dot(groundB.size, side)) * 0.5f;
            var spots = new List<Vector3>();
            foreach (float a in new[] { -5.5f, 0f, 5.5f }) spots.Add(g + dir * a + side * fenceSide * (across - 0.9f));
            spots.Add(frontEdge + dir * 1.2f + side * 4.2f); spots.Add(frontEdge + dir * 1.2f - side * 4.2f);
            for (int i = 0; i < spots.Count; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(pot, plants.transform);
                go.name = "PlantPot_" + i; go.transform.position = spots[i]; go.transform.rotation = Quaternion.Euler(0f, 73f * i, 0f);
                SetLayer(go, env); AddBoxColliders(go); AddLod(go, lod, 0.06f, 0.02f); StaticFlags(go);
            }
        }

        // Invisible walls around the playable rectangle (NavMeshSurface reads render meshes only, so these never touch navigation).
        static void Boundary(Transform root, Bounds shellB, Bounds groundB, Vector3 dir, Vector3 side, int env)
        {
            float back = Mathf.Abs(Vector3.Dot(shellB.size, dir)) * 0.5f + 1.5f;
            float far = Vector3.Dot(Flat(groundB.center) - Flat(shellB.center), dir) + Mathf.Abs(Vector3.Dot(groundB.size, dir)) * 0.5f + 4f;
            float half = Mathf.Abs(Vector3.Dot(groundB.size, side)) * 0.5f + 4f;
            Vector3 c = Flat(shellB.center);
            Wall(root, "Wall_Back", c - dir * back, dir, side, 0.5f, half * 2f, env);
            Wall(root, "Wall_Far", c + dir * far, dir, side, 0.5f, half * 2f, env);
            Wall(root, "Wall_SideA", c + dir * ((far - back) * 0.5f) + side * half, dir, side, far + back, 0.5f, env);
            Wall(root, "Wall_SideB", c + dir * ((far - back) * 0.5f) - side * half, dir, side, far + back, 0.5f, env);
        }

        static void Wall(Transform root, string name, Vector3 centre, Vector3 dir, Vector3 side, float lenDir, float lenSide, int env)
        {
            var go = new GameObject(name); go.transform.SetParent(root, false);
            go.transform.position = centre + Vector3.up * 3f; go.layer = env;
            go.transform.rotation = Quaternion.LookRotation(side, Vector3.up);   // local X runs along dir
            go.AddComponent<BoxCollider>().size = new Vector3(lenDir, 6f, lenSide);
        }

        static void PerfMarkers(Transform root, Vector3 start, Vector3 frontEdge, Bounds groundB, Vector3 dir, Vector3 side)
        {
            Mark(root, "Marker_Perf_Counter", start + Vector3.up * 1.6f, Quaternion.LookRotation(dir, Vector3.up));
            Mark(root, "Marker_Perf_Door", frontEdge + dir * 0.8f + Vector3.up * 1.6f, Quaternion.LookRotation(dir, Vector3.up));
            Mark(root, "Marker_Perf_Patio", Flat(groundB.center) + Vector3.up * 1.6f, Quaternion.LookRotation(-dir * 0.8f + side * 0.6f, Vector3.up));
        }

        static void Mark(Transform root, string name, Vector3 pos, Quaternion rot)
        {
            var g = new GameObject(name); g.transform.SetParent(root, false); g.transform.SetPositionAndRotation(pos, rot);
        }

        static void SavePrefabs(params GameObject[] roots)
        {
            System.IO.Directory.CreateDirectory("Assets/Prefabs/World");
            foreach (var r in roots)
                if (r != null) PrefabUtility.SaveAsPrefabAssetAndConnect(r, "Assets/Prefabs/World/" + r.name + ".prefab", InteractionMode.AutomatedAction);
        }

        // Which way the chair mesh faces, in its own XZ plane: opposite the backrest.
        internal static Vector3 ChairFront(GameObject chairPrefab)
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

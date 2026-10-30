using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace CanteenRush.World.Editor
{
    // Reads the generated Main.unity and prints the numbers LAYOUT.md claims, so they are checked rather than assumed.
    // Batch: Unity.exe -batchmode -nographics -quit -projectPath <path> -executeMethod CanteenRush.World.Editor.SceneVerifier.Run
    public static class SceneVerifier
    {
        [MenuItem("Canteen Rush/World/Verify Main Scene")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);
            var sb = new StringBuilder("SCENE VERIFY\n");
            var shell = GameObject.Find("Shell"); var caseA = GameObject.Find("DisplayCase_A"); var fridge = GameObject.Find("FridgeBody");
            var ground = GameObject.Find("Ground"); var start = GameObject.Find("PlayerStart"); var entrance = GameObject.Find("Marker_CustomerEntrance");
            Bounds sb_ = B(shell), cb = B(caseA), fb = B(fridge), gb = B(ground);
            sb.AppendFormat("shell centre {0} size {1}\n", sb_.center, sb_.size);
            sb.AppendFormat("patio ground centre {0} size {1}; centre distance {2:F1} m\n", gb.center, gb.size, Vector3.Distance(Flat(sb_.center), Flat(gb.center)));
            sb.AppendFormat("display case A {0} size {1}; fridge {2} size {3}\n", cb.center, cb.size, fb.center, fb.size);
            float gap = Mathf.Abs(fb.center.x - cb.center.x) - fb.extents.x - cb.extents.x;
            sb.AppendFormat("aisle gap between case A and fridge along X: {0:F2} m (fridge door swings toward the case)\n", gap);
            sb.AppendFormat("PlayerStart {0}; inside shell bounds: {1}\n", start.transform.position, sb_.Contains(new Vector3(start.transform.position.x, sb_.center.y, start.transform.position.z)));

            var tri = new NavMeshTriangulation();
            tri = NavMesh.CalculateTriangulation();
            sb.AppendFormat("navmesh triangles: {0}\n", tri.indices.Length / 3);
            NavMeshHit eh; bool entOn = NavMesh.SamplePosition(entrance.transform.position, out eh, 1.5f, NavMesh.AllAreas);
            sb.AppendFormat("entrance {0} on navmesh: {1}\n", entrance.transform.position, entOn);

            foreach (var sp in Object.FindObjectsByType<SeatPoint>(FindObjectsSortMode.None).OrderBy(x => x.Index))
            {
                NavMeshHit h; bool on = NavMesh.SamplePosition(sp.transform.position, out h, 1.0f, NavMesh.AllAreas);
                var path = new NavMeshPath(); bool ok = entOn && on && NavMesh.CalculatePath(eh.position, h.position, NavMesh.AllAreas, path);
                float len = 0; if (ok) for (int i = 1; i < path.corners.Length; i++) len += Vector3.Distance(path.corners[i - 1], path.corners[i]);
                sb.AppendFormat("seat {0} at {1} navmesh:{2} reachable:{3} path {4:F1} m from entrance, {5:F1} m from player start\n", sp.Index, sp.transform.position, on, ok && path.status == NavMeshPathStatus.PathComplete, len,
                                Vector3.Distance(Flat(sp.transform.position), Flat(start.transform.position)));
            }

            foreach (var n in new[] { "Ground", "Fence", "Building", "Planter", "Frames", "Doors", "Panels", "Apron", "Floor_Canteen" })
            {
                var g = GameObject.Find(n); if (g == null) continue;
                var b = B(g); sb.AppendFormat("node {0}: centre {1} size {2} layer {3}\n", n, b.center, b.size, LayerMask.LayerToName(g.layer));
            }
            var tris = NavMesh.CalculateTriangulation();
            var min = new Vector3(1e9f, 0, 1e9f); var max = new Vector3(-1e9f, 0, -1e9f);
            foreach (var v in tris.vertices) { min = Vector3.Min(min, v); max = Vector3.Max(max, v); }
            sb.AppendFormat("navmesh vertex bounds x {0:F1}..{1:F1} z {2:F1}..{3:F1}, {4} verts\n", min.x, max.x, min.z, max.z, tris.vertices.Length);
            for (int i = 0; i < tris.indices.Length; i += 3)
            {
                var c = (tris.vertices[tris.indices[i]] + tris.vertices[tris.indices[i + 1]] + tris.vertices[tris.indices[i + 2]]) / 3f;
                sb.AppendFormat("  tri centre ({0:F1}, {1:F1})\n", c.x, c.z);
            }
            var chairPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Props/Prop_ChairBlue.fbx");
            Vector3 front = MainSceneBuilder.ChairFront(chairPrefab);
            sb.AppendFormat("chair local front vector {0}\n", front);
            int aligned = 0, total = 0;
            var tables = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("Table_") && t.GetComponent<MeshFilter>() != null).ToList();
            foreach (var c in GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("Chair_") && t.GetComponent<MeshFilter>() != null))
            {
                total++;
                var nearest = tables.OrderBy(t => Vector3.Distance(Flat(t.position), Flat(c.position))).First();
                Vector3 want = Flat(nearest.position - c.position).normalized, got = Flat(c.rotation * front).normalized;
                if (Vector3.Dot(want, got) > 0.95f) aligned++;
            }
            sb.AppendFormat("chairs facing their table (dot > 0.95): {0}/{1}; tables {2}\n", aligned, total, tables.Count);
            foreach (var t in tables) sb.AppendFormat("  {0} at {1} bounds {2}\n", t.name, t.position, t.GetComponent<MeshRenderer>().bounds.size);

            var layers = new[] { "Environment", "Stations", "Triggers" };
            foreach (var l in layers) sb.AppendFormat("objects on layer {0}: {1}\n", l, Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.gameObject.layer == LayerMask.NameToLayer(l)));
            sb.AppendFormat("spawn points: {0}\n", Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).Length);
            Debug.Log(sb.ToString());
        }

        static Bounds B(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        static Vector3 Flat(Vector3 v) { return new Vector3(v.x, 0f, v.z); }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace CanteenRush.World
{
    // A lattice screen (vertical and horizontal bars) built as ONE mesh, floor to roof, so the side walls of the roofed area need no
    // imported asset and cost a single draw call. X runs along Length, Y from 0 up to Height, Z is the bar depth.
    [ExecuteAlways, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GridGrill : MonoBehaviour
    {
        public float Length = 30f, Height = 7.5f, Pitch = 0.5f, Bar = 0.045f, Depth = 0.07f;

        Mesh mesh;

        void OnEnable() { Rebuild(); }

        void OnValidate() { if (isActiveAndEnabled) Rebuild(); }

        public void Rebuild()
        {
            if (mesh == null) { mesh = new Mesh { name = "GridGrill", hideFlags = HideFlags.DontSave }; }
            mesh.Clear();
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();

            int cols = Mathf.Max(1, Mathf.RoundToInt(Length / Pitch)), rows = Mathf.Max(1, Mathf.RoundToInt(Height / Pitch));
            for (int i = 0; i <= cols; i++) Box(v, n, uv, t, new Vector3(-Length * 0.5f + Length * i / cols, Height * 0.5f, 0f), new Vector3(Bar, Height, Depth));
            for (int j = 0; j <= rows; j++) Box(v, n, uv, t, new Vector3(0f, Height * j / rows, 0f), new Vector3(Length, Bar, Depth * 0.8f));

            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        static readonly Vector3[] Dirs = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };

        static void Box(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t, Vector3 c, Vector3 size)
        {
            Vector3 h = size * 0.5f;
            foreach (var d in Dirs)
            {
                Vector3 u = d == Vector3.up || d == Vector3.down ? Vector3.right : Vector3.up;
                Vector3 w = Vector3.Cross(d, u);
                Vector3 centre = c + Vector3.Scale(d, h);
                Vector3 ue = Vector3.Scale(u, h), we = Vector3.Scale(w, h);
                int b = v.Count;
                v.Add(centre - ue - we); v.Add(centre + ue - we); v.Add(centre + ue + we); v.Add(centre - ue + we);
                for (int k = 0; k < 4; k++) n.Add(d);
                uv.Add(Vector2.zero); uv.Add(Vector2.right); uv.Add(Vector2.one); uv.Add(Vector2.up);
                // wind so the face points along d whatever the cross product's handedness gave
                bool flip = Vector3.Dot(Vector3.Cross(u, w), d) < 0f;
                if (flip) { t.Add(b); t.Add(b + 1); t.Add(b + 2); t.Add(b); t.Add(b + 2); t.Add(b + 3); }
                else { t.Add(b); t.Add(b + 2); t.Add(b + 1); t.Add(b); t.Add(b + 3); t.Add(b + 2); }
            }
        }
    }
}

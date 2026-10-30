using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace CanteenRush.World.Editor
{
    // Lighting (spec section 9), bake, occlusion, scene statistics and the Windows build. Batch entry points are the *Batch methods:
    //   Unity.exe -batchmode -nographics -quit -projectPath <path> -executeMethod CanteenRush.World.Editor.LightingBuilder.BakeBatch
    // Lighting is a decision, not a default: mixed lights in Baked Indirect mode (not Shadowmask: one realtime sun, and Shadowmask
    // would add a second lightmap-sized texture per scene for no visible gain at patio distances). Details in docs/PERF_REPORT.md.
    public static class LightingBuilder
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        // ---- called by MainSceneBuilder while the scene is being generated ----
        internal static void Build(Transform lighting, Transform canteen, Bounds shellB, Bounds groundB, Bounds fridgeB, Vector3 dir, Vector3 side, Vector3 frontEdge, Vector3 start)
        {
            GraphicsSettings.lightsUseColorTemperature = true;

            var skyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/M_Sky.mat");
            if (skyMat == null)
            {
                skyMat = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(skyMat, "Assets/Art/Materials/M_Sky.mat");
            }
            skyMat.SetFloat("_SunSize", 0.04f); skyMat.SetFloat("_AtmosphereThickness", 1f); skyMat.SetFloat("_Exposure", 1.25f);
            skyMat.SetColor("_SkyTint", new Color(0.55f, 0.62f, 0.75f)); skyMat.SetColor("_GroundColor", new Color(0.37f, 0.35f, 0.33f));
            EditorUtility.SetDirty(skyMat);

            // The one realtime light: a mixed sun. Static architecture takes its shadow from the bake, dynamic items from the 25 m realtime shadow.
            var sun = new GameObject("Sun"); sun.transform.SetParent(lighting, false);
            sun.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            var sunLight = sun.AddComponent<Light>();
            sunLight.type = LightType.Directional; sunLight.lightmapBakeType = LightmapBakeType.Mixed;
            sunLight.shadows = LightShadows.Hard; sunLight.shadowStrength = 0.9f; sunLight.intensity = 1.25f;
            sunLight.useColorTemperature = true; sunLight.colorTemperature = 5600f;

            var cycle = new GameObject("SunCycle"); cycle.transform.SetParent(lighting, false);
            var sc = cycle.AddComponent<SunCycle>(); sc.Sun = sunLight; sc.SkyTemplate = skyMat;

            RenderSettings.sun = sunLight; RenderSettings.skybox = skyMat;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = sc.SkyStart; RenderSettings.ambientEquatorColor = sc.EquatorStart; RenderSettings.ambientGroundColor = sc.GroundStart;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 45f; RenderSettings.fogEndDistance = 150f; RenderSettings.fogColor = sc.FogStart;

            // Interior: two baked rectangle lights where the source's area lights hung, one baked point light by the fridge; all warm.
            var fixtures = FindNode(canteen, "Fixtures");
            Vector3 fx = fixtures != null ? Bounds(fixtures.gameObject).center : shellB.center;
            float ly = Mathf.Min(fx.y, shellB.max.y - 0.35f);
            foreach (float k in new[] { -1.2f, 1.2f })
            {
                var go = new GameObject(k < 0 ? "Light_Counter_A" : "Light_Counter_B"); go.transform.SetParent(lighting, false);
                go.transform.position = new Vector3(fx.x, ly, fx.z) + side * k; go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                var l = go.AddComponent<Light>();
                l.type = LightType.Rectangle; l.lightmapBakeType = LightmapBakeType.Baked; l.areaSize = new Vector2(1.2f, 0.4f);
                l.useColorTemperature = true; l.colorTemperature = 3500f; l.intensity = 6f; l.range = 8f;
            }
            var fp = new GameObject("Light_Fridge"); fp.transform.SetParent(lighting, false);
            fp.transform.position = fridgeB.center + dir * 0.9f + Vector3.up * 0.8f;
            var pl = fp.AddComponent<Light>();
            pl.type = LightType.Point; pl.lightmapBakeType = LightmapBakeType.Baked; pl.range = 5f; pl.intensity = 3.5f;
            pl.useColorTemperature = true; pl.colorTemperature = 3500f;

            // Light probes: counter, fridge, door and a grid across the patio, so carried items and customers take the right light.
            var probes = new GameObject("LightProbes"); probes.transform.SetParent(lighting, false);
            var pg = probes.AddComponent<LightProbeGroup>();
            var pts = new System.Collections.Generic.List<Vector3>();
            Vector3 c = new Vector3(shellB.center.x, 0f, shellB.center.z);
            foreach (float a in new[] { -2f, 0f, 2f }) foreach (float b in new[] { -2.5f, 0f, 2.5f }) foreach (float h in new[] { 0.9f, 1.8f }) pts.Add(c + dir * a + side * b + Vector3.up * h);
            pts.Add(new Vector3(start.x, 1.2f, start.z)); pts.Add(fridgeB.center + dir * 0.9f + Vector3.up * 0.4f);
            foreach (float s in new[] { -1.5f, 0f, 1.5f }) foreach (float h in new[] { 0.9f, 1.8f }) pts.Add(frontEdge + dir * 0.6f + side * s + Vector3.up * h);
            Vector3 g = new Vector3(groundB.center.x, 0f, groundB.center.z);
            float along = Mathf.Abs(Vector3.Dot(groundB.size, dir)) * 0.5f - 1f, across = Mathf.Abs(Vector3.Dot(groundB.size, side)) * 0.5f - 1f;
            for (int i = 0; i < 5; i++) for (int j = 0; j < 3; j++)
                pts.Add(g + dir * Mathf.Lerp(-along, along, i / 4f) + side * Mathf.Lerp(-across, across, j / 2f) + Vector3.up * 1.2f);
            pg.probePositions = pts.ToArray();

            // One baked 128 px reflection probe for the canteen (fridge glass, plates, cooler).
            var rp = new GameObject("ReflectionProbe_Canteen"); rp.transform.SetParent(lighting, false);
            rp.transform.position = new Vector3(shellB.center.x, 1.4f, shellB.center.z);
            var probe = rp.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked; probe.resolution = 128; probe.hdr = false; probe.boxProjection = true;
            probe.size = shellB.size + new Vector3(1f, 0f, 1f); probe.center = Vector3.zero; probe.importance = 1;

            // Lighting Settings asset: CPU progressive so it bakes on any machine (including this laptop), 3 lightmaps of 1024 at most.
            Directory.CreateDirectory("Assets/Lighting");
            string lsPath = "Assets/Lighting/Main_LightingSettings.lighting";
            var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(lsPath);
            if (ls == null) { ls = new LightingSettings(); AssetDatabase.CreateAsset(ls, lsPath); }
            ls.lightmapper = LightingSettings.Lightmapper.ProgressiveCPU;
            ls.bakedGI = true; ls.realtimeGI = false;
            ls.mixedBakeMode = MixedLightingMode.IndirectOnly;
            ls.lightmapResolution = 8f; ls.lightmapPadding = 2; ls.lightmapMaxSize = 1024;
            ls.directSampleCount = 32; ls.indirectSampleCount = 128; ls.environmentSampleCount = 64; ls.maxBounces = 2;
            ls.lightmapCompression = LightmapCompression.NormalQuality; ls.ao = false;
            LightmapEditorSettings.reflectionCubemapCompression = ReflectionCubemapCompression.Uncompressed;   // the BC6H path produced garbage reflections on the dev machine
            ls.directionalityMode = LightmapsMode.NonDirectional;   // halves lightmap memory; the speculars come from the reflection probe and the realtime sun
            EditorUtility.SetDirty(ls);
            Lightmapping.lightingSettings = ls;

            // Interior gets ~18 texels/m (2.25x the 8 of the patio); far, large or decorative renderers stay out of the lightmap.
            foreach (var r in canteen.GetComponentsInChildren<MeshRenderer>())
            {
                var so = new SerializedObject(r);
                var p = so.FindProperty("m_ScaleInLightmap");
                if (p != null) { p.floatValue = 2.25f; so.ApplyModifiedPropertiesWithoutUndo(); }
            }
        }

        static Transform FindNode(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform c in root) { var r = FindNode(c, name); if (r != null) return r; }
            return null;
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        // ---- batch entry points ----
        [MenuItem("Canteen Rush/World/Bake Lighting")]
        public static void BakeBatch()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            LightmapEditorSettings.reflectionCubemapCompression = ReflectionCubemapCompression.Uncompressed;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool ok = Lightmapping.Bake();
            Debug.Log("LightingBuilder: bake " + (ok ? "ok" : "FAILED") + " in " + sw.Elapsed.TotalSeconds.ToString("F0") + " s; lightmaps: " + LightmapSettings.lightmaps.Length);
            foreach (var lm in LightmapSettings.lightmaps)
                if (lm.lightmapColor != null) Debug.Log("  lightmap " + lm.lightmapColor.name + " " + lm.lightmapColor.width + "x" + lm.lightmapColor.height);
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
        }

        // Run WITHOUT -quit: the occlusion job needs editor ticks, so it polls from the update loop and exits itself.
        public static void OcclusionBatch()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            bool started = StaticOcclusionCulling.Compute();
            Debug.Log("LightingBuilder: occlusion compute started: " + started);
            double t0 = EditorApplication.timeSinceStartup;
            EditorApplication.update += () =>
            {
                if (StaticOcclusionCulling.isRunning && EditorApplication.timeSinceStartup - t0 < 600) return;
                Debug.Log("LightingBuilder: occlusion finished after " + (EditorApplication.timeSinceStartup - t0).ToString("F0") + " s");
                EditorSceneManager.SaveOpenScenes();
                AssetDatabase.SaveAssets();
                EditorApplication.Exit(0);
            };
        }

        // Renders the three standard views (plus the end-of-round sun) through the real pipeline into PNGs. Needs a graphics device: run
        // WITHOUT -nographics. Output folder: -shotdir <dir> (default <repo>/Builds/shots).
        public static void ScreenshotBatch()
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../Builds/shots"));
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-shotdir"); if (i >= 0 && i + 1 < args.Length) dir = args[i + 1];
            Directory.CreateDirectory(dir);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var cycle = Object.FindFirstObjectByType<SunCycle>();
            foreach (var spec in new[] { ("counter", 0f), ("interior", 0f), ("door", 0f), ("patio", 0f), ("patio_golden", 1f), ("interior_golden", 1f) })
            {
                string marker = "Marker_Perf_" + (spec.Item1.StartsWith("counter") || spec.Item1.StartsWith("interior") ? "Counter" : spec.Item1.StartsWith("door") ? "Door" : "Patio");
                var m = GameObject.Find(marker); if (m == null) { Debug.LogWarning("missing " + marker); continue; }
                if (cycle != null) cycle.Preview(spec.Item2);
                var go = new GameObject("shotcam"); go.transform.SetPositionAndRotation(m.transform.position, m.transform.rotation * (spec.Item1.StartsWith("interior") ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity));
                var cam = go.AddComponent<Camera>(); cam.fieldOfView = 75f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 300f; cam.allowHDR = false;
                var rt = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                rt.antiAliasing = 4; cam.targetTexture = rt;
                for (int k = 0; k < 2; k++) cam.Render();
                var prev = RenderTexture.active; RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
                RenderTexture.active = prev;
                File.WriteAllBytes(Path.Combine(dir, "shot_" + spec.Item1 + ".png"), tex.EncodeToPNG());
                Object.DestroyImmediate(tex); RenderTexture.ReleaseTemporary(rt); Object.DestroyImmediate(go);
            }
            if (cycle != null) cycle.Preview(0f);
            Debug.Log("LightingBuilder: screenshots written to " + dir);
        }

        public static void StatsBatch()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var sb = new StringBuilder("SCENE STATS\n");
            long tris = 0, triStatic = 0; int renderers = 0, lodRenderers = 0;
            var mats = new System.Collections.Generic.HashSet<Material>(); var tex = new System.Collections.Generic.HashSet<Texture>();
            foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var mf = mr.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                renderers++;
                long t = 0; for (int i = 0; i < mf.sharedMesh.subMeshCount; i++) t += mf.sharedMesh.GetIndexCount(i) / 3;
                tris += t; if (GameObjectUtility.AreStaticEditorFlagsSet(mr.gameObject, StaticEditorFlags.BatchingStatic)) triStatic += t;
                if (mr.GetComponentInParent<LODGroup>() != null) lodRenderers++;
                foreach (var m in mr.sharedMaterials) if (m != null) { mats.Add(m); foreach (var n in m.GetTexturePropertyNames()) { var tx = m.GetTexture(n); if (tx != null) tex.Add(tx); } }
            }
            sb.AppendFormat("renderers {0}, triangles (all LODs at full) {1}, in batching-static renderers {2}, renderers under LODGroups {3}\n", renderers, tris, triStatic, lodRenderers);
            sb.AppendFormat("distinct materials {0}, shaders {1}, textures {2}\n", mats.Count, mats.Select(m => m.shader).Distinct().Count(), tex.Count);
            long texBytes = 0;
            foreach (var x in tex.OfType<Texture2D>()) texBytes += (long)(x.width * x.height * 1.0 * 1.33);   // DXT5/BC7 ~ 1 byte per texel plus mips
            foreach (var x in tex.Where(t => !(t is Texture2D))) texBytes += 0;
            sb.AppendFormat("texture memory estimate (BC7/DXT5 + mips): {0:F1} MB\n", texBytes / 1048576.0);
            int lights = 0, rt = 0;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) { lights++; if (l.lightmapBakeType != LightmapBakeType.Baked) rt++; }
            sb.AppendFormat("lights {0}, not fully baked {1}; light probe groups {2}; reflection probes {3}\n", lights, rt, Object.FindObjectsByType<LightProbeGroup>(FindObjectsSortMode.None).Length, Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None).Length);
            sb.AppendFormat("lightmaps {0}\n", LightmapSettings.lightmaps.Length);
            foreach (var lm in LightmapSettings.lightmaps) if (lm.lightmapColor != null) sb.AppendFormat("  {0}x{1}\n", lm.lightmapColor.width, lm.lightmapColor.height);
            sb.AppendFormat("colliders {0}, rigidbodies {1}\n", Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Length, Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Length);
            Debug.Log(sb.ToString());
        }

        [MenuItem("Canteen Rush/World/Build Windows Player")]
        public static void BuildWindowsBatch()
        {
            PlayerSettings.enableFrameTimingStats = true;
            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../Builds/CanteenRush"));
            Directory.CreateDirectory(outDir);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(outDir, "CanteenRush.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("LightingBuilder: build " + report.summary.result + ", " + (report.summary.totalSize / 1048576.0).ToString("F0") + " MB, " + report.summary.totalTime.TotalSeconds.ToString("F0") + " s -> " + outDir);
            if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}

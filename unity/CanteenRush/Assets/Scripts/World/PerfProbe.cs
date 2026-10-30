using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace CanteenRush.World
{
    // Player-build measurement for docs/PERF_REPORT.md. Inert unless the player was started with -perfprobe.
    //   CanteenRush.exe -perfprobe -perfout <dir> [-perfquality <index>] [-perfdemo] -screen-width 1600 -screen-height 900 -screen-fullscreen 0
    // Visits Marker_Perf_Counter / _Door / _Patio, records frame times, counts what was actually drawn, saves a screenshot, quits.
    public class PerfProbe : MonoBehaviour
    {
        static readonly string[] Spots = { "counter", "door", "patio" };
        const int WarmFrames = 90, SampleFrames = 300;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-perfprobe") < 0) return;
            var go = new GameObject("PerfProbe");
            DontDestroyOnLoad(go);
            var probe = go.AddComponent<PerfProbe>();
            probe.StartCoroutine(probe.Run(args));
        }

        static string Arg(string[] args, string key, string fallback)
        {
            int i = Array.IndexOf(args, key);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
        }

        IEnumerator Run(string[] args)
        {
            string outDir = Arg(args, "-perfout", Application.persistentDataPath);
            Directory.CreateDirectory(outDir);
            int q; if (int.TryParse(Arg(args, "-perfquality", "-1"), out q) && q >= 0) QualitySettings.SetQualityLevel(q, true);
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            bool demo = Array.IndexOf(args, "-perfdemo") >= 0;

            yield return null;
            yield return new WaitForSeconds(1.5f);
            var cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            var sun = FindFirstObjectByType<SunCycle>();
            if (sun != null) sun.DemoMode = false;

            var sb = new StringBuilder();
            var inv = CultureInfo.InvariantCulture;
            sb.Append("{\n  \"gpu\": \"").Append(SystemInfo.graphicsDeviceName).Append("\", \"api\": \"").Append(SystemInfo.graphicsDeviceType)
              .Append("\", \"resolution\": \"").Append(Screen.width).Append("x").Append(Screen.height).Append("\", \"quality\": \"")
              .Append(QualitySettings.names[QualitySettings.GetQualityLevel()]).Append("\",\n  \"spots\": [\n");

            for (int s = 0; s < Spots.Length; s++)
            {
                var m = GameObject.Find("Marker_Perf_" + Spots[s]);
                if (m == null || cam == null) { sb.Append("    {\"spot\": \"").Append(Spots[s]).Append("\", \"error\": \"marker or camera missing\"}").Append(s < Spots.Length - 1 ? ",\n" : "\n"); continue; }
                cam.transform.SetPositionAndRotation(m.transform.position, m.transform.rotation);
                if (sun != null && demo) sun.SetProgress(s * 0.5f);
                for (int i = 0; i < WarmFrames; i++) yield return null;

                var ms = new List<float>(SampleFrames);
                var timings = new FrameTiming[1];
                double gpu = 0, cpu = 0; int gpuN = 0;
                for (int i = 0; i < SampleFrames; i++)
                {
                    FrameTimingManager.CaptureFrameTimings();
                    ms.Add(Time.unscaledDeltaTime * 1000f);
                    if (FrameTimingManager.GetLatestTimings(1, timings) > 0 && timings[0].gpuFrameTime > 0) { gpu += timings[0].gpuFrameTime; cpu += timings[0].cpuFrameTime; gpuN++; }
                    yield return null;
                }
                ms.Sort();
                float avg = 0; foreach (var v in ms) avg += v; avg /= ms.Count;

                long tris = 0; int renderers = 0; var mats = new HashSet<Material>(); var shaders = new HashSet<Shader>();
                foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                {
                    if (!r.enabled || !r.isVisible) continue;
                    var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                    renderers++;
                    var mats_ = r.sharedMaterials;
                    for (int k = 0; k < mf.sharedMesh.subMeshCount && k < mats_.Length; k++) tris += mf.sharedMesh.GetIndexCount(k) / 3;
                    foreach (var mat in mats_) if (mat != null) { mats.Add(mat); shaders.Add(mat.shader); }
                }

                ScreenCapture.CaptureScreenshot(Path.Combine(outDir, "shot_" + Spots[s] + ".png"));
                yield return null; yield return null;

                sb.Append("    {\"spot\": \"").Append(Spots[s]).Append("\", \"avg_ms\": ").Append(avg.ToString("F2", inv))
                  .Append(", \"p95_ms\": ").Append(ms[(int)(ms.Count * 0.95f)].ToString("F2", inv))
                  .Append(", \"max_ms\": ").Append(ms[ms.Count - 1].ToString("F2", inv))
                  .Append(", \"fps\": ").Append((1000f / avg).ToString("F0", inv))
                  .Append(", \"gpu_ms\": ").Append(gpuN > 0 ? (gpu / gpuN).ToString("F2", inv) : "null")
                  .Append(", \"cpu_ms\": ").Append(gpuN > 0 ? (cpu / gpuN).ToString("F2", inv) : "null")
                  .Append(", \"visible_tris\": ").Append(tris).Append(", \"visible_renderers\": ").Append(renderers)
                  .Append(", \"materials\": ").Append(mats.Count).Append(", \"shaders\": ").Append(shaders.Count).Append("}")
                  .Append(s < Spots.Length - 1 ? ",\n" : "\n");
            }
            sb.Append("  ]\n}\n");
            File.WriteAllText(Path.Combine(outDir, "perf_results.json"), sb.ToString());
            Application.Quit();
        }
    }
}

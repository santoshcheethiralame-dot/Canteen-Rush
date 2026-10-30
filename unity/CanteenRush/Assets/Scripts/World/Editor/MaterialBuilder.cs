using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CanteenRush.World.Editor
{
    // Turns the per-module Blender material dumps (Assets/Art/Materials/source/*.materials.json) into shared URP/Lit assets
    // and remaps every FBX onto them, so a colour change is a JSON edit plus one menu click and nobody edits .mat files by hand.
    // Batch: Unity.exe -batchmode -nographics -quit -projectPath <path> -executeMethod CanteenRush.World.Editor.MaterialBuilder.BuildBatch
    public static class MaterialBuilder
    {
        const string SourceDir = "Assets/Art/Materials/source";
        const string OutDir = "Assets/Art/Materials";

#pragma warning disable 0649   // filled by JsonUtility
        [System.Serializable] class MatDef { public string name; public float[] base_color; public float metallic; public float roughness; public float[] emission; public float emission_strength; public float alpha; }
        [System.Serializable] class MatList { public MatDef[] items; }
#pragma warning restore 0649

        // Blender nodes that were image textures and whose picture was supplied (everything else stays a flat colour).
        static readonly Dictionary<string, string> Textures = new Dictionary<string, string>
        {
            { "Env_Canteen/marble", "Assets/Art/Textures/T_Stone.jpg" },
        };

        [MenuItem("Canteen Rush/World/Build Materials")]
        public static void Build()
        {
            var made = new Dictionary<string, Material>();
            foreach (var jsonPath in Directory.GetFiles(SourceDir, "*.materials.json"))
            {
                string module = Path.GetFileName(jsonPath).Replace(".materials.json", "");
                var list = JsonUtility.FromJson<MatList>("{\"items\":" + File.ReadAllText(jsonPath) + "}");
                foreach (var d in list.items) made[module + "/" + d.name] = Make(module, d);
            }
            AssetDatabase.SaveAssets();

            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Art" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                if (imp == null) continue;
                string module = Path.GetFileNameWithoutExtension(path);
                bool changed = false;
                foreach (var kv in made)
                {
                    if (!kv.Key.StartsWith(module + "/")) continue;
                    var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key.Substring(module.Length + 1));
                    imp.AddRemap(id, kv.Value); changed = true;
                }
                if (changed) imp.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            Debug.Log("MaterialBuilder: " + made.Count + " materials built and remapped.");
        }

        public static void BuildBatch() { Build(); }

        static Material Make(string module, MatDef d)
        {
            string asset = OutDir + "/M_" + module + "__" + Regex.Replace(d.name, "[^A-Za-z0-9]+", "_") + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(asset);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, asset);
            }
            var c = new Color(d.base_color[0], d.base_color[1], d.base_color[2], 1f).gamma;   // Blender stores linear, Unity colour pickers are sRGB
            float alpha = d.alpha > 0f ? d.alpha : 1f;
            float smooth = 1f - Mathf.Clamp01(d.roughness);
            string key = module + "/" + d.name;

            // Spec §9: glass is opaque-tinted except the display case fronts; the canteen's two glass materials are those fronts.
            if (module == "Env_Canteen" && (d.name == "glass" || d.name == "glass.001")) { alpha = 0.25f; smooth = 0.95f; }
            if (d.name.ToLower() == "glass") smooth = Mathf.Max(smooth, 0.9f);

            // The sources painted these with image textures that were never supplied; give them the colour a viewer expects.
            string n = d.name.ToLower();
            if (n.StartsWith("leaves_texture")) { c = new Color(0.20f, 0.46f, 0.18f); smooth = 0.3f; d.metallic = 0f; }
            else if (n.StartsWith("pot_texture")) { c = new Color(0.62f, 0.30f, 0.20f); smooth = 0.35f; d.metallic = 0f; }
            else if (n.StartsWith("clay")) { c = new Color(0.60f, 0.34f, 0.24f); smooth = 0.2f; }
            else if (n == "blocks_brown") { c = new Color(0.55f, 0.38f, 0.28f); smooth = 0.2f; d.metallic = 0f; }
            else if (module == "Env_Terrace" && (n == "mrable" || n.StartsWith("material.0"))) { c = new Color(0.80f, 0.78f, 0.73f); smooth = 0.5f; }
            else if (module == "Env_PodsBackdrop" && n == "marble.001") { c = new Color(0.80f, 0.78f, 0.73f); smooth = 0.5f; }

            m.SetColor("_BaseColor", new Color(c.r, c.g, c.b, alpha));
            m.SetFloat("_Metallic", Mathf.Clamp01(d.metallic));
            m.SetFloat("_Smoothness", smooth);
            m.enableInstancing = true;

            if (Textures.TryGetValue(key, out string tex))
            {
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>(tex);
                if (t != null) { m.SetTexture("_BaseMap", t); m.SetColor("_BaseColor", new Color(0.75f, 0.75f, 0.75f, alpha)); }
            }

            bool emissive = (d.emission != null && d.emission.Length >= 3 && d.emission[0] + d.emission[1] + d.emission[2] > 0.01f) || key == "Env_Canteen/light";
            if (emissive)
            {
                Color e = key == "Env_Canteen/light" ? new Color(1f, 0.82f, 0.55f) * 1.6f : new Color(d.emission[0], d.emission[1], d.emission[2]).gamma * Mathf.Max(d.emission_strength, 1f);
                m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", e);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }
            else { m.DisableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.black); }

            SetSurface(m, alpha < 0.99f);
            EditorUtility.SetDirty(m);
            return m;
        }

        // What the URP Lit inspector does when "Surface Type" is switched, written out so it runs headless.
        internal static void SetSurface(Material m, bool transparent)
        {
            if (transparent)
            {
                m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                m.SetShaderPassEnabled("ShadowCaster", false);
            }
            else
            {
                m.SetFloat("_Surface", 0f);
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                m.SetFloat("_ZWrite", 1f);
                m.SetOverrideTag("RenderType", "Opaque");
                m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = -1;
                m.SetShaderPassEnabled("ShadowCaster", true);
            }
        }
    }
}

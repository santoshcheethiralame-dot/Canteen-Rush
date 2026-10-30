using UnityEngine;

namespace CanteenRush.World
{
    // Day to golden hour over one round. Drives only what is realtime (sun, ambient, fog, sky exposure); the baked GI is never redone.
    // Cr.World does not reference Cr.Core, so gameplay pushes the clock in:
    //   GameEvents.OnRoundTimeChanged += t => sunCycle.SetRemaining(t, config.RoundSeconds);
    // DemoMode runs its own clock for screenshots and the perf probe.
    public class SunCycle : MonoBehaviour
    {
        public Light Sun;
        public Material SkyTemplate;
        public float Azimuth = -30f;
        public float StartElevation = 55f, EndElevation = 12f;
        public float StartKelvin = 5600f, EndKelvin = 3800f;
        public Color SkyStart = new Color(0.45f, 0.62f, 0.90f), SkyEnd = new Color(0.62f, 0.42f, 0.38f);
        public Color EquatorStart = new Color(0.62f, 0.68f, 0.74f), EquatorEnd = new Color(0.55f, 0.40f, 0.30f);
        public Color GroundStart = new Color(0.30f, 0.30f, 0.28f), GroundEnd = new Color(0.20f, 0.15f, 0.12f);
        public Color FogStart = new Color(0.72f, 0.80f, 0.88f), FogEnd = new Color(0.78f, 0.58f, 0.46f);
        public float ExposureStart = 1.25f, ExposureEnd = 0.95f;
        public bool DemoMode;
        public float DemoSeconds = 180f;

        const float Interval = 0.1f;   // 10 Hz is plenty for a sun that crosses 43 degrees in three minutes
        const float Smoothing = 4f;
        float target, current = -1f, demoClock, nextApply;
        Material sky;

        public float Progress { get { return Mathf.Max(current, 0f); } }

        public void SetRemaining(float remaining, float roundSeconds)
        {
            target = roundSeconds > 0f ? 1f - Mathf.Clamp01(remaining / roundSeconds) : 0f;
        }

        public void SetProgress(float p) { target = Mathf.Clamp01(p); }

        // Editor and screenshot tooling: jump to a point in the round without waiting for it.
        public void Preview(float t) { target = current = Mathf.Clamp01(t); Apply(current); }

        void Awake()
        {
            if (Sun == null) Sun = RenderSettings.sun;
            if (SkyTemplate != null) { sky = new Material(SkyTemplate); RenderSettings.skybox = sky; }
            RenderSettings.sun = Sun;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 45f;
            RenderSettings.fogEndDistance = 150f;
            Apply(0f);
            current = 0f;
        }

        void OnDestroy() { if (sky != null) Destroy(sky); }

        void Update()
        {
            if (DemoMode) { demoClock += Time.deltaTime; target = Mathf.Clamp01(demoClock / DemoSeconds); }
            if (Time.unscaledTime < nextApply) return;
            nextApply = Time.unscaledTime + Interval;
            if (Mathf.Abs(current - target) < 1e-4f) return;
            current = Mathf.Lerp(current, target, 1f - Mathf.Exp(-Smoothing * Interval));
            Apply(current);
        }

        void Apply(float t)
        {
            if (Sun != null)
            {
                float k = Mathf.Lerp(StartKelvin, EndKelvin, t);
                Sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(StartElevation, EndElevation, t), Azimuth, 0f);
                Sun.useColorTemperature = true;
                Sun.colorTemperature = k;
                if (!UnityEngine.Rendering.GraphicsSettings.lightsUseColorTemperature) Sun.color = Mathf.CorrelatedColorTemperatureToRGB(k);
            }
            RenderSettings.ambientSkyColor = Color.Lerp(SkyStart, SkyEnd, t);
            RenderSettings.ambientEquatorColor = Color.Lerp(EquatorStart, EquatorEnd, t);
            RenderSettings.ambientGroundColor = Color.Lerp(GroundStart, GroundEnd, t);
            RenderSettings.fogColor = Color.Lerp(FogStart, FogEnd, t);
            if (sky != null && sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", Mathf.Lerp(ExposureStart, ExposureEnd, t));
        }
    }
}

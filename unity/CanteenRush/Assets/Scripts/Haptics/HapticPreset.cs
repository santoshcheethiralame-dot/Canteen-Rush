using UnityEngine;
namespace CanteenRush.Haptics
{
    /// One row of MASTER_SPEC §11. Contract fields: Name, Amplitude, Duration, IsRamp.
    /// Additive fields: Pulses/Gap (ServeOk is a double pulse), DebugColor (HapticsDebugHud),
    /// RampInput/RampAmplitude (how a ramp maps its driving value to strength).
    [CreateAssetMenu(menuName = "Canteen Rush/Haptic Preset")]
    public sealed class HapticPreset : ScriptableObject
    {
        public string Name;
        [Range(0f, 1f)] public float Amplitude = 0.25f;
        [Min(0f)] public float Duration = 0.04f;
        public bool IsRamp;
        [Min(1)] public int Pulses = 1;
        [Min(0f)] public float Gap;
        public Color DebugColor = Color.white;
        /// WaterFill: level 0..1 -> 0.10..0.45. TrayTiltWarn: 20..35 deg -> 0.15..0.70.
        public Vector2 RampInput = new Vector2(0f, 1f);
        public Vector2 RampAmplitude = new Vector2(0.1f, 0.45f);

        public float RampAmplitudeFor(float input) =>
            Mathf.Lerp(RampAmplitude.x, RampAmplitude.y, Mathf.InverseLerp(RampInput.x, RampInput.y, input));
    }
}

using UnityEngine;
namespace CanteenRush.Audio
{
    /// Custom logarithmic rolloff (MASTER_SPEC §10): full volume up to minDistance, then the inverse-distance
    /// law (-6 dB per doubling, like a real point source), with the last 15 % of the range faded to silence
    /// so a source is truly inaudible at maxDistance (Unity's built-in log curve never reaches zero).
    /// Unity evaluates a custom curve with x = distance / maxDistance.
    public static class AudioRolloff
    {
        const int Keys = 24;

        public static float Gain(float distance, float min, float max)
        {
            if (distance <= min) return 1f;
            if (distance >= max) return 0f;
            float g = min / distance;
            float fadeStart = max * 0.85f;
            if (distance > fadeStart) g *= 1f - (distance - fadeStart) / (max - fadeStart);
            return g;
        }

        public static AnimationCurve Build(float min, float max)
        {
            min = Mathf.Max(0.01f, min); max = Mathf.Max(min + 0.01f, max);
            var keys = new Keyframe[Keys];
            for (int i = 0; i < Keys; i++)
            {
                // Denser sampling near the listener where the curve bends most.
                float x = Mathf.Pow(i / (float)(Keys - 1), 2f);
                keys[i] = new Keyframe(x, Gain(x * max, min, max));
            }
            var curve = new AnimationCurve(keys);
            for (int i = 0; i < Keys; i++) curve.SmoothTangents(i, 0f);
            return curve;
        }
    }
}

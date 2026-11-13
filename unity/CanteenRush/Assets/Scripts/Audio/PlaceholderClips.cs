using System.Collections.Generic;
using UnityEngine;
namespace CanteenRush.Audio
{
    /// Last-resort clips generated in code, so the audio pipeline is audible even if a cue set has no clip
    /// assigned (e.g. a fresh clone before the WAVs import). Each cue ID gets its own recognisable sound:
    /// loops are filtered noise or a hum, one-shots are short tones/bursts with a pitch derived from the ID.
    /// The shipped clips in Assets/Audio/Clips come from tools/audio/generate_audio.py instead.
    public static class PlaceholderClips
    {
        const int Rate = 44100;
        static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        public static AudioClip For(string cueId)
        {
            if (string.IsNullOrEmpty(cueId)) return null;
            if (Cache.TryGetValue(cueId, out var clip) && clip != null) return clip;
            clip = Make(cueId);
            Cache[cueId] = clip;
            return clip;
        }

        static AudioClip Make(string id)
        {
            bool loop = id.StartsWith("amb_") || id == AudioCueIds.WaterFill || id == AudioCueIds.Music;
            float seconds = loop ? 4f : (id == AudioCueIds.RoundEnd ? 1.2f : 0.25f);
            int n = (int)(seconds * Rate);
            var data = new float[n];
            uint hash = 2166136261;
            foreach (char c in id) hash = (hash ^ c) * 16777619;
            float freq = 220f * Mathf.Pow(2f, (hash % 24) / 12f);
            var rng = new System.Random((int)hash);
            float lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float noise = (float)(rng.NextDouble() * 2 - 1);
                float s;
                if (id == AudioCueIds.FridgeHum) s = 0.4f * Mathf.Sin(2 * Mathf.PI * 100 * t) + 0.15f * Mathf.Sin(2 * Mathf.PI * 200 * t);
                else if (loop) { lp += 0.05f * (noise - lp); s = lp * 2f; }
                else
                {
                    float env = Mathf.Exp(-t * (id.StartsWith("footsteps") || id.Contains("clink") || id.Contains("latch") ? 40f : 10f));
                    s = env * (0.6f * Mathf.Sin(2 * Mathf.PI * freq * t) + 0.2f * noise);
                }
                // 5 ms fades so loops and one-shots never click.
                float edge = Mathf.Min(1f, Mathf.Min(i, n - 1 - i) / (0.005f * Rate));
                data[i] = s * 0.5f * edge;
            }
            var clip = AudioClip.Create("placeholder_" + id, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}

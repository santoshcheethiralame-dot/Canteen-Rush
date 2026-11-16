using UnityEngine;
using UnityEngine.Audio;
namespace CanteenRush.Audio
{
    /// One row of the MASTER_SPEC §10 cue table. Contract fields (contracts §5): CueId, Variants,
    /// PitchRange, VolumeRange, MinDistance, MaxDistance, Loop, Group.
    /// Additive fields: Spatial (false = 2D cue), Bus (group fallback by name when Group is unset),
    /// Occludable (AudioOcclusion runs on the playing source).
    [CreateAssetMenu(menuName = "Canteen Rush/Audio Cue Set")]
    public sealed class AudioCueSet : ScriptableObject
    {
        public string CueId;
        public AudioClip[] Variants = System.Array.Empty<AudioClip>();
        public Vector2 PitchRange = new Vector2(1f, 1f);
        public Vector2 VolumeRange = new Vector2(1f, 1f);
        public float MinDistance = 1f;
        public float MaxDistance = 10f;
        public bool Loop;
        public AudioMixerGroup Group;
        public bool Spatial = true;
        public AudioBus Bus = AudioBus.SFX;
        public bool Occludable;

        public bool HasClip
        {
            get { foreach (var v in Variants) if (v != null) return true; return false; }
        }

        public AudioClip Pick(int variant = -1)
        {
            if (Variants == null || Variants.Length == 0) return null;
            if (variant >= 0 && variant < Variants.Length && Variants[variant] != null) return Variants[variant];
            for (int tries = 0; tries < 4; tries++)
            {
                var c = Variants[Random.Range(0, Variants.Length)];
                if (c != null) return c;
            }
            foreach (var v in Variants) if (v != null) return v;
            return null;
        }
    }
}

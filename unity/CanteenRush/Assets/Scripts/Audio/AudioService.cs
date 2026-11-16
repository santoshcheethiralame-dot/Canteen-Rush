using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
namespace CanteenRush.Audio
{
    /// The audio manager (MASTER_SPEC §10):
    /// - cue library: AudioCueSet assets by ID;
    /// - 12 pooled one-shot AudioSources, the quietest one is stolen when all are busy;
    /// - dedicated looping sources for ambience, water fill and music (CreateLoop);
    /// - Indoor/Outdoor mixer snapshots with a 0.4 s cross-fade;
    /// - every 0.1 s, decides the zone from AudioZoneTrigger volumes around the listener.
    public sealed class AudioService : MonoBehaviour, IAudioService
    {
        public const int DefaultPoolSize = 12;
        public const float SnapshotFadeSeconds = 0.4f;

        [SerializeField] AudioCueSet[] _cues = Array.Empty<AudioCueSet>();
        [SerializeField] AudioMixer _mixer;
        [SerializeField] int _poolSize = DefaultPoolSize;
        [SerializeField] float _spread = 15f;   // degrees, spec: 0-30 for point sources

        public static AudioService Instance { get; private set; }
        /// Raised after the snapshot changes; indoor-only ambience fades on it.
        public static event Action<AudioZone> ZoneChanged;
        /// Raised for every cue that actually starts (tests, debug panel).
        public static event Action<string, Vector3> CuePlayed;

        readonly Dictionary<string, AudioCueSet> _byId = new Dictionary<string, AudioCueSet>(StringComparer.Ordinal);
        readonly Dictionary<AudioBus, AudioMixerGroup> _groups = new Dictionary<AudioBus, AudioMixerGroup>();
        readonly Dictionary<string, AnimationCurve> _curves = new Dictionary<string, AnimationCurve>(StringComparer.Ordinal);
        AudioSource[] _pool = Array.Empty<AudioSource>();
        AudioOcclusion[] _poolOcclusion = Array.Empty<AudioOcclusion>();
        float[] _poolGain = Array.Empty<float>();
        float[] _poolMin = Array.Empty<float>(), _poolMax = Array.Empty<float>();
        static AudioListener _listener;
        float _zoneTimer;
        bool _zoneKnown;

        public AudioZone CurrentZone { get; private set; } = AudioZone.Outdoor;
        public int PoolSize => _pool.Length;
        public AudioMixer Mixer => _mixer;
        public IEnumerable<AudioCueSet> Cues => _byId.Values;

        public static Vector3? ListenerPosition
        {
            get
            {
                if (_listener == null || !_listener.isActiveAndEnabled) _listener = FindFirstObjectByType<AudioListener>();
                return _listener != null ? _listener.transform.position : (Vector3?)null;
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Debug.LogWarning("[Audio] Second AudioService disabled."); enabled = false; return; }
            Instance = this;
            SetCues(_cues);
            ResolveGroups();
            BuildPool();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void SetCues(AudioCueSet[] cues)
        {
            _cues = cues ?? Array.Empty<AudioCueSet>();
            _byId.Clear();
            foreach (var c in _cues) if (c != null && !string.IsNullOrEmpty(c.CueId)) _byId[c.CueId] = c;
        }

        public void SetMixer(AudioMixer mixer) { _mixer = mixer; ResolveGroups(); }

        void ResolveGroups()
        {
            _groups.Clear();
            if (_mixer == null) return;
            foreach (AudioBus bus in Enum.GetValues(typeof(AudioBus)))
                foreach (var g in _mixer.FindMatchingGroups(bus.ToString()))
                    if (g.name == bus.ToString()) { _groups[bus] = g; break; }
        }

        void BuildPool()
        {
            _pool = new AudioSource[_poolSize];
            _poolOcclusion = new AudioOcclusion[_poolSize];
            _poolGain = new float[_poolSize]; _poolMin = new float[_poolSize]; _poolMax = new float[_poolSize];
            for (int i = 0; i < _poolSize; i++)
            {
                var go = new GameObject("OneShot_" + i.ToString("00"));
                go.transform.SetParent(transform, false);
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                _pool[i] = s;
                _poolOcclusion[i] = go.AddComponent<AudioOcclusion>();
                _poolOcclusion[i].enabled = false;
            }
        }

        public bool TryGetCue(string cueId, out AudioCueSet cue) => _byId.TryGetValue(cueId ?? string.Empty, out cue);

        /// The clip a cue would play (an assigned variant, or the generated placeholder). Used by tests.
        public AudioClip ResolveClip(string cueId, int variant = -1)
        {
            if (TryGetCue(cueId, out var cue)) { var c = cue.Pick(variant); if (c != null) return c; }
            return PlaceholderClips.For(cueId);
        }

        public AudioMixerGroup GroupFor(AudioCueSet cue)
        {
            if (cue != null && cue.Group != null) return cue.Group;
            return cue != null && _groups.TryGetValue(cue.Bus, out var g) ? g : null;
        }

        // ---------- IAudioService ----------
        public void PlayAt(string cueId, Vector3 worldPos, float volumeScale = 1f) => Play(cueId, worldPos, volumeScale, 1f, -1);

        public void PlayUi(string cueId) => Play(cueId, ListenerPosition ?? transform.position, 1f, 1f, -1, forceTwoD: true);

        public void SetSnapshot(AudioZone zone)
        {
            if (_zoneKnown && zone == CurrentZone) return;
            _zoneKnown = true;
            CurrentZone = zone;
            var snap = _mixer != null ? _mixer.FindSnapshot(zone.ToString()) : null;
            if (snap != null) snap.TransitionTo(SnapshotFadeSeconds);
            ZoneChanged?.Invoke(zone);
        }

        /// Full one-shot API: pitch multiplier (order bell per seat) and an explicit variant (fridge open/close click).
        public AudioSource Play(string cueId, Vector3 worldPos, float volumeScale, float pitchScale, int variant, bool forceTwoD = false)
        {
            TryGetCue(cueId, out var cue);
            var clip = cue != null ? cue.Pick(variant) : null;
            if (clip == null) clip = PlaceholderClips.For(cueId);
            if (clip == null || volumeScale <= 0f) return null;

            int i = PickVoice();
            var s = _pool[i];
            s.Stop();
            Configure(s, cue, cueId, forceTwoD);
            s.loop = false;
            s.clip = clip;
            s.transform.position = worldPos;
            float vol = Mathf.Clamp01(RandomIn(cue != null ? cue.VolumeRange : Vector2.one) * volumeScale);
            s.pitch = RandomIn(cue != null ? cue.PitchRange : Vector2.one) * pitchScale;
            bool occlude = cue != null && cue.Occludable && !forceTwoD && cue.Spatial;
            _poolOcclusion[i].BaseVolume = vol;
            _poolOcclusion[i].enabled = occlude;
            s.volume = vol;
            _poolGain[i] = vol;
            _poolMin[i] = s.minDistance; _poolMax[i] = s.maxDistance;
            s.Play();
            CuePlayed?.Invoke(cueId, worldPos);
            return s;
        }

        /// A dedicated looping source parented to `parent` (ambience emitters, water fill, music).
        public AudioSource CreateLoop(string cueId, Transform parent, bool addOcclusion)
        {
            TryGetCue(cueId, out var cue);
            var go = new GameObject("Loop_" + cueId);
            go.transform.SetParent(parent != null ? parent : transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            Configure(s, cue, cueId, false);
            s.loop = true;
            s.clip = cue != null ? cue.Pick() : null;
            if (s.clip == null) s.clip = PlaceholderClips.For(cueId);
            s.volume = cue != null ? cue.VolumeRange.y : 1f;
            if (addOcclusion && s.spatialBlend > 0) go.AddComponent<AudioOcclusion>();
            return s;
        }

        void Configure(AudioSource s, AudioCueSet cue, string cueId, bool forceTwoD)
        {
            bool spatial = !forceTwoD && (cue == null || cue.Spatial);
            s.outputAudioMixerGroup = GroupFor(cue);
            s.spatialBlend = spatial ? 1f : 0f;
            s.spatialize = spatial;          // routes through the spatializer plugin when one is set (HRTF)
            s.dopplerLevel = 0f;
            s.spread = spatial ? _spread : 0f;
            float min = cue != null ? cue.MinDistance : 1f, max = cue != null ? cue.MaxDistance : 10f;
            s.minDistance = min; s.maxDistance = max;
            s.rolloffMode = AudioRolloffMode.Custom;
            s.SetCustomCurve(AudioSourceCurveType.CustomRolloff, CurveFor(cueId, min, max));
            s.priority = cue != null && cue.Bus == AudioBus.UI ? 32 : 128;
        }

        AnimationCurve CurveFor(string cueId, float min, float max)
        {
            if (!_curves.TryGetValue(cueId ?? string.Empty, out var curve)) _curves[cueId ?? string.Empty] = curve = AudioRolloff.Build(min, max);
            return curve;
        }

        /// A free voice if there is one; otherwise steal the voice that is currently quietest at the listener.
        int PickVoice()
        {
            for (int i = 0; i < _pool.Length; i++) if (!_pool[i].isPlaying) return i;
            var listener = ListenerPosition ?? transform.position;
            int best = 0; float bestGain = float.MaxValue;
            for (int i = 0; i < _pool.Length; i++)
            {
                var s = _pool[i];
                float g = _poolGain[i] * (s.spatialBlend > 0 ? AudioRolloff.Gain(Vector3.Distance(listener, s.transform.position), _poolMin[i], _poolMax[i]) : 1f);
                if (g < bestGain) { bestGain = g; best = i; }
            }
            return best;
        }

        public int ActiveVoices { get { int n = 0; foreach (var s in _pool) if (s.isPlaying) n++; return n; } }

        static float RandomIn(Vector2 range) => range.x == range.y ? range.x : UnityEngine.Random.Range(range.x, range.y);

        void Update()
        {
            _zoneTimer -= Time.unscaledDeltaTime;
            if (_zoneTimer > 0) return;
            _zoneTimer = 0.1f;
            var p = ListenerPosition;
            if (p.HasValue && AudioZoneTrigger.TryEvaluate(p.Value, out var zone)) SetSnapshot(zone);
        }
    }
}

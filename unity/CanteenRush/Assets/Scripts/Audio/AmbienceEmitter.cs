using UnityEngine;
namespace CanteenRush.Audio
{
    /// A looping ambience bed (fridge hum, canteen room tone, rooftop wind/birds, patio crowd).
    /// Volume = cue volume x Level (set by the binder: crowd size, fridge door) x zone gate.
    /// IndoorOnly / OutdoorOnly emitters fade over 0.4 s (and pause when silent) as the snapshot changes.
    public sealed class AmbienceEmitter : MonoBehaviour
    {
        public enum Gate { Always, IndoorOnly, OutdoorOnly }
        [SerializeField] string _cueId;
        [SerializeField] Gate _gate = Gate.Always;
        [SerializeField] bool _randomStart = true;
        [SerializeField] float _level = 1f;
        AudioSource _source;
        AudioOcclusion _occlusion;
        float _cueVolume = 1f, _gate01 = 1f, _gateTarget = 1f;

        public string CueId => _cueId;
        public AudioSource Source => _source;
        public float Level { get => _level; set => _level = Mathf.Max(0f, value); }

        public void Configure(string cueId, Gate gate, float level) { _cueId = cueId; _gate = gate; _level = level; }

        void Start()
        {
            var audio = AudioService.Instance;
            if (audio == null) { Debug.LogWarning("[Audio] AmbienceEmitter " + _cueId + " has no AudioService."); enabled = false; return; }
            _source = audio.CreateLoop(_cueId, transform, true);
            _occlusion = _source.GetComponent<AudioOcclusion>();
            _cueVolume = _source.volume;
            if (_randomStart && _source.clip != null) _source.timeSamples = Random.Range(0, _source.clip.samples);
            _gate01 = _gateTarget = GateFor(audio.CurrentZone);
            Apply();
            if (_gate01 > 0) _source.Play();
        }

        void OnEnable() => AudioService.ZoneChanged += Zone;
        void OnDisable() => AudioService.ZoneChanged -= Zone;
        void Zone(AudioZone zone) => _gateTarget = GateFor(zone);

        float GateFor(AudioZone zone) =>
            _gate == Gate.Always ? 1f : (_gate == Gate.IndoorOnly) == (zone == AudioZone.Indoor) ? 1f : 0f;

        void Update()
        {
            if (_source == null) return;
            _gate01 = Mathf.MoveTowards(_gate01, _gateTarget, Time.deltaTime / AudioService.SnapshotFadeSeconds);
            Apply();
            bool audible = _gate01 > 0.001f && _level > 0.001f;
            if (audible && !_source.isPlaying) _source.UnPause();
            if (audible && !_source.isPlaying) _source.Play();
            else if (!audible && _source.isPlaying) _source.Pause();
        }

        void Apply()
        {
            float v = _cueVolume * _level * _gate01;
            if (_occlusion != null) _occlusion.BaseVolume = v; else _source.volume = v;
        }
    }
}

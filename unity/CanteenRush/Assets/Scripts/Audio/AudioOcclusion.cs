using UnityEngine;
namespace CanteenRush.Audio
{
    /// MASTER_SPEC §10 occlusion: every 0.1 s a linecast from the listener to this source against the
    /// Environment layer; while blocked, the low-pass cutoff eases 22000 -> 1200 Hz and volume to x0.7.
    /// Owners set BaseVolume instead of AudioSource.volume so the two never fight.
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioOcclusion : MonoBehaviour
    {
        public const float TickSeconds = 0.1f, OpenCutoff = 22000f, BlockedCutoff = 1200f, BlockedVolume = 0.7f;
        [SerializeField] LayerMask _mask;
        [SerializeField] float _easeSeconds = 0.25f;
        AudioSource _source;
        AudioLowPassFilter _filter;
        float _timer, _blocked01;
        bool _isBlocked;

        public float BaseVolume { get; set; } = 1f;
        public bool IsBlocked => _isBlocked;
        public float Blocked01 => _blocked01;

        void Awake()
        {
            _source = GetComponent<AudioSource>();
            _filter = GetComponent<AudioLowPassFilter>();
            if (_filter == null) _filter = gameObject.AddComponent<AudioLowPassFilter>();
            if (_mask.value == 0) _mask = LayerMask.GetMask("Environment");
            _filter.cutoffFrequency = OpenCutoff;
        }

        void OnEnable() { _timer = Random.value * TickSeconds; _blocked01 = 0; _isBlocked = false; Apply(); }

        void OnDisable()
        {
            if (_filter != null) _filter.cutoffFrequency = OpenCutoff;
            if (_source != null) _source.volume = BaseVolume;
        }

        void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0)
            {
                _timer += TickSeconds;
                var listener = AudioService.ListenerPosition;
                _isBlocked = listener.HasValue && Physics.Linecast(listener.Value, transform.position, _mask, QueryTriggerInteraction.Ignore);
            }
            _blocked01 = Mathf.MoveTowards(_blocked01, _isBlocked ? 1f : 0f, Time.deltaTime / Mathf.Max(0.01f, _easeSeconds));
            Apply();
        }

        void Apply()
        {
            // Interpolate the cutoff in log space: perceived brightness is logarithmic in frequency.
            _filter.cutoffFrequency = Mathf.Exp(Mathf.Lerp(Mathf.Log(OpenCutoff), Mathf.Log(BlockedCutoff), _blocked01));
            _source.volume = BaseVolume * Mathf.Lerp(1f, BlockedVolume, _blocked01);
        }
    }
}

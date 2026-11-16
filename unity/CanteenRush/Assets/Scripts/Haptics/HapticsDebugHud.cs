using CanteenRush.Core;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CanteenRush.Haptics
{
    /// Makes haptics visible where hardware cannot vibrate (XR Device Simulator, viva laptop).
    /// - A world-space marker above each controller flashes: colour = preset, size = amplitude, fade = duration.
    /// - The same flash is mirrored in the screen corner for that hand (always visible in the Game view).
    /// - A scrolling 8-line log of the last pulses.
    /// Toggle with F9 (or the Visible property). On by default; switches itself off once a real
    /// controller with haptics is detected, unless the user toggled it explicitly.
    public sealed class HapticsDebugHud : MonoBehaviour
    {
        const int LogLines = 8;
        [SerializeField] HapticsServiceBase _service;
        [SerializeField] Key _toggleKey = Key.F9;
        [SerializeField] float _markerHeight = 0.12f;
        [SerializeField] float _maxMarkerSize = 0.06f;
        [SerializeField] float _minFlashSeconds = 0.15f;   // a 20 ms pulse would be invisible otherwise

        readonly string[] _log = new string[LogLines];
        int _logHead, _logCount;
        readonly Flash[] _flash = new Flash[2];
        readonly Transform[] _markers = new Transform[2];
        readonly Renderer[] _markerRenderers = new Renderer[2];
        MaterialPropertyBlock _block;
        bool _userToggled, _visible = true;
        GUIStyle _style;
        Texture2D _white;

        struct Flash { public float Start, Length, Amplitude; public Color Color; public string Label; }

        public bool Visible { get => _visible; set { _visible = value; _userToggled = true; ApplyVisibility(); } }
        public int LogCount => _logCount;
        public string LastLine => _logCount == 0 ? null : _log[(_logHead + LogLines - 1) % LogLines];

        void Awake()
        {
            if (_service == null) _service = GetComponent<HapticsServiceBase>();
            if (_service == null) _service = FindFirstObjectByType<HapticsServiceBase>();
            _block = new MaterialPropertyBlock();
        }

        void OnEnable() => HapticsServiceBase.PulseSent += OnPulse;
        void OnDisable() => HapticsServiceBase.PulseSent -= OnPulse;

        void OnPulse(HapticPulse p)
        {
            int h = (int)p.Hand;
            _flash[h] = new Flash
            {
                Start = Time.unscaledTime, Length = Mathf.Max(p.Duration, _minFlashSeconds),
                Amplitude = p.Amplitude, Color = p.Color, Label = p.Preset ?? "pulse"
            };
            if (p.IsRamp && _logCount > 0 && LastLine != null && LastLine.StartsWith(RampPrefix(p))) return;   // ramps: one line, not 20/s
            _log[_logHead] = string.Format("{0}{1:0.00} amp {2:0.000}s{3}", RampPrefix(p), p.Amplitude, p.Duration, p.Delivered ? "" : "  (HUD only)");
            _logHead = (_logHead + 1) % LogLines;
            _logCount = Mathf.Min(_logCount + 1, LogLines);
            if (_visible) Debug.Log("[Haptics] " + LastLine);
        }

        static string RampPrefix(HapticPulse p) =>
            string.Format("{0,-5} {1,-13}{2} ", p.Hand == Hand.Left ? "L" : "R", p.Preset ?? "pulse", p.IsRamp ? " ~ramp" : "");

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb[_toggleKey].wasPressedThisFrame) Visible = !_visible;
            if (!_userToggled && _service != null && _service.HasHapticDevice && _visible) { _visible = false; ApplyVisibility(); }
            UpdateMarkers();
        }

        void ApplyVisibility()
        {
            for (int i = 0; i < 2; i++) if (_markers[i] != null) _markers[i].gameObject.SetActive(_visible);
        }

        [SerializeField] Transform _fallbackLeft, _fallbackRight;
        /// Sandbox/simulator without HapticImpulsePlayers: flash above these instead.
        public void SetFallbackAnchors(Transform left, Transform right) { _fallbackLeft = left; _fallbackRight = right; }

        Transform AnchorFor(Hand hand)
        {
            if (_service is XRHapticsService xr) { var a = xr.Anchor(hand); if (a != null) return a; }
            return hand == Hand.Left ? _fallbackLeft : _fallbackRight;
        }

        void UpdateMarkers()
        {
            if (!_visible) return;
            for (int h = 0; h < 2; h++)
            {
                var anchor = AnchorFor((Hand)h);
                if (anchor == null) continue;
                if (_markers[h] == null) CreateMarker(h);
                float k = Intensity(h);
                _markers[h].position = anchor.position + Vector3.up * _markerHeight;
                _markers[h].localScale = Vector3.one * Mathf.Lerp(0.01f, _maxMarkerSize, Mathf.Max(0.15f, _flash[h].Amplitude) * k + 0.15f * (1 - k));
                var c = Color.Lerp(new Color(0.3f, 0.3f, 0.3f, 0.6f), _flash[h].Color, k);
                _block.SetColor("_BaseColor", c); _block.SetColor("_Color", c);
                _markerRenderers[h].SetPropertyBlock(_block);
            }
        }

        void CreateMarker(int h)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "HapticMarker_" + (Hand)h;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            var r = go.GetComponent<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null) r.sharedMaterial = new Material(shader);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            _markers[h] = go.transform; _markerRenderers[h] = r;
        }

        float Intensity(int h)
        {
            var f = _flash[h];
            if (f.Length <= 0) return 0;
            return Mathf.Clamp01(1f - (Time.unscaledTime - f.Start) / f.Length);
        }

        void OnGUI()
        {
            if (!_visible) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(14, Screen.height / 50) };
                _style.normal.textColor = Color.white;
                _white = Texture2D.whiteTexture;
            }
            float line = _style.fontSize + 4, w = _style.fontSize * 26, x = 10, y = Screen.height - line * (LogLines + 2) - 10;
            GUI.color = new Color(0, 0, 0, 0.55f);
            GUI.DrawTexture(new Rect(x - 4, y - 4, w, line * (LogLines + 1) + 8), _white);
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y, w, line), "HAPTICS (F9)  L/R = hand, amplitude, duration", _style);
            for (int i = 0; i < _logCount; i++)
                GUI.Label(new Rect(x, y + line * (i + 1), w, line), _log[(_logHead + LogLines - _logCount + i) % LogLines], _style);
            // Corner flashes: left hand bottom-left above the log, right hand bottom-right.
            for (int h = 0; h < 2; h++)
            {
                float k = Intensity(h);
                if (k <= 0) continue;
                float size = Mathf.Lerp(16, 90, _flash[h].Amplitude) * (0.5f + 0.5f * k);
                float cx = h == 0 ? 60 : Screen.width - 60, cy = y - 60;
                var c = _flash[h].Color; c.a = k;
                GUI.color = c;
                GUI.DrawTexture(new Rect(cx - size / 2, cy - size / 2, size, size), _white);
                GUI.color = Color.white;
                GUI.Label(new Rect(h == 0 ? cx - 40 : cx - 120, cy + 48, 160, line), (h == 0 ? "L " : "R ") + _flash[h].Label, _style);
            }
        }
    }
}

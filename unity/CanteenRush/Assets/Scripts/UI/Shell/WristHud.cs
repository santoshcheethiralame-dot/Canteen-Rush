using CanteenRush.Core;
using UnityEngine;
using UnityEngine.UI;
namespace CanteenRush.UI
{
    /// Wrist HUD (MASTER_SPEC §12): round time, score, wave. In the simulator it rides on the left controller
    /// and is always visible during a round (no palm-up gesture without hand tracking). If no left controller is
    /// found (e.g. a plain camera), it hangs at the lower-left of the view instead.
    public sealed class WristHud : MonoBehaviour
    {
        static readonly string[] LeftNames = { "Left Controller", "LeftHand Controller", "Left Hand", "Left_NearFarInteractor" };
        [SerializeField] Vector3 _wristOffset = new Vector3(0f, 0.05f, -0.05f);
        [SerializeField] Vector3 _wristEuler = new Vector3(50f, 0f, 0f);
        [SerializeField] Vector3 _viewOffset = new Vector3(-0.22f, -0.22f, 0.6f);
        Canvas _canvas;
        Text _time, _score, _wave;
        Transform _anchor;
        bool _onWrist;
        float _nextSearch;
        RoundState _state = RoundState.Boot;
        float _remaining;
        int _scoreValue;

        public string TimeText => _time.text;
        public string ScoreText => _score.text;
        public string WaveText => _wave.text;
        public bool IsShown => _canvas.gameObject.activeSelf;
        public bool OnWrist => _onWrist;

        void Awake()
        {
            _canvas = UiFactory.Canvas(transform, "WristHUD_Canvas", new Vector2(220, 130));
            _canvas.transform.localScale = Vector3.one * 0.0006f;   // small on the wrist; text still >= 24 units
            var root = (RectTransform)_canvas.transform;
            _time = UiFactory.Label(root, "Time", "3:00", 56, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.52f), new Vector2(0.6f, 0.98f), UiFactory.Accent);
            _wave = UiFactory.Label(root, "Wave", "", 32, TextAnchor.MiddleRight, new Vector2(0.55f, 0.52f), new Vector2(0.95f, 0.98f));
            _score = UiFactory.Label(root, "Score", "0", 44, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.02f), new Vector2(0.95f, 0.5f));
            _canvas.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            GameEvents.OnRoundStateChanged += RoundChanged;
            GameEvents.OnRoundTimeChanged += TimeChanged;
            GameEvents.OnScoreChanged += ScoreChanged;
        }

        void OnDisable()
        {
            GameEvents.OnRoundStateChanged -= RoundChanged;
            GameEvents.OnRoundTimeChanged -= TimeChanged;
            GameEvents.OnScoreChanged -= ScoreChanged;
        }

        void RoundChanged(RoundState s)
        {
            _state = s;
            _canvas.gameObject.SetActive(s == RoundState.Tutorial || s == RoundState.Wave1 || s == RoundState.Wave2 || s == RoundState.Wave3);
            _wave.text = s == RoundState.Tutorial ? "PRACTICE" : s == RoundState.Wave1 ? "WAVE 1/3" : s == RoundState.Wave2 ? "WAVE 2/3" : s == RoundState.Wave3 ? "WAVE 3/3" : "";
            RefreshTime();
        }

        void TimeChanged(float remaining) { _remaining = Mathf.Max(0f, remaining); RefreshTime(); }
        void ScoreChanged(int score) { _scoreValue = score; _score.text = score + " pts"; }

        void RefreshTime()
        {
            int s = Mathf.CeilToInt(_remaining);
            _time.text = _state == RoundState.Tutorial ? "--:--" : (s / 60) + ":" + (s % 60).ToString("00");
            _time.color = _state != RoundState.Tutorial && _remaining <= 15f ? new Color(1f, 0.35f, 0.3f) : UiFactory.Accent;
        }

        void LateUpdate()
        {
            if (!_canvas.gameObject.activeSelf) return;
            if (!_onWrist && Time.unscaledTime >= _nextSearch) { _nextSearch = Time.unscaledTime + 1f; FindAnchor(); }
            var t = _canvas.transform;
            if (_onWrist && _anchor != null)
            {
                t.SetPositionAndRotation(_anchor.TransformPoint(_wristOffset), _anchor.rotation * Quaternion.Euler(_wristEuler));
                return;
            }
            var cam = Camera.main;
            if (cam == null) return;
            t.SetPositionAndRotation(cam.transform.TransformPoint(_viewOffset), cam.transform.rotation);
        }

        void FindAnchor()
        {
            foreach (var n in LeftNames)
            {
                var go = GameObject.Find(n);
                if (go != null && go.activeInHierarchy) { _anchor = go.transform; _onWrist = true; return; }
            }
        }
    }
}

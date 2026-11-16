using CanteenRush.Core;
using UnityEngine;
using UnityEngine.UI;
namespace CanteenRush.UI
{
    /// Tutorial hints (MASTER_SPEC §6.5, §12): three steps, advanced by GameEvents.OnTutorialStep (raised by P2's
    /// GameManager): 0 grab a donut, 1 put it on the tray, 2 serve the customer, 3 finished.
    /// The next target gets a pulsing ring around it and a floating instruction card above it.
    /// Targets are found by name/tag only (no gameplay reference): "Item_Donut*", tag "Tray", and the seat of the
    /// practice order (OrderPlaced while in Tutorial).
    public sealed class TutorialHint : MonoBehaviour
    {
        public static readonly string[] Steps =
        {
            "1 / 3   Grab a donut from the display case",
            "2 / 3   Put the donut on a tray",
            "3 / 3   Carry the tray to the customer and set it on the table",
            "Great! The round starts now"
        };
        [SerializeField] float _pulseHz = 1.5f;
        Canvas _card;
        Text _text;
        Transform _ring;
        Renderer _ringRenderer;
        MaterialPropertyBlock _block;
        int _step = -1;
        Vector3? _target;
        Vector3 _seat;
        bool _haveSeat;
        float _doneUntil, _nextRetarget;

        public int Step => _step;
        public bool IsShown => _card.gameObject.activeSelf;
        public string HintText => _text.text;
        public Vector3? Target => _target;

        void Awake()
        {
            _card = UiFactory.Canvas(transform, "TutorialHint_Canvas", new Vector2(760, 170));
            _text = UiFactory.Label(_card.transform, "Step", "", UiFactory.BodySize, TextAnchor.MiddleCenter, new Vector2(0.03f, 0), new Vector2(0.97f, 1), UiFactory.Ink);
            _card.gameObject.SetActive(false);
            _block = new MaterialPropertyBlock();
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "TutorialPulse";
            Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(transform, false);
            _ringRenderer = ring.GetComponent<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null) _ringRenderer.sharedMaterial = new Material(shader);
            _ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ring = ring.transform;
            ring.SetActive(false);
        }

        void OnEnable()
        {
            GameEvents.OnTutorialStep += StepChanged;
            GameEvents.OnRoundStateChanged += RoundChanged;
            GameEvents.OnOrderPlaced += OrderPlaced;
        }

        void OnDisable()
        {
            GameEvents.OnTutorialStep -= StepChanged;
            GameEvents.OnRoundStateChanged -= RoundChanged;
            GameEvents.OnOrderPlaced -= OrderPlaced;
        }

        RoundState _state;
        void RoundChanged(RoundState s)
        {
            _state = s;
            if (s != RoundState.Tutorial && Time.unscaledTime > _doneUntil) Hide();
        }

        void OrderPlaced(Order o) { if (_state == RoundState.Tutorial && o != null) { _seat = o.SeatPosition; _haveSeat = true; } }

        public void StepChanged(int step)
        {
            _step = step;
            if (step < 0) { Hide(); return; }
            _text.text = Steps[Mathf.Clamp(step, 0, Steps.Length - 1)];
            _card.gameObject.SetActive(true);
            if (step >= 3) { _doneUntil = Time.unscaledTime + 2.5f; _target = null; _ring.gameObject.SetActive(false); PlaceCard(null); return; }
            Retarget();
        }

        void Hide()
        {
            _step = -1; _target = null; _haveSeat = false;
            _card.gameObject.SetActive(false);
            _ring.gameObject.SetActive(false);
        }

        void Retarget()
        {
            _target = FindTarget(_step);
            _ring.gameObject.SetActive(_target.HasValue);
            PlaceCard(_target);
        }

        Vector3? FindTarget(int step)
        {
            var cam = Camera.main;
            var from = cam != null ? cam.transform.position : transform.position;
            switch (step)
            {
                case 0: return Nearest(from, null, "Item_Donut");
                case 1: return Nearest(from, "Tray", null);
                case 2: return _haveSeat ? _seat : Nearest(from, "ServeSocket", null);
                default: return null;
            }
        }

        static Vector3? Nearest(Vector3 from, string tag, string namePrefix)
        {
            Vector3? best = null; float bestD = float.MaxValue;
            if (tag != null)
            {
                GameObject[] tagged;
                try { tagged = GameObject.FindGameObjectsWithTag(tag); } catch (UnityException) { tagged = System.Array.Empty<GameObject>(); }
                foreach (var go in tagged) Consider(go.transform.position, from, ref best, ref bestD);
            }
            if (namePrefix != null)
                foreach (var r in Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
                    if (r.gameObject.activeInHierarchy && r.name.StartsWith(namePrefix)) Consider(r.position, from, ref best, ref bestD);
            return best;
        }

        static void Consider(Vector3 p, Vector3 from, ref Vector3? best, ref float bestD)
        {
            float d = (p - from).sqrMagnitude;
            if (d < bestD) { bestD = d; best = p; }
        }

        void PlaceCard(Vector3? target)
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 pos;
            if (target.HasValue && (target.Value - cam.transform.position).sqrMagnitude < 4f * 4f)
                pos = target.Value + Vector3.up * 0.45f;
            else
            {
                var fwd = cam.transform.forward; fwd.y = 0; if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
                pos = cam.transform.position + fwd.normalized * 1.3f + Vector3.up * 0.25f;
            }
            var look = pos - cam.transform.position; look.y = 0;
            _card.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(look.sqrMagnitude > 1e-4f ? look : Vector3.forward));
            _card.transform.localScale = Vector3.one * UiFactory.CanvasScale * 0.8f;
        }

        void Update()
        {
            if (_step >= 3 && Time.unscaledTime > _doneUntil && _card.gameObject.activeSelf) Hide();
            if (_step < 0 || _step >= 3) return;
            // Items get picked up and respawn; follow the nearest target twice a second (no per-frame searches).
            if (Time.unscaledTime >= _nextRetarget) { _nextRetarget = Time.unscaledTime + 0.5f; Retarget(); }
            if (!_target.HasValue) return;
            float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * _pulseHz * 2f * Mathf.PI);
            float d = Mathf.Lerp(0.35f, 0.5f, k);
            _ring.position = _target.Value + Vector3.up * 0.02f;
            _ring.localScale = new Vector3(d, 0.005f, d);
            var c = Color.Lerp(new Color(1f, 0.72f, 0.18f, 0.4f), new Color(1f, 0.95f, 0.5f, 1f), k);
            _block.SetColor("_BaseColor", c); _block.SetColor("_Color", c);
            _ringRenderer.SetPropertyBlock(_block);
        }
    }
}

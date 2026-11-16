using System.Collections.Generic;
using CanteenRush.Core;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CanteenRush.Gameplay.Desktop
{
    /// Desktop-mode HUD: round bar (phase, clock, score), order tickets with patience bars, crosshair and context
    /// prompt, score toasts, and full-screen title / results / pause cards. IMGUI with generated rounded panels,
    /// so it needs no prefab, font or scene edits.
    [RequireComponent(typeof(DesktopController))]
    public sealed class DesktopHud : MonoBehaviour
    {
        static readonly (string key, string what)[] Keys =
        {
            ("W A S D", "walk   (Shift: sprint)"), ("Mouse", "look"), ("Click / E", "grab, drop, open fridge"),
            ("F", "hold to pump water"), ("Right mouse", "hold + drag to tilt a tray"), ("Click customer", "serve the tray you carry"),
            ("Enter", "start / play again"), ("Esc", "pause"), ("F1", "show / hide controls"), ("F9", "haptics HUD"), ("F5", "VR simulator")
        };

        static readonly Color Ink = new Color(.07f, .08f, .11f, .82f), InkSoft = new Color(.07f, .08f, .11f, .6f),
            Paper = new Color(.97f, .95f, .9f), Muted = new Color(.72f, .74f, .8f),
            Accent = new Color(1f, .62f, .26f), Good = new Color(.3f, .85f, .6f), Bad = new Color(1f, .36f, .36f), Warn = new Color(1f, .8f, .3f);

        DesktopController _c;
        Texture2D _round, _pill, _white;
        GUIStyle _box, _pillStyle, _text, _textBold, _small, _big, _huge, _key;
        bool _guide = true, _guideTouched;
        int _score, _served, _perfect, _expired, _lastScore;
        readonly List<(string text, Color color, float born)> _toasts = new List<(string, Color, float)>();

        void Awake() { _c = GetComponent<DesktopController>(); }
        void OnEnable()
        {
            GameEvents.OnScoreChanged += Score; GameEvents.OnOrderServed += Served; GameEvents.OnOrderExpired += Expired;
            GameEvents.OnRoundStateChanged += State; GameEvents.OnItemGrabbed += Grabbed;
        }
        void OnDisable()
        {
            GameEvents.OnScoreChanged -= Score; GameEvents.OnOrderServed -= Served; GameEvents.OnOrderExpired -= Expired;
            GameEvents.OnRoundStateChanged -= State; GameEvents.OnItemGrabbed -= Grabbed;
        }
        void Score(int total) { _score = total; }
        void Served(ServeResult r)
        {
            _served++; if (r.Outcome == ServeOutcome.Perfect) _perfect++;
            Toast((r.Points >= 0 ? "+" : "") + r.Points + (r.Outcome == ServeOutcome.Perfect ? "   PERFECT!" : r.Wrong > 0 || r.Missing > 0 ? "   not quite" : ""),
                r.Outcome == ServeOutcome.Perfect ? Good : r.Points > 0 ? Warn : Bad);
        }
        void Expired(Order o) { _expired++; Toast("Table " + (o.SeatIndex + 1) + " gave up", Bad); }
        void State(RoundState s)
        {
            if (s == RoundState.Tutorial || s == RoundState.Wave1) { if (s == RoundState.Tutorial || _lastState != RoundState.Tutorial) { _served = _perfect = _expired = 0; } }
            if (s == RoundState.Wave1) Toast("Round on!  3 minutes", Accent);
            if (s == RoundState.Wave2) Toast("Wave 2: busier", Accent);
            if (s == RoundState.Wave3) Toast("Final wave!", Accent);
            _lastState = s;
        }
        RoundState _lastState;
        void Grabbed(ItemId id, Hand h, Vector3 p) { if (!_guideTouched) _guide = false; }
        void Toast(string text, Color color) { _toasts.Add((text, color, Time.unscaledTime)); if (_toasts.Count > 4) _toasts.RemoveAt(0); }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame) { _guide = !_guide; _guideTouched = true; }
        }

        // ---------- drawing helpers ----------
        static Texture2D RoundTex(int size, float radius)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Max(radius - x - .5f, x + .5f - (size - radius)));
                    float dy = Mathf.Max(0, Mathf.Max(radius - y - .5f, y + .5f - (size - radius)));
                    float a = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + .5f);
                    t.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            t.Apply(); return t;
        }

        void Styles()
        {
            if (_box != null) return;
            _round = RoundTex(32, 12); _pill = RoundTex(32, 15.5f); _white = Texture2D.whiteTexture;
            _box = new GUIStyle { normal = { background = _round }, border = new RectOffset(13, 13, 13, 13) };
            _pillStyle = new GUIStyle { normal = { background = _pill }, border = new RectOffset(15, 15, 15, 15) };
            _text = new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.MiddleLeft, normal = { textColor = Paper }, wordWrap = false, clipping = TextClipping.Overflow };
            _textBold = new GUIStyle(_text) { fontStyle = FontStyle.Bold };
            _small = new GUIStyle(_text) { fontSize = 13, normal = { textColor = Muted } };
            _big = new GUIStyle(_textBold) { fontSize = 30, alignment = TextAnchor.MiddleCenter };
            _huge = new GUIStyle(_textBold) { fontSize = 64, alignment = TextAnchor.MiddleCenter };
            _key = new GUIStyle(_textBold) { fontSize = 14, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(.1f, .1f, .12f) } };
        }

        void Panel(Rect r, Color c, GUIStyle style = null)
        {
            if (Event.current.type != EventType.Repaint) return;
            var old = GUI.color; GUI.color = c; (style ?? _box).Draw(r, false, false, false, false); GUI.color = old;
        }
        void Fill(Rect r, Color c) { var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, _white); GUI.color = old; }
        void Label(Rect r, string s, GUIStyle st, Color c) { var old = st.normal.textColor; st.normal.textColor = c; GUI.Label(r, s, st); st.normal.textColor = old; }
        float KeyCap(float x, float y, string k)
        {
            float w = Mathf.Max(26, _key.CalcSize(new GUIContent(k)).x + 14);
            Panel(new Rect(x, y, w, 26), Paper, _pillStyle); GUI.Label(new Rect(x, y, w, 26), k, _key);
            return w;
        }

        // ---------- layout ----------
        void OnGUI()
        {
            if (!DesktopController.Active || !_c.Ready) return;
            Styles();
            float scale = Mathf.Clamp(Screen.height / 900f, .75f, 1.8f);
            var keep = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float w = Screen.width / scale, h = Screen.height / scale;
            var state = _c.Game != null ? _c.Game.State : RoundState.Boot;

            if (state == RoundState.Menu) { TitleCard(w, h); GUI.matrix = keep; return; }
            if (state == RoundState.Results) { ResultsCard(w, h); GUI.matrix = keep; return; }

            RoundBar(w, state);
            Tickets(w);
            Crosshair(w, h);
            Toasts(w, h);
            if (_guide) Guide(h); else HintGuide(h);
            if (_c.Paused) PauseCard(w, h);
            GUI.matrix = keep;
        }

        void RoundBar(float w, RoundState state)
        {
            string phase = state == RoundState.Tutorial ? "PRACTICE" : state == RoundState.Wave1 ? "WAVE 1 / 3" : state == RoundState.Wave2 ? "WAVE 2 / 3" : state == RoundState.Wave3 ? "WAVE 3 / 3" : "";
            var r = new Rect(w / 2 - 230, 14, 460, 62);
            Panel(r, Ink);
            Label(new Rect(r.x + 20, r.y + 6, 160, 24), phase, _small, Accent);
            Label(new Rect(r.x + 20, r.y + 26, 160, 30), state == RoundState.Tutorial ? "learn the ropes" : "keep them fed", _text, Muted);
            float t = _c.Game != null ? _c.Game.Remaining : 0;
            bool urgent = state != RoundState.Tutorial && t < 20f;
            string clock = state == RoundState.Tutorial ? "--:--" : $"{(int)t / 60}:{(int)t % 60:00}";
            Label(new Rect(r.x + 160, r.y, 140, 62), clock, _big, urgent && (Time.unscaledTime % 1f) < .5f ? Bad : Paper);
            Label(new Rect(r.x + 300, r.y + 6, 140, 24), "SCORE", new GUIStyle(_small) { alignment = TextAnchor.MiddleRight }, Muted);
            Label(new Rect(r.x + 300, r.y + 26, 140, 30), _score.ToString("N0"), new GUIStyle(_textBold) { fontSize = 24, alignment = TextAnchor.MiddleRight }, Paper);
        }

        void Tickets(float w)
        {
            var orders = _c.Game != null && _c.Game.Orders != null ? _c.Game.Orders.Active : null;
            if (orders == null) return;
            float x = w - 290, y = 14;
            Label(new Rect(x, y, 276, 22), orders.Count == 0 ? "NO ORDERS YET" : "ORDERS", _small, Muted); y += 24;
            var looked = _c.LookedAtCustomer;
            foreach (var o in orders)
            {
                float p = o.Patience01;
                Color bar = p > .5f ? Good : p > .25f ? Warn : Bad;
                bool focus = looked != null && looked.SeatIndex == o.SeatIndex;
                var r = new Rect(x, y, 276, 64);
                Panel(r, focus ? new Color(.16f, .14f, .1f, .9f) : Ink);
                if (focus) Panel(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), new Color(Accent.r, Accent.g, Accent.b, .5f));
                Label(new Rect(r.x + 14, r.y + 6, 120, 22), "TABLE " + (o.SeatIndex + 1), _small, focus ? Accent : Muted);
                Label(new Rect(r.x + 14, r.y + 24, 250, 24), _c.OrderText(o.SeatIndex, "?"), _textBold, Paper);
                Fill(new Rect(r.x + 14, r.y + 52, 248, 5), new Color(1, 1, 1, .12f));
                Fill(new Rect(r.x + 14, r.y + 52, 248 * Mathf.Clamp01(p), 5), bar);
                y += 72;
            }
        }

        void Crosshair(float w, float h)
        {
            var f = _c.Focus;
            Color c = f == DesktopController.Kind.None ? new Color(1, 1, 1, .9f) : f == DesktopController.Kind.Drop ? Accent : Good;
            float s = f == DesktopController.Kind.None ? 6 : 14;
            Panel(new Rect(w / 2 - s / 2 - 2, h / 2 - s / 2 - 2, s + 4, s + 4), new Color(0, 0, 0, .5f), _pillStyle);
            Panel(new Rect(w / 2 - s / 2, h / 2 - s / 2, s, s), c, _pillStyle);
            if (string.IsNullOrEmpty(_c.FocusLabel)) return;
            bool action = f != DesktopController.Kind.None;
            string key = f == DesktopController.Kind.Pump ? "F" : "E";
            float tw = _textBold.CalcSize(new GUIContent(_c.FocusLabel)).x;
            float width = tw + (action ? 62 : 32);
            var r = new Rect(w / 2 - width / 2, h / 2 + 30, width, 38);
            Panel(r, InkSoft, _pillStyle);
            float x = r.x + 14;
            if (action) x += KeyCap(x, r.y + 6, key) + 10;
            Label(new Rect(x, r.y, tw + 4, 38), _c.FocusLabel, _textBold, Paper);
        }

        void Toasts(float w, float h)
        {
            float now = Time.unscaledTime, y = h * .3f;
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                var (text, color, born) = _toasts[i];
                float age = now - born; if (age > 2.6f) { _toasts.RemoveAt(i); continue; }
                float a = Mathf.Clamp01(2.6f - age) * Mathf.Clamp01(age * 6f);
                var st = new GUIStyle(_big) { fontSize = 34 };
                Label(new Rect(0, y - age * 18 + 2, w, 44), text, st, new Color(0, 0, 0, .55f * a));
                Label(new Rect(0, y - age * 18, w, 44), text, st, new Color(color.r, color.g, color.b, a));
                y += 46;
            }
        }

        void Guide(float h)
        {
            float rowH = 25, height = 42 + Keys.Length * rowH;
            var r = new Rect(14, h - height - 14, 370, height);
            Panel(r, InkSoft);
            Label(new Rect(r.x + 16, r.y + 8, 300, 22), "CONTROLS", _small, Accent);
            for (int i = 0; i < Keys.Length; i++)
            {
                float y = r.y + 34 + i * rowH;
                KeyCap(r.x + 14, y, Keys[i].key);
                Label(new Rect(r.x + 150, y, 210, 26), Keys[i].what, _small, Paper);
            }
        }
        void HintGuide(float h)
        {
            var r = new Rect(14, h - 46, 170, 32);
            Panel(r, InkSoft, _pillStyle); KeyCap(r.x + 6, r.y + 3, "F1"); Label(new Rect(r.x + 46, r.y, 120, 32), "controls", _small, Paper);
        }

        void Dim(float w, float h, float a) { Fill(new Rect(0, 0, w, h), new Color(.03f, .03f, .05f, a)); }

        void TitleCard(float w, float h)
        {
            Dim(w, h, .55f);
            Label(new Rect(0, h * .2f, w, 80), "CANTEEN RUSH", _huge, Paper);
            Label(new Rect(0, h * .2f + 74, w, 30), "Rooftop canteen, 13th floor.  Load the tray, beat the clock, keep every table happy.", new GUIStyle(_text) { alignment = TextAnchor.MiddleCenter }, Muted);
            var b = new Rect(w / 2 - 170, h * .2f + 130, 340, 60);
            float pulse = .85f + .15f * Mathf.Sin(Time.unscaledTime * 3f);
            Panel(b, new Color(Accent.r * pulse, Accent.g * pulse, Accent.b * pulse, 1), _pillStyle);
            Label(b, "Press ENTER to play", new GUIStyle(_big) { fontSize = 24 }, new Color(.1f, .08f, .05f));
            float cardW = 640, rowH = 25, cardH = 50 + 6 * rowH;
            var card = new Rect(w / 2 - cardW / 2, h * .2f + 220, cardW, cardH);
            Panel(card, Ink);
            Label(new Rect(card.x + 20, card.y + 10, 300, 22), "HOW TO PLAY", _small, Accent);
            for (int i = 0; i < Keys.Length && i < 12; i++)
            {
                float col = i < 6 ? 0 : cardW / 2, row = i % 6;
                float y = card.y + 38 + row * rowH;
                KeyCap(card.x + 20 + col, y, Keys[i].key);
                Label(new Rect(card.x + 140 + col, y, 180, 26), Keys[i].what, _small, Paper);
            }
        }

        void ResultsCard(float w, float h)
        {
            Dim(w, h, .6f);
            var cfg = _c.Game != null ? _c.Game.Config : null;
            int stars = cfg != null ? Mathf.Clamp(ScoreCalculator.Stars(_score, cfg), 0, 3) : 0;
            var card = new Rect(w / 2 - 260, h * .18f, 520, 400);
            Panel(card, Ink);
            Label(new Rect(card.x, card.y + 22, card.width, 30), "ROUND OVER", new GUIStyle(_small) { alignment = TextAnchor.MiddleCenter, fontSize = 16 }, Accent);
            Label(new Rect(card.x, card.y + 52, card.width, 80), _score.ToString("N0"), _huge, Paper);
            Label(new Rect(card.x, card.y + 124, card.width, 24), "points", new GUIStyle(_small) { alignment = TextAnchor.MiddleCenter }, Muted);
            for (int i = 0; i < 3; i++)
                Panel(new Rect(card.x + card.width / 2 - 78 + i * 56, card.y + 160, 44, 44), i < stars ? Accent : new Color(1, 1, 1, .12f), _pillStyle);
            string[] rows = { "Orders served", _served.ToString(), "Perfect", _perfect.ToString(), "Walked out", _expired.ToString() };
            for (int i = 0; i < 3; i++)
            {
                float y = card.y + 228 + i * 34;
                Label(new Rect(card.x + 60, y, 260, 30), rows[i * 2], _text, Muted);
                Label(new Rect(card.x + 300, y, 160, 30), rows[i * 2 + 1], new GUIStyle(_textBold) { alignment = TextAnchor.MiddleRight }, i == 2 && _expired > 0 ? Bad : Paper);
            }
            var b = new Rect(card.x + 90, card.y + card.height - 64, card.width - 180, 46);
            Panel(b, Accent, _pillStyle);
            Label(b, "ENTER  play again", new GUIStyle(_big) { fontSize = 20 }, new Color(.1f, .08f, .05f));
        }

        void PauseCard(float w, float h)
        {
            Dim(w, h, .55f);
            Label(new Rect(0, h / 2 - 90, w, 70), "PAUSED", _huge, Paper);
            Label(new Rect(0, h / 2 - 10, w, 30), "Click or Esc to resume      M  back to the title screen      F5  VR simulator", new GUIStyle(_text) { alignment = TextAnchor.MiddleCenter }, Muted);
        }
    }
}

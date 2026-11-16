using CanteenRush.Core;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CanteenRush.Gameplay.Desktop
{
    /// Crosshair, context prompt, key guide and pause card for desktop mode. IMGUI, so it needs no prefab or scene edits.
    [RequireComponent(typeof(DesktopController))]
    public sealed class DesktopHud : MonoBehaviour
    {
        static readonly (string key, string what)[] Keys =
        {
            ("W A S D", "walk (Shift sprint)"), ("Mouse", "look"), ("Click / E", "grab, drop, open fridge"),
            ("F (hold)", "pump water"), ("Right mouse + move", "tilt the tray"), ("Enter", "start / replay"),
            ("Esc", "pause"), ("F1", "hide this"), ("F5", "VR simulator")
        };
        DesktopController _c;
        GUIStyle _label, _small, _title;
        bool _guide = true;
        float _guideUntil;
        Texture2D _dot;

        void Awake() { _c = GetComponent<DesktopController>(); _guideUntil = Time.unscaledTime + 25f; }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame) { _guide = !_guide; _guideUntil = float.MaxValue; }
            if (_guide && Time.unscaledTime > _guideUntil && _c.Holding) _guide = false;
        }

        void Styles()
        {
            if (_label != null) return;
            _label = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(1, 1, 1, .92f) } };
            _title = new GUIStyle(_label) { fontSize = 30 };
            _dot = Texture2D.whiteTexture;
        }

        static void Box(Rect r, Color c) { var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old; }

        void OnGUI()
        {
            if (!DesktopController.Active || !_c.Ready) return;
            Styles();
            float w = Screen.width, h = Screen.height, s = Mathf.Clamp(h / 900f, .8f, 1.6f);
            var matrix = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1)); w /= s; h /= s;
            Color accent = _c.Focus == DesktopController.Kind.None ? new Color(1, 1, 1, .85f) : _c.Focus == DesktopController.Kind.Drop ? new Color(1f, .78f, .3f) : new Color(.35f, .95f, .6f);
            // crosshair: dot, grows into a ring-ish square when something is usable
            float size = _c.Focus == DesktopController.Kind.None ? 5 : 11;
            Box(new Rect(w / 2 - size / 2 - 1, h / 2 - size / 2 - 1, size + 2, size + 2), new Color(0, 0, 0, .55f));
            Box(new Rect(w / 2 - size / 2, h / 2 - size / 2, size, size), accent);
            if (!string.IsNullOrEmpty(_c.FocusLabel))
            {
                string text = _c.FocusLabel;
                if (_c.Focus == DesktopController.Kind.Grab || _c.Focus == DesktopController.Kind.Drop || _c.Focus == DesktopController.Kind.Door) text = "[E / Click]  " + text;
                var r = new Rect(w / 2 - 230, h / 2 + 34, 460, 34);
                Box(r, new Color(0, 0, 0, .55f)); GUI.Label(r, text, _label);
            }
            var state = _c.Game != null ? _c.Game.State : RoundState.Boot;
            if (state == RoundState.Menu || state == RoundState.Results)
            {
                var r = new Rect(w / 2 - 300, h - 120, 600, 52);
                Box(r, new Color(0, 0, 0, .6f)); GUI.Label(r, state == RoundState.Menu ? "Press ENTER to start" : "Press ENTER to play again   |   M (in pause) for menu", _title);
            }
            if (_guide) DrawGuide(h);
            else GUI.Label(new Rect(14, h - 34, 300, 26), "F1: controls", _small);
            if (_c.Paused)
            {
                Box(new Rect(0, 0, w, h), new Color(0, 0, 0, .6f));
                GUI.Label(new Rect(w / 2 - 300, h / 2 - 80, 600, 60), "PAUSED", _title);
                GUI.Label(new Rect(w / 2 - 300, h / 2 - 10, 600, 30), "Click or Esc to resume   |   M back to menu", _label);
            }
            GUI.matrix = matrix;
        }

        void DrawGuide(float h)
        {
            float rows = Keys.Length, height = 34 + rows * 24;
            var r = new Rect(14, h - height - 14, 330, height);
            Box(r, new Color(0, 0, 0, .55f));
            GUI.Label(new Rect(r.x + 12, r.y + 4, 300, 26), "CONTROLS", _small);
            for (int i = 0; i < Keys.Length; i++)
            {
                GUI.Label(new Rect(r.x + 12, r.y + 30 + i * 24, 150, 24), Keys[i].key, new GUIStyle(_small) { fontStyle = FontStyle.Bold, normal = { textColor = new Color(.6f, 1f, .75f) } });
                GUI.Label(new Rect(r.x + 150, r.y + 30 + i * 24, 175, 24), Keys[i].what, _small);
            }
        }
    }
}

using CanteenRush.Core;
using UnityEngine;
using UnityEngine.UI;
namespace CanteenRush.UI
{
    /// Menu board (MASTER_SPEC §12): Play, Quit, Seated toggle, Teleport toggle.
    /// Play raises GameEvents.PlayRequested and the toggles raise GameEvents.ComfortChanged; P2's GameManager and
    /// ComfortSettings handle them. The shell never references gameplay code (docs/DECISIONS.md, 2026-11-13).
    public sealed class MenuBoard : WorldBoard
    {
        bool _seated, _teleport;
        Button _play, _quit, _seatedButton, _teleportButton;

        public bool Seated => _seated;
        public bool Teleport => _teleport;
        protected override Vector2 SizeMm => new Vector2(900, 760);
        protected override bool VisibleIn(RoundState s) => s == RoundState.Boot || s == RoundState.Menu;

        protected override void Build(RectTransform root)
        {
            UiFactory.Label(root, "Title", "CANTEEN RUSH", UiFactory.TitleSize, TextAnchor.MiddleCenter, new Vector2(0, 0.80f), new Vector2(1, 0.97f), UiFactory.Accent);
            UiFactory.Label(root, "Subtitle", "Serve the rooftop. Fill orders, load the tray, follow the bell.", UiFactory.SmallSize,
                TextAnchor.MiddleCenter, new Vector2(0.06f, 0.68f), new Vector2(0.94f, 0.80f));
            _play = UiFactory.Button(root, "Play", "PLAY", new Vector2(0.15f, 0.49f), new Vector2(0.85f, 0.64f), Play);
            _seatedButton = UiFactory.Button(root, "Seated", "", new Vector2(0.15f, 0.33f), new Vector2(0.49f, 0.45f), ToggleSeated);
            _teleportButton = UiFactory.Button(root, "Teleport", "", new Vector2(0.51f, 0.33f), new Vector2(0.85f, 0.45f), ToggleTeleport);
            _quit = UiFactory.Button(root, "Quit", "QUIT", new Vector2(0.30f, 0.15f), new Vector2(0.70f, 0.27f), Quit);
            UiFactory.Label(root, "Hint", "Point with a controller ray and pull the trigger", UiFactory.SmallSize,
                TextAnchor.MiddleCenter, new Vector2(0, 0.02f), new Vector2(1, 0.12f), new Color(0.75f, 0.78f, 0.82f));
            RefreshLabels();
        }

        void RefreshLabels()
        {
            UiFactory.SetLabel(_seatedButton, _seated ? "SEATED: ON" : "SEATED: OFF");
            UiFactory.SetLabel(_teleportButton, _teleport ? "TELEPORT: ON" : "TELEPORT: OFF");
        }

        public void Play() => GameEvents.PlayRequested();
        public void ToggleSeated() { _seated = !_seated; RefreshLabels(); GameEvents.ComfortChanged(_seated, _teleport); }
        public void ToggleTeleport() { _teleport = !_teleport; RefreshLabels(); GameEvents.ComfortChanged(_seated, _teleport); }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public Button PlayButton => _play;
        public Button QuitButton => _quit;
        public Button SeatedButton => _seatedButton;
        public Button TeleportButton => _teleportButton;
    }
}

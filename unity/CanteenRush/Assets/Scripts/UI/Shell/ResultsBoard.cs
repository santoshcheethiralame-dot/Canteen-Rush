using CanteenRush.Core;
using UnityEngine;
using UnityEngine.UI;
namespace CanteenRush.UI
{
    /// Results board (MASTER_SPEC §12): total score, correct / wrong / spill / expired counts, stars, Replay, Menu.
    /// Tallies from GameEvents during Wave1..3 only, so tutorial practice never counts.
    public sealed class ResultsBoard : WorldBoard
    {
        [SerializeField] GameConfig _config;
        readonly RoundStats _stats = new RoundStats();
        Text _score, _stars, _breakdown;
        Button _replay, _menu;
        bool _counting;

        public RoundStats Stats => _stats;
        protected override Vector2 SizeMm => new Vector2(900, 780);
        protected override bool VisibleIn(RoundState s) => s == RoundState.Results;
        public void SetConfig(GameConfig config) => _config = config;

        protected override void Build(RectTransform root)
        {
            UiFactory.Label(root, "Title", "ROUND OVER", UiFactory.TitleSize, TextAnchor.MiddleCenter, new Vector2(0, 0.84f), new Vector2(1, 0.98f), UiFactory.Accent);
            _stars = UiFactory.Label(root, "Stars", "", 110, TextAnchor.MiddleCenter, new Vector2(0, 0.70f), new Vector2(1, 0.85f), UiFactory.Accent);
            _score = UiFactory.Label(root, "Score", "", 72, TextAnchor.MiddleCenter, new Vector2(0, 0.58f), new Vector2(1, 0.70f));
            _breakdown = UiFactory.Label(root, "Breakdown", "", UiFactory.BodySize, TextAnchor.UpperCenter, new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.57f));
            _replay = UiFactory.Button(root, "Replay", "REPLAY", new Vector2(0.10f, 0.05f), new Vector2(0.48f, 0.19f), GameEvents.PlayRequested);
            _menu = UiFactory.Button(root, "Menu", "MENU", new Vector2(0.52f, 0.05f), new Vector2(0.90f, 0.19f), GameEvents.MenuRequested);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            GameEvents.OnScoreChanged += Score;
            GameEvents.OnOrderServed += Served;
            GameEvents.OnOrderExpired += Expired;
            GameEvents.OnItemDropped += Dropped;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            GameEvents.OnScoreChanged -= Score;
            GameEvents.OnOrderServed -= Served;
            GameEvents.OnOrderExpired -= Expired;
            GameEvents.OnItemDropped -= Dropped;
        }

        protected override void RoundChanged(RoundState s)
        {
            bool playing = s == RoundState.Wave1 || s == RoundState.Wave2 || s == RoundState.Wave3;
            if (s == RoundState.Wave1 && !_counting) _stats.Reset();
            _counting = playing;
            if (s == RoundState.Results) Refresh();
            base.RoundChanged(s);
        }

        void Score(int total) { if (_counting || !IsShown) _stats.Score = total; }
        void Served(ServeResult r) { if (_counting) _stats.Add(r); }
        void Expired(Order o) { if (_counting) _stats.Expired++; }
        void Dropped(ItemId i, float speed, Vector3 p) { if (_counting) _stats.Spills++; }

        public void Refresh()
        {
            int stars = _config != null ? ScoreCalculator.Stars(_stats.Score, _config) : RoundStats.Stars(_stats.Score, new[] { 300, 700, 1200 });
            stars = Mathf.Clamp(stars, 0, 3);
            _stars.text = new string('★', stars) + new string('☆', 3 - stars);
            // Stars also in words: the star glyph relies on the OS font fallback of the built-in font.
            _score.text = _stats.Score + " points  -  " + stars + " of 3 stars";
            LastStars = stars;
            _breakdown.text = string.Format("Orders served  {0}   (perfect {1})\nCorrect items  {2}\nWrong items  {3}\nSpills  {4}\nExpired orders  {5}",
                _stats.Served, _stats.Perfect, _stats.Correct, _stats.Wrong, _stats.Spills, _stats.Expired);
        }

        public int LastStars { get; private set; }
        public string StarsText => _stars != null ? _stars.text : null;
        public Button ReplayButton => _replay;
        public Button MenuButton => _menu;
    }
}

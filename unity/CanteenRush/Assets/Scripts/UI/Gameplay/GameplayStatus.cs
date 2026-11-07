using CanteenRush.Core;
using UnityEngine;
using UnityEngine.UI;
namespace CanteenRush.UI.Gameplay
{
    public sealed class GameplayStatus : MonoBehaviour
    {
        [SerializeField] Text _text;
        [SerializeField] GameConfig _config;
        int _score, _step;
        float _remaining;
        RoundState _state;
        void OnEnable()
        {
            GameEvents.OnScoreChanged += Score; GameEvents.OnRoundTimeChanged += TimeChanged;
            GameEvents.OnRoundStateChanged += StateChanged; GameEvents.OnTutorialStep += Tutorial;
        }
        void OnDisable()
        {
            GameEvents.OnScoreChanged -= Score; GameEvents.OnRoundTimeChanged -= TimeChanged;
            GameEvents.OnRoundStateChanged -= StateChanged; GameEvents.OnTutorialStep -= Tutorial;
        }
        void Score(int score) { _score = score; Refresh(); }
        void TimeChanged(float remaining) { _remaining = remaining; Refresh(); }
        void StateChanged(RoundState state) { _state = state; Refresh(); }
        void Tutorial(int step) { _step = step; Refresh(); }
        void Refresh()
        {
            if (_text == null) return;
            if (_state == RoundState.Tutorial)
                _text.text = _step == 0 ? "Practice: grab a donut" : _step == 1 ? "Practice: snap the donut onto a tray" : "Practice: serve the donut customer";
            else if (_state == RoundState.Menu) _text.text = "Canteen Rush | Enter: Play | F6: Seated | F7: Teleport";
            else if (_state == RoundState.Results)
                _text.text = "Results | Score " + _score + (_config != null ? " | Stars " + ScoreCalculator.Stars(_score, _config) : "") + " | Enter: Replay | F8: Menu";
            else _text.text = _state + " | Score " + _score + " | " + Mathf.CeilToInt(_remaining) + "s";
        }
    }
}

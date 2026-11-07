using CanteenRush.Core;
using UnityEngine;
namespace CanteenRush.Gameplay
{
    public sealed class ScoreService : MonoBehaviour
    {
        [SerializeField] GameConfig _config;
        public int Total { get; private set; }
        bool _acceptSpills = true;
        void OnEnable() { GameEvents.OnItemDropped += OnItemDropped; GameEvents.OnRoundStateChanged += StateChanged; }
        void OnDisable() { GameEvents.OnItemDropped -= OnItemDropped; GameEvents.OnRoundStateChanged -= StateChanged; }
        void StateChanged(RoundState state) { _acceptSpills = state == RoundState.Tutorial || state == RoundState.Wave1 || state == RoundState.Wave2 || state == RoundState.Wave3; }
        void OnItemDropped(ItemId item, float speed, Vector3 position) { if (_acceptSpills) Apply(ScoreCalculator.Spill(_config)); }
        public void ResetScore() { Total = 0; GameEvents.ScoreChanged(Total); }
        public void Apply(int points) { Total = Mathf.Max(0, Total + points); GameEvents.ScoreChanged(Total); }
    }
}

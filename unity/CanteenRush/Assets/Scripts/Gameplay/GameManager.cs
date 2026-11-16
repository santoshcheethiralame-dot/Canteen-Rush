using CanteenRush.Core;
using CanteenRush.World;
using UnityEngine;
namespace CanteenRush.Gameplay
{
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] GameConfig _config;
        [SerializeField] OrderService _orders;
        [SerializeField] ScoreService _score;
        [SerializeField] CustomerManager _customers;
        [SerializeField] ItemSpawner[] _spawners = System.Array.Empty<ItemSpawner>();
        Tray[] _trays;
        FridgeDoor[] _doors;
        ServeSocket[] _serveSockets;
        WaterCooler[] _coolers;
        WaveDirector _wave;
        SunCycle _sun;
        float _clockReport;
        int _tutorialStep;
        bool _tutorialSeen, _finishTutorial, _retryTutorial;
        public RoundState State { get; private set; } = RoundState.Boot;
        public float Remaining => _wave == null ? _config.RoundSeconds : Mathf.Max(0, _config.RoundSeconds - _wave.Elapsed);
        public bool IsPlaying => State == RoundState.Wave1 || State == RoundState.Wave2 || State == RoundState.Wave3;
        void Awake() { _sun = FindFirstObjectByType<SunCycle>(); }
        void OnEnable() { GameEvents.OnItemGrabbed += Grabbed; GameEvents.OnItemSocketed += Socketed; GameEvents.OnOrderServed += Served; GameEvents.OnPlayRequested += Play; GameEvents.OnMenuRequested += Menu; }
        void OnDisable() { GameEvents.OnItemGrabbed -= Grabbed; GameEvents.OnItemSocketed -= Socketed; GameEvents.OnOrderServed -= Served; GameEvents.OnPlayRequested -= Play; GameEvents.OnMenuRequested -= Menu; }
        void Start() { if (State == RoundState.Boot) SetState(RoundState.Menu); }
        void SetState(RoundState state) { State = state; GameEvents.RoundStateChanged(state); }
        void ResetRoundObjects()
        {
            _spawners = FindObjectsByType<ItemSpawner>(FindObjectsSortMode.None);
            _trays = FindObjectsByType<Tray>(FindObjectsSortMode.None);
            _doors = FindObjectsByType<FridgeDoor>(FindObjectsSortMode.None);
            _serveSockets = FindObjectsByType<ServeSocket>(FindObjectsSortMode.None);
            _coolers = FindObjectsByType<WaterCooler>(FindObjectsSortMode.None);
            _customers.ResetCustomers(); _orders.ResetOrders();
            foreach (var socket in _serveSockets) socket.ResetSocket();
            foreach (var tray in _trays) tray.ResetTray();
            foreach (var spawner in _spawners) spawner.ResetSpawner();
            foreach (var cooler in _coolers) cooler.ResetPump();
            foreach (var door in _doors) door.ResetDoor();
            _score.ResetScore(); _clockReport = 0;
        }
        [ContextMenu("Play / Replay")]
        public void Play()
        {
            _finishTutorial = false; _retryTutorial = false; ResetRoundObjects(); SetState(RoundState.Menu);
            var random = new System.Random(); _wave = new WaveDirector(_config, random);
            if (!_tutorialSeen)
            {
                _tutorialStep = 0; SetState(RoundState.Tutorial);
                _customers.BeginTutorial(); GameEvents.TutorialStep(0);
            }
            else { _customers.BeginRound(_wave, random); SetState(RoundState.Wave1); }
            ReportClock();
        }
        void Grabbed(ItemId item, Hand hand, Vector3 pos) { if (State == RoundState.Tutorial && _tutorialStep == 0 && item == ItemId.Donut) AdvanceTutorial(); }
        void Socketed(ItemId item, Vector3 pos) { if (State == RoundState.Tutorial && _tutorialStep == 1 && item == ItemId.Donut) AdvanceTutorial(); }
        void Served(ServeResult result)
        {
            if (State != RoundState.Tutorial) return;
            if (_tutorialStep == 2 && result.Correct > 0) AdvanceTutorial();
            else _retryTutorial = true;
        }
        void AdvanceTutorial()
        {
            _tutorialStep++; GameEvents.TutorialStep(_tutorialStep);
            // Let all practice-serve listeners finish before resetting the round.
            if (_tutorialStep == 3) { _tutorialSeen = true; _finishTutorial = true; }
        }
        [ContextMenu("Return to menu")]
        public void Menu()
        {
            _finishTutorial = false; _retryTutorial = false; ResetRoundObjects(); _wave = null;
            SetState(RoundState.Menu); ReportClock();
        }
        void Update() { Tick(Time.deltaTime); }
        public void Tick(float dt)
        {
            if (_finishTutorial || _retryTutorial) { _finishTutorial = false; _retryTutorial = false; Play(); }
            if (!IsPlaying || dt <= 0) return;
            _wave.Tick(dt);
            if (_wave.Current == RoundState.Results)
            {
                _customers.ResetCustomers(); _orders.ResetOrders();
                foreach (var socket in _serveSockets) socket.ResetSocket();
                ReportClock(); SetState(RoundState.Results); return;
            }
            _orders.Tick(dt); _customers.Tick();
            if (State != _wave.Current) SetState(_wave.Current);
            _clockReport += dt;
            if (_clockReport >= .1f) { _clockReport %= .1f; ReportClock(); }
        }
        void ReportClock() { GameEvents.RoundTimeChanged(Remaining); if (_sun != null) _sun.SetRemaining(Remaining, _config.RoundSeconds); }
    }
}

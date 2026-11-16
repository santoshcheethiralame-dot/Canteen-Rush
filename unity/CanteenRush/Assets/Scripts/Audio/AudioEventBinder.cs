using System.Collections.Generic;
using CanteenRush.Core;
using UnityEngine;
namespace CanteenRush.Audio
{
    /// GameEvents -> cues, every row of the MASTER_SPEC §10 table. A pure listener: subscribes in OnEnable,
    /// unsubscribes in OnDisable, never calls gameplay code. Every haptic preset has a sound here too
    /// (see docs/LOG_P3.md "haptic + sound pairs").
    public sealed class AudioEventBinder : MonoBehaviour
    {
        public const int SeatCount = 8;
        [SerializeField] AudioService _audio;
        [SerializeField] ItemDef[] _items = System.Array.Empty<ItemDef>();
        [SerializeField] float _maxImpactSpeed = 4f, _minImpactSpeed = 0.25f;
        [SerializeField] float _waterPitchEmpty = 0.9f, _waterPitchFull = 1.2f;
        [SerializeField] float _crowdFullAt = 4f;
        [SerializeField] float _fridgeHumOpenBoost = 0.6f;
        [SerializeField] float _tiltRattleInterval = 0.25f;

        readonly Dictionary<int, Order> _orders = new Dictionary<int, Order>();
        readonly float[] _doorOpen = new float[2];
        AudioSource _water, _music;
        Vector3 _pumpPos;
        float _waterLastTick = float.NegativeInfinity, _lastRattle = float.NegativeInfinity, _musicTarget;
        bool _filling;

        void Awake() { if (_audio == null) _audio = GetComponent<AudioService>(); if (_audio == null) _audio = AudioService.Instance; }

        void Start()
        {
            if (_audio == null) return;
            _water = _audio.CreateLoop(AudioCueIds.WaterFill, transform, false);
            _music = _audio.CreateLoop(AudioCueIds.Music, transform, false);
            _pumpPos = AudioSceneInstaller.FridgePosition;
        }

        void OnEnable()
        {
            GameEvents.OnRoundStateChanged += RoundChanged;
            GameEvents.OnOrderPlaced += OrderPlaced;
            GameEvents.OnOrderServed += OrderServed;
            GameEvents.OnOrderExpired += OrderExpired;
            GameEvents.OnCustomerCount += CustomerCount;
            GameEvents.OnItemGrabbed += ItemGrabbed;
            GameEvents.OnItemReleased += ItemReleased;
            GameEvents.OnItemSocketed += ItemSocketed;
            GameEvents.OnItemDropped += ItemDropped;
            GameEvents.OnItemImpact += ItemImpact;
            GameEvents.OnFridgeDoorMoved += DoorMoved;
            GameEvents.OnFridgeDoorLimit += DoorLimit;
            GameEvents.OnPumpActivated += Pump;
            GameEvents.OnCupFilling += CupFilling;
            GameEvents.OnCupFilled += CupFilled;
            GameEvents.OnTrayTilt += TrayTilt;
            GameEvents.OnTrayServedPlaced += TrayPlaced;
            GameEvents.OnFootstep += Footstep;
            GameEvents.OnUiHover += UiHover;
            GameEvents.OnUiClick += UiClick;
        }

        void OnDisable()
        {
            GameEvents.OnRoundStateChanged -= RoundChanged;
            GameEvents.OnOrderPlaced -= OrderPlaced;
            GameEvents.OnOrderServed -= OrderServed;
            GameEvents.OnOrderExpired -= OrderExpired;
            GameEvents.OnCustomerCount -= CustomerCount;
            GameEvents.OnItemGrabbed -= ItemGrabbed;
            GameEvents.OnItemReleased -= ItemReleased;
            GameEvents.OnItemSocketed -= ItemSocketed;
            GameEvents.OnItemDropped -= ItemDropped;
            GameEvents.OnItemImpact -= ItemImpact;
            GameEvents.OnFridgeDoorMoved -= DoorMoved;
            GameEvents.OnFridgeDoorLimit -= DoorLimit;
            GameEvents.OnPumpActivated -= Pump;
            GameEvents.OnCupFilling -= CupFilling;
            GameEvents.OnCupFilled -= CupFilled;
            GameEvents.OnTrayTilt -= TrayTilt;
            GameEvents.OnTrayServedPlaced -= TrayPlaced;
            GameEvents.OnFootstep -= Footstep;
            GameEvents.OnUiHover -= UiHover;
            GameEvents.OnUiClick -= UiClick;
        }

        void Play(string cue, Vector3 pos, float volume = 1f, float pitch = 1f, int variant = -1)
        {
            if (_audio != null) _audio.Play(cue, pos, volume, pitch, variant);
        }

        Vector3 Listener => AudioService.ListenerPosition ?? transform.position;

        /// Signature spatial cue: one semitone per seat. Seats are numbered nearest table first,
        /// so a higher bell means a farther table (seat 0 = x1.00 ... seat 7 = x1.50).
        public static float SeatPitch(int seatIndex) => Mathf.Pow(2f, Mathf.Clamp(seatIndex, 0, SeatCount - 1) / 12f);

        float MaterialPitch(ItemId id)
        {
            foreach (var d in _items)
                if (d != null && d.Id == id)
                    switch (d.AudioMaterial)
                    {
                        case AudioMaterial.Glass: return 1.25f;
                        case AudioMaterial.Metal: return 1.12f;
                        case AudioMaterial.Paper: return 0.85f;
                        default: return 1f;
                    }
            return id == ItemId.Shake || id == ItemId.Water ? 1.25f : id == ItemId.Cola ? 1.12f : 1f;
        }

        // ---------- Round flow ----------
        void RoundChanged(RoundState s)
        {
            bool playing = s == RoundState.Wave1 || s == RoundState.Wave2 || s == RoundState.Wave3;
            _musicTarget = playing ? 1f : 0f;
            if (s == RoundState.Results) { if (_audio != null) _audio.PlayUi(AudioCueIds.RoundEnd); }
            if (s == RoundState.Menu || s == RoundState.Results || s == RoundState.Tutorial) { _orders.Clear(); StopWater(); }
            if (s == RoundState.Wave1) _orders.Clear();
        }

        // ---------- Orders / customers ----------
        void OrderPlaced(Order o)
        {
            if (o == null) return;
            _orders[o.Id] = o;
            Play(AudioCueIds.OrderBell, o.SeatPosition + Vector3.up * 1.2f, 1f, SeatPitch(o.SeatIndex));
        }

        void OrderServed(ServeResult r)
        {
            _orders.TryGetValue(r.OrderId, out var o);
            _orders.Remove(r.OrderId);
            var pos = o != null ? o.SeatPosition + Vector3.up * 1.3f : Listener;
            if (r.Outcome == ServeOutcome.Empty)
            {
                if (_audio != null) _audio.PlayUi(AudioCueIds.UiError);
                return;
            }
            Play(AudioCueIds.CustomerHappy, pos, r.Outcome == ServeOutcome.Perfect ? 1f : 0.8f, r.Outcome == ServeOutcome.Perfect ? 1.05f : 0.92f);
            if (r.Outcome == ServeOutcome.Perfect && _audio != null) _audio.PlayUi(AudioCueIds.UiConfirm);
        }

        void OrderExpired(Order o)
        {
            if (o != null) _orders.Remove(o.Id);
            Play(AudioCueIds.CustomerAngry, o != null ? o.SeatPosition + Vector3.up * 1.3f : Listener);
            if (_audio != null) _audio.PlayUi(AudioCueIds.UiError);
        }

        void CustomerCount(int n)
        {
            var crowd = AudioSceneInstaller.Find(AudioCueIds.PatioCrowd);
            if (crowd != null) crowd.Level = Mathf.Clamp01(n / _crowdFullAt);
        }

        // ---------- Items ----------
        void ItemGrabbed(ItemId item, Hand hand, Vector3 pos) => Play(AudioCueIds.Grab, pos, 1f, MaterialPitch(item));
        void ItemReleased(ItemId item, Hand hand, Vector3 pos) => Play(AudioCueIds.Release, pos, 1f, MaterialPitch(item));
        void ItemSocketed(ItemId item, Vector3 pos) => Play(AudioCueIds.TrayClink, pos, 0.8f);
        void ItemDropped(ItemId item, float speed, Vector3 pos) =>
            Play(item == ItemId.Shake ? AudioCueIds.ItemDropGlass : AudioCueIds.ItemDrop, pos, Mathf.Lerp(0.6f, 1f, speed / _maxImpactSpeed));

        void ItemImpact(ItemId item, float speed, Vector3 pos)
        {
            if (speed < _minImpactSpeed) return;
            Play(AudioCueIds.TrayClink, pos, Mathf.Clamp01(speed / _maxImpactSpeed), MaterialPitch(item));
        }

        void TrayPlaced(Vector3 pos, bool hadOrder) => Play(AudioCueIds.TrayClink, pos, 1f, 0.9f);

        void TrayTilt(float angle, Hand hand)
        {
            // Pairs with the TrayTiltWarn haptic ramp: a rattle that speeds up and grows with the angle.
            if (angle <= 20f) return;
            float k = Mathf.InverseLerp(20f, 35f, angle);
            if (Time.time - _lastRattle < Mathf.Lerp(_tiltRattleInterval, _tiltRattleInterval * 0.4f, k)) return;
            _lastRattle = Time.time;
            Play(AudioCueIds.TrayClink, Listener + Vector3.down * 0.6f, Mathf.Lerp(0.25f, 0.7f, k), 1.1f);
        }

        // ---------- Stations ----------
        void DoorMoved(int door, float open01, bool detent)
        {
            int d = Mathf.Clamp(door, 0, 1);
            bool opening = open01 >= _doorOpen[d];
            _doorOpen[d] = open01;
            if (detent) Play(AudioCueIds.FridgeLatch, AudioSceneInstaller.FridgePosition, 1f, 1f, opening ? 0 : 1);
            // Open door: the compressor is exposed, the hum gets louder.
            var hum = AudioSceneInstaller.Find(AudioCueIds.FridgeHum);
            if (hum != null) hum.Level = 1f + _fridgeHumOpenBoost * Mathf.Max(_doorOpen[0], _doorOpen[1]);
        }

        void DoorLimit(int door, bool atOpenLimit)
        {
            // Pairs with the DoorStop haptic: a duller, lower thunk of the latch clip.
            if (atOpenLimit) Play(AudioCueIds.FridgeLatch, AudioSceneInstaller.FridgePosition, 0.7f, 0.75f, 1);
        }

        void Pump(bool pressed, Vector3 pos)
        {
            _pumpPos = pos;
            if (pressed) Play(AudioCueIds.PumpPress, pos);
            else StopWater();
        }

        void CupFilling(float level, Hand hand)
        {
            if (_water == null) return;
            _waterLastTick = Time.time;
            _water.transform.position = _pumpPos + Vector3.down * 0.3f;
            _water.pitch = Mathf.Lerp(_waterPitchEmpty, _waterPitchFull, Mathf.Clamp01(level));
            if (!_filling) { _filling = true; _water.Play(); }
        }

        void CupFilled(Vector3 pos) => StopWater();

        void StopWater()
        {
            _filling = false;
            if (_water != null && _water.isPlaying) _water.Stop();
        }

        // ---------- Player / UI ----------
        void Footstep(Surface s, Vector3 pos) =>
            Play(s == Surface.Tile ? AudioCueIds.FootstepsTile : AudioCueIds.FootstepsStone, pos);

        void UiHover() { if (_audio != null) _audio.Play(AudioCueIds.UiClick, Listener, 0.35f, 1.35f, -1, true); }
        void UiClick() { if (_audio != null) _audio.PlayUi(AudioCueIds.UiClick); }

        void Update()
        {
            // CupFilling arrives every frame while filling; if it stops arriving the cup left the spout.
            if (_filling && Time.time - _waterLastTick > 0.2f) StopWater();
            if (_music != null)
            {
                float v = Mathf.MoveTowards(_music.volume, _musicTarget * 0.8f, Time.deltaTime / 1.5f);
                _music.volume = v;
                if (v > 0 && !_music.isPlaying) _music.Play();
                else if (v <= 0 && _music.isPlaying) _music.Stop();
            }
        }
    }
}

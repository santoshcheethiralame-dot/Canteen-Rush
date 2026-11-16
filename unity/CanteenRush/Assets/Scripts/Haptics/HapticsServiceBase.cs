using System;
using System.Collections.Generic;
using CanteenRush.Core;
using UnityEngine;
namespace CanteenRush.Haptics
{
    /// Shared logic for every haptics backend: preset lookup, the 30 ms per-hand rate limit,
    /// ramp coalescing (one impulse per 50 ms however often Ramp() is called), multi-pulse
    /// presets, and the PulseSent hook the debug HUD listens to. Subclasses implement SendToDevice only.
    public abstract class HapticsServiceBase : MonoBehaviour, IHapticsService
    {
        public const float MinInterval = 0.03f;   // spec §11: never more than one pulse per 30 ms per hand
        public const float RampRefresh = 0.05f;   // WaterFill: "continuous 0.05 refresh"
        public const float RampTimeout = 0.15f;   // a ramp nobody refreshes stops by itself

        [SerializeField] HapticPreset[] _presets = Array.Empty<HapticPreset>();

        /// Every impulse that passed the rate limit (sent to hardware or not).
        public static event Action<HapticPulse> PulseSent;
        /// Every preset request, before rate limiting: tests and the HUD use it to see the event -> preset mapping.
        public static event Action<string, Hand> PresetRequested;

        readonly Dictionary<string, HapticPreset> _byName = new Dictionary<string, HapticPreset>(StringComparer.Ordinal);
        readonly float[] _lastSent = { float.NegativeInfinity, float.NegativeInfinity };
        readonly float[] _rampAmplitude = new float[2];
        readonly float[] _rampLastRefresh = { float.NegativeInfinity, float.NegativeInfinity };
        readonly float[] _rampLastSent = { float.NegativeInfinity, float.NegativeInfinity };
        readonly HapticPreset[] _rampPreset = new HapticPreset[2];
        readonly List<Scheduled> _scheduled = new List<Scheduled>(8);   // later pulses of multi-pulse presets
        struct Scheduled { public float Time; public Hand Hand; public HapticPreset Preset; }

        public IReadOnlyCollection<HapticPreset> Presets => _byName.Values;
        protected virtual float Now => Time.unscaledTime;
        /// True when a real controller that can vibrate is connected (the debug HUD hides itself then).
        public abstract bool HasHapticDevice { get; }
        /// Backend hook. Returns true if a physical device accepted the impulse.
        protected abstract bool SendToDevice(Hand hand, float amplitude, float duration);

        protected virtual void Awake() => RebuildLookup();

        public void SetPresets(HapticPreset[] presets) { _presets = presets ?? Array.Empty<HapticPreset>(); RebuildLookup(); }

        void RebuildLookup()
        {
            _byName.Clear();
            foreach (var p in _presets) if (p != null && !string.IsNullOrEmpty(p.Name)) _byName[p.Name] = p;
        }

        public bool TryGetPreset(string presetName, out HapticPreset preset) => _byName.TryGetValue(presetName ?? string.Empty, out preset);

        public void Pulse(Hand hand, float amplitude, float durationSeconds) => Send(hand, amplitude, durationSeconds, null, false, Color.white);

        public void Play(string presetName, Hand hand)
        {
            PresetRequested?.Invoke(presetName, hand);
            if (!TryGetPreset(presetName, out var p)) { Debug.LogWarning("[Haptics] Unknown preset " + presetName); return; }
            if (p.IsRamp) { SetRamp(hand, p.Amplitude, p); return; }
            Send(hand, p.Amplitude, p.Duration, p.Name, false, p.DebugColor);
            for (int i = 1; i < p.Pulses; i++)
                _scheduled.Add(new Scheduled { Time = Now + i * (p.Duration + p.Gap), Hand = hand, Preset = p });
        }

        /// Ramp driven by a preset's input mapping (e.g. cup level or tray angle).
        public void PlayRamp(string presetName, Hand hand, float input)
        {
            PresetRequested?.Invoke(presetName, hand);
            if (!TryGetPreset(presetName, out var p)) { Debug.LogWarning("[Haptics] Unknown preset " + presetName); return; }
            SetRamp(hand, p.RampAmplitudeFor(input), p);
        }

        public void Ramp(Hand hand, float amplitude01) => SetRamp(hand, amplitude01, null);

        void SetRamp(Hand hand, float amplitude01, HapticPreset preset)
        {
            int h = (int)hand;
            _rampAmplitude[h] = Mathf.Clamp01(amplitude01);
            _rampLastRefresh[h] = Now;
            _rampPreset[h] = preset;
        }

        public void StopRamp(Hand hand) { _rampLastRefresh[(int)hand] = float.NegativeInfinity; _rampAmplitude[(int)hand] = 0; }
        public bool IsRamping(Hand hand) => Now - _rampLastRefresh[(int)hand] <= RampTimeout;

        protected virtual void Update()
        {
            float now = Now;
            for (int i = _scheduled.Count - 1; i >= 0; i--)
            {
                var s = _scheduled[i];
                if (now < s.Time) continue;
                _scheduled.RemoveAt(i);
                Send(s.Hand, s.Preset.Amplitude, s.Preset.Duration, s.Preset.Name, false, s.Preset.DebugColor);
            }
            for (int h = 0; h < 2; h++)
            {
                if (now - _rampLastRefresh[h] > RampTimeout || now - _rampLastSent[h] < RampRefresh) continue;
                var p = _rampPreset[h];
                // Slightly longer than the refresh so consecutive impulses blend into one continuous buzz.
                if (Send((Hand)h, _rampAmplitude[h], RampRefresh * 1.2f, p != null ? p.Name : null, true, p != null ? p.DebugColor : Color.cyan))
                    _rampLastSent[h] = now;
            }
        }

        bool Send(Hand hand, float amplitude, float duration, string preset, bool isRamp, Color color)
        {
            int h = (int)hand;
            float now = Now;
            if (now - _lastSent[h] < MinInterval) return false;
            _lastSent[h] = now;
            amplitude = Mathf.Clamp01(amplitude);
            bool delivered = amplitude > 0 && duration > 0 && SendToDevice(hand, amplitude, duration);
            PulseSent?.Invoke(new HapticPulse(hand, amplitude, duration, preset, isRamp, color, delivered));
            return true;
        }

        /// Tests and round resets: forget timing state and any pending pulses or ramps.
        public void ResetTiming()
        {
            for (int h = 0; h < 2; h++) { _lastSent[h] = _rampLastRefresh[h] = _rampLastSent[h] = float.NegativeInfinity; _rampPreset[h] = null; }
            _scheduled.Clear();
        }
    }
}

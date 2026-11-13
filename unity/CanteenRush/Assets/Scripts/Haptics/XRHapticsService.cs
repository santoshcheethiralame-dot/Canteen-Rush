using System.Collections.Generic;
using CanteenRush.Core;
using UnityEngine;
using UnityEngine.XR;
using Hand = CanteenRush.Core.Hand;
using InputDevice = UnityEngine.XR.InputDevice;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
namespace CanteenRush.Haptics
{
    /// Thin adapter onto XRI 3.1.2. Each Starter Assets controller ("Left Controller", "Right Controller")
    /// carries a HapticImpulsePlayer; we resolve one per Hand from the object names and call
    /// HapticImpulsePlayer.SendHapticImpulse(amplitude, duration), checked against the XRI 3.1 API reference.
    public sealed class XRHapticsService : HapticsServiceBase
    {
        [SerializeField] HapticImpulsePlayer _left;
        [SerializeField] HapticImpulsePlayer _right;
        [SerializeField] float _rescanSeconds = 2f;
        float _nextScan;
        bool _hasDevice;
        static readonly List<InputDevice> Devices = new List<InputDevice>(4);

        public override bool HasHapticDevice => _hasDevice;
        public Transform Anchor(Hand hand) { var p = hand == Hand.Left ? _left : _right; return p != null ? p.transform : null; }

        protected override void Update()
        {
            if (Time.unscaledTime >= _nextScan) { _nextScan = Time.unscaledTime + _rescanSeconds; Rescan(); }
            base.Update();
        }

        void Rescan()
        {
            if (_left == null || _right == null)
                foreach (var player in FindObjectsByType<HapticImpulsePlayer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    var hand = HandOf(player.transform);
                    if (hand == Hand.Left && _left == null) _left = player;
                    else if (hand == Hand.Right && _right == null) _right = player;
                }
            // XR Device Simulator controllers are Input System devices, not XR InputDevices,
            // so only real hardware that reports impulse support switches this on.
            _hasDevice = false;
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand, Devices);
            foreach (var d in Devices)
                if (d.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse) { _hasDevice = true; break; }
        }

        /// Walks up the hierarchy for "Left"/"Right" in an object name (Starter Assets naming).
        public static Hand? HandOf(Transform t)
        {
            for (; t != null; t = t.parent)
            {
                var n = t.name;
                if (n.IndexOf("Left", System.StringComparison.OrdinalIgnoreCase) >= 0) return Hand.Left;
                if (n.IndexOf("Right", System.StringComparison.OrdinalIgnoreCase) >= 0) return Hand.Right;
            }
            return null;
        }

        protected override bool SendToDevice(Hand hand, float amplitude, float duration)
        {
            var player = hand == Hand.Left ? _left : _right;
            return player != null && player.isActiveAndEnabled && player.SendHapticImpulse(amplitude, duration);
        }
    }
}

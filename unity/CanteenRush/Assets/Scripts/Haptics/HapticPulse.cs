using CanteenRush.Core;
using UnityEngine;
namespace CanteenRush.Haptics
{
    /// One impulse that left the service, for the debug HUD and the tests. A struct so logging allocates nothing.
    public readonly struct HapticPulse
    {
        public readonly Hand Hand;
        public readonly float Amplitude;
        public readonly float Duration;
        public readonly string Preset;   // null for raw Pulse()/Ramp() calls
        public readonly bool IsRamp;
        public readonly Color Color;
        public readonly bool Delivered;  // true if a physical controller accepted it

        public HapticPulse(Hand hand, float amplitude, float duration, string preset, bool isRamp, Color color, bool delivered)
        { Hand = hand; Amplitude = amplitude; Duration = duration; Preset = preset; IsRamp = isRamp; Color = color; Delivered = delivered; }
    }
}

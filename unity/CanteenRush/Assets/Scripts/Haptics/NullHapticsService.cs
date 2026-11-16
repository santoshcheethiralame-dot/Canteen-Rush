using CanteenRush.Core;
namespace CanteenRush.Haptics
{
    /// Editor/test backend: all the shared logic (rate limit, ramps, HUD events) runs, but no device is touched.
    public sealed class NullHapticsService : HapticsServiceBase
    {
        public override bool HasHapticDevice => false;
        protected override bool SendToDevice(Hand hand, float amplitude, float duration) => false;
    }
}

using CanteenRush.Core;
namespace CanteenRush.Haptics
{
    /// The only way anything in the game makes a controller vibrate (contracts §6).
    public interface IHapticsService
    {
        void Pulse(Hand hand, float amplitude, float durationSeconds);
        void Play(string presetName, Hand hand);    // looks up a HapticPreset
        void Ramp(Hand hand, float amplitude01);    // call every frame; the service coalesces and sends
        void StopRamp(Hand hand);
    }
}

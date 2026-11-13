using CanteenRush.Core;
using UnityEngine;
namespace CanteenRush.Haptics
{
    /// GameEvents -> haptic presets, exactly the MASTER_SPEC §11 table. A pure listener:
    /// subscribes in OnEnable, unsubscribes in OnDisable, never calls gameplay code.
    ///
    /// Hand rules where an event does not say which hand:
    /// - "last holder hand" (SocketSnap, Spill, DoorLatch/DoorStop) = the hand of the most recent ItemGrabbed/Released.
    /// - PumpPress = the hand NOT holding the cup (one hand holds the cup, the other presses the pump).
    /// - UiTick = the pointing hand, Right unless a UI script tells us otherwise via UiHand.
    public sealed class HapticsEventBinder : MonoBehaviour
    {
        [SerializeField] HapticsServiceBase _service;
        [SerializeField] float _trayWarnDeg = 20f;
        Hand _lastHand = Hand.Right, _cupHand = Hand.Left;
        public static Hand UiHand = Hand.Right;

        void Awake() { if (_service == null) _service = GetComponent<HapticsServiceBase>(); }
        public void SetService(HapticsServiceBase service) => _service = service;

        void OnEnable()
        {
            GameEvents.OnItemGrabbed += ItemGrabbed;
            GameEvents.OnItemReleased += ItemReleased;
            GameEvents.OnItemSocketed += ItemSocketed;
            GameEvents.OnItemDropped += ItemDropped;
            GameEvents.OnFridgeDoorMoved += DoorMoved;
            GameEvents.OnFridgeDoorLimit += DoorLimit;
            GameEvents.OnPumpActivated += Pump;
            GameEvents.OnCupFilling += CupFilling;
            GameEvents.OnCupFilled += CupFilled;
            GameEvents.OnTrayTilt += TrayTilt;
            GameEvents.OnOrderServed += Served;
            GameEvents.OnOrderExpired += Expired;
            GameEvents.OnUiHover += UiHover;
            GameEvents.OnUiClick += UiClick;
            GameEvents.OnRoundStateChanged += RoundChanged;
        }

        void OnDisable()
        {
            GameEvents.OnItemGrabbed -= ItemGrabbed;
            GameEvents.OnItemReleased -= ItemReleased;
            GameEvents.OnItemSocketed -= ItemSocketed;
            GameEvents.OnItemDropped -= ItemDropped;
            GameEvents.OnFridgeDoorMoved -= DoorMoved;
            GameEvents.OnFridgeDoorLimit -= DoorLimit;
            GameEvents.OnPumpActivated -= Pump;
            GameEvents.OnCupFilling -= CupFilling;
            GameEvents.OnCupFilled -= CupFilled;
            GameEvents.OnTrayTilt -= TrayTilt;
            GameEvents.OnOrderServed -= Served;
            GameEvents.OnOrderExpired -= Expired;
            GameEvents.OnUiHover -= UiHover;
            GameEvents.OnUiClick -= UiClick;
            GameEvents.OnRoundStateChanged -= RoundChanged;
        }

        static Hand Other(Hand h) => h == Hand.Left ? Hand.Right : Hand.Left;
        void Play(string preset, Hand hand) { if (_service != null) _service.Play(preset, hand); }
        void Both(string preset) { Play(preset, Hand.Left); Play(preset, Hand.Right); }

        void ItemGrabbed(ItemId item, Hand hand, Vector3 pos) { _lastHand = hand; Play(HapticPresetNames.Grab, hand); }
        void ItemReleased(ItemId item, Hand hand, Vector3 pos) { _lastHand = hand; Play(HapticPresetNames.Release, hand); }
        void ItemSocketed(ItemId item, Vector3 pos) => Play(HapticPresetNames.SocketSnap, _lastHand);
        void ItemDropped(ItemId item, float speed, Vector3 pos) => Play(HapticPresetNames.Spill, _lastHand);
        void DoorMoved(int door, float open01, bool detent) { if (detent) Play(HapticPresetNames.DoorLatch, _lastHand); }
        void DoorLimit(int door, bool atOpenLimit) { if (atOpenLimit) Play(HapticPresetNames.DoorStop, _lastHand); }
        void Pump(bool pressed, Vector3 pos)
        {
            if (pressed) Play(HapticPresetNames.PumpPress, Other(_cupHand));
            else if (_service != null) _service.StopRamp(_cupHand);
        }
        void CupFilling(float level, Hand hand)
        {
            _cupHand = hand;
            if (_service != null) _service.PlayRamp(HapticPresetNames.WaterFill, hand, level);
        }
        void CupFilled(Vector3 pos) { if (_service != null) _service.StopRamp(_cupHand); }
        void TrayTilt(float angle, Hand hand)
        {
            if (angle > _trayWarnDeg && _service != null) _service.PlayRamp(HapticPresetNames.TrayTiltWarn, hand, angle);
        }
        void Served(ServeResult r) => Both(r.Outcome == ServeOutcome.Perfect ? HapticPresetNames.ServeOk : HapticPresetNames.ServePartial);
        void Expired(Order o) => Both(HapticPresetNames.OrderExpired);
        void UiHover() => Play(HapticPresetNames.UiTick, UiHand);
        void UiClick() => Play(HapticPresetNames.UiTick, UiHand);
        void RoundChanged(RoundState s)
        {
            if (s == RoundState.Menu || s == RoundState.Results)
                if (_service != null) { _service.StopRamp(Hand.Left); _service.StopRamp(Hand.Right); }
        }
    }
}

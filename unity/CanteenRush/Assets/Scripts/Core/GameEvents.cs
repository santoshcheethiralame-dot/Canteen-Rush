using System;
using UnityEngine;

namespace CanteenRush.Core
{
    public static class GameEvents
    {
        // ---- Round / flow ----
        public static event Action<RoundState> OnRoundStateChanged;
        public static event Action<float> OnRoundTimeChanged;        // seconds remaining, fired at 10 Hz
        public static event Action<int> OnScoreChanged;              // new total (>= 0)

        // ---- Orders / customers ----
        public static event Action<Order> OnOrderPlaced;
        public static event Action<Order, float> OnOrderPatience;    // order, patience01, fired at 4 Hz while Waiting
        public static event Action<ServeResult> OnOrderServed;
        public static event Action<Order> OnOrderExpired;
        public static event Action<int, CustomerState> OnCustomerState; // seatIndex, state
        public static event Action<int> OnCustomerCount;             // active customers

        // ---- Items / hands ----
        public static event Action<ItemId, Hand, Vector3> OnItemGrabbed;      // item, hand, worldPos
        public static event Action<ItemId, Hand, Vector3> OnItemReleased;
        public static event Action<ItemId, Vector3> OnItemSocketed;           // placed in a tray slot
        public static event Action<ItemId, float, Vector3> OnItemDropped;     // item, impactSpeed m/s, worldPos (spill)
        public static event Action<ItemId, float, Vector3> OnItemImpact;      // non-spill collisions, for clinks
        public static event Action<ItemId, Vector3> OnItemSpawned;

        // ---- Stations ----
        public static event Action<int, float, bool> OnFridgeDoorMoved;       // doorIndex(0 upper,1 lower), open01, detentCrossed
        public static event Action<int, bool> OnFridgeDoorLimit;              // doorIndex, atOpenLimit(true)/closed(false)
        public static event Action<bool, Vector3> OnPumpActivated;            // pressed, worldPos
        public static event Action<float, Hand> OnCupFilling;                 // level01, hand holding cup (when known)
        public static event Action<Vector3> OnCupFilled;                      // became Water
        public static event Action<float, Hand> OnTrayTilt;                   // angleDeg, hand holding tray
        public static event Action<Vector3, bool> OnTrayServedPlaced;         // position, hadActiveOrder

        // ---- Player ----
        public static event Action<float> OnPlayerMoveSpeed;                  // m/s, for vignette & footsteps
        public static event Action<Surface, Vector3> OnFootstep;              // surface, position

        // ---- Tutorial ----
        public static event Action<int> OnTutorialStep;                       // 0..2 = active step, 3 = finished

        // ---- UI ----
        public static event Action OnUiHover;
        public static event Action OnUiClick;

        // Raising API (one line each; null-safe)
        public static void RoundStateChanged(RoundState s) => OnRoundStateChanged?.Invoke(s);
        public static void RoundTimeChanged(float t) => OnRoundTimeChanged?.Invoke(t);
        public static void ScoreChanged(int s) => OnScoreChanged?.Invoke(s);
        public static void OrderPlaced(Order o) => OnOrderPlaced?.Invoke(o);
        public static void OrderPatience(Order o, float p01) => OnOrderPatience?.Invoke(o, p01);
        public static void OrderServed(ServeResult r) => OnOrderServed?.Invoke(r);
        public static void OrderExpired(Order o) => OnOrderExpired?.Invoke(o);
        public static void CustomerState(int seat, CustomerState s) => OnCustomerState?.Invoke(seat, s);
        public static void CustomerCount(int n) => OnCustomerCount?.Invoke(n);
        public static void ItemGrabbed(ItemId i, Hand h, Vector3 p) => OnItemGrabbed?.Invoke(i, h, p);
        public static void ItemReleased(ItemId i, Hand h, Vector3 p) => OnItemReleased?.Invoke(i, h, p);
        public static void ItemSocketed(ItemId i, Vector3 p) => OnItemSocketed?.Invoke(i, p);
        public static void ItemDropped(ItemId i, float speed, Vector3 p) => OnItemDropped?.Invoke(i, speed, p);
        public static void ItemImpact(ItemId i, float speed, Vector3 p) => OnItemImpact?.Invoke(i, speed, p);
        public static void ItemSpawned(ItemId i, Vector3 p) => OnItemSpawned?.Invoke(i, p);
        public static void FridgeDoorMoved(int d, float open01, bool detent) => OnFridgeDoorMoved?.Invoke(d, open01, detent);
        public static void FridgeDoorLimit(int d, bool open) => OnFridgeDoorLimit?.Invoke(d, open);
        public static void PumpActivated(bool pressed, Vector3 p) => OnPumpActivated?.Invoke(pressed, p);
        public static void CupFilling(float level01, Hand h) => OnCupFilling?.Invoke(level01, h);
        public static void CupFilled(Vector3 p) => OnCupFilled?.Invoke(p);
        public static void TrayTilt(float angle, Hand h) => OnTrayTilt?.Invoke(angle, h);
        public static void TrayServedPlaced(Vector3 p, bool hadOrder) => OnTrayServedPlaced?.Invoke(p, hadOrder);
        public static void PlayerMoveSpeed(float v) => OnPlayerMoveSpeed?.Invoke(v);
        public static void Footstep(Surface s, Vector3 p) => OnFootstep?.Invoke(s, p);
        public static void TutorialStep(int step) => OnTutorialStep?.Invoke(step);
        public static void UiHover() => OnUiHover?.Invoke();
        public static void UiClick() => OnUiClick?.Invoke();

        public static void ResetAll() { OnRoundStateChanged = null; OnRoundTimeChanged = null; OnScoreChanged = null; OnOrderPlaced = null; OnOrderPatience = null; OnOrderServed = null; OnOrderExpired = null; OnCustomerState = null; OnCustomerCount = null; OnItemGrabbed = null; OnItemReleased = null; OnItemSocketed = null; OnItemDropped = null; OnItemImpact = null; OnItemSpawned = null; OnFridgeDoorMoved = null; OnFridgeDoorLimit = null; OnPumpActivated = null; OnCupFilling = null; OnCupFilled = null; OnTrayTilt = null; OnTrayServedPlaced = null; OnPlayerMoveSpeed = null; OnFootstep = null; OnTutorialStep = null; OnUiHover = null; OnUiClick = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad() => ResetAll();
    }
}

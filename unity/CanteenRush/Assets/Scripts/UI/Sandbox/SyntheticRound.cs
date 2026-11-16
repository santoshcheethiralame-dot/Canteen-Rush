using System.Collections;
using CanteenRush.Core;
using UnityEngine;
namespace CanteenRush.P3Sandbox
{
    /// A scripted round made only of GameEvents calls, so the listeners (audio, haptics, UI) can be exercised
    /// without P2's gameplay. Used by Sandbox_P3 (stand-in for GameManager) and by the PlayMode tests.
    /// It raises every gameplay event in GameEvents at least once and ends in Results (the UI events come from the shell).
    public static class SyntheticRound
    {
        public static int LastScore { get; private set; }
        public static bool Running { get; private set; }

        public static Vector3 SeatPosition(int seat) =>
            new Vector3((seat % 2 == 0 ? -1 : 1) * 2.4f, 0.45f, 9.5f + 1.25f * seat);   // nearest table first, like Main

        static Order MakeOrder(int id, int seat, params ItemId[] items) => new Order
        {
            Id = id, SeatIndex = seat, SeatPosition = SeatPosition(seat), Items = items,
            PatienceMax = 25f + 15f * items.Length, PatienceRemaining = 25f + 15f * items.Length
        };

        static object Wait(float s) => s > 0 ? new WaitForSeconds(s) : null;

        /// step = seconds between beats (0 = one frame per beat, for tests). includeTutorial = start with the 3 hints.
        public static IEnumerator Play(float step, bool includeTutorial = true)
        {
            Running = true;
            int score = 0;
            var counter = Vector3.zero + Vector3.up;
            GameEvents.RoundStateChanged(RoundState.Menu);
            yield return Wait(step);

            if (includeTutorial)
            {
                GameEvents.RoundStateChanged(RoundState.Tutorial);
                var practice = MakeOrder(1000, 0, ItemId.Donut);
                GameEvents.OrderPlaced(practice);
                GameEvents.TutorialStep(0); yield return Wait(step * 2);
                GameEvents.ItemGrabbed(ItemId.Donut, Hand.Right, counter);
                GameEvents.TutorialStep(1); yield return Wait(step * 2);
                GameEvents.ItemSocketed(ItemId.Donut, counter);
                GameEvents.TutorialStep(2); yield return Wait(step * 2);
                GameEvents.TrayServedPlaced(practice.SeatPosition, true);
                GameEvents.OrderServed(new ServeResult(practice.Id, ServeOutcome.Perfect, 1, 0, 0, 150, 50));
                GameEvents.TutorialStep(3); yield return Wait(step * 2);
                GameEvents.ScoreChanged(0);
            }

            GameEvents.RoundStateChanged(RoundState.Wave1);
            GameEvents.ScoreChanged(score);
            float remaining = 180f;
            GameEvents.RoundTimeChanged(remaining);

            // ---- Wave 1: single items, fridge, water ----
            var o1 = MakeOrder(1, 1, ItemId.Cola);
            GameEvents.CustomerState(1, CustomerState.Arriving); GameEvents.CustomerCount(1);
            GameEvents.CustomerState(1, CustomerState.Ordering); GameEvents.OrderPlaced(o1); GameEvents.CustomerState(1, CustomerState.Waiting);
            yield return Wait(step);
            GameEvents.FridgeDoorMoved(0, 0.05f, false); GameEvents.FridgeDoorMoved(0, 0.08f, true); yield return Wait(step);
            GameEvents.FridgeDoorMoved(0, 0.95f, false); GameEvents.FridgeDoorLimit(0, true); yield return Wait(step);
            GameEvents.ItemSpawned(ItemId.Cola, counter);
            GameEvents.ItemGrabbed(ItemId.Cola, Hand.Left, counter); yield return Wait(step);
            GameEvents.FridgeDoorMoved(0, 0.07f, true); GameEvents.FridgeDoorLimit(0, false);
            GameEvents.ItemSocketed(ItemId.Cola, counter);
            GameEvents.ItemImpact(ItemId.Cola, 1.5f, counter);
            GameEvents.OrderPatience(o1, 0.8f);
            GameEvents.PlayerMoveSpeed(1.8f);
            for (int i = 0; i < 4; i++) { GameEvents.Footstep(i < 2 ? Surface.Tile : Surface.Stone, counter); yield return Wait(step * 0.5f); }
            GameEvents.PlayerMoveSpeed(0f);
            GameEvents.TrayServedPlaced(o1.SeatPosition, true);
            score += 100 + 40; GameEvents.OrderServed(new ServeResult(1, ServeOutcome.Perfect, 1, 0, 0, 140, 40)); GameEvents.ScoreChanged(score);
            GameEvents.CustomerState(1, CustomerState.Eating); yield return Wait(step);
            GameEvents.CustomerState(1, CustomerState.Leaving); GameEvents.CustomerCount(0);

            var o2 = MakeOrder(2, 4, ItemId.Water);
            GameEvents.OrderPlaced(o2); GameEvents.CustomerCount(1);
            GameEvents.ItemGrabbed(ItemId.Water, Hand.Left, counter);
            GameEvents.PumpActivated(true, counter + Vector3.right);
            for (float level = 0; level <= 1.001f; level += 0.125f) { GameEvents.CupFilling(level, Hand.Left); yield return Wait(step * 0.25f); }
            GameEvents.CupFilled(counter); GameEvents.PumpActivated(false, counter + Vector3.right);
            GameEvents.ItemReleased(ItemId.Water, Hand.Left, counter);
            remaining = 120f; GameEvents.RoundTimeChanged(remaining);

            // ---- Wave 2: combos, a tilt and a spill ----
            GameEvents.RoundStateChanged(RoundState.Wave2);
            var o3 = MakeOrder(3, 6, ItemId.Donut, ItemId.Shake);
            GameEvents.OrderPlaced(o3); GameEvents.CustomerCount(2);
            GameEvents.ItemGrabbed(ItemId.Shake, Hand.Right, counter);
            for (float a = 18f; a <= 36f; a += 3f) { GameEvents.TrayTilt(a, Hand.Right); yield return Wait(step * 0.25f); }
            GameEvents.ItemDropped(ItemId.Shake, 3.2f, counter + Vector3.down);
            score = Mathf.Max(0, score - 10); GameEvents.ScoreChanged(score);
            score += 100 + 30; GameEvents.OrderServed(new ServeResult(2, ServeOutcome.Partial, 1, 0, 0, 130, 30)); GameEvents.ScoreChanged(score);
            GameEvents.OrderPatience(o3, 0.2f);
            remaining = 60f; GameEvents.RoundTimeChanged(remaining);

            // ---- Wave 3: three customers at once, one expires, one wrong item ----
            GameEvents.RoundStateChanged(RoundState.Wave3);
            var o4 = MakeOrder(4, 7, ItemId.Cake, ItemId.Water);
            var o5 = MakeOrder(5, 2, ItemId.Donut, ItemId.Cola, ItemId.Cake);
            GameEvents.OrderPlaced(o4); GameEvents.OrderPlaced(o5); GameEvents.CustomerCount(4);
            yield return Wait(step);
            GameEvents.OrderExpired(o3); GameEvents.CustomerState(6, CustomerState.Angry);
            score = Mathf.Max(0, score - 50); GameEvents.ScoreChanged(score);
            score += 275 + 25; GameEvents.OrderServed(new ServeResult(4, ServeOutcome.Perfect, 2, 0, 0, 300, 25)); GameEvents.ScoreChanged(score);
            score += 175; GameEvents.OrderServed(new ServeResult(5, ServeOutcome.Partial, 2, 1, 1, 175, 0)); GameEvents.ScoreChanged(score);
            GameEvents.TrayServedPlaced(SeatPosition(3), false);
            for (float t = 10f; t >= 0f; t -= 2.5f) { GameEvents.RoundTimeChanged(t); yield return Wait(step * 0.25f); }
            GameEvents.CustomerCount(0);
            GameEvents.RoundTimeChanged(0f);
            GameEvents.RoundStateChanged(RoundState.Results);
            LastScore = score;
            Running = false;
        }

        /// Expected tallies of Play() for tests.
        public const int ExpectedServed = 4, ExpectedSpills = 1, ExpectedExpired = 1, ExpectedCorrect = 6, ExpectedWrong = 1;
    }
}

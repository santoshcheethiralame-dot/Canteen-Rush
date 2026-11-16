using CanteenRush.Core;
namespace CanteenRush.UI
{
    /// Plain tally of one round, filled from GameEvents by ResultsBoard. Separate so it is unit-testable.
    public sealed class RoundStats
    {
        public int Score, Correct, Wrong, Spills, Expired, Served, Perfect;

        public void Reset() { Score = Correct = Wrong = Spills = Expired = Served = Perfect = 0; }

        public void Add(ServeResult r)
        {
            Served++;
            Correct += r.Correct;
            Wrong += r.Wrong;
            if (r.Outcome == ServeOutcome.Perfect) Perfect++;
        }

        public static int Stars(int score, int[] thresholds)
        {
            int stars = 0;
            if (thresholds != null) foreach (var t in thresholds) if (score >= t) stars++;
            return stars;
        }
    }
}

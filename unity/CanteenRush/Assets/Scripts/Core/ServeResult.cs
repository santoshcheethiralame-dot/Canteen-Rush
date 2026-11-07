namespace CanteenRush.Core
{
    public readonly struct ServeResult
    {
        public readonly int OrderId;
        public readonly ServeOutcome Outcome;
        public readonly int Correct;      // items that matched the order
        public readonly int Wrong;        // items on tray that were not requested (or surplus duplicates)
        public readonly int Missing;      // requested items not on tray
        public readonly int Points;       // net points awarded (>= -infinity; round total is floored at 0)
        public readonly int SpeedBonus;
        public ServeResult(int orderId, ServeOutcome outcome, int correct, int wrong, int missing, int points, int speedBonus)
        { OrderId = orderId; Outcome = outcome; Correct = correct; Wrong = wrong; Missing = missing; Points = points; SpeedBonus = speedBonus; }
    }
}

using System.Collections.Generic;
using UnityEngine;
namespace CanteenRush.Core
{
    public static class ScoreCalculator
    {
        public static ServeResult Score(Order order, IReadOnlyList<ItemId> onTray, float patience01, GameConfig cfg)
        {
            var match = OrderMatcher.Evaluate(order.Items, onTray);
            bool empty = match.correct + match.wrong == 0;
            var outcome = empty ? ServeOutcome.Empty : match.wrong == 0 && match.missing == 0 && match.correct > 0 ? ServeOutcome.Perfect : ServeOutcome.Partial;
            int bonus = match.correct > 0 ? Mathf.RoundToInt(cfg.SpeedBonusMax * Mathf.Clamp01(patience01)) : 0;
            float multiplier = outcome == ServeOutcome.Perfect && match.correct >= 2 ? 1f + cfg.ComboPerExtraItem * (match.correct - 1) : 1f;
            int points = Mathf.RoundToInt(match.correct * cfg.PointsPerItem * multiplier) + bonus - match.wrong * cfg.WrongItemPenalty;
            return new ServeResult(order.Id, outcome, match.correct, match.wrong, match.missing, points, bonus);
        }
        public static int Spill(GameConfig cfg) => -cfg.SpillPenalty;
        public static int Timeout(GameConfig cfg) => -cfg.TimeoutPenalty;
        public static int Stars(int score, GameConfig cfg)
        {
            int stars = 0;
            for (int i = 0; i < cfg.StarThresholds.Length; i++) if (score >= cfg.StarThresholds[i]) stars++;
            return stars;
        }
    }
}

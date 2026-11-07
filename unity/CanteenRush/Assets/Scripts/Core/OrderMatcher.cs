using System.Collections.Generic;
namespace CanteenRush.Core
{
    public static class OrderMatcher
    {
        public static (int correct, int wrong, int missing) Evaluate(IReadOnlyList<ItemId> requested, IReadOnlyList<ItemId> onTray)
        {
            int correct = 0, wrong = 0, missing = 0;
            // The menu is a small fixed enum; scanning avoids allocations in serving paths.
            for (int id = (int)ItemId.Donut; id <= (int)ItemId.Water; id++)
            {
                int need = 0, have = 0;
                for (int i = 0; i < requested.Count; i++) if ((int)requested[i] == id) need++;
                for (int i = 0; i < onTray.Count; i++) if ((int)onTray[i] == id) have++;
                int matched = System.Math.Min(need, have);
                correct += matched; wrong += have - matched; missing += need - matched;
            }
            return (correct, wrong, missing);
        }
    }
}

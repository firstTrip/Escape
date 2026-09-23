using System;

namespace ChannelZero.Runtime.Core
{
    public static class NarrativeConditionEvaluator
    {
        public static bool Evaluate(string expression, NarrativeTextContext context)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return true;
            if (context == null)
                return false;

            string[] clauses = expression.Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (string rawClause in clauses)
            {
                string clause = rawClause.Trim();
                int colon = clause.IndexOf(':');
                int equals = clause.IndexOf('=');
                if (colon <= 0 || equals <= colon + 1)
                    return false;

                string kind = clause[..colon].Trim();
                string key = clause[(colon + 1)..equals].Trim();
                string expected = clause[(equals + 1)..].Trim();
                if (!EvaluateClause(kind, key, expected, context))
                    return false;
            }
            return true;
        }

        private static bool EvaluateClause(string kind, string key, string expected, NarrativeTextContext context)
        {
            switch (kind.ToLowerInvariant())
            {
                case "flag":
                    return bool.TryParse(expected, out bool expectedFlag) &&
                        context.TryGetFlag(key, out bool actualFlag) && actualFlag == expectedFlag;
                case "item":
                    return bool.TryParse(expected, out bool expectedItem) && context.HasItem(key) == expectedItem;
                case "slot":
                    return context.TryGetSlot(key, out string slot) &&
                        string.Equals(slot, expected, StringComparison.OrdinalIgnoreCase);
                case "result":
                    return context.TryGetResult(key, out string result) &&
                        string.Equals(result, expected, StringComparison.OrdinalIgnoreCase);
                default:
                    return false;
            }
        }
    }
}

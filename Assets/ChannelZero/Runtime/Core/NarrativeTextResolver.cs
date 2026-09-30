using System;
using System.Collections.Generic;
using System.Linq;

namespace ChannelZero.Runtime.Core
{
    public sealed class NarrativeTextResolver
    {
        private static readonly string[] TypeOrder = { "description", "document", "monologue", "dialogue", "feedback" };
        private readonly NarrativeTextCatalog catalog;
        public string Locale => catalog.Locale;

        public NarrativeTextResolver(NarrativeTextCatalog catalog)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public IReadOnlyList<NarrativeTextEntry> Resolve(string room, string target, ChannelEra era,
            string trigger, NarrativeTextContext context)
        {
            IEnumerable<NarrativeTextEntry> candidates = catalog.Entries.Where(entry =>
                string.Equals(entry.room, room, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(entry.target, target, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(entry.trigger, trigger, StringComparison.OrdinalIgnoreCase) &&
                (string.Equals(entry.era, "ANY", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(entry.era, ((int)era).ToString(), StringComparison.OrdinalIgnoreCase)) &&
                (!entry.once || !context.Session.HasSeenText(entry.id)) &&
                NarrativeConditionEvaluator.Evaluate(entry.condition, context));

            List<NarrativeTextEntry> resolved = new();
            foreach (string type in TypeOrder)
            {
                NarrativeTextEntry selected = candidates
                    .Where(entry => string.Equals(entry.type, type, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(entry => entry.priority)
                    .ThenBy(entry => entry.id, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (selected != null)
                    resolved.Add(selected);
            }
            return resolved;
        }

        public IReadOnlyList<NarrativeTextEntry> ResolveById(string textId, NarrativeTextContext context)
        {
            if (string.IsNullOrWhiteSpace(textId) ||
                !catalog.TryGetById(textId, out NarrativeTextEntry entry) ||
                (entry.once && context.Session.HasSeenText(entry.id)) ||
                !NarrativeConditionEvaluator.Evaluate(entry.condition, context))
                return Array.Empty<NarrativeTextEntry>();
            return new[] { entry };
        }

        public string ResolveLocalizedText(string textId, string fallback = "")
        {
            return !string.IsNullOrWhiteSpace(textId) &&
                   catalog.TryGetById(textId, out NarrativeTextEntry entry) &&
                   !string.IsNullOrWhiteSpace(entry.text)
                ? entry.text
                : fallback ?? string.Empty;
        }
    }
}

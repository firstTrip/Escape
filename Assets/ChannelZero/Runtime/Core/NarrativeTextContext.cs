using System;
using System.Collections.Generic;

namespace ChannelZero.Runtime.Core
{
    public sealed class NarrativeTextContext
    {
        private readonly Dictionary<string, bool> flags = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> items = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> slots = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> results = new(StringComparer.OrdinalIgnoreCase);

        public ChannelZeroSessionState Session { get; }

        public NarrativeTextContext(ChannelZeroSessionState session)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            foreach (string itemId in session.inventoryItemIds)
                items.Add(itemId);
            foreach (PuzzleStateEntry entry in session.puzzleStates)
            {
                if (entry?.puzzleId == null)
                    continue;
                if (entry.puzzleId.StartsWith("flag:", StringComparison.OrdinalIgnoreCase) &&
                    bool.TryParse(entry.stateId, out bool flagValue))
                    flags[entry.puzzleId[5..]] = flagValue;
                else if (entry.puzzleId.StartsWith("slot:", StringComparison.OrdinalIgnoreCase))
                    slots[entry.puzzleId[5..]] = entry.stateId;
                else if (entry.puzzleId.StartsWith("result:", StringComparison.OrdinalIgnoreCase))
                    results[entry.puzzleId[7..]] = entry.stateId;
            }
        }

        public NarrativeTextContext SetFlag(string key, bool value) { flags[key] = value; return this; }
        public NarrativeTextContext SetItem(string key, bool value = true) { if (value) items.Add(key); else items.Remove(key); return this; }
        public NarrativeTextContext SetSlot(string key, string value) { slots[key] = value; return this; }
        public NarrativeTextContext SetResult(string key, string value) { results[key] = value; return this; }

        public bool TryGetFlag(string key, out bool value)
        {
            if (flags.TryGetValue(key, out value))
                return true;
            value = false;
            return true;
        }
        public bool HasItem(string key) => items.Contains(key) || Session.HasItem(key);
        public bool TryGetSlot(string key, out string value)
        {
            if (slots.TryGetValue(key, out value))
                return true;
            value = "empty";
            return true;
        }
        public bool TryGetResult(string key, out string value) => results.TryGetValue(key, out value);
    }
}

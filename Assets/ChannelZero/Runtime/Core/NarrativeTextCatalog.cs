using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChannelZero.Runtime.Core
{
    public sealed class NarrativeTextCatalog
    {
        private readonly List<NarrativeTextEntry> entries;
        private readonly Dictionary<string, NarrativeTextEntry> byId;

        public int SchemaVersion { get; }
        public string Locale { get; }
        public IReadOnlyList<NarrativeTextEntry> Entries => entries;

        private NarrativeTextCatalog(NarrativeTextFile file)
        {
            SchemaVersion = file.schemaVersion;
            Locale = file.locale;
            entries = file.entries ?? new List<NarrativeTextEntry>();
            byId = new Dictionary<string, NarrativeTextEntry>(StringComparer.Ordinal);
            foreach (NarrativeTextEntry entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.id))
                    throw new FormatException("Narrative entry ID is required.");
                if (!byId.TryAdd(entry.id, entry))
                    throw new FormatException($"Duplicate narrative entry ID: {entry.id}");
            }
        }

        public static NarrativeTextCatalog FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Narrative JSON is empty.", nameof(json));

            NarrativeTextFile file = JsonUtility.FromJson<NarrativeTextFile>(json);
            if (file == null || file.schemaVersion != 1 || file.entries == null)
                throw new FormatException("Unsupported or invalid narrative JSON.");
            return new NarrativeTextCatalog(file);
        }

        public bool TryGetById(string id, out NarrativeTextEntry entry) => byId.TryGetValue(id, out entry);
    }
}

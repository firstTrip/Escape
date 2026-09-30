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

        public static NarrativeTextCatalog Combine(NarrativeTextCatalog preferred,
            NarrativeTextCatalog fallback, Func<NarrativeTextEntry, bool> includeFallback)
        {
            if (preferred == null) throw new ArgumentNullException(nameof(preferred));
            NarrativeTextFile file = new() { schemaVersion = 1, locale = preferred.Locale };
            file.entries.AddRange(preferred.entries);
            HashSet<string> ids = new(preferred.entries.ConvertAll(entry => entry.id), StringComparer.Ordinal);
            if (fallback != null)
                foreach (NarrativeTextEntry entry in fallback.entries)
                    if ((includeFallback?.Invoke(entry) ?? true) && ids.Add(entry.id)) file.entries.Add(entry);
            return new NarrativeTextCatalog(file);
        }

        public static NarrativeTextCatalog LoadDefault()
        {
            return Load("ko-KR");
        }

        public static NarrativeTextCatalog Load(string localeCode)
        {
            string requested = string.IsNullOrWhiteSpace(localeCode) ? "ko-KR" : localeCode;
            TextAsset legacyAsset = Resources.Load<TextAsset>($"ChannelZero/Data/narrative_{requested}.v1");
            TextAsset chapterOneAsset = Resources.Load<TextAsset>($"ChannelZero/Data/narrative_chapter1_v2_{requested}");
            if ((legacyAsset == null || chapterOneAsset == null) && requested != "ko-KR")
            {
                legacyAsset = Resources.Load<TextAsset>("ChannelZero/Data/narrative_ko-KR.v1");
                chapterOneAsset = Resources.Load<TextAsset>("ChannelZero/Data/narrative_chapter1_v2_ko-KR");
            }
            if (legacyAsset == null || chapterOneAsset == null)
                throw new InvalidOperationException("Narrative definition resource is missing.");
            return Combine(FromJson(chapterOneAsset.text), FromJson(legacyAsset.text),
                entry => !string.Equals(entry.chapter, "CH1", StringComparison.OrdinalIgnoreCase));
        }

        public bool TryGetById(string id, out NarrativeTextEntry entry) => byId.TryGetValue(id, out entry);
    }
}

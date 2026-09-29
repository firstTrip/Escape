using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChannelZero.Runtime.Core
{
    public sealed class PuzzleDefinitionCatalog
    {
        private readonly Dictionary<string, PuzzleDefinition> puzzlesByCloseup = new(StringComparer.Ordinal);
        private readonly Dictionary<string, InventoryItemDefinition> itemsById = new(StringComparer.Ordinal);

        public string Locale { get; private set; } = "ko-KR";
        public IReadOnlyCollection<PuzzleDefinition> Puzzles => puzzlesByCloseup.Values;

        public static PuzzleDefinitionCatalog FromJson(string json)
        {
            PuzzleDefinitionFile file = JsonUtility.FromJson<PuzzleDefinitionFile>(json);
            if (file == null || file.schemaVersion != 1)
                throw new FormatException("Unsupported puzzle definition JSON.");
            PuzzleDefinitionCatalog catalog = new() { Locale = file.locale };
            foreach (InventoryItemDefinition item in file.items)
            {
                if (string.IsNullOrWhiteSpace(item.id) || !catalog.itemsById.TryAdd(item.id, item))
                    throw new FormatException($"Invalid or duplicate item ID: {item?.id}");
            }
            foreach (PuzzleDefinition puzzle in file.puzzles)
            {
                if (string.IsNullOrWhiteSpace(puzzle.RuntimePuzzleId) || string.IsNullOrWhiteSpace(puzzle.closeupId) ||
                    string.IsNullOrWhiteSpace(puzzle.hotspotId) || !IsPlayableEra(puzzle.activeEra) ||
                    !catalog.puzzlesByCloseup.TryAdd(puzzle.closeupId, puzzle))
                    throw new FormatException($"Invalid or duplicate puzzle closeup: {puzzle?.closeupId}");
                puzzle.id = puzzle.RuntimePuzzleId;
                puzzle.puzzleId = puzzle.RuntimePuzzleId;
                puzzle.prerequisiteFlags ??= Array.Empty<string>();
                puzzle.inactiveTextIdByEra ??= Array.Empty<EraTextIdEntry>();
                puzzle.resultStateByEra ??= Array.Empty<EraResultStateEntry>();
            }
            if (file.puzzles.GroupBy(puzzle => puzzle.RuntimePuzzleId, StringComparer.Ordinal)
                .Any(group => group.Select(puzzle => puzzle.activeEra).Distinct().Count() != 1))
                throw new FormatException("Every puzzle ID must have exactly one active era.");
            return catalog;
        }

        public static PuzzleDefinitionCatalog Combine(PuzzleDefinitionCatalog preferred,
            PuzzleDefinitionCatalog fallback, Func<PuzzleDefinition, bool> includeFallback)
        {
            if (preferred == null) throw new ArgumentNullException(nameof(preferred));
            PuzzleDefinitionCatalog combined = new() { Locale = preferred.Locale };
            foreach (InventoryItemDefinition item in preferred.itemsById.Values)
                combined.itemsById[item.id] = item;
            foreach (PuzzleDefinition puzzle in preferred.puzzlesByCloseup.Values)
                combined.puzzlesByCloseup[puzzle.closeupId] = puzzle;
            if (fallback == null) return combined;
            foreach (InventoryItemDefinition item in fallback.itemsById.Values)
                if (!combined.itemsById.ContainsKey(item.id)) combined.itemsById[item.id] = item;
            foreach (PuzzleDefinition puzzle in fallback.puzzlesByCloseup.Values)
                if ((includeFallback?.Invoke(puzzle) ?? true) && !combined.puzzlesByCloseup.ContainsKey(puzzle.closeupId))
                    combined.puzzlesByCloseup[puzzle.closeupId] = puzzle;
            return combined;
        }

        public static PuzzleDefinitionCatalog LoadDefault()
        {
            return Load("ko-KR");
        }

        public static PuzzleDefinitionCatalog Load(string localeCode)
        {
            string requested = string.IsNullOrWhiteSpace(localeCode) ? "ko-KR" : localeCode;
            TextAsset legacyAsset = Resources.Load<TextAsset>($"ChannelZero/Data/puzzles_{requested}.v1");
            TextAsset chapterOneAsset = Resources.Load<TextAsset>($"ChannelZero/Data/puzzles_chapter1_v2_{requested}");
            if ((legacyAsset == null || chapterOneAsset == null) && requested != "ko-KR")
            {
                legacyAsset = Resources.Load<TextAsset>("ChannelZero/Data/puzzles_ko-KR.v1");
                chapterOneAsset = Resources.Load<TextAsset>("ChannelZero/Data/puzzles_chapter1_v2_ko-KR");
            }
            if (legacyAsset == null || chapterOneAsset == null)
                throw new InvalidOperationException("Puzzle definition resource is missing.");
            return Combine(FromJson(chapterOneAsset.text), FromJson(legacyAsset.text),
                puzzle => puzzle.chapterTwo ||
                    puzzle.RuntimePuzzleId.StartsWith("ch2.", StringComparison.Ordinal));
        }

        public bool TryGetPuzzle(string closeupId, out PuzzleDefinition definition) => puzzlesByCloseup.TryGetValue(closeupId, out definition);
        public bool TryGetItem(string itemId, out InventoryItemDefinition definition) => itemsById.TryGetValue(itemId, out definition);

        private static bool IsPlayableEra(int era) => era == 1961 || era == 1981 || era == 2001 || era == 2021;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChannelZero.Runtime.Core
{
    public static class ChannelZeroContentGraphValidator
    {
        private static readonly ChannelEra[] Eras =
        {
            ChannelEra.Year1961, ChannelEra.Year1981, ChannelEra.Year2001, ChannelEra.Year2021,
        };

        public static IReadOnlyList<string> Validate(IEnumerable<string> sceneHotspotIds,
            PuzzleDefinitionCatalog puzzles, ChannelZeroInteractionCatalog interactions,
            ChannelZeroHotspotLayoutCatalog layouts, NarrativeTextCatalog narratives,
            ChannelZeroCloseupCatalog closeups)
        {
            List<string> issues = new();
            HashSet<string> sceneIds = new(sceneHotspotIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            HashSet<string> routedIds = interactions.Routes.Select(route => route.interactionId)
                .ToHashSet(StringComparer.Ordinal);
            foreach (string hotspotId in sceneIds)
                if (!routedIds.Contains(hotspotId))
                    issues.Add($"Route missing for scene hotspot: {hotspotId}");

            foreach (PuzzleDefinition puzzle in puzzles.Puzzles)
            {
                if (!routedIds.Contains(puzzle.hotspotId))
                    issues.Add($"Puzzle route missing: {puzzle.RuntimePuzzleId}/{puzzle.hotspotId}");
                if (!layouts.TryResolve(puzzle.hotspotId, puzzle.ActiveEra, out HotspotLayoutDefinition activeLayout) ||
                    !activeLayout.enabled)
                    issues.Add($"Active layout missing or disabled: {puzzle.RuntimePuzzleId}/{puzzle.activeEra}");
                foreach (EraTextIdEntry entry in puzzle.inactiveTextIdByEra ?? Array.Empty<EraTextIdEntry>())
                    if (!narratives.TryGetById(entry.textId, out _))
                        issues.Add($"Inactive narrative missing: {puzzle.RuntimePuzzleId}/{entry.textId}");
                foreach (EraResultStateEntry result in puzzle.resultStateByEra ?? Array.Empty<EraResultStateEntry>())
                    if (string.IsNullOrWhiteSpace(result.stateId))
                        issues.Add($"Empty result state: {puzzle.RuntimePuzzleId}/{result.era}");
                if (!closeups.TryGetArtwork(puzzle.closeupId, puzzle.openArtwork, puzzle.ActiveEra, out _))
                    issues.Add($"Active artwork missing: {puzzle.closeupId}/{puzzle.activeEra}/{puzzle.openArtwork}");
            }

            foreach (InteractionRouteDefinition route in interactions.Routes)
                if (route.ActionType == InteractionActionType.OpenCloseup &&
                    !puzzles.TryGetPuzzle(route.closeupId, out _) && route.closeupId != ChannelZeroIds.PrologueServiceRequestCloseup)
                    issues.Add($"Closeup route has no puzzle definition: {route.interactionId}/{route.closeupId}");

            foreach (IGrouping<string, HotspotLayoutDefinition> eraGroup in layouts.Layouts
                .Where(layout => layout.enabled && layout.era != 0 && sceneIds.Contains(layout.interactionId))
                .GroupBy(layout => RoomPrefix(layout.interactionId) + ":" + layout.era))
            {
                HotspotLayoutDefinition[] entries = eraGroup.ToArray();
                for (int i = 0; i < entries.Length; i++)
                    for (int j = i + 1; j < entries.Length; j++)
                    {
                        Rect a = entries[i].NormalizedTopLeftRect;
                        Rect b = entries[j].NormalizedTopLeftRect;
                        Rect overlap = Rect.MinMaxRect(Mathf.Max(a.xMin, b.xMin), Mathf.Max(a.yMin, b.yMin),
                            Mathf.Min(a.xMax, b.xMax), Mathf.Min(a.yMax, b.yMax));
                        if (overlap.width <= 0f || overlap.height <= 0f)
                            continue;
                        float ratio = overlap.width * overlap.height / Mathf.Min(a.width * a.height, b.width * b.height);
                        if (ratio >= 0.2f)
                            issues.Add($"Hotspot overlap {eraGroup.Key}: {entries[i].interactionId}/{entries[j].interactionId} ({ratio:P0})");
                    }
            }
            return issues.Distinct().ToArray();
        }

        private static string RoomPrefix(string interactionId)
        {
            int separator = interactionId?.IndexOf('_') ?? -1;
            return separator > 0 ? interactionId.Substring(0, separator) : interactionId ?? string.Empty;
        }
    }
}

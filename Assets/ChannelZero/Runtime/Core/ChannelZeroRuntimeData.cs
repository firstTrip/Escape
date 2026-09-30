using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChannelZero.Runtime.Core
{
    [Serializable]
    public sealed class TimelineSnapshot
    {
        public string snapshotId;
        public string roomId;
        public ChannelEra era;
        public string visualStateId;
        public string scenarioStageId;
        public string selectedInventoryItemId;
        public List<string> inventoryItemIds = new();
        public List<PuzzleStateEntry> puzzleStates = new();

        public TimelineSnapshot Clone()
        {
            TimelineSnapshot clone = new()
            {
                snapshotId = snapshotId,
                roomId = roomId,
                era = era,
                visualStateId = visualStateId,
                scenarioStageId = scenarioStageId,
                selectedInventoryItemId = selectedInventoryItemId,
            };
            clone.inventoryItemIds.AddRange(inventoryItemIds ?? new List<string>());
            foreach (PuzzleStateEntry entry in puzzleStates)
                clone.puzzleStates.Add(entry.Clone());
            return clone;
        }
    }

    [Serializable]
    public sealed class InventoryItemDefinition
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public bool canHold = true;
    }

    [Serializable]
    public sealed class EraTextIdEntry
    {
        public int era;
        public string textId;
    }

    [Serializable]
    public sealed class EraResultStateEntry
    {
        public int era;
        public string stateId;
    }

    [Serializable]
    public sealed class PuzzleDefinition
    {
        public string id;
        public string puzzleId;
        public string closeupId;
        public int activeEra;
        public string hotspotId;
        public string[] prerequisiteFlags = Array.Empty<string>();
        public string solvedFlag;
        public EraTextIdEntry[] inactiveTextIdByEra = Array.Empty<EraTextIdEntry>();
        public EraResultStateEntry[] resultStateByEra = Array.Empty<EraResultStateEntry>();
        public string ruleType;
        public string title;
        [TextArea] public string instruction;
        public string answer;
        public string clueFlag;
        public string completeFlag;
        public bool chapterTwo;
        public int[] textOnlyEras = Array.Empty<int>();
        public bool allowInactiveEraReview;
        public EraResultStateEntry[] reviewArtworkStateByEra = Array.Empty<EraResultStateEntry>();
        public string openArtwork = "default";
        public string completeArtwork = "default";
        public string[] lootItems = Array.Empty<string>();
        public string[] lootLabels = Array.Empty<string>();
        [TextArea] public string[] hints = Array.Empty<string>();

        public string RuntimePuzzleId => string.IsNullOrWhiteSpace(puzzleId) ? id : puzzleId;
        public ChannelEra ActiveEra => (ChannelEra)activeEra;
        public bool UsesTextOnlyReview(ChannelEra era) =>
            Array.IndexOf(textOnlyEras ?? Array.Empty<int>(), (int)era) >= 0;
        public string ReviewArtworkState(ChannelEra era) =>
            Array.Find(reviewArtworkStateByEra ?? Array.Empty<EraResultStateEntry>(),
                entry => entry.era == (int)era)?.stateId ?? ChannelZeroIds.DefaultVisualState;

        public string InactiveTextId(ChannelEra era) =>
            Array.Find(inactiveTextIdByEra ?? Array.Empty<EraTextIdEntry>(), entry => entry.era == (int)era)?.textId
            ?? string.Empty;

        public string ResultState(ChannelEra era) =>
            Array.Find(resultStateByEra ?? Array.Empty<EraResultStateEntry>(), entry => entry.era == (int)era)?.stateId
            ?? string.Empty;
    }

    [Serializable]
    internal sealed class PuzzleDefinitionFile
    {
        public int schemaVersion;
        public string locale;
        public List<InventoryItemDefinition> items = new();
        public List<PuzzleDefinition> puzzles = new();
    }
}

using System;
using UnityEngine;

namespace ChannelZero.Runtime.Core
{
    public sealed class ChannelZeroGameContext
    {
        public ChannelZeroSessionState State { get; }
        public ChannelZeroSaveService Save { get; }
        public PuzzleDefinitionCatalog PuzzleCatalog { get; }
        public ChannelZeroInteractionCatalog InteractionCatalog { get; }
        public ChannelZeroPuzzleService Puzzles { get; }
        public ChannelZeroInventoryService Inventory { get; }
        public ChannelZeroRoomNavigationService Navigation { get; }
        public ChannelZeroChapterProgressionService Progression { get; }
        public ChannelZeroScenarioProgressionService Scenario { get; }
        public ChannelZeroProgressGuidanceService Guidance { get; }
        public ChannelZeroTimelineService Timeline { get; }
        public ChannelZeroComparisonOverlayService Comparison { get; }
        public ChannelZeroHintService Hints { get; }
        public ChannelZeroInteractionRouter Interactions { get; }
        public ChannelZeroOperationService Operations { get; }
        public NarrativeTextResolver Narrative { get; }

        private ChannelZeroGameContext(ChannelZeroSessionState state, ChannelZeroSaveService save,
            ChannelZeroBuiltInPuzzleRegistry registry, ChannelZeroInteractionCatalog interactions,
            string localeCode)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Save = save ?? throw new ArgumentNullException(nameof(save));
            PuzzleCatalog = registry.Catalog;
            InteractionCatalog = interactions;
            Inventory = new ChannelZeroInventoryService(state, PuzzleCatalog);
            Navigation = new ChannelZeroRoomNavigationService(state);
            Scenario = new ChannelZeroScenarioProgressionService(state, interactions);
            Guidance = new ChannelZeroProgressGuidanceService(state);
            Timeline = new ChannelZeroTimelineService(state, Inventory, PuzzleCatalog);
            Progression = new ChannelZeroChapterProgressionService(state, interactions, Timeline);
            Comparison = new ChannelZeroComparisonOverlayService(state, Timeline);
            Hints = new ChannelZeroHintService(state, PuzzleCatalog);
            Puzzles = new ChannelZeroPuzzleService(registry, state, Hints, Timeline, Progression);
            Interactions = new ChannelZeroInteractionRouter(interactions);
            Operations = new ChannelZeroOperationService(state, Timeline, Inventory, Progression, Comparison);
            Narrative = LoadNarrative(localeCode);
            Progression.Refresh();
        }

        public static ChannelZeroGameContext Create(ChannelZeroSessionState state,
            ChannelZeroSaveService save = null, string localeCode = "ko-KR")
        {
            save ??= new ChannelZeroSaveService(ChannelZeroSaveService.CreateDefaultStore());
            return new ChannelZeroGameContext(state, save,
                new ChannelZeroBuiltInPuzzleRegistry(localeCode), ChannelZeroInteractionCatalog.LoadDefault(), localeCode);
        }

        private static NarrativeTextResolver LoadNarrative(string localeCode)
        {
            return new NarrativeTextResolver(NarrativeTextCatalog.Load(localeCode));
        }
    }
}

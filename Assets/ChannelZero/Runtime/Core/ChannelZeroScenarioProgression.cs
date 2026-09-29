using System;
using System.Collections.Generic;
using System.Linq;

namespace ChannelZero.Runtime.Core
{
    public enum ChannelZeroScenarioStage
    {
        ReadServiceRequest,
        EnterLivingRoom,
        InspectBaselinePhoto,
        ObserveChildTv,
        StabilizeCrtSignal,
        RepairCrt,
        RestoreReturnCircuit,
        LearnVaseCausality,
        FindChildTraces,
        StabilizeFourEras,
        TreatJinwoo,
        SolveClockKnot,
        ResolveLivingRoom,
        EnterWorkshop,
        TestWorkshopParts,
        InvestigateWorkshop,
        RecoverFoldingCrank,
        ChapterTwoComplete,
    }

    public readonly struct ScenarioAccessResult
    {
        public bool IsUnlocked { get; }
        public string LockedMessage { get; }

        public ScenarioAccessResult(bool isUnlocked, string lockedMessage = null)
        {
            IsUnlocked = isUnlocked;
            LockedMessage = lockedMessage ?? ChannelZeroPuzzleService.NoActionMessage;
        }
    }

    public interface IChannelZeroUnlockRule
    {
        bool AppliesTo(string interactionId);
        ScenarioAccessResult Evaluate(ChannelZeroSessionState state);
    }

    public sealed class PredicateUnlockRule : IChannelZeroUnlockRule
    {
        private readonly HashSet<string> interactionIds;
        private readonly Func<ChannelZeroSessionState, bool> predicate;
        private readonly string lockedMessage;

        public PredicateUnlockRule(IEnumerable<string> interactionIds,
            Func<ChannelZeroSessionState, bool> predicate, string lockedMessage)
        {
            this.interactionIds = new HashSet<string>(interactionIds ?? Array.Empty<string>());
            this.predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
            this.lockedMessage = lockedMessage;
        }

        public bool AppliesTo(string interactionId) => interactionIds.Contains(interactionId);

        public ScenarioAccessResult Evaluate(ChannelZeroSessionState state) =>
            new(predicate(state), lockedMessage);
    }

    public sealed class ChannelZeroScenarioProgressionService
    {
        private readonly ChannelZeroSessionState state;
        private readonly ChannelZeroInteractionCatalog catalog;
        private readonly List<IChannelZeroUnlockRule> rules;

        public ChannelZeroScenarioProgressionService(ChannelZeroSessionState state,
            ChannelZeroInteractionCatalog catalog = null,
            IEnumerable<IChannelZeroUnlockRule> additionalRules = null)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.catalog = catalog ?? ChannelZeroInteractionCatalog.LoadDefault();
            rules = new List<IChannelZeroUnlockRule>();
            if (additionalRules != null)
                rules.AddRange(additionalRules);
        }

        public ChannelZeroScenarioStage CurrentStage
        {
            get
            {
                if (!HasReadServiceRequest() && !state.GetFlag(Chapter1Flags.ServiceRequestSeen)) return ChannelZeroScenarioStage.ReadServiceRequest;
                if (!state.visitedRoomIds.Contains(ChannelZeroIds.LivingRoom)) return ChannelZeroScenarioStage.EnterLivingRoom;
                if (!state.GetFlag(Chapter1Flags.Photo2001BaselineSeen)) return ChannelZeroScenarioStage.InspectBaselinePhoto;
                if (!state.GetFlag(Chapter1Flags.ChildSofaIntroSeen)) return ChannelZeroScenarioStage.ObserveChildTv;
                if (!state.GetFlag(Chapter1Flags.TVPhysicalRepairDone)) return ChannelZeroScenarioStage.RepairCrt;
                if (!state.GetFlag(Chapter1Flags.ReturnCircuitSolved)) return ChannelZeroScenarioStage.RestoreReturnCircuit;
                if (!state.GetFlag(Chapter1Flags.VaseCausalityLearned)) return ChannelZeroScenarioStage.LearnVaseCausality;
                if (!state.GetFlag(Chapter1Flags.ChildTrace2749Known)) return ChannelZeroScenarioStage.FindChildTraces;
                if (!state.GetFlag(Chapter1Flags.SignalStabilizerSolved)) return ChannelZeroScenarioStage.StabilizeFourEras;
                if (!state.GetFlag(Chapter1Flags.JinwooHandPreserved)) return ChannelZeroScenarioStage.TreatJinwoo;
                if (!state.GetFlag(Chapter1Flags.ClockKnotSolved)) return ChannelZeroScenarioStage.SolveClockKnot;
                if (!state.GetFlag("Chapter1Complete")) return ChannelZeroScenarioStage.ResolveLivingRoom;
                if (!state.visitedRoomIds.Contains(ChannelZeroIds.WorkshopRoom)) return ChannelZeroScenarioStage.EnterWorkshop;
                if (!state.GetFlag("TubeTestNormal")) return ChannelZeroScenarioStage.TestWorkshopParts;
                if (!state.GetFlag("FloorPlanSeen") || !state.GetFlag("AllJournalPagesSeen"))
                    return ChannelZeroScenarioStage.InvestigateWorkshop;
                if (!state.HasItem(ChannelZeroIds.FoldingCrankItem)) return ChannelZeroScenarioStage.RecoverFoldingCrank;
                return ChannelZeroScenarioStage.ChapterTwoComplete;
            }
        }

        public ScenarioAccessResult Evaluate(string interactionId)
        {
            if (catalog.TryGetUnlock(interactionId, out InteractionUnlockDefinition definition))
            {
                bool flagsReady = (definition.requiredFlags ?? Array.Empty<string>()).All(state.GetFlag);
                bool itemsReady = definition.requiredItemsAny == null || definition.requiredItemsAny.Length == 0 ||
                    definition.requiredItemsAny.Any(state.HasItem);
                bool recordsReady = definition.requiredRecordsAny == null || definition.requiredRecordsAny.Length == 0 ||
                    definition.requiredRecordsAny.Any(state.recordIds.Contains);
                bool eraReady = definition.requiredEra == 0 || (int)state.era == definition.requiredEra;
                if (!flagsReady || !itemsReady || !recordsReady || !eraReady)
                    return new ScenarioAccessResult(false, definition.lockedMessage);
            }
            foreach (IChannelZeroUnlockRule rule in rules)
                if (rule.AppliesTo(interactionId))
                    return rule.Evaluate(state);
            return new ScenarioAccessResult(true);
        }

        private bool HasReadServiceRequest() =>
            state.recordIds.Contains(ChannelZeroIds.PrologueServiceRequestCloseup) ||
            state.recordIds.Contains("PRO.ENTRY.MAIL.DOC.REQUEST");

    }

    public sealed class ChannelZeroProgressGuidanceService
    {
        private readonly ChannelZeroSessionState state;

        public ChannelZeroProgressGuidanceService(ChannelZeroSessionState state) =>
            this.state = state ?? throw new ArgumentNullException(nameof(state));

        public bool TryGetNext(out string narrativeTextId)
        {
            narrativeTextId = null;
            string candidate = ResolveCandidate();
            if (string.IsNullOrWhiteSpace(candidate) || state.HasSeenText(candidate))
                return false;
            narrativeTextId = candidate;
            return true;
        }

        private string ResolveCandidate()
        {
            if (state.roomId == ChannelZeroIds.LivingRoom)
            {
                if (!state.GetFlag(Chapter1Flags.Photo2001BaselineSeen)) return "CH1.LIVING.PHOTO.DESC.BASELINE";
                if (!state.GetFlag(Chapter1Flags.ChildSofaIntroSeen)) return "CH1.LIVING.TV.DESC.UNPLUGGED";
                if (state.GetFlag(Chapter1Flags.TVRearInspected) && !state.GetFlag(Chapter1Flags.ScrewdriverOwned)) return "CH1.LIVING.TVREAR.FEEDBACK.NEED_TOOL";
                if (state.GetFlag(Chapter1Flags.FirstSlip1961Done) && !state.GetFlag(Chapter1Flags.ReturnCircuitSolved)) return "CH1.LIVING.RETURN.MONO.GOAL";
                if (!state.GetFlag(Chapter1Flags.VaseCausalityLearned)) return "CH1.LIVING.VASE.MONO.FALLING";
                if (!state.GetFlag(Chapter1Flags.ChildTrace2749Known)) return "CH1.REPEAT.TRACE.INCOMPLETE";
                if (!state.GetFlag(Chapter1Flags.SignalStabilizerSolved)) return "CH1.REPEAT.STABILIZER.LOCKED";
                if (!state.GetFlag(Chapter1Flags.JinwooHandPreserved)) return "CH1.REPEAT.JINWOO.NEED_FUTURE";
                if (!state.GetFlag(Chapter1Flags.ClockKnotSolved)) return "CH1.REPEAT.CLOCK.LOCKED";
            }

            if (state.roomId == ChannelZeroIds.WorkshopRoom)
            {
                if (!state.GetFlag("PartsDrawerSorted")) return "GUIDE.CH2.DRAWER_1981";
                if (!state.GetFlag("TubeTestNormal")) return "GUIDE.CH2.TESTER_1961";
                if (!state.GetFlag("FloorPlanSeen")) return "GUIDE.CH2.PLAN_1961";
                if (!state.GetFlag("AllJournalPagesSeen")) return "GUIDE.CH2.JOURNAL_ACROSS_ERAS";
                if (!state.HasItem(ChannelZeroIds.FoldingCrankItem)) return "GUIDE.CH2.CRANK_1961";
            }
            return null;
        }
    }
}

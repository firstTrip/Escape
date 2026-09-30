using System;
using System.Collections.Generic;
using System.Linq;

namespace ChannelZero.Runtime.Core
{
    public enum Chapter1SequencePhase
    {
        ServiceRequest,
        BaselinePhoto,
        ChildSofaIntro,
        PhysicalRepair,
        ForcedSlip1961,
        ReturnCircuit,
        PhotoReplacement,
        ManualExploration,
        ClockKnot,
        Complete,
    }

    public enum CausalEventPhase
    {
        Unavailable,
        EventReady,
        CauseObserved,
        FutureObserved,
        CorrectionReady,
        Corrected,
        Confirmed,
    }

    public sealed class Chapter1SequenceDirector
    {
        private readonly ChannelZeroSessionState state;

        public Chapter1SequenceDirector(ChannelZeroSessionState state) =>
            this.state = state ?? throw new ArgumentNullException(nameof(state));

        public Chapter1SequencePhase Phase
        {
            get
            {
                if (state.GetFlag(Chapter1Flags.Chapter1Complete)) return Chapter1SequencePhase.Complete;
                if (!state.GetFlag(Chapter1Flags.ServiceRequestSeen)) return Chapter1SequencePhase.ServiceRequest;
                if (!state.GetFlag(Chapter1Flags.Photo2001BaselineSeen)) return Chapter1SequencePhase.BaselinePhoto;
                if (!state.GetFlag(Chapter1Flags.ChildSofaIntroSeen)) return Chapter1SequencePhase.ChildSofaIntro;
                if (!state.GetFlag(Chapter1Flags.TVPhysicalRepairDone)) return Chapter1SequencePhase.PhysicalRepair;
                if (!state.GetFlag(Chapter1Flags.FirstSlip1961Done)) return Chapter1SequencePhase.ForcedSlip1961;
                if (!state.GetFlag(Chapter1Flags.ReturnCircuitSolved)) return Chapter1SequencePhase.ReturnCircuit;
                if (!state.GetFlag(Chapter1Flags.Photo2001ChildCreated) ||
                    !state.GetFlag(Chapter1Flags.ManualDialUnlocked)) return Chapter1SequencePhase.PhotoReplacement;
                if (!state.GetFlag(Chapter1Flags.ClockKnotSolved)) return Chapter1SequencePhase.ManualExploration;
                return Chapter1SequencePhase.ClockKnot;
            }
        }

        public bool CanUseWorldHotspot(string hotspotId)
        {
            if (state.roomId != ChannelZeroIds.LivingRoom)
                return true;
            return Phase switch
            {
                Chapter1SequencePhase.BaselinePhoto => hotspotId == "Living_Photos",
                Chapter1SequencePhase.ChildSofaIntro =>
                    !state.GetFlag(Chapter1Flags.UnpluggedTVObserved)
                        ? hotspotId == "Living_CRT"
                        : hotspotId == "Living_Child",
                Chapter1SequencePhase.ForcedSlip1961 or Chapter1SequencePhase.ReturnCircuit =>
                    hotspotId == "Living_ReturnCircuit" || hotspotId == "Living_REC" ||
                    hotspotId == "Living_CRT" || hotspotId == "Living_Photos",
                _ => true,
            };
        }

        public void MarkServiceRequestSeen() => state.SetFlag(Chapter1Flags.ServiceRequestSeen);
        public void MarkBaselinePhotoSeen() => state.SetFlag(Chapter1Flags.Photo2001BaselineSeen);

        public bool BeginSofaChildEncounter()
        {
            if (!state.GetFlag(Chapter1Flags.Photo2001BaselineSeen) ||
                state.GetFlag(Chapter1Flags.ChildSofaIntroSeen) ||
                state.GetFlag(Chapter1Flags.UnpluggedTVObserved))
                return false;
            state.SetFlag(Chapter1Flags.UnpluggedTVObserved);
            return true;
        }

        public bool RevealSofaChildAfterGlitch()
        {
            if (!state.GetFlag(Chapter1Flags.UnpluggedTVObserved) ||
                state.GetFlag(Chapter1Flags.SofaChildAppeared) ||
                state.GetFlag(Chapter1Flags.ChildSofaIntroSeen))
                return false;
            state.SetFlag(Chapter1Flags.SofaChildAppeared);
            return true;
        }

        public bool CompleteSofaChildEncounter()
        {
            if (!state.GetFlag(Chapter1Flags.SofaChildAppeared) ||
                state.GetFlag(Chapter1Flags.ChildSofaIntroSeen))
                return false;
            state.SetFlag(Chapter1Flags.ChildSofaIntroSeen);
            return true;
        }

        public bool CompletePhysicalRepair()
        {
            if (!state.GetFlag(Chapter1Flags.ChildSofaIntroSeen))
                return false;
            state.SetFlag(Chapter1Flags.TVPhysicalRepairDone);
            state.SetFlag(Chapter1Flags.FirstSlip1961Done);
            state.Tune(ChannelEra.Year1961);
            state.operation = ChannelOperation.None;
            state.ClearTransientInteraction();
            return true;
        }

        public bool CompleteReturnCircuit()
        {
            if (!state.GetFlag(Chapter1Flags.ReturnWiringSolved) ||
                !state.GetFlag(Chapter1Flags.ReturnTubeSequenceSolved))
                return false;
            state.SetFlag(Chapter1Flags.ReturnCircuitSolved);
            state.Tune(ChannelEra.Year2001);
            state.SetFlag(Chapter1Flags.Photo2001ChildCreated);
            state.SetFlag(Chapter1Flags.ManualDialUnlocked);
            state.operation = ChannelOperation.None;
            state.ClearTransientInteraction();
            return true;
        }

        public bool RefreshCompletion()
        {
            bool complete = Chapter1Flags.CompletionRequirements.All(state.GetFlag);
            state.SetFlag(Chapter1Flags.Chapter1Complete, complete);
            return complete;
        }

        public void ApplySkipEndState(Chapter1SequencePhase phase)
        {
            switch (phase)
            {
                case Chapter1SequencePhase.ChildSofaIntro:
                    state.SetFlag(Chapter1Flags.UnpluggedTVObserved);
                    state.SetFlag(Chapter1Flags.SofaChildAppeared);
                    state.SetFlag(Chapter1Flags.ChildSofaIntroSeen);
                    break;
                case Chapter1SequencePhase.ForcedSlip1961:
                    state.SetFlag(Chapter1Flags.TVPhysicalRepairDone);
                    state.SetFlag(Chapter1Flags.FirstSlip1961Done);
                    state.Tune(ChannelEra.Year1961);
                    break;
                case Chapter1SequencePhase.PhotoReplacement:
                    state.SetFlag(Chapter1Flags.ReturnWiringSolved);
                    state.SetFlag(Chapter1Flags.ReturnTubeSequenceSolved);
                    CompleteReturnCircuit();
                    break;
            }
            state.ClearTransientInteraction();
        }
    }

    public sealed class YearDialController
    {
        private readonly ChannelZeroSessionState state;
        public static readonly ChannelEra[] AvailableYears =
        {
            ChannelEra.Year1961, ChannelEra.Year1981, ChannelEra.Year2001, ChannelEra.Year2021,
        };

        public YearDialController(ChannelZeroSessionState state) =>
            this.state = state ?? throw new ArgumentNullException(nameof(state));

        public bool IsUnlocked => state.GetFlag(Chapter1Flags.ManualDialUnlocked);

        public bool TryTune(ChannelEra year)
        {
            if (!IsUnlocked || Array.IndexOf(AvailableYears, year) < 0)
                return false;
            state.ClearTransientInteraction();
            state.Tune(year);
            return true;
        }
    }

    public sealed class CausalEventService
    {
        public const string VaseEvent = "VaseEvent";
        public const string JinwooHandEvent = "JinwooHandEvent";
        private readonly ChannelZeroSessionState state;

        public CausalEventService(ChannelZeroSessionState state) =>
            this.state = state ?? throw new ArgumentNullException(nameof(state));

        public CausalEventPhase GetPhase(string eventId)
        {
            string value = state.GetPuzzleState("causal:" + eventId, CausalEventPhase.Unavailable.ToString());
            return Enum.TryParse(value, out CausalEventPhase phase) ? phase : CausalEventPhase.Unavailable;
        }

        public void SetPhase(string eventId, CausalEventPhase phase) =>
            state.SetPuzzleState("causal:" + eventId, phase.ToString(),
                StatePersistence.CausalEvent, ChannelZeroIds.LivingRoom, true, ChannelEra.Year1961);

        public bool PrepareReplay(string eventId)
        {
            CausalEventPhase phase = GetPhase(eventId);
            if (phase != CausalEventPhase.FutureObserved && phase != CausalEventPhase.CorrectionReady)
                return false;
            SetPhase(eventId, CausalEventPhase.EventReady);
            return true;
        }

        public void OnYearEntered(ChannelEra era)
        {
            if (era != ChannelEra.Year1961)
                return;
            PrepareReplay(VaseEvent);
            PrepareReplay(JinwooHandEvent);
        }
    }

    public sealed class LocalSafetyCheckpointService
    {
        private readonly ChannelZeroSessionState state;
        public LocalSafetyCheckpointService(ChannelZeroSessionState state) =>
            this.state = state ?? throw new ArgumentNullException(nameof(state));

        public void Capture(string puzzleId, string localState) =>
            state.SetPuzzleState("checkpoint:" + puzzleId, localState,
                StatePersistence.Committed, state.roomId, true, state.era);

        public string Restore(string puzzleId, string fallback = "available")
        {
            state.ClearTransientInteraction();
            return state.GetPuzzleState("checkpoint:" + puzzleId, fallback);
        }
    }

    public sealed class ReturnCircuitPuzzle
    {
        private static readonly string[] WiringAnswer = { "red", "blue", "yellow", "green" };
        private static readonly int[] TubeAnswer = { 2, 4, 1, 3 };
        private readonly ChannelZeroSessionState state;
        private readonly LocalSafetyCheckpointService safety;

        public ReturnCircuitPuzzle(ChannelZeroSessionState state, LocalSafetyCheckpointService safety = null)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.safety = safety ?? new LocalSafetyCheckpointService(state);
        }

        public bool ConnectWire(string color)
        {
            int index = ParseInt(state.GetPuzzleState(ChannelZeroPuzzleIds.ReturnCircuit + ".wireIndex", "0"));
            if (index >= WiringAnswer.Length) return true;
            if (!string.Equals(WiringAnswer[index], color, StringComparison.OrdinalIgnoreCase)) return false;
            index++;
            state.SetPuzzleState(ChannelZeroPuzzleIds.ReturnCircuit + ".wireIndex", index.ToString(),
                StatePersistence.Transient, ChannelZeroIds.LivingRoom, true, ChannelEra.Year1961);
            safety.Capture(ChannelZeroPuzzleIds.ReturnCircuit + ".wiring", index.ToString());
            if (index == WiringAnswer.Length) state.SetFlag(Chapter1Flags.ReturnWiringSolved);
            return true;
        }

        public bool SelectTube(int tube)
        {
            if (!state.GetFlag(Chapter1Flags.ReturnWiringSolved)) return false;
            int index = ParseInt(state.GetPuzzleState(ChannelZeroPuzzleIds.ReturnCircuit + ".tubeIndex", "0"));
            if (tube != TubeAnswer[Math.Min(index, TubeAnswer.Length - 1)])
            {
                state.SetPuzzleState(ChannelZeroPuzzleIds.ReturnCircuit + ".tubeIndex", "0",
                    StatePersistence.Transient, ChannelZeroIds.LivingRoom, true, ChannelEra.Year1961);
                return false;
            }
            index++;
            state.SetPuzzleState(ChannelZeroPuzzleIds.ReturnCircuit + ".tubeIndex", index.ToString(),
                StatePersistence.Transient, ChannelZeroIds.LivingRoom, true, ChannelEra.Year1961);
            if (index == TubeAnswer.Length) state.SetFlag(Chapter1Flags.ReturnTubeSequenceSolved);
            return true;
        }

        public void RecoverFromOverload()
        {
            string safeIndex = safety.Restore(ChannelZeroPuzzleIds.ReturnCircuit + ".wiring", "0");
            state.SetPuzzleState(ChannelZeroPuzzleIds.ReturnCircuit + ".wireIndex", safeIndex,
                StatePersistence.Transient, ChannelZeroIds.LivingRoom, true, ChannelEra.Year1961);
        }

        private static int ParseInt(string value) => int.TryParse(value, out int parsed) ? parsed : 0;
    }

    public sealed class VaseCausalityPuzzle
    {
        private readonly ChannelZeroSessionState state;
        private readonly CausalEventService causal;
        public VaseCausalityPuzzle(ChannelZeroSessionState state, CausalEventService causal = null)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.causal = causal ?? new CausalEventService(state);
        }
        public void LeaveVase() { state.SetFlag(Chapter1Flags.VaseBroken1961); causal.SetPhase(CausalEventService.VaseEvent, CausalEventPhase.CauseObserved); }
        public void ObserveBrokenFuture() { causal.SetPhase(CausalEventService.VaseEvent, CausalEventPhase.FutureObserved); }
        public void MoveVase() { state.SetFlag(Chapter1Flags.VaseBroken1961, false); state.SetFlag(Chapter1Flags.VasePreserved1961); causal.SetPhase(CausalEventService.VaseEvent, CausalEventPhase.Corrected); }
        public void ConfirmPreservedFuture() { state.SetFlag(Chapter1Flags.VaseCausalityLearned); causal.SetPhase(CausalEventService.VaseEvent, CausalEventPhase.Confirmed); }
    }

    public sealed class ChildTraceStabilizerPuzzle
    {
        private readonly ChannelZeroSessionState state;
        public ChildTraceStabilizerPuzzle(ChannelZeroSessionState state) => this.state = state;
        public void ObserveTrace(ChannelEra era)
        {
            state.SetFlag(era switch
            {
                ChannelEra.Year1961 => Chapter1Flags.ChildTrace1961Seen,
                ChannelEra.Year1981 => Chapter1Flags.ChildTrace1981Seen,
                ChannelEra.Year2001 => Chapter1Flags.ChildTrace2001Seen,
                _ => Chapter1Flags.ChildTrace2021Seen,
            });
            bool all = new[] { Chapter1Flags.ChildTrace1961Seen, Chapter1Flags.ChildTrace1981Seen,
                Chapter1Flags.ChildTrace2001Seen, Chapter1Flags.ChildTrace2021Seen }.All(state.GetFlag);
            if (all) state.SetFlag(Chapter1Flags.ChildTrace2749Known);
        }
        public bool Submit(string digits)
        {
            if (!state.GetFlag(Chapter1Flags.ChildTrace2749Known) || digits != "2749") return false;
            state.SetFlag(Chapter1Flags.SignalStabilizerSolved);
            state.SetFlag(Chapter1Flags.ClockKeyOwned);
            state.AddItem(ChannelZeroPuzzleIds.ClockKey);
            return true;
        }
    }

    public sealed class JinwooHandCausalityPuzzle
    {
        private readonly ChannelZeroSessionState state;
        private readonly CausalEventService causal;
        public JinwooHandCausalityPuzzle(ChannelZeroSessionState state, CausalEventService causal = null)
        { this.state = state; this.causal = causal ?? new CausalEventService(state); }
        public void ObserveInjury() { state.SetFlag(Chapter1Flags.JinwooInjurySeen); causal.SetPhase(CausalEventService.JinwooHandEvent, CausalEventPhase.CauseObserved); }
        public void ObserveAmputationFuture() { state.SetFlag(Chapter1Flags.JinwooAmputationFutureSeen); causal.SetPhase(CausalEventService.JinwooHandEvent, CausalEventPhase.FutureObserved); }
        public bool Use(string itemId)
        {
            if (itemId == ChannelZeroPuzzleIds.Bandage && !state.GetFlag(Chapter1Flags.JinwooDisinfected)) return false;
            if (itemId == ChannelZeroPuzzleIds.Disinfectant) state.SetFlag(Chapter1Flags.JinwooDisinfected);
            else if (itemId == ChannelZeroPuzzleIds.Bandage) state.SetFlag(Chapter1Flags.JinwooBandaged);
            else return false;
            if (state.GetFlag(Chapter1Flags.JinwooDisinfected) && state.GetFlag(Chapter1Flags.JinwooBandaged))
                causal.SetPhase(CausalEventService.JinwooHandEvent, CausalEventPhase.Corrected);
            return true;
        }
        public void ConfirmPreservedFuture() { state.SetFlag(Chapter1Flags.JinwooHandPreserved); causal.SetPhase(CausalEventService.JinwooHandEvent, CausalEventPhase.Confirmed); }
    }

    public sealed class ClockKnotPuzzle
    {
        private readonly ChannelZeroSessionState state;
        private readonly LocalSafetyCheckpointService safety;
        public ClockKnotPuzzle(ChannelZeroSessionState state, LocalSafetyCheckpointService safety = null)
        { this.state = state; this.safety = safety ?? new LocalSafetyCheckpointService(state); }
        public bool CanEnter => state.era == ChannelEra.Year1981 &&
            state.GetFlag(Chapter1Flags.SignalStabilizerSolved) && state.GetFlag(Chapter1Flags.ClockKeyOwned) &&
            state.GetFlag(Chapter1Flags.JinwooHandPreserved);
        public bool Enter() { if (!CanEnter) return false; state.SetFlag(Chapter1Flags.ClockKnotEntered); safety.Capture(ChannelZeroPuzzleIds.ClockKnot, "entered"); return true; }
        public bool Solve(bool clockKeyInserted, bool commonGearFixed, int hour, int minute)
        {
            if (!state.GetFlag(Chapter1Flags.ClockKnotEntered) || !clockKeyInserted || !commonGearFixed || hour != 8 || minute != 8) return false;
            state.SetFlag(Chapter1Flags.ClockKnotSolved); state.SetFlag(Chapter1Flags.WorkshopKeyOwned);
            state.AddItem(ChannelZeroPuzzleIds.WorkshopKey); return true;
        }
        public string RecoverFromOverwind() => safety.Restore(ChannelZeroPuzzleIds.ClockKnot, "available");
    }
}

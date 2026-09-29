using ChannelZero.Runtime.Core;
using NUnit.Framework;

namespace ChannelZero.Tests.EditMode
{
    public sealed class ChannelZeroPuzzleServiceTests
    {
        [Test]
        public void ChapterOne_NewSingleDialPathCompletesWithoutRecMasterOr8888()
        {
            ChannelZeroSessionState state = LivingState();
            state.SetFlag(Chapter1Flags.Photo2001BaselineSeen);
            state.SetFlag(Chapter1Flags.ChildSofaIntroSeen);
            ChannelZeroPuzzleService puzzles = Create(state);
            Act(puzzles, ChannelZeroIds.LivingCrtRearCloseup, "inspect_screws");
            Act(puzzles, ChannelZeroIds.LivingToolboxCloseup, "open", "take:Screwdriver");
            Act(puzzles, ChannelZeroIds.LivingTubeStorageCloseup, "open", "take:ReplacementTube");
            Use(puzzles, ChannelZeroIds.LivingCrtRearCloseup, ChannelZeroPuzzleIds.Screwdriver);
            Act(puzzles, ChannelZeroIds.LivingCrtRearCloseup, "remove_tube");
            Use(puzzles, ChannelZeroIds.LivingCrtRearCloseup, ChannelZeroPuzzleIds.ReplacementTube);
            Act(puzzles, ChannelZeroIds.LivingCrtRearCloseup, "lock_bracket");
            new Chapter1SequenceDirector(state).CompletePhysicalRepair();

            Act(puzzles, ChannelZeroIds.LivingRecPanelCloseup,
                "wire:red", "wire:blue", "wire:yellow", "wire:green",
                "tube:2", "tube:4", "tube:1", "tube:3");
            new Chapter1SequenceDirector(state).CompleteReturnCircuit();

            state.Tune(ChannelEra.Year1961);
            Act(puzzles, ChannelZeroIds.LivingLockboxCloseup, "leave");
            state.Tune(ChannelEra.Year1981);
            Act(puzzles, ChannelZeroIds.LivingLockboxCloseup, "observe_broken");
            new CausalEventService(state).OnYearEntered(ChannelEra.Year1961);
            state.Tune(ChannelEra.Year1961);
            Act(puzzles, ChannelZeroIds.LivingLockboxCloseup, "move");
            state.Tune(ChannelEra.Year1981);
            Act(puzzles, ChannelZeroIds.LivingLockboxCloseup, "confirm");

            foreach (ChannelEra era in YearDialController.AvailableYears)
            {
                state.Tune(era);
                Act(puzzles, ChannelZeroIds.LivingFamilyPhotosCloseup, "observe");
            }
            state.Tune(ChannelEra.Year2001);
            Act(puzzles, ChannelZeroIds.LivingNumberRugCloseup,
                "digit:2", "digit:7", "digit:4", "digit:9");

            state.Tune(ChannelEra.Year1961);
            Assert.That(puzzles.TryOpen(ChannelZeroIds.LivingHandTreatmentCloseup, out _), Is.True);
            state.Tune(ChannelEra.Year2001);
            Act(puzzles, ChannelZeroIds.LivingHandTreatmentCloseup, "observe_amputation");
            new CausalEventService(state).OnYearEntered(ChannelEra.Year1961);
            state.Tune(ChannelEra.Year1961);
            Act(puzzles, ChannelZeroIds.LivingMedicalCabinetCloseup,
                "open", "take:Disinfectant", "take:Bandage");
            Use(puzzles, ChannelZeroIds.LivingHandTreatmentCloseup, ChannelZeroPuzzleIds.Disinfectant);
            Use(puzzles, ChannelZeroIds.LivingHandTreatmentCloseup, ChannelZeroPuzzleIds.Bandage);
            state.Tune(ChannelEra.Year2001);
            Act(puzzles, ChannelZeroIds.LivingHandTreatmentCloseup, "confirm_preserved");

            state.Tune(ChannelEra.Year1981);
            Act(puzzles, ChannelZeroIds.LivingClockCloseup,
                "enter", "insert_key", "fix_gear", "set_0808", "release_second");
            state.SetFlag(Chapter1Flags.WorkshopKeyUsed);
            new ChannelZeroChapterProgressionService(state).Refresh();

            Assert.That(state.GetFlag(Chapter1Flags.Chapter1Complete), Is.True);
            Assert.That(state.HasItem(ChannelZeroPuzzleIds.WorkshopKey), Is.True);
            Assert.That(state.HasItem(ChannelZeroPuzzleIds.MasterTape), Is.False);
            Assert.That(state.GetFlag("RecMasterComplete"), Is.False);
            Assert.That(state.GetFlag("Lockbox8888Solved"), Is.False);
        }

        [Test]
        public void SignalStabilizer_WrongInputResetsAndCanRetry()
        {
            ChannelZeroSessionState state = LivingState();
            state.SetFlag(Chapter1Flags.ChildTrace2749Known);
            ChannelZeroPuzzleService puzzles = Create(state);
            Act(puzzles, ChannelZeroIds.LivingNumberRugCloseup,
                "digit:2", "digit:7", "digit:4", "digit:8");
            Assert.That(state.GetPuzzleState(ChannelZeroPuzzleIds.ChildTraceStabilizer + ".input", ""), Is.Empty);
            Act(puzzles, ChannelZeroIds.LivingNumberRugCloseup,
                "digit:2", "digit:7", "digit:4", "digit:9");
            Assert.That(state.GetFlag(Chapter1Flags.SignalStabilizerSolved), Is.True);
        }

        [Test]
        public void MedicalCabinet_DoesNotRespawnConsumedTreatmentItems()
        {
            ChannelZeroSessionState state = LivingState(ChannelEra.Year1961);
            state.SetFlag(Chapter1Flags.SignalStabilizerSolved);
            state.SetFlag(Chapter1Flags.JinwooAmputationFutureSeen);
            ChannelZeroPuzzleService puzzles = Create(state);
            Act(puzzles, ChannelZeroIds.LivingMedicalCabinetCloseup,
                "open", "take:Disinfectant", "take:Bandage");
            Assert.That(puzzles.TryOpen(ChannelZeroIds.LivingHandTreatmentCloseup, out _), Is.True);
            Use(puzzles, ChannelZeroIds.LivingHandTreatmentCloseup, ChannelZeroPuzzleIds.Disinfectant);
            Use(puzzles, ChannelZeroIds.LivingHandTreatmentCloseup, ChannelZeroPuzzleIds.Bandage);
            Assert.That(puzzles.EvaluateAccess(ChannelZeroIds.LivingMedicalCabinetCloseup).Mode,
                Is.EqualTo(PuzzleInteractionMode.ResultOnly));
            Assert.That(state.HasItem(ChannelZeroPuzzleIds.Disinfectant), Is.False);
            Assert.That(state.HasItem(ChannelZeroPuzzleIds.Bandage), Is.False);
        }

        [Test]
        public void HandTreatment_RejectsBandageBeforeDisinfectantWithoutConsumingIt()
        {
            ChannelZeroSessionState state = LivingState(ChannelEra.Year1961);
            state.SetFlag(Chapter1Flags.SignalStabilizerSolved);
            state.AddItem(ChannelZeroPuzzleIds.Disinfectant);
            state.AddItem(ChannelZeroPuzzleIds.Bandage);
            ChannelZeroPuzzleService puzzles = Create(state);
            Assert.That(puzzles.TryOpen(ChannelZeroIds.LivingHandTreatmentCloseup, out PuzzleView initial), Is.True);
            Assert.That(initial.acceptsInventoryItems, Is.True);
            Use(puzzles, ChannelZeroIds.LivingHandTreatmentCloseup, ChannelZeroPuzzleIds.Bandage);
            Assert.That(state.HasItem(ChannelZeroPuzzleIds.Bandage), Is.True);
            Use(puzzles, ChannelZeroIds.LivingHandTreatmentCloseup, ChannelZeroPuzzleIds.Disinfectant);
            Use(puzzles, ChannelZeroIds.LivingHandTreatmentCloseup, ChannelZeroPuzzleIds.Bandage);
            Assert.That(state.GetFlag(Chapter1Flags.JinwooBandaged), Is.True);
        }

        [Test]
        public void TvRepair_UsesInventoryAndDoesNotCreateJinwooInjuryEarly()
        {
            ChannelZeroSessionState state = LivingState();
            state.SetFlag(Chapter1Flags.ChildSofaIntroSeen);
            state.AddItem(ChannelZeroPuzzleIds.Screwdriver);
            state.AddItem(ChannelZeroPuzzleIds.ReplacementTube);
            ChannelZeroPuzzleService puzzles = Create(state);
            Act(puzzles, ChannelZeroIds.LivingCrtRearCloseup, "inspect_screws");
            Assert.That(puzzles.TryOpen(ChannelZeroIds.LivingCrtRearCloseup, out PuzzleView closed), Is.True);
            Assert.That(closed.acceptsInventoryItems, Is.True);
            Assert.That(closed.actions, Is.Empty);
            Use(puzzles, ChannelZeroIds.LivingCrtRearCloseup, ChannelZeroPuzzleIds.Screwdriver);
            Act(puzzles, ChannelZeroIds.LivingCrtRearCloseup, "remove_tube");
            Use(puzzles, ChannelZeroIds.LivingCrtRearCloseup, ChannelZeroPuzzleIds.ReplacementTube);
            Act(puzzles, ChannelZeroIds.LivingCrtRearCloseup, "lock_bracket");
            Assert.That(state.GetFlag(Chapter1Flags.TVPhysicalRepairDone), Is.True);
            Assert.That(state.GetFlag(Chapter1Flags.JinwooInjurySeen), Is.False);
        }

        [Test]
        public void TvFront_OffersRearOnlyAfterSofaEncounterCompletes()
        {
            ChannelZeroSessionState state = LivingState();
            state.SetFlag(Chapter1Flags.Photo2001BaselineSeen);
            ChannelZeroPuzzleService puzzles = Create(state);
            Assert.That(puzzles.TryOpen(ChannelZeroIds.LivingCrtFrontCloseup, out PuzzleView before), Is.True);
            Assert.That(before.actions, Is.Empty);
            state.SetFlag(Chapter1Flags.ChildSofaIntroSeen);
            Assert.That(puzzles.TryOpen(ChannelZeroIds.LivingCrtFrontCloseup, out PuzzleView after), Is.True);
            Assert.That(after.actions.Exists(action => action.actionId == "inspect_rear"), Is.True);
        }

        [Test]
        public void ChapterTwo_RemainsAvailableBehindMigrationBoundary()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            state.MoveTo(ChannelZeroIds.WorkshopRoom, StoryChapter.Chapter2);
            ChannelZeroPuzzleService puzzles = Create(state);
            state.Tune(ChannelEra.Year1981);
            Act(puzzles, ChannelZeroIds.WorkshopPartsDrawerCloseup,
                "open", "take:NormalTube", "take:FaultyTube");
            state.Tune(ChannelEra.Year1961);
            Act(puzzles, ChannelZeroIds.WorkshopTubeTesterCloseup,
                "load:faulty", "test", "load:normal", "test");
            Act(puzzles, ChannelZeroIds.WorkshopFloorPlanCloseup, "inspect");
            Assert.That(state.GetFlag("TubeTestNormal"), Is.True);
            Assert.That(state.GetFlag("FloorPlanSeen"), Is.True);
        }

        private static ChannelZeroSessionState LivingState(ChannelEra era = ChannelEra.Year2001)
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            state.MoveTo(ChannelZeroIds.LivingRoom, StoryChapter.Chapter1);
            state.Tune(era);
            state.SetFlag(Chapter1Flags.ServiceRequestSeen);
            return state;
        }

        private static ChannelZeroPuzzleService Create(ChannelZeroSessionState state)
        {
            ChannelZeroBuiltInPuzzleRegistry registry = new();
            ChannelZeroInventoryService inventory = new(state, registry.Catalog);
            ChannelZeroTimelineService timeline = new(state, inventory, registry.Catalog);
            return new ChannelZeroPuzzleService(registry, state, recording: timeline);
        }

        private static void Act(ChannelZeroPuzzleService service, string closeupId, params string[] actions)
        {
            foreach (string action in actions)
                Assert.That(service.TryExecute(closeupId, action, out _), Is.True, $"{closeupId}/{action}");
        }

        private static void Use(ChannelZeroPuzzleService service, string closeupId, string itemId) =>
            Assert.That(service.TryUseItem(closeupId, itemId, out _), Is.True, $"{closeupId}/{itemId}");
    }
}

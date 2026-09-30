using System.Linq;
using ChannelZero.Runtime.Core;
using NUnit.Framework;
using UnityEngine;

namespace ChannelZero.Tests.EditMode
{
    public sealed class ChannelZeroGameServicesTests
    {
        private static PuzzleDefinitionCatalog Catalog()
        {
            return PuzzleDefinitionCatalog.LoadDefault();
        }

        [Test]
        public void DataCatalog_LoadsAllPlayableCloseupsAndLocalizedItems()
        {
            PuzzleDefinitionCatalog catalog = Catalog();
            Assert.That(catalog.Puzzles, Has.Count.EqualTo(17));
            Assert.That(catalog.TryGetPuzzle(ChannelZeroIds.LivingNumberRugCloseup, out PuzzleDefinition rug), Is.True);
            Assert.That(rug.answer, Is.EqualTo("2749"));
            Assert.That(rug.puzzleId, Is.EqualTo(ChannelZeroPuzzleIds.ChildTraceStabilizer));
            Assert.That(rug.ruleType, Is.EqualTo("signal_stabilizer"));
            Assert.That(rug.ActiveEra, Is.EqualTo(ChannelEra.Year2001));
            Assert.That(rug.hotspotId, Is.EqualTo("Living_NumberRug"));
            Assert.That(rug.hints, Has.Length.EqualTo(3));
            Assert.That(catalog.TryGetItem(ChannelZeroPuzzleIds.Screwdriver, out InventoryItemDefinition screwdriver), Is.True);
            Assert.That(screwdriver.displayName, Is.EqualTo("일자 드라이버"));
        }

        [Test]
        public void Inventory_SelectsLocalizedItemAndHoldUsesSelection()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            state.AddItem(ChannelZeroPuzzleIds.Screwdriver);
            ChannelZeroInventoryService inventory = new(state, Catalog());

            Assert.That(inventory.Select(ChannelZeroPuzzleIds.Screwdriver), Is.True);
            Assert.That(inventory.DisplayName(ChannelZeroPuzzleIds.Screwdriver), Is.EqualTo("일자 드라이버"));
            Assert.That(inventory.HoldSelected(), Is.True);
            Assert.That(state.heldItemId, Is.EqualTo(ChannelZeroPuzzleIds.Screwdriver));
        }

        [Test]
        public void LegacyTimelineRewind_DoesNotMutateChapterOneV2PuzzleState()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            state.MoveTo(ChannelZeroIds.LivingRoom, StoryChapter.Chapter1);
            ChannelZeroInventoryService inventory = new(state, Catalog());
            ChannelZeroTimelineService timeline = new(state, inventory);
            state.SetPuzzleState(ChannelZeroPuzzleIds.Rug2749 + ".input", "2");
            timeline.Record();

            state.SetPuzzleState(ChannelZeroPuzzleIds.Rug2749 + ".input", "27");
            state.SetFlag("FloorPlanSeen");
            state.SetPuzzleState("hint:ch2.floor_plan", "2");
            Assert.That(timeline.Rewind(), Is.True);

            Assert.That(state.GetPuzzleState(ChannelZeroPuzzleIds.Rug2749 + ".input"), Is.EqualTo("27"));
            Assert.That(state.GetFlag("FloorPlanSeen"), Is.True);
            Assert.That(state.GetPuzzleState("hint:ch2.floor_plan"), Is.EqualTo("2"));
        }

        [Test]
        public void LoadComparison_OpensReadOnlyOverlayWithoutChangingWorldState()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            state.MoveTo(ChannelZeroIds.LivingRoom, StoryChapter.Chapter1);
            PuzzleDefinitionCatalog catalog = Catalog();
            ChannelZeroTimelineService timeline = new(state, new ChannelZeroInventoryService(state, catalog), catalog);
            state.SetPuzzleState(ChannelZeroPuzzleIds.Rug2749 + ".input", "2");
            timeline.Record();
            state.SetPuzzleState(ChannelZeroPuzzleIds.Rug2749 + ".input", "27");
            state.operation = ChannelOperation.Load;
            string before = JsonUtility.ToJson(state);

            ChannelZeroComparisonOverlayService comparison = new(state, timeline);
            Assert.That(comparison.OpenLatest(), Is.True);

            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            Assert.That(comparison.IsOpen, Is.True);
            Assert.That(comparison.Current.Record.era, Is.EqualTo(ChannelEra.Year2001));
            Assert.That(comparison.Current.Differences, Is.Not.Empty);
            comparison.Close();
            Assert.That(comparison.IsOpen, Is.False);
        }

        [Test]
        public void LegacyTimelineLoadApi_NoLongerRestoresPhysicalState()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            state.MoveTo(ChannelZeroIds.LivingRoom, StoryChapter.Chapter1);
            ChannelZeroTimelineService timeline = new(state,
                new ChannelZeroInventoryService(state, Catalog()));
            state.SetPuzzleState(ChannelZeroPuzzleIds.Rug2749 + ".input", "2");
            timeline.Record();
            state.SetPuzzleState(ChannelZeroPuzzleIds.Rug2749 + ".input", "27");

            Assert.That(timeline.Load(), Is.True);
            Assert.That(state.GetPuzzleState(ChannelZeroPuzzleIds.Rug2749 + ".input"), Is.EqualTo("27"));
        }

        [Test]
        public void PuzzleStateCalculator_MapsLockedAvailableInProgressAndSolved()
        {
            PuzzleDefinitionCatalog catalog = Catalog();
            catalog.TryGetPuzzle(ChannelZeroIds.LivingNumberRugCloseup, out PuzzleDefinition rug);
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            ChannelZeroPuzzleStateCalculator calculator = new();

            Assert.That(calculator.Evaluate(rug, state), Is.EqualTo(PuzzleLifecycleState.Locked));
            state.SetFlag(Chapter1Flags.ChildTrace2749Known);
            Assert.That(calculator.Evaluate(rug, state), Is.EqualTo(PuzzleLifecycleState.Available));
            state.SetPuzzleState(ChannelZeroPuzzleIds.ChildTraceStabilizer + ".input", "2");
            Assert.That(calculator.Evaluate(rug, state), Is.EqualTo(PuzzleLifecycleState.InProgress));
            state.SetFlag(Chapter1Flags.SignalStabilizerSolved);
            Assert.That(calculator.Evaluate(rug, state), Is.EqualTo(PuzzleLifecycleState.Solved));
        }

        [Test]
        public void Timeline_RewindOnlyRestoresCurrentRoomAndKeepsHeldItemOutsideHistory()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            state.MoveTo(ChannelZeroIds.LivingRoom, StoryChapter.Chapter1);
            state.SetPuzzleState(ChannelZeroPuzzleIds.CrtRear, "open");
            state.AddItem(ChannelZeroPuzzleIds.Screwdriver);
            ChannelZeroInventoryService inventory = new(state, Catalog());
            ChannelZeroTimelineService timeline = new(state, inventory);
            timeline.Record();

            state.SetPuzzleState(ChannelZeroPuzzleIds.CrtRear, "bracket_removed");
            state.SetPuzzleState(ChannelZeroPuzzleIds.TubeTester, "normal");
            state.AddItem(ChannelZeroPuzzleIds.ReplacementTube);
            state.AddItem(ChannelZeroPuzzleIds.Bandage);
            state.AddItem(ChannelZeroIds.FoldingCrankItem);
            inventory.Select(ChannelZeroPuzzleIds.Bandage);
            timeline.Hold();

            Assert.That(timeline.Rewind(), Is.True);
            Assert.That(state.GetPuzzleState(ChannelZeroPuzzleIds.CrtRear), Is.EqualTo("open"));
            Assert.That(state.GetPuzzleState(ChannelZeroPuzzleIds.TubeTester), Is.EqualTo("normal"));
            Assert.That(state.HasItem(ChannelZeroPuzzleIds.Screwdriver), Is.True);
            Assert.That(state.HasItem(ChannelZeroPuzzleIds.ReplacementTube), Is.False);
            Assert.That(state.HasItem(ChannelZeroPuzzleIds.Bandage), Is.True);
            Assert.That(state.HasItem(ChannelZeroIds.FoldingCrankItem), Is.True);
        }

        [Test]
        public void ScenarioProgression_GatesLocationsFromNarrativeState()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            ChannelZeroScenarioProgressionService scenario = new(state);

            Assert.That(scenario.CurrentStage, Is.EqualTo(ChannelZeroScenarioStage.ReadServiceRequest));
            Assert.That(scenario.Evaluate(ChannelZeroIds.EntryLivingDoor).IsUnlocked, Is.False);

            state.MarkRecordRead(ChannelZeroIds.PrologueServiceRequestCloseup);
            Assert.That(scenario.Evaluate(ChannelZeroIds.EntryLivingDoor).IsUnlocked, Is.True);
            state.MoveTo(ChannelZeroIds.WorkshopRoom, StoryChapter.Chapter2);
            Assert.That(scenario.Evaluate("Workshop_TubeTester").IsUnlocked, Is.False);

            state.AddItem(ChannelZeroPuzzleIds.NormalTube);
            Assert.That(scenario.Evaluate("Workshop_TubeTester").IsUnlocked, Is.True);
            Assert.That(scenario.Evaluate(ChannelZeroIds.WorkshopFloorPlan).IsUnlocked, Is.False);
            state.SetFlag("TubeTestNormal");
            Assert.That(scenario.Evaluate(ChannelZeroIds.WorkshopFloorPlan).IsUnlocked, Is.True);
            state.SetFlag("FloorPlanSeen");
            state.Tune(ChannelEra.Year2001);
            Assert.That(scenario.Evaluate(ChannelZeroIds.WorkshopFoldingCrank).IsUnlocked, Is.False);
            state.Tune(ChannelEra.Year1961);
            Assert.That(scenario.Evaluate(ChannelZeroIds.WorkshopFoldingCrank).IsUnlocked, Is.True);
        }

        [Test]
        public void Timeline_RewindWithoutExactRecordResetsCurrentEraToInitialState()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            state.MoveTo(ChannelZeroIds.LivingRoom, StoryChapter.Chapter1);
            ChannelZeroTimelineService timeline = new(state, new ChannelZeroInventoryService(state, Catalog()));
            timeline.Record();

            state.Tune(ChannelEra.Year1961);
            state.SetPuzzleState(ChannelZeroPuzzleIds.JinwooHand, "disinfected");
            Assert.That(timeline.Play(), Is.Not.Null, "PLAY may inspect the nearest room record.");
            Assert.That(timeline.Rewind(), Is.True);
            Assert.That(state.GetPuzzleState(ChannelZeroPuzzleIds.JinwooHand), Is.EqualTo("locked"),
                "Without an exact checkpoint, REW must reset unresolved physical state for the current era.");
        }

        [Test]
        public void Timeline_RecordCreatesTheSingleAuthoritativeRecordingSlot()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            state.MoveTo(ChannelZeroIds.LivingRoom, StoryChapter.Chapter1);
            state.Tune(ChannelEra.Year1981);
            ChannelZeroTimelineService timeline = new(state, new ChannelZeroInventoryService(state, Catalog()));

            timeline.Record();
            Assert.That(timeline.HasRecording(ChannelZeroIds.LivingRoom, ChannelEra.Year1981), Is.True);
            Assert.That(state.GetFlag("Rec1981"), Is.False,
                "Legacy Rec flags must not become a second source of truth.");
        }

        [Test]
        public void HintService_AdvancesAndCapsAtThirdHint()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            ChannelZeroHintService hints = new(state, Catalog());

            string first = hints.Request(ChannelZeroIds.LivingNumberRugCloseup);
            hints.Request(ChannelZeroIds.LivingNumberRugCloseup);
            string third = hints.Request(ChannelZeroIds.LivingNumberRugCloseup);
            string capped = hints.Request(ChannelZeroIds.LivingNumberRugCloseup);

            Assert.That(first, Does.StartWith("힌트 1/3"));
            Assert.That(third, Does.StartWith("힌트 3/3"));
            Assert.That(capped, Does.StartWith("힌트 3/3"));
        }

        [Test]
        public void Navigation_BlocksWorkshopUntilChapterOneComplete()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            state.MoveTo(ChannelZeroIds.LivingRoom, StoryChapter.Chapter1);
            ChannelZeroRoomNavigationService navigation = new(state);

            Assert.That(navigation.TryEnterWorkshop(), Is.False);
            state.SetFlag("Chapter1Complete");
            Assert.That(navigation.TryEnterWorkshop(), Is.True);
            Assert.That(state.roomId, Is.EqualTo(ChannelZeroIds.WorkshopRoom));
        }

        [Test]
        public void ChapterOneBlockerMessage_ReportsOnlyRemainingProgressKinds()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            state.MoveTo(ChannelZeroIds.LivingRoom, StoryChapter.Chapter1);
            foreach (string flag in new[] { Chapter1Flags.TVPhysicalRepairDone,
                Chapter1Flags.ReturnCircuitSolved, Chapter1Flags.VaseCausalityLearned,
                Chapter1Flags.SignalStabilizerSolved, Chapter1Flags.JinwooHandPreserved })
                state.SetFlag(flag);
            PuzzleDefinitionCatalog catalog = Catalog();
            ChannelZeroTimelineService timeline = new(state, new ChannelZeroInventoryService(state, catalog), catalog);
            foreach (ChannelEra era in new[] { ChannelEra.Year1961, ChannelEra.Year1981 })
            {
                state.Tune(era);
                timeline.Record();
            }
            ChannelZeroChapterProgressionService progression = new(state, recording: timeline);

            Assert.That(progression.DescribeChapterOneBlockers(), Does.Contain("08:08 괘종시계"));
            Assert.That(progression.DescribeChapterOneBlockers(), Does.Contain("작업실 열쇠"));
            Assert.That(progression.DescribeChapterOneBlockers(), Does.Not.Contain("REC"));
            Assert.That(progression.DescribeChapterOneBlockers(), Does.Not.Contain("MASTER"));
        }

        [Test]
        public void HiddenLegacyRewind_DoesNotMutateChapterOneV2PuzzleState()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            state.MoveTo(ChannelZeroIds.LivingRoom, StoryChapter.Chapter1);
            PuzzleDefinitionCatalog catalog = Catalog();
            ChannelZeroInventoryService inventory = new(state, catalog);
            ChannelZeroTimelineService timeline = new(state, inventory, catalog);
            ChannelZeroChapterProgressionService progression = new(state, recording: timeline);
            ChannelZeroOperationService operations = new(state, timeline, inventory, progression);
            state.SetFlag("FamilyPhotoClueSeen");
            state.SetPuzzleState(ChannelZeroPuzzleIds.Rug2749 + ".input", "27");

            string preview = operations.Execute(ChannelOperation.Rew);
            Assert.That(preview, Does.Contain("미리보기"));
            Assert.That(state.GetPuzzleState(ChannelZeroPuzzleIds.Rug2749 + ".input"), Is.EqualTo("27"));

            string applied = operations.Execute(ChannelOperation.Rew);
            Assert.That(applied, Does.Contain("REW 완료"));
            Assert.That(state.GetPuzzleState(ChannelZeroPuzzleIds.Rug2749 + ".input"), Is.EqualTo("27"));
        }

        [Test]
        public void Navigation_BlocksLivingUntilServiceRequestWasRead()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            ChannelZeroRoomNavigationService navigation = new(state);

            Assert.That(navigation.TryEnterLiving(), Is.False);
            Assert.That(state.roomId, Is.EqualTo(ChannelZeroIds.EntryRoom));
            state.MarkRecordRead(ChannelZeroIds.PrologueServiceRequestCloseup);
            Assert.That(navigation.TryEnterLiving(), Is.True);
            Assert.That(state.roomId, Is.EqualTo(ChannelZeroIds.LivingRoom));
        }

        [Test]
        public void CompletedOrUnavailablePuzzle_ShowsNoActionMessageInsteadOfChecklist()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            ChannelZeroBuiltInPuzzleRegistry registry = new();
            ChannelZeroHintService hints = new(state, registry.Catalog);
            ChannelZeroPuzzleService puzzles = new(registry, state, hints);
            state.SetFlag(Chapter1Flags.ChildTrace2749Known);

            foreach (string digit in new[] { "digit:2", "digit:7", "digit:4", "digit:9" })
                puzzles.TryExecute(ChannelZeroIds.LivingNumberRugCloseup, digit, out _);

            Assert.That(puzzles.TryOpen(ChannelZeroIds.LivingNumberRugCloseup, out PuzzleView rugReview), Is.True);
            Assert.That(rugReview.actions, Is.Empty);
            Assert.That(puzzles.EvaluateAccess(ChannelZeroIds.LivingNumberRugCloseup).Mode,
                Is.EqualTo(PuzzleInteractionMode.ResultOnly));

            state.Tune(ChannelEra.Year2001);
            Assert.That(puzzles.TryOpen(ChannelZeroIds.WorkshopFoldingCrankInspect, out _), Is.False);
            Assert.That(puzzles.EvaluateAccess(ChannelZeroIds.WorkshopFoldingCrankInspect).Mode,
                Is.EqualTo(PuzzleInteractionMode.InactiveEra));
        }

        [Test]
        public void PuzzleCatalog_AssignsExactlyOneActiveEraPerPuzzleId()
        {
            PuzzleDefinitionCatalog catalog = Catalog();
            NarrativeTextCatalog narrative = NarrativeTextCatalog.LoadDefault();
            foreach (var group in catalog.Puzzles.GroupBy(definition => definition.RuntimePuzzleId))
            {
                Assert.That(group.Select(definition => definition.activeEra).Distinct().Count(), Is.EqualTo(1),
                    group.Key);
                foreach (PuzzleDefinition definition in group)
                    foreach (EraTextIdEntry entry in definition.inactiveTextIdByEra)
                        Assert.That(narrative.TryGetById(entry.textId, out _), Is.True,
                            $"{definition.RuntimePuzzleId}/{entry.era}/{entry.textId}");
            }
        }

        [Test]
        public void ChildTracePuzzle_RemainsInteractiveAcrossAllFourEras()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            ChannelZeroPuzzleService puzzles = new(new ChannelZeroBuiltInPuzzleRegistry(), state);
            state.Tune(ChannelEra.Year1981);

            state.SetFlag(Chapter1Flags.ManualDialUnlocked);
            Assert.That(puzzles.TryOpen(ChannelZeroIds.LivingFamilyPhotosCloseup, out PuzzleView review), Is.True);
            PuzzleInteractionAccess access = puzzles.EvaluateAccess(ChannelZeroIds.LivingFamilyPhotosCloseup);
            Assert.That(access.Mode, Is.EqualTo(PuzzleInteractionMode.CommonSystem));
            Assert.That(access.AcceptsPuzzleActions, Is.True);
            Assert.That(review.actions.Exists(action => action.actionId == "observe"), Is.True);
        }

        [Test]
        public void SignalStabilizer_Solves2749WithoutAnyRecordingUi()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            state.SetFlag(Chapter1Flags.ChildTrace2749Known);
            ChannelZeroPuzzleService puzzles = new(new ChannelZeroBuiltInPuzzleRegistry(), state);
            foreach (string digit in new[] { "digit:2", "digit:7", "digit:4", "digit:9" })
                Assert.That(puzzles.TryExecute(ChannelZeroIds.LivingNumberRugCloseup, digit, out _), Is.True);

            PuzzleInteractionAccess access = puzzles.EvaluateAccess(ChannelZeroIds.LivingNumberRugCloseup);
            Assert.That(access.Mode, Is.EqualTo(PuzzleInteractionMode.ResultOnly));
            Assert.That(state.GetFlag(Chapter1Flags.SignalStabilizerSolved), Is.True);
            Assert.That(state.HasItem(ChannelZeroPuzzleIds.ClockKey), Is.True);
            Assert.That(puzzles.TryOpen(ChannelZeroIds.LivingNumberRugCloseup, out PuzzleView review), Is.True);
            Assert.That(review.actions, Is.Empty);
        }

        [Test]
        public void Timeline_RewindPreservesSolvedPuzzleAndReadKnowledge()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            state.MoveTo(ChannelZeroIds.LivingRoom, StoryChapter.Chapter1);
            PuzzleDefinitionCatalog catalog = Catalog();
            ChannelZeroTimelineService timeline = new(state,
                new ChannelZeroInventoryService(state, catalog), catalog);
            state.SetPuzzleState(ChannelZeroPuzzleIds.Rug2749 + ".input", "27");
            timeline.Record();
            state.SetPuzzleState(ChannelZeroPuzzleIds.Rug2749, "solved");
            state.SetFlag("Rug2749Solved");
            state.MarkRecordRead("record:test");
            state.MarkTextSeen("text:test");

            Assert.That(timeline.Rewind(), Is.True);
            Assert.That(state.GetPuzzleState(ChannelZeroPuzzleIds.Rug2749), Is.EqualTo("solved"));
            Assert.That(state.GetFlag("Rug2749Solved"), Is.True);
            Assert.That(state.recordIds, Contains.Item("record:test"));
            Assert.That(state.seenTextIds, Contains.Item("text:test"));
        }
    }
}

using System.Linq;
using ChannelZero.Runtime.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace ChannelZero.Runtime.Core.Tests
{
    public sealed class ChannelZeroChapter1RuntimeTests
    {
        [Test]
        public void OpeningGates_ForcePhotoThenTwoStepTvIntro()
        {
            ChannelZeroSessionState state = LivingState();
            Chapter1SequenceDirector director = new(state);

            Assert.That(director.CanUseWorldHotspot("Living_CRT"), Is.False);
            Assert.That(director.CanUseWorldHotspot("Living_Photos"), Is.True);
            director.MarkBaselinePhotoSeen();
            Assert.That(director.BeginSofaChildEncounter(), Is.True);
            Assert.That(state.GetFlag(Chapter1Flags.ChildSofaIntroSeen), Is.False);
            Assert.That(director.CanUseWorldHotspot("Living_CRT"), Is.False);
            Assert.That(state.GetFlag(Chapter1Flags.SofaChildAppeared), Is.False);
            Assert.That(director.RevealSofaChildAfterGlitch(), Is.True);
            Assert.That(director.CanUseWorldHotspot("Living_Child"), Is.True);
            Assert.That(director.CompleteSofaChildEncounter(), Is.True);
            Assert.That(state.GetFlag(Chapter1Flags.ChildSofaIntroSeen), Is.True);
        }

        [Test]
        public void RepairAndReturn_AreAutomaticAndUnlockOnlySingleYearDial()
        {
            ChannelZeroSessionState state = LivingState();
            state.SetFlag(Chapter1Flags.Photo2001BaselineSeen);
            state.SetFlag(Chapter1Flags.ChildSofaIntroSeen);
            Chapter1SequenceDirector director = new(state);
            YearDialController dial = new(state);

            Assert.That(dial.TryTune(ChannelEra.Year2021), Is.False);
            Assert.That(director.CompletePhysicalRepair(), Is.True);
            Assert.That(state.era, Is.EqualTo(ChannelEra.Year1961));
            state.SetFlag(Chapter1Flags.ReturnWiringSolved);
            state.SetFlag(Chapter1Flags.ReturnTubeSequenceSolved);
            Assert.That(director.CompleteReturnCircuit(), Is.True);
            Assert.That(state.era, Is.EqualTo(ChannelEra.Year2001));
            Assert.That(state.GetFlag(Chapter1Flags.Photo2001ChildCreated), Is.True);
            Assert.That(dial.TryTune(ChannelEra.Year2021), Is.True);
        }

        [Test]
        public void ReturnCircuit_WrongWireKeepsCorrectProgress_AndWrongTubeResetsInputOnly()
        {
            ChannelZeroSessionState state = LivingState(ChannelEra.Year1961);
            ReturnCircuitPuzzle puzzle = new(state);
            Assert.That(puzzle.ConnectWire("red"), Is.True);
            Assert.That(puzzle.ConnectWire("yellow"), Is.False);
            Assert.That(state.GetPuzzleState(ChannelZeroPuzzleIds.ReturnCircuit + ".wireIndex"), Is.EqualTo("1"));
            Assert.That(puzzle.ConnectWire("blue"), Is.True);
            Assert.That(puzzle.ConnectWire("yellow"), Is.True);
            Assert.That(puzzle.ConnectWire("green"), Is.True);
            Assert.That(puzzle.SelectTube(2), Is.True);
            Assert.That(puzzle.SelectTube(1), Is.False);
            Assert.That(state.GetPuzzleState(ChannelZeroPuzzleIds.ReturnCircuit + ".tubeIndex"), Is.EqualTo("0"));
            foreach (int tube in new[] { 2, 4, 1, 3 }) Assert.That(puzzle.SelectTube(tube), Is.True);
            Assert.That(state.GetFlag(Chapter1Flags.ReturnTubeSequenceSolved), Is.True);
        }

        [Test]
        public void CausalReplay_ResetsOnlySelectedEvent()
        {
            ChannelZeroSessionState state = LivingState(ChannelEra.Year1961);
            CausalEventService service = new(state);
            service.SetPhase(CausalEventService.VaseEvent, CausalEventPhase.FutureObserved);
            service.SetPhase(CausalEventService.JinwooHandEvent, CausalEventPhase.Confirmed);
            state.SetFlag(Chapter1Flags.TVPhysicalRepairDone);

            Assert.That(service.PrepareReplay(CausalEventService.VaseEvent), Is.True);
            Assert.That(service.GetPhase(CausalEventService.VaseEvent), Is.EqualTo(CausalEventPhase.EventReady));
            Assert.That(service.GetPhase(CausalEventService.JinwooHandEvent), Is.EqualTo(CausalEventPhase.Confirmed));
            Assert.That(state.GetFlag(Chapter1Flags.TVPhysicalRepairDone), Is.True);
        }

        [Test]
        public void Stabilizer_RequiresAllTracesAnd2749_ThenAwardsClockKey()
        {
            ChannelZeroSessionState state = LivingState();
            ChildTraceStabilizerPuzzle puzzle = new(state);
            foreach (ChannelEra era in YearDialController.AvailableYears) puzzle.ObserveTrace(era);
            Assert.That(state.GetFlag(Chapter1Flags.ChildTrace2749Known), Is.True);
            Assert.That(puzzle.Submit("2748"), Is.False);
            Assert.That(puzzle.Submit("2749"), Is.True);
            Assert.That(state.HasItem(ChannelZeroPuzzleIds.ClockKey), Is.True);
        }

        [Test]
        public void JinwooTreatment_RejectsBandageBeforeDisinfectant()
        {
            ChannelZeroSessionState state = LivingState(ChannelEra.Year1961);
            JinwooHandCausalityPuzzle puzzle = new(state);
            puzzle.ObserveInjury();
            Assert.That(puzzle.Use(ChannelZeroPuzzleIds.Bandage), Is.False);
            Assert.That(puzzle.Use(ChannelZeroPuzzleIds.Disinfectant), Is.True);
            Assert.That(puzzle.Use(ChannelZeroPuzzleIds.Bandage), Is.True);
            Assert.That(state.GetFlag(Chapter1Flags.JinwooBandaged), Is.True);
        }

        [Test]
        public void ClockKnot_RequiresPrerequisitesAndAwardsWorkshopKey()
        {
            ChannelZeroSessionState state = LivingState(ChannelEra.Year1981);
            ClockKnotPuzzle puzzle = new(state);
            Assert.That(puzzle.Enter(), Is.False);
            state.SetFlag(Chapter1Flags.SignalStabilizerSolved);
            state.SetFlag(Chapter1Flags.ClockKeyOwned);
            state.SetFlag(Chapter1Flags.JinwooHandPreserved);
            Assert.That(puzzle.Enter(), Is.True);
            Assert.That(puzzle.Solve(true, true, 8, 8), Is.True);
            Assert.That(state.HasItem(ChannelZeroPuzzleIds.WorkshopKey), Is.True);
        }

        [Test]
        public void V3Migration_RemovesLegacyChapterOneCompletionWithoutAutoSolvingNewFlow()
        {
            ChannelZeroSessionState state = LivingState();
            state.saveVersion = 3;
            state.AddItem(ChannelZeroPuzzleIds.MasterTape);
            state.SetFlag("Rug2749Solved");
            state.SetFlag("Lockbox8888Solved");
            state.timelineSnapshots.Add(new TimelineSnapshot());

            new ChannelZeroSaveMigrationV3ToV4().Migrate(state);

            Assert.That(state.saveVersion, Is.EqualTo(4));
            Assert.That(state.HasItem(ChannelZeroPuzzleIds.MasterTape), Is.False);
            Assert.That(state.GetFlag(Chapter1Flags.SignalStabilizerSolved), Is.False);
            Assert.That(state.GetFlag(Chapter1Flags.ClockKnotSolved), Is.False);
            Assert.That(state.timelineSnapshots, Is.Empty);
            Assert.That(state.puzzleStates.Any(entry => entry.puzzleId.Contains("8888")), Is.False);
        }

        [Test]
        public void NormalizeAfterLoad_ClearsTransientUiAndTweenState()
        {
            ChannelZeroSessionState state = LivingState();
            state.operation = ChannelOperation.Load;
            state.activeCloseupId = "LIV-Z01";
            state.closeupInputLocked = true;
            state.SetPuzzleState("transient:tween", "half", StatePersistence.Transient);
            state.NormalizeAfterLoad();
            Assert.That(state.operation, Is.EqualTo(ChannelOperation.None));
            Assert.That(state.activeCloseupId, Is.Empty);
            Assert.That(state.closeupInputLocked, Is.False);
            Assert.That(state.puzzleStates.Any(entry => entry.persistence == StatePersistence.Transient), Is.False);
        }

        [Test]
        public void CompletedFrameSequences_UseApprovedLengthRangesAndCurrentPlaceholderCounts()
        {
            Assert.That(Chapter1GlitchFrameSequencePlayer.SofaPlaceholderFrameIds, Has.Length.EqualTo(8));
            Assert.That(Chapter1GlitchFrameSequencePlayer.EraTransitionPlaceholderFrameIds, Has.Length.EqualTo(7));

            GameObject owner = new("FrameSequenceTest");
            try
            {
                var player = owner.AddComponent<Chapter1GlitchFrameSequencePlayer>();
                Sprite[] sofa = Enumerable.Repeat(Sprite.Create(new Texture2D(1, 1),
                    new Rect(0, 0, 1, 1), Vector2.zero), 8).ToArray();
                Sprite[] transition = Enumerable.Repeat(Sprite.Create(new Texture2D(1, 1),
                    new Rect(0, 0, 1, 1), Vector2.zero), 7).ToArray();
                player.EditorConfigure(null, sofa, transition);

                Assert.That(player.HasApprovedSofaSequence, Is.True);
                Assert.That(player.HasApprovedEraTransition, Is.True);

                player.EditorConfigure(null, sofa.Take(5).ToArray(), transition.Take(4).ToArray());
                Assert.That(player.HasApprovedSofaSequence, Is.False);
                Assert.That(player.HasApprovedEraTransition, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void ChapterOneRebuild_LoadsAll104SpritesAndKeepsAuthoredLayerOrder()
        {
            Assert.That(Chapter1RebuildResources.CountLoadedSprites(), Is.EqualTo(104));
            Assert.That(Chapter1RebuildResources.GlitchFrameDurations,
                Is.EqualTo(new[] { 0.095f, 0.070f, 0.120f, 0.080f, 0.145f, 0.420f }));

            GameObject owner = new("TVStackOwner", typeof(RectTransform));
            GameObject artworkObject = new("Artwork", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            artworkObject.transform.SetParent(owner.transform, false);
            try
            {
                var stack = owner.AddComponent<Chapter1TvCloseupLayerStack>();
                stack.Configure(artworkObject.GetComponent<UnityEngine.UI.Image>());
                Assert.That(stack.ResourcesReady, Is.True);
                Assert.That(stack.LayerOrder, Is.EqualTo(new[]
                {
                    "ScreenContent", "BasePlate", "GlitchOverlay",
                    "GlassReflection", "Scanline", "ScreenGlow",
                }));
                Assert.That(stack.Show(Chapter1TvDisplayState.Off), Is.True);
                Transform root = owner.transform.Find("TVLayerStack");
                Assert.That(root.GetChild(0).name, Is.EqualTo("ScreenContent"));
                Assert.That(root.GetChild(1).name, Is.EqualTo("BasePlate"));
                Assert.That(root.GetChild(2).gameObject.activeSelf, Is.False,
                    "The approved off state uses only screen_off and base_plate.");
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void ChapterOneRebuild_RoutesPhotoRearAndTunerStateOverrides()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            state.MoveTo(ChannelZeroIds.LivingRoom, StoryChapter.Chapter1);
            state.SetFlag(Chapter1Flags.Photo2001ChildCreated);
            Assert.That(Chapter1RebuildResources.TryLoadCloseupOverride(state,
                ChannelZeroIds.LivingFamilyPhotosCloseup, out Sprite photo), Is.True);
            Assert.That(photo.name, Is.EqualTo("ch1_photo_2001_child_added_v02"));

            state.SetPuzzleState(ChannelZeroPuzzleIds.CrtRear, "open");
            Assert.That(Chapter1RebuildResources.TryLoadCloseupOverride(state,
                ChannelZeroIds.LivingCrtRearCloseup, out Sprite rear), Is.True);
            Assert.That(rear.name, Is.EqualTo("ch1_tv_rear_burned_tube_macro_2001_v01"));

            state.Tune(ChannelEra.Year1961);
            state.SetFlag(Chapter1Flags.FirstSlip1961Done);
            state.SetPuzzleState(ChannelZeroPuzzleIds.ReturnCircuit + ".wireIndex", "2");
            Assert.That(Chapter1RebuildResources.TryLoadCloseupOverride(state,
                ChannelZeroIds.LivingRecPanelCloseup, out Sprite tuner), Is.True);
            Assert.That(tuner.name, Is.EqualTo("ch1_console_tuner_1961_high_v01"));

            state.Tune(ChannelEra.Year2001);
            state.SetPuzzleState(ChannelZeroPuzzleIds.CrtRear, "screws_loosened");
            Sprite[] repairLayers = Chapter1RebuildResources.LoadCloseupOverlays(state,
                ChannelZeroIds.LivingCrtRearCloseup);
            Assert.That(repairLayers, Has.Length.EqualTo(1));
            Assert.That(repairLayers[0].name, Is.EqualTo("ch1_b02_rear_screw_loosened_layer_v01"));

            state.Tune(ChannelEra.Year1961);
            state.SetFlag(Chapter1Flags.ReturnWiringSolved, false);
            state.SetPuzzleState(ChannelZeroPuzzleIds.ReturnCircuit + ".wireIndex", "3");
            Sprite[] wireLayers = Chapter1RebuildResources.LoadCloseupOverlays(state,
                ChannelZeroIds.LivingRecPanelCloseup);
            Assert.That(wireLayers, Has.Length.EqualTo(3));
            Assert.That(wireLayers[2].name, Is.EqualTo("ch1_c02_wire_yellow_layer_v01"));

            state.SetFlag(Chapter1Flags.ChildTrace2749Known);
            Assert.That(Chapter1RebuildResources.TryLoadCloseupOverride(state,
                ChannelZeroIds.LivingNumberRugCloseup, out Sprite stabilizer), Is.True);
            Assert.That(stabilizer.name, Is.EqualTo("ch1_e08_stabilizer_input_ready_v01"));

            state.SetFlag(Chapter1Flags.JinwooInjurySeen);
            state.SetFlag(Chapter1Flags.JinwooDisinfected);
            Assert.That(Chapter1RebuildResources.TryLoadCloseupOverride(state,
                ChannelZeroIds.LivingHandTreatmentCloseup, out Sprite wrist), Is.True);
            Assert.That(wrist.name, Is.EqualTo("ch1_f01_wrist_cleaned_closeup_v01"));

            state.Tune(ChannelEra.Year1981);
            state.SetFlag(Chapter1Flags.ClockKnotEntered);
            Assert.That(Chapter1RebuildResources.TryLoadCloseupOverride(state,
                ChannelZeroIds.LivingClockCloseup, out Sprite gears), Is.True);
            Assert.That(gears.name, Is.EqualTo("ch1_g02_clock_dial_080759_v01"));
        }

        [Test]
        public void ChapterOneFollowup_RoutesEraAndProgressSpecificCloseups()
        {
            ChannelZeroSessionState state = LivingState(ChannelEra.Year1961);
            state.SetFlag(Chapter1Flags.FirstSlip1961Done);
            state.SetPuzzleState(ChannelZeroPuzzleIds.ReturnCircuit + ".clueStage", "frame_back");
            Assert.That(Chapter1RebuildResources.TryLoadCloseupOverride(state,
                ChannelZeroIds.LivingRecPanelCloseup, out Sprite frameBack), Is.True);
            Assert.That(frameBack.name, Is.EqualTo("ch1_c07_family_frame_back_v01"));

            state.SetPuzzleState(ChannelZeroPuzzleIds.ReturnCircuit + ".clueStage", "trace_seen");
            Assert.That(Chapter1RebuildResources.TryLoadCloseupOverride(state,
                ChannelZeroIds.LivingRecPanelCloseup, out Sprite trace), Is.True);
            Assert.That(trace.name, Is.EqualTo("ch1_c08_tone_color_trace_textless_v01"));

            Assert.That(Chapter1RebuildResources.TryLoadCloseupOverride(state,
                ChannelZeroIds.LivingLockboxCloseup, out Sprite fallingVase), Is.True);
            Assert.That(fallingVase.name, Is.EqualTo("ch1_e01_vase_falling_1961_v01"));
            state.SetFlag(Chapter1Flags.VasePreserved1961);
            Assert.That(Chapter1RebuildResources.TryLoadCloseupOverride(state,
                ChannelZeroIds.LivingLockboxCloseup, out Sprite safeVase), Is.True);
            Assert.That(safeVase.name, Is.EqualTo("ch1_e01_vase_safe_1961_v01"));

            state.SetFlag(Chapter1Flags.ManualDialUnlocked);
            state.SetFlag(Chapter1Flags.ChildTrace1961Seen);
            Assert.That(Chapter1RebuildResources.TryLoadCloseupOverride(state,
                ChannelZeroIds.LivingFamilyPhotosCloseup, out Sprite childTrace), Is.True);
            Assert.That(childTrace.name, Is.EqualTo("ch1_e03_child_trace_photo_1961_v01"));
            Sprite[] alignment = Chapter1RebuildResources.LoadCloseupOverlays(state,
                ChannelZeroIds.LivingFamilyPhotosCloseup);
            Assert.That(alignment, Has.Length.EqualTo(1));
            Assert.That(alignment[0].name, Is.EqualTo("ch1_e09_alignment_1961_overlay_v01"));

            state.Tune(ChannelEra.Year2001);
            state.SetFlag(Chapter1Flags.JinwooInjurySeen);
            Assert.That(Chapter1RebuildResources.TryLoadCloseupOverride(state,
                ChannelZeroIds.LivingHandTreatmentCloseup, out Sprite emptySleeve), Is.True);
            Assert.That(emptySleeve.name, Is.EqualTo("ch1_f02_jinwoo_photo_right_sleeve_empty_2001_v01"));
            Assert.That(Chapter1RebuildResources.LoadCloseupOverlays(state,
                ChannelZeroIds.LivingHandTreatmentCloseup)[0].name,
                Is.EqualTo("ch1_f03_medical_record_amputation_bg_v01"));

            state.Tune(ChannelEra.Year1981);
            state.SetFlag(Chapter1Flags.ClockKnotEntered);
            state.SetPuzzleState(ChannelZeroPuzzleIds.ClockKnot + ".step", "time");
            Assert.That(Chapter1RebuildResources.TryLoadCloseupOverride(state,
                ChannelZeroIds.LivingClockCloseup, out Sprite clock0800), Is.True);
            Assert.That(clock0800.name, Is.EqualTo("ch1_g02_clock_dial_080800_v01"));
            state.SetPuzzleState(ChannelZeroPuzzleIds.ClockKnot + ".step", "tick");
            Assert.That(Chapter1RebuildResources.TryLoadCloseupOverride(state,
                ChannelZeroIds.LivingClockCloseup, out Sprite clock0801), Is.True);
            Assert.That(clock0801.name, Is.EqualTo("ch1_g02_clock_dial_080801_v01"));

            state.SetFlag(Chapter1Flags.EndingRecognitionActive);
            Assert.That(Chapter1RebuildResources.TryLoadCloseupOverride(state,
                ChannelZeroIds.LivingCrtFrontCloseup, out Sprite ending), Is.True);
            Assert.That(ending.name, Is.EqualTo("ch1_g09_ending_tv_child_recognition_v01"));
        }

        [Test]
        public void CloseupPuzzleText_ExposesStableLocaleKeysWithKoreanFallback()
        {
            ChannelZeroBuiltInPuzzleRegistry registry = new("en-US");
            Assert.That(registry.Catalog.Locale, Is.EqualTo("ko-KR"),
                "Missing locale assets must fall back to the authored Korean catalog.");
            ChannelZeroSessionState state = LivingState();
            state.SetFlag(Chapter1Flags.TVRearInspected);
            ChannelZeroPuzzleService service = new(registry, state);
            Assert.That(service.TryOpen(ChannelZeroIds.LivingToolboxCloseup, out PuzzleView view), Is.True);
            Assert.That(view.titleKey, Is.EqualTo("puzzle.ch1.toolbox.title"));
            Assert.That(view.bodyKey, Is.EqualTo("puzzle.ch1.toolbox.body.default"));
            Assert.That(view.title, Is.Not.Empty);
            Assert.That(view.actions[0].labelKey, Is.EqualTo("puzzle.ch1.toolbox.action.open"));
        }

        private static ChannelZeroSessionState LivingState(ChannelEra era = ChannelEra.Year2001)
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            state.MoveTo(ChannelZeroIds.LivingRoom, StoryChapter.Chapter1);
            state.Tune(era);
            state.SetFlag(Chapter1Flags.ServiceRequestSeen);
            return state;
        }
    }
}

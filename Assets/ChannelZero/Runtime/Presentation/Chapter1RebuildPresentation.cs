using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChannelZero.Runtime.Presentation
{
    public enum Chapter1TvDisplayState
    {
        Off,
        Noise,
        Glitch,
    }

    public static class Chapter1RebuildResources
    {
        public static readonly float[] GlitchFrameDurations =
            { 0.095f, 0.070f, 0.120f, 0.080f, 0.145f, 0.420f };

        public const string Root = "ChannelZero/Chapter1Rebuild/";
        public const string ScreenOff = Root + "TVReusable/ch1_tv_closeup_screen_off_v01";
        public const string ScreenNoise = Root + "TVReusable/ch1_tv_closeup_screen_noise_v01";
        public const string BasePlate = Root + "TVReusable/ch1_tv_closeup_base_plate_screen_transparent_v01";
        public const string GlassReflection = Root + "TVReusable/ch1_tv_closeup_glass_reflection_v01";
        public const string Scanline = Root + "TVReusable/ch1_tv_closeup_scanline_v01";
        public const string ScreenGlow = Root + "TVReusable/ch1_tv_closeup_screen_glow_v01";
        public const string PhotoChildAdded = Root + "RuntimeSetV02/Photo/ch1_photo_2001_child_added_v02";
        public const string PhotoChildOverlay = Root + "RuntimeSetV02/Photo/ch1_photo_2001_child_added_overlay_v02";
        public const string TvRearBurnedTube = Root + "RuntimeSetV02/TVRear/ch1_tv_rear_burned_tube_macro_2001_v01";
        public const string TvRearRepairComplete = Root + "RuntimeSetV02/TVRear/ch1_tv_rear_repair_complete_2001_v01";
        public const string RuntimeSetV03Root = Root + "RuntimeSetV03/";

        public const string TvRepairScrews = RuntimeSetV03Root + "TVRepair/ch1_b02_rear_screw_loosened_layer_v01";
        public const string TvRepairCover = RuntimeSetV03Root + "TVRepair/ch1_b03_service_cover_detached_layer_v01";
        public const string TvRepairBracket = RuntimeSetV03Root + "TVRepair/ch1_b04_brass_bracket_open_layer_v01";
        public const string TvRepairTubeOff = RuntimeSetV03Root + "TVRepair/ch1_b06_replacement_tube_correct_layer_v01";
        public const string TvRepairSocketEmpty = RuntimeSetV03Root + "TVRepair/ch1_b07_tube_socket_empty_layer_v01";
        public const string TvRepairTubePreheat = RuntimeSetV03Root + "TVRepair/ch1_b08_replacement_tube_preheat_red_layer_v01";
        public const string TvRepairTubeOperating = RuntimeSetV03Root + "TVRepair/ch1_b08_replacement_tube_operating_orange_layer_v01";
        public const string ReturnWireRed = RuntimeSetV03Root + "ReturnCircuit/ch1_c02_wire_red_layer_v01";
        public const string ReturnWireBlue = RuntimeSetV03Root + "ReturnCircuit/ch1_c02_wire_blue_layer_v01";
        public const string ReturnWireYellow = RuntimeSetV03Root + "ReturnCircuit/ch1_c02_wire_yellow_layer_v01";
        public const string ReturnWireGreen = RuntimeSetV03Root + "ReturnCircuit/ch1_c02_wire_green_layer_v01";
        public const string ReturnTubeOff = RuntimeSetV03Root + "ReturnCircuit/ch1_c09_return_tube_panel_off_closeup_v02";
        public const string ReturnTubePreheat = RuntimeSetV03Root + "ReturnCircuit/ch1_c10_return_tube_panel_preheat_v02";
        public const string ReturnTubeOperating = RuntimeSetV03Root + "ReturnCircuit/ch1_c10_return_tube_panel_operating_v02";
        public const string StabilizerHatch = RuntimeSetV03Root + "Stabilizer/ch1_e07_rug_corner_lifted_hatch_closed_v01";
        public const string StabilizerInput = RuntimeSetV03Root + "Stabilizer/ch1_e08_stabilizer_input_ready_v01";
        public const string StabilizerSolved = RuntimeSetV03Root + "Stabilizer/ch1_e08_stabilizer_solved_v01";
        public const string WristCleaned = RuntimeSetV03Root + "Causality/ch1_f01_wrist_cleaned_closeup_v01";
        public const string WristBandaged = RuntimeSetV03Root + "Causality/ch1_f01_wrist_bandaged_closeup_v01";
        public const string FirstAidPickup = RuntimeSetV03Root + "Causality/ch1_f03_antiseptic_bandage_pickup_layer_v01";
        public const string ClockGearsDefault = RuntimeSetV03Root + "Clock/ch1_g03_clock_rear_gears_default_v01";
        public const string ClockGearsFixed = RuntimeSetV03Root + "Clock/ch1_g03_clock_rear_gears_fixed_v01";
        public const string ClockCompartmentOpen = RuntimeSetV03Root + "Clock/ch1_g05_hidden_compartment_open_closeup_v01";
        public const string WorkshopKeyLayer = RuntimeSetV03Root + "Clock/ch1_g06_workshop_key_inventory_layer_v01";
        public const string RuntimeSetV04Root = Root + "RuntimeSetV04/";
        public const string FrameFront1961 = RuntimeSetV04Root + "Frame/ch1_c07_family_frame_front_1961_v01";
        public const string FrameBack = RuntimeSetV04Root + "Frame/ch1_c07_family_frame_back_v01";
        public const string FrameBackOpen = RuntimeSetV04Root + "Frame/ch1_c07_family_frame_back_open_v01";
        public const string FrameToneTrace = RuntimeSetV04Root + "Frame/ch1_c08_tone_color_trace_textless_v01";
        public const string VaseFalling1961 = RuntimeSetV04Root + "Vase/ch1_e01_vase_falling_1961_v01";
        public const string VaseSafe1961 = RuntimeSetV04Root + "Vase/ch1_e01_vase_safe_1961_v01";
        public const string VaseBroken1961 = RuntimeSetV04Root + "Vase/ch1_e01_vase_broken_1961_v01";
        public const string VaseAbsent1981 = RuntimeSetV04Root + "Vase/ch1_e02_vase_absent_crack_1981_v01";
        public const string VaseIntact1981 = RuntimeSetV04Root + "Vase/ch1_e02_vase_intact_1981_v01";
        public const string Trace1961 = RuntimeSetV04Root + "ChildTrace/ch1_e03_child_trace_photo_1961_v01";
        public const string Trace1981 = RuntimeSetV04Root + "ChildTrace/ch1_e04_child_trace_tv_reflection_1981_v01";
        public const string Trace2001 = RuntimeSetV04Root + "ChildTrace/ch1_e05_child_trace_photo_2001_v01";
        public const string Trace2021 = RuntimeSetV04Root + "ChildTrace/ch1_e06_child_trace_empty_frame_reflection_2021_v01";
        public const string Align1961 = RuntimeSetV04Root + "Alignment/ch1_e09_alignment_1961_overlay_v01";
        public const string Align1981 = RuntimeSetV04Root + "Alignment/ch1_e09_alignment_1981_overlay_v01";
        public const string Align2001 = RuntimeSetV04Root + "Alignment/ch1_e09_alignment_2001_overlay_v01";
        public const string Align2021 = RuntimeSetV04Root + "Alignment/ch1_e09_alignment_2021_overlay_v01";
        public const string ClockKeyInventory = RuntimeSetV04Root + "ClockKey/ch1_e10_clock_winding_key_inventory_v01";
        public const string ClockKeyWorld = RuntimeSetV04Root + "ClockKey/ch1_e10_clock_winding_key_world_v01";
        public const string WristBeforeInjury = RuntimeSetV04Root + "Wrist/ch1_f01_wrist_before_injury_closeup_v01";
        public const string JinwooPreservedPhoto = RuntimeSetV04Root + "Causality/ch1_f02_jinwoo_photo_hand_preserved_2001_v01";
        public const string JinwooEmptySleevePhoto = RuntimeSetV04Root + "Causality/ch1_f02_jinwoo_photo_right_sleeve_empty_2001_v01";
        public const string MedicalAmputation = RuntimeSetV04Root + "Causality/ch1_f03_medical_record_amputation_bg_v01";
        public const string MedicalTreated = RuntimeSetV04Root + "Causality/ch1_f03_medical_record_treated_bg_v01";
        public const string ClockFrontDefault = RuntimeSetV04Root + "Clock/ch1_g01_clock_default_1981_v01";
        public const string ClockTimeKnot = RuntimeSetV04Root + "Clock/ch1_g01_clock_time_knot_1981_v01";
        public const string Clock0759 = RuntimeSetV04Root + "Clock/ch1_g02_clock_dial_080759_v01";
        public const string Clock0800 = RuntimeSetV04Root + "Clock/ch1_g02_clock_dial_080800_v01";
        public const string Clock0801 = RuntimeSetV04Root + "Clock/ch1_g02_clock_dial_080801_v01";
        public const string ClockTrace1961 = RuntimeSetV04Root + "Clock/ch1_g03_clock_gears_trace_1961_v01";
        public const string ClockTrace1981 = RuntimeSetV04Root + "Clock/ch1_g03_clock_rear_gears_default_v01";
        public const string ClockTrace2001 = RuntimeSetV04Root + "Clock/ch1_g03_clock_gears_trace_2001_v01";
        public const string BrassGearDefault = RuntimeSetV04Root + "Clock/ch1_g04_brass_gear_default_layer_v01";
        public const string BrassGearFixed = RuntimeSetV04Root + "Clock/ch1_g04_brass_gear_fixed_layer_v01";
        public const string JinwooNoteBlank = RuntimeSetV04Root + "Clock/ch1_g07_jinwoo_note_blank_layer_v01";
        public const string EndingTvRecognition = RuntimeSetV04Root + "Ending/ch1_g09_ending_tv_child_recognition_v01";

        public static readonly string[] TunerPaths = Sequence("RuntimeSetV02/Tuner", new[]
        {
            "ch1_console_tuner_1961_low_v01",
            "ch1_console_tuner_1961_mid_v01",
            "ch1_console_tuner_1961_high_v01",
        });

        public static readonly string[] RuntimeSetV02Paths =
        {
            PhotoChildAdded, PhotoChildOverlay, TvRearBurnedTube, TvRearRepairComplete,
            TunerPaths[0], TunerPaths[1], TunerPaths[2],
        };

        public static readonly string[] RuntimeSetV03Paths =
        {
            TvRepairScrews, TvRepairCover, TvRepairBracket, TvRepairTubeOff,
            TvRepairSocketEmpty, TvRepairTubePreheat, TvRepairTubeOperating,
            ReturnWireRed, ReturnWireBlue, ReturnWireYellow, ReturnWireGreen,
            ReturnTubeOff, ReturnTubePreheat, ReturnTubeOperating,
            StabilizerHatch, StabilizerInput, StabilizerSolved,
            WristCleaned, WristBandaged, FirstAidPickup,
            ClockGearsDefault, ClockGearsFixed, ClockCompartmentOpen, WorkshopKeyLayer,
        };

        public static readonly string[] RuntimeSetV04Paths =
        {
            FrameFront1961, FrameBack, FrameBackOpen, FrameToneTrace,
            VaseFalling1961, VaseSafe1961, VaseBroken1961, VaseAbsent1981, VaseIntact1981,
            Trace1961, Trace1981, Trace2001, Trace2021,
            Align1961, Align1981, Align2001, Align2021, ClockKeyInventory, ClockKeyWorld,
            WristBeforeInjury, JinwooPreservedPhoto, JinwooEmptySleevePhoto,
            MedicalAmputation, MedicalTreated, ClockFrontDefault, ClockTimeKnot,
            Clock0759, Clock0800, Clock0801, ClockTrace1961, ClockTrace1981, ClockTrace2001,
            BrassGearDefault, BrassGearFixed, JinwooNoteBlank, EndingTvRecognition,
        };

        public static readonly string[] TvReusablePaths =
        {
            BasePlate,
            GlassReflection,
            Root + "TVReusable/ch1_tv_closeup_glitch_example_v01",
            Scanline,
            Root + "TVReusable/ch1_tv_closeup_screen_black_fill_v01",
            ScreenGlow,
            Root + "TVReusable/ch1_tv_closeup_screen_mask_luma_v01",
            Root + "TVReusable/ch1_tv_closeup_screen_mask_rgba_v01",
            ScreenNoise,
            ScreenOff,
        };

        public static readonly string[] TvGlitchCloseupPaths = Sequence("TVGlitchCloseup", new[]
        {
            "ch1_tv_glitch_closeup_f01_noise_seed_overlay_v01",
            "ch1_tv_glitch_closeup_f02_sync_drift_overlay_v01",
            "ch1_tv_glitch_closeup_f03_hard_tear_overlay_v01",
            "ch1_tv_glitch_closeup_f04_horizontal_collapse_overlay_v01",
            "ch1_tv_glitch_closeup_f05_rebound_static_overlay_v01",
            "ch1_tv_glitch_closeup_f06_off_recovered_overlay_v01",
        });

        public static readonly string[] TvGlitchRoomPaths = Sequence("TVGlitchRoom", new[]
        {
            "ch1_tv_glitch_room_f01_noise_seed_v01", "ch1_tv_glitch_room_f02_sync_drift_v01",
            "ch1_tv_glitch_room_f03_hard_tear_v01", "ch1_tv_glitch_room_f04_horizontal_collapse_v01",
            "ch1_tv_glitch_room_f05_rebound_static_v01", "ch1_tv_glitch_room_f06_off_recovered_v01",
        });

        public static readonly string[] SofaPaths = Sequence("Sofa", new[]
        {
            "ch1_a05_sofa_child_f01_seated_v01", "ch1_a05_sofa_child_f02_head_up_v01",
            "ch1_a05_sofa_child_f03_body_turn_v01", "ch1_a05_sofa_child_f04_half_rise_v01",
            "ch1_a05_sofa_child_f05_standing_v01", "ch1_a05_sofa_child_f06_approach_v01",
            "ch1_a05_sofa_child_f07_cushion_only_v01", "ch1_a05_sofa_child_f08_empty_v01",
        });

        public static readonly string[] TimeSlipPaths = Sequence("TimeSlip", new[]
        {
            "ch1_d03_timeslip_2001_to_1961_f01_2001_dominant_v01",
            "ch1_d03_timeslip_2001_to_1961_f02_first_bleed_v01",
            "ch1_d03_timeslip_2001_to_1961_f03_mixed_v01",
            "ch1_d03_timeslip_2001_to_1961_f04_furniture_swap_v01",
            "ch1_d03_timeslip_2001_to_1961_f05_1961_emerging_v01",
            "ch1_d03_timeslip_2001_to_1961_f06_1961_dominant_v01",
            "ch1_d03_timeslip_2001_to_1961_f07_1961_settled_v01",
        });

        public static Sprite[] LoadSprites(IReadOnlyList<string> paths)
        {
            Sprite[] sprites = new Sprite[paths.Count];
            for (int i = 0; i < paths.Count; i++) sprites[i] = Resources.Load<Sprite>(paths[i]);
            return sprites;
        }

        public static int CountLoadedSprites()
        {
            int count = 0;
            foreach (string path in AllPaths()) if (Resources.Load<Sprite>(path) != null) count++;
            return count;
        }

        public static IEnumerable<string> AllPaths()
        {
            foreach (string path in TvReusablePaths) yield return path;
            foreach (string path in TvGlitchCloseupPaths) yield return path;
            foreach (string path in TvGlitchRoomPaths) yield return path;
            foreach (string path in SofaPaths) yield return path;
            foreach (string path in TimeSlipPaths) yield return path;
            foreach (string path in RuntimeSetV02Paths) yield return path;
            foreach (string path in RuntimeSetV03Paths) yield return path;
            foreach (string path in RuntimeSetV04Paths) yield return path;
        }

        public static bool TryLoadCloseupOverride(ChannelZero.Runtime.Core.ChannelZeroSessionState state,
            string closeupId, out Sprite sprite)
        {
            sprite = null;
            if (state == null) return false;

            string path = null;
            if (closeupId == ChannelZero.Runtime.Core.ChannelZeroIds.LivingCrtFrontCloseup &&
                state.GetFlag(ChannelZero.Runtime.Core.Chapter1Flags.EndingRecognitionActive))
                path = EndingTvRecognition;
            else if (closeupId == ChannelZero.Runtime.Core.ChannelZeroIds.LivingFamilyPhotosCloseup &&
                state.GetFlag(ChannelZero.Runtime.Core.Chapter1Flags.ManualDialUnlocked))
                path = state.era switch
                {
                    ChannelZero.Runtime.Core.ChannelEra.Year1961 => Trace1961,
                    ChannelZero.Runtime.Core.ChannelEra.Year1981 => Trace1981,
                    ChannelZero.Runtime.Core.ChannelEra.Year2001 => Trace2001,
                    _ => Trace2021,
                };
            else if (closeupId == ChannelZero.Runtime.Core.ChannelZeroIds.LivingFamilyPhotosCloseup &&
                state.era == ChannelZero.Runtime.Core.ChannelEra.Year2001 &&
                state.GetFlag(ChannelZero.Runtime.Core.Chapter1Flags.Photo2001ChildCreated))
                path = PhotoChildAdded;
            else if (closeupId == ChannelZero.Runtime.Core.ChannelZeroIds.LivingLockboxCloseup)
            {
                bool preserved = state.GetFlag(ChannelZero.Runtime.Core.Chapter1Flags.VasePreserved1961);
                if (state.era == ChannelZero.Runtime.Core.ChannelEra.Year1961)
                    path = preserved ? VaseSafe1961 : state.GetFlag(ChannelZero.Runtime.Core.Chapter1Flags.VaseBroken1961)
                        ? VaseBroken1961 : VaseFalling1961;
                else if (state.era == ChannelZero.Runtime.Core.ChannelEra.Year1981)
                    path = preserved ? VaseIntact1981 : VaseAbsent1981;
            }
            else if (closeupId == ChannelZero.Runtime.Core.ChannelZeroIds.LivingCrtRearCloseup)
            {
                string stage = state.GetPuzzleState(ChannelZero.Runtime.Core.ChannelZeroPuzzleIds.CrtRear);
                if (stage == "open") path = TvRearBurnedTube;
                else if (stage == "bracket_locked") path = TvRearRepairComplete;
            }
            else if (closeupId == ChannelZero.Runtime.Core.ChannelZeroIds.LivingRecPanelCloseup &&
                state.era == ChannelZero.Runtime.Core.ChannelEra.Year1961 &&
                state.GetFlag(ChannelZero.Runtime.Core.Chapter1Flags.FirstSlip1961Done))
            {
                bool wiringSolved = state.GetFlag(ChannelZero.Runtime.Core.Chapter1Flags.ReturnWiringSolved);
                bool tubesSolved = state.GetFlag(ChannelZero.Runtime.Core.Chapter1Flags.ReturnTubeSequenceSolved);
                if (tubesSolved) path = ReturnTubeOperating;
                else if (wiringSolved)
                {
                    int tubeIndex = ParseStateIndex(state,
                        ChannelZero.Runtime.Core.ChannelZeroPuzzleIds.ReturnCircuit + ".tubeIndex");
                    path = tubeIndex > 0 ? ReturnTubePreheat : ReturnTubeOff;
                }
                else
                {
                    string clueStage = state.GetPuzzleState(
                        ChannelZero.Runtime.Core.ChannelZeroPuzzleIds.ReturnCircuit + ".clueStage", "tuner");
                    path = clueStage switch
                    {
                        "frame_back" => FrameBack,
                        "frame_open" => FrameBackOpen,
                        "trace_seen" => FrameToneTrace,
                        _ => TunerPaths[ParseStateIndex(state,
                            ChannelZero.Runtime.Core.ChannelZeroPuzzleIds.ReturnCircuit + ".wireIndex") % TunerPaths.Length],
                    };
                }
            }
            else if (closeupId == ChannelZero.Runtime.Core.ChannelZeroIds.LivingNumberRugCloseup &&
                state.GetFlag(ChannelZero.Runtime.Core.Chapter1Flags.ChildTrace2749Known))
                path = state.GetFlag(ChannelZero.Runtime.Core.Chapter1Flags.SignalStabilizerSolved)
                    ? StabilizerSolved : StabilizerInput;
            else if (closeupId == ChannelZero.Runtime.Core.ChannelZeroIds.LivingHandTreatmentCloseup)
            {
                if (state.era == ChannelZero.Runtime.Core.ChannelEra.Year2001 &&
                    state.GetFlag(ChannelZero.Runtime.Core.Chapter1Flags.JinwooInjurySeen))
                    path = state.GetFlag(ChannelZero.Runtime.Core.Chapter1Flags.JinwooBandaged)
                        ? JinwooPreservedPhoto : JinwooEmptySleevePhoto;
                else if (!state.GetFlag(ChannelZero.Runtime.Core.Chapter1Flags.JinwooInjurySeen)) path = WristBeforeInjury;
                else if (state.GetFlag(ChannelZero.Runtime.Core.Chapter1Flags.JinwooBandaged)) path = WristBandaged;
                else if (state.GetFlag(ChannelZero.Runtime.Core.Chapter1Flags.JinwooDisinfected)) path = WristCleaned;
            }
            else if (closeupId == ChannelZero.Runtime.Core.ChannelZeroIds.LivingClockCloseup)
            {
                if (state.era == ChannelZero.Runtime.Core.ChannelEra.Year1961) path = ClockTrace1961;
                else if (state.era == ChannelZero.Runtime.Core.ChannelEra.Year2001) path = ClockTrace2001;
                else if (state.era != ChannelZero.Runtime.Core.ChannelEra.Year1981) path = ClockFrontDefault;
                else
                {
                string step = state.GetPuzzleState(
                    ChannelZero.Runtime.Core.ChannelZeroPuzzleIds.ClockKnot + ".step", "key");
                if (state.GetFlag(ChannelZero.Runtime.Core.Chapter1Flags.ClockKnotSolved)) path = ClockCompartmentOpen;
                else if (step == "tick") path = Clock0801;
                else if (step == "time") path = Clock0800;
                else if (step == "gear") path = ClockGearsDefault;
                else if (state.GetFlag(ChannelZero.Runtime.Core.Chapter1Flags.ClockKnotEntered)) path = Clock0759;
                else path = ClockTimeKnot;
                }
            }

            if (string.IsNullOrWhiteSpace(path)) return false;
            sprite = Resources.Load<Sprite>(path);
            return sprite != null;
        }

        public static Sprite[] LoadCloseupOverlays(ChannelZero.Runtime.Core.ChannelZeroSessionState state,
            string closeupId)
        {
            List<string> paths = new();
            if (state == null) return Array.Empty<Sprite>();
            if (closeupId == ChannelZero.Runtime.Core.ChannelZeroIds.LivingCrtRearCloseup)
            {
                string stage = state.GetPuzzleState(ChannelZero.Runtime.Core.ChannelZeroPuzzleIds.CrtRear);
                if (stage == "screws_loosened") paths.Add(TvRepairScrews);
                else if (stage == "tube_removed")
                {
                    paths.Add(TvRepairCover);
                    paths.Add(TvRepairBracket);
                    paths.Add(TvRepairSocketEmpty);
                }
                else if (stage == "replaced")
                {
                    paths.Add(TvRepairCover);
                    paths.Add(TvRepairBracket);
                    paths.Add(TvRepairTubeOff);
                }
            }
            else if (closeupId == ChannelZero.Runtime.Core.ChannelZeroIds.LivingRecPanelCloseup &&
                !state.GetFlag(ChannelZero.Runtime.Core.Chapter1Flags.ReturnWiringSolved))
            {
                string[] wirePaths = { ReturnWireRed, ReturnWireBlue, ReturnWireYellow, ReturnWireGreen };
                int count = Mathf.Clamp(ParseStateIndex(state,
                    ChannelZero.Runtime.Core.ChannelZeroPuzzleIds.ReturnCircuit + ".wireIndex"), 0, wirePaths.Length);
                for (int i = 0; i < count; i++) paths.Add(wirePaths[i]);
            }
            else if (closeupId == ChannelZero.Runtime.Core.ChannelZeroIds.LivingFamilyPhotosCloseup &&
                TraceSeen(state))
                paths.Add(state.era switch
                {
                    ChannelZero.Runtime.Core.ChannelEra.Year1961 => Align1961,
                    ChannelZero.Runtime.Core.ChannelEra.Year1981 => Align1981,
                    ChannelZero.Runtime.Core.ChannelEra.Year2001 => Align2001,
                    _ => Align2021,
                });
            else if (closeupId == ChannelZero.Runtime.Core.ChannelZeroIds.LivingMedicalCabinetCloseup &&
                state.GetFlag(ChannelZero.Runtime.Core.Chapter1Flags.JinwooAmputationFutureSeen) &&
                !state.GetFlag("MedicalSuppliesTaken"))
                paths.Add(FirstAidPickup);
            else if (closeupId == ChannelZero.Runtime.Core.ChannelZeroIds.LivingHandTreatmentCloseup &&
                state.era == ChannelZero.Runtime.Core.ChannelEra.Year2001 &&
                state.GetFlag(ChannelZero.Runtime.Core.Chapter1Flags.JinwooInjurySeen))
                paths.Add(state.GetFlag(ChannelZero.Runtime.Core.Chapter1Flags.JinwooBandaged)
                    ? MedicalTreated : MedicalAmputation);
            else if (closeupId == ChannelZero.Runtime.Core.ChannelZeroIds.LivingNumberRugCloseup &&
                state.GetFlag(ChannelZero.Runtime.Core.Chapter1Flags.SignalStabilizerSolved))
                paths.Add(ClockKeyWorld);
            else if (closeupId == ChannelZero.Runtime.Core.ChannelZeroIds.LivingClockCloseup &&
                state.GetFlag(ChannelZero.Runtime.Core.Chapter1Flags.ClockKnotSolved))
            {
                paths.Add(WorkshopKeyLayer);
                paths.Add(JinwooNoteBlank);
            }
            else if (closeupId == ChannelZero.Runtime.Core.ChannelZeroIds.LivingClockCloseup &&
                state.era == ChannelZero.Runtime.Core.ChannelEra.Year1981)
            {
                string step = state.GetPuzzleState(
                    ChannelZero.Runtime.Core.ChannelZeroPuzzleIds.ClockKnot + ".step", "key");
                if (step == "gear") paths.Add(BrassGearDefault);
                else if (step == "time" || step == "tick") paths.Add(BrassGearFixed);
            }

            return LoadSprites(paths);
        }

        private static int ParseStateIndex(ChannelZero.Runtime.Core.ChannelZeroSessionState state, string key)
        {
            return int.TryParse(state.GetPuzzleState(key, "0"), out int parsed) ? parsed : 0;
        }

        private static bool TraceSeen(ChannelZero.Runtime.Core.ChannelZeroSessionState state)
        {
            return state.GetFlag(state.era switch
            {
                ChannelZero.Runtime.Core.ChannelEra.Year1961 => ChannelZero.Runtime.Core.Chapter1Flags.ChildTrace1961Seen,
                ChannelZero.Runtime.Core.ChannelEra.Year1981 => ChannelZero.Runtime.Core.Chapter1Flags.ChildTrace1981Seen,
                ChannelZero.Runtime.Core.ChannelEra.Year2001 => ChannelZero.Runtime.Core.Chapter1Flags.ChildTrace2001Seen,
                _ => ChannelZero.Runtime.Core.Chapter1Flags.ChildTrace2021Seen,
            });
        }

        private static string[] Sequence(string folder, string[] names)
        {
            string[] paths = new string[names.Length];
            for (int i = 0; i < names.Length; i++) paths[i] = Root + folder + "/" + names[i];
            return paths;
        }
    }

    [DisallowMultipleComponent]
    public sealed class Chapter1TvCloseupLayerStack : MonoBehaviour
    {
        [SerializeField] private bool reduceFlashing;
        [SerializeField] private bool reduceMotion;

        private Image fallbackArtwork;
        private RectTransform root;
        private Image screenContent;
        private Image basePlate;
        private Image glitchOverlay;
        private Image glassReflection;
        private Image scanline;
        private Image screenGlow;
        private Sprite screenOff;
        private Sprite screenNoise;
        private Sprite[] glitchFrames = Array.Empty<Sprite>();

        public string[] LayerOrder => new[]
            { "ScreenContent", "BasePlate", "GlitchOverlay", "GlassReflection", "Scanline", "ScreenGlow" };
        public bool ResourcesReady { get; private set; }
        public bool IsPlayingGlitch { get; private set; }

        public void Configure(Image artwork)
        {
            fallbackArtwork = artwork;
            EnsureStack();
        }

        public bool Show(Chapter1TvDisplayState state)
        {
            EnsureStack();
            if (!ResourcesReady)
            {
                Hide();
                return false;
            }

            if (IsPlayingGlitch && state != Chapter1TvDisplayState.Off)
            {
                root.gameObject.SetActive(true);
                if (fallbackArtwork != null) fallbackArtwork.enabled = false;
                return true;
            }

            root.gameObject.SetActive(true);
            if (fallbackArtwork != null) fallbackArtwork.enabled = false;
            screenContent.sprite = state == Chapter1TvDisplayState.Off ? screenOff : screenNoise;
            screenContent.gameObject.SetActive(true);
            basePlate.gameObject.SetActive(true);
            bool effects = state != Chapter1TvDisplayState.Off;
            glitchOverlay.gameObject.SetActive(state == Chapter1TvDisplayState.Glitch);
            glassReflection.gameObject.SetActive(effects);
            scanline.gameObject.SetActive(effects && !reduceFlashing);
            screenGlow.gameObject.SetActive(effects);
            return true;
        }

        public void PlayGlitch(Action completed)
        {
            if (!Show(Chapter1TvDisplayState.Glitch) || glitchFrames.Length != 6)
            {
                completed?.Invoke();
                return;
            }
            StopAllCoroutines();
            IsPlayingGlitch = true;
            StartCoroutine(PlayGlitchFrames(completed));
        }

        public void Hide()
        {
            StopAllCoroutines();
            IsPlayingGlitch = false;
            if (root != null) root.gameObject.SetActive(false);
            if (fallbackArtwork != null) fallbackArtwork.enabled = true;
        }

        private IEnumerator PlayGlitchFrames(Action completed)
        {
            if (reduceMotion)
            {
                glitchOverlay.sprite = glitchFrames[glitchFrames.Length - 1];
                yield return new WaitForSecondsRealtime(0.42f);
            }
            else
            {
                for (int i = 0; i < glitchFrames.Length; i++)
                {
                    glitchOverlay.sprite = glitchFrames[i];
                    float duration = Chapter1RebuildResources.GlitchFrameDurations[i];
                    if (reduceFlashing) duration = Mathf.Max(duration, 0.145f);
                    yield return new WaitForSecondsRealtime(duration);
                }
            }
            IsPlayingGlitch = false;
            Show(Chapter1TvDisplayState.Off);
            completed?.Invoke();
        }

        private void EnsureStack()
        {
            if (root != null) return;
            Transform parent = fallbackArtwork != null ? fallbackArtwork.transform.parent : transform;
            GameObject rootObject = new("TVLayerStack", typeof(RectTransform));
            rootObject.transform.SetParent(parent, false);
            root = rootObject.GetComponent<RectTransform>();
            Stretch(root);
            if (fallbackArtwork != null)
                root.SetSiblingIndex(fallbackArtwork.transform.GetSiblingIndex() + 1);

            screenContent = CreateLayer("ScreenContent");
            basePlate = CreateLayer("BasePlate");
            glitchOverlay = CreateLayer("GlitchOverlay");
            glassReflection = CreateLayer("GlassReflection");
            scanline = CreateLayer("Scanline");
            screenGlow = CreateLayer("ScreenGlow");

            screenOff = Resources.Load<Sprite>(Chapter1RebuildResources.ScreenOff);
            screenNoise = Resources.Load<Sprite>(Chapter1RebuildResources.ScreenNoise);
            basePlate.sprite = Resources.Load<Sprite>(Chapter1RebuildResources.BasePlate);
            glassReflection.sprite = Resources.Load<Sprite>(Chapter1RebuildResources.GlassReflection);
            scanline.sprite = Resources.Load<Sprite>(Chapter1RebuildResources.Scanline);
            screenGlow.sprite = Resources.Load<Sprite>(Chapter1RebuildResources.ScreenGlow);
            glitchFrames = Chapter1RebuildResources.LoadSprites(Chapter1RebuildResources.TvGlitchCloseupPaths);
            ResourcesReady = screenOff != null && screenNoise != null && basePlate.sprite != null &&
                glassReflection.sprite != null && scanline.sprite != null && screenGlow.sprite != null &&
                Array.TrueForAll(glitchFrames, sprite => sprite != null);
            rootObject.SetActive(false);
        }

        private Image CreateLayer(string layerName)
        {
            GameObject layer = new(layerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            layer.transform.SetParent(root, false);
            RectTransform rect = layer.GetComponent<RectTransform>();
            Stretch(rect);
            Image image = layer.GetComponent<Image>();
            image.preserveAspect = false;
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}

namespace ChannelZero.Runtime.Core
{
    public enum ChannelEra
    {
        Year1961 = 1961,
        Year1981 = 1981,
        Year2001 = 2001,
        Year2021 = 2021,
    }

    public enum ChannelOperation
    {
        None,
        Rec,
        Play,
        Load,
        Rew,
        Hold,
    }

    public enum StoryChapter
    {
        Prologue = 0,
        Chapter1 = 1,
        Chapter2 = 2,
        Chapter3 = 3,
        Chapter4 = 4,
        Finale = 5,
    }

    public enum DisappearingStairState
    {
        Hidden,
        Foreshadowed,
        LocksKnown,
        ReadyForLaterChapter,
        Open,
    }

    public static class ChannelZeroIds
    {
        public const string EntryRoom = "Entry";
        public const string LivingRoom = "LivingRoom";
        public const string WorkshopRoom = "Workshop";
        public const string DefaultVisualState = "default";
        public const string RugHint9VisualState = "hint9";

        public const string EntryLivingDoor = "Entry_LivingDoor";
        public const string EntryMail = "Entry_Mail";
        public const string LivingWorkshopDoor = "Living_WorkshopDoor";
        public const string WorkshopFloorPlan = "Workshop_FloorPlan";
        public const string WorkshopFoldingCrank = "Workshop_FoldingCrank";
        public const string FoldingCrankItem = "item.floor_crank";

        public const string PrologueServiceRequestCloseup = "PRO-Z01";

        public const string LivingToolboxCloseup = "LIV-Z00";
        public const string LivingCrtFrontCloseup = "LIV-Z01";
        public const string LivingCrtRearCloseup = "LIV-Z02";
        public const string LivingTubeStorageCloseup = "LIV-Z03";
        public const string LivingNumberRugCloseup = "LIV-Z04";
        public const string LivingMedicalCabinetCloseup = "LIV-Z05";
        public const string LivingHandTreatmentCloseup = "LIV-Z06";
        public const string LivingLockboxCloseup = "LIV-Z07";
        public const string LivingRecPanelCloseup = "LIV-Z08";
        public const string LivingRecSlotsCloseup = "LIV-Z09";
        public const string LivingFamilyPhotosCloseup = "LIV-Z10";
        public const string LivingWiringDiagramCloseup = "LIV-Z11";
        public const string LivingClockCloseup = "LIV-Z12";

        public const string WorkshopTubeTesterCloseup = "WKS-Z01";
        public const string WorkshopPartsDrawerCloseup = "WKS-Z02";
        public const string WorkshopFloorPlanCloseup = "WKS-Z03";
        public const string WorkshopWiringDiagramCloseup = "WKS-Z04";
        public const string WorkshopRepairLogCloseup = "WKS-Z05";
        public const string WorkshopKeyCutterCloseup = "WKS-Z06";
        public const string WorkshopFoldingCrankInspect = "WKS-I01";

        public const string SewingPatternCloseup = "SEW-Z01";
        public const string BasementLockCapsCloseup = "BMT-Z01";
        public const string BasementCrankSocketCloseup = "BMT-Z02";
        public const string BasementFloorPlanRecordCloseup = "BMT-Z03";
    }

    public static class ChannelZeroPuzzleIds
    {
        public const string Toolbox = "ch1.toolbox";
        public const string Rug2749 = "ch1.rug2749";
        public const string Lockbox8888 = "ch1.lockbox8888";
        public const string CrtRear = "ch1.crt_rear";
        public const string TubeCase = "ch1.tube_case";
        public const string MedicalCabinet = "ch1.medical";
        public const string JinwooHand = "ch1.jinwoo_hand";
        public const string RecMaster = "ch1.rec_master";
        public const string FamilyPhotos = "ch1.photos";
        public const string CrtSignal = "ch1.crt_signal";
        public const string ReturnCircuit = "ch1.return_circuit";
        public const string VaseCausality = "ch1.vase_causality";
        public const string ChildTraceStabilizer = "ch1.child_trace_stabilizer";
        public const string ClockKnot = "ch1.clock_knot";
        public const string WorkshopDrawer = "ch2.parts_drawer";
        public const string TubeTester = "ch2.tube_tester";
        public const string FloorPlan = "ch2.floor_plan";
        public const string RepairJournal = "ch2.repair_journal";
        public const string Wiring = "ch2.wiring";
        public const string FoldingCrank = "ch2.folding_crank";

        public const string Screwdriver = "Screwdriver";
        public const string ReplacementTube = "ReplacementTube";
        public const string Disinfectant = "Disinfectant";
        public const string Bandage = "Bandage";
        public const string MasterTape = "MasterTape";
        public const string ClockKey = "ClockKey";
        public const string WorkshopKey = "WorkshopKey";
        public const string NormalTube = "NormalTube";
        public const string FaultyTube = "FaultyTube";
    }

    public static class Chapter1Flags
    {
        public const string ServiceRequestSeen = "ServiceRequestSeen";
        public const string Photo2001BaselineSeen = "Photo2001BaselineSeen";
        public const string UnpluggedTVObserved = "UnpluggedTVObserved";
        public const string SofaChildAppeared = "SofaChildAppeared";
        public const string ChildSofaIntroSeen = "ChildSofaIntroSeen";
        public const string TVRearInspected = "TVRearInspected";
        public const string ScrewdriverOwned = "ScrewdriverOwned";
        public const string ReplacementTubeOwned = "ReplacementTubeOwned";
        public const string TVPhysicalRepairDone = "TVPhysicalRepairDone";
        public const string FirstSlip1961Done = "FirstSlip1961Done";
        public const string ReturnWiringSolved = "ReturnWiringSolved";
        public const string ReturnTubeSequenceSolved = "ReturnTubeSequenceSolved";
        public const string ReturnCircuitSolved = "ReturnCircuitSolved";
        public const string Photo2001ChildCreated = "Photo2001ChildCreated";
        public const string ManualDialUnlocked = "ManualDialUnlocked";
        public const string VaseBroken1961 = "VaseBroken1961";
        public const string VasePreserved1961 = "VasePreserved1961";
        public const string VaseCausalityLearned = "VaseCausalityLearned";
        public const string ChildTrace1961Seen = "ChildTrace1961Seen";
        public const string ChildTrace1981Seen = "ChildTrace1981Seen";
        public const string ChildTrace2001Seen = "ChildTrace2001Seen";
        public const string ChildTrace2021Seen = "ChildTrace2021Seen";
        public const string ChildTrace2749Known = "ChildTrace2749Known";
        public const string SignalStabilizerSolved = "SignalStabilizerSolved";
        public const string ClockKeyOwned = "ClockKeyOwned";
        public const string JinwooInjurySeen = "JinwooInjurySeen";
        public const string JinwooAmputationFutureSeen = "JinwooAmputationFutureSeen";
        public const string JinwooDisinfected = "JinwooDisinfected";
        public const string JinwooBandaged = "JinwooBandaged";
        public const string JinwooHandPreserved = "JinwooHandPreserved";
        public const string ClockKnotEntered = "ClockKnotEntered";
        public const string ClockKnotSolved = "ClockKnotSolved";
        public const string WorkshopKeyOwned = "WorkshopKeyOwned";
        public const string WorkshopKeyUsed = "WorkshopKeyUsed";
        public const string EndingRecognitionActive = "EndingRecognitionActive";
        public const string Chapter1Complete = "Chapter1Complete";
        public const string LegacyMigrationNoticePending = "LegacyChapter1MigrationNoticePending";

        public static readonly string[] CompletionRequirements =
        {
            TVPhysicalRepairDone, ReturnCircuitSolved, Photo2001ChildCreated,
            ManualDialUnlocked, VaseCausalityLearned, SignalStabilizerSolved,
            JinwooHandPreserved, ClockKnotSolved, WorkshopKeyUsed,
        };
    }
}

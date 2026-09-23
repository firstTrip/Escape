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

        public const string EntryLivingDoor = "Entry_LivingDoor";
        public const string EntryMail = "Entry_Mail";
        public const string LivingWorkshopDoor = "Living_WorkshopDoor";
        public const string WorkshopFloorPlan = "Workshop_FloorPlan";
        public const string WorkshopFoldingCrank = "Workshop_FoldingCrank";
        public const string FoldingCrankItem = "item.floor_crank";

        public const string PrologueServiceRequestCloseup = "PRO-Z01";

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
}

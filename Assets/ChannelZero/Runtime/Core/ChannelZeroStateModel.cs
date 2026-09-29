using System;
using System.Collections.Generic;

namespace ChannelZero.Runtime.Core
{
    public enum StatePersistence
    {
        Physical,
        Knowledge,
        Meta,
        Committed,
        CausalEvent,
        Transient,
    }

    public readonly struct StateDescriptor
    {
        public StatePersistence Persistence { get; }
        public string RoomId { get; }
        public bool EraScoped { get; }

        public StateDescriptor(StatePersistence persistence, string roomId = "", bool eraScoped = false)
        {
            Persistence = persistence;
            RoomId = roomId ?? string.Empty;
            EraScoped = eraScoped;
        }
    }

    public static class ChannelZeroStateCatalog
    {
        private static readonly Dictionary<string, StateDescriptor> descriptors =
            new(StringComparer.Ordinal)
            {
                [ChannelZeroPuzzleIds.Toolbox] = Physical(ChannelZeroIds.LivingRoom),
                [ChannelZeroPuzzleIds.Rug2749] = Physical(ChannelZeroIds.LivingRoom),
                [ChannelZeroPuzzleIds.Lockbox8888] = Physical(ChannelZeroIds.LivingRoom),
                [ChannelZeroPuzzleIds.CrtRear] = Physical(ChannelZeroIds.LivingRoom),
                [ChannelZeroPuzzleIds.TubeCase] = Physical(ChannelZeroIds.LivingRoom),
                [ChannelZeroPuzzleIds.MedicalCabinet] = Physical(ChannelZeroIds.LivingRoom),
                [ChannelZeroPuzzleIds.JinwooHand] = Physical(ChannelZeroIds.LivingRoom),
                [ChannelZeroPuzzleIds.RecMaster] = Physical(ChannelZeroIds.LivingRoom),
                [ChannelZeroPuzzleIds.FamilyPhotos] = Physical(ChannelZeroIds.LivingRoom),
                [ChannelZeroPuzzleIds.CrtSignal] = Physical(ChannelZeroIds.LivingRoom),
                [ChannelZeroPuzzleIds.ReturnCircuit] = Committed(ChannelZeroIds.LivingRoom),
                [ChannelZeroPuzzleIds.VaseCausality] = Causal(ChannelZeroIds.LivingRoom),
                [ChannelZeroPuzzleIds.ChildTraceStabilizer] = Committed(ChannelZeroIds.LivingRoom),
                [ChannelZeroPuzzleIds.ClockKnot] = Committed(ChannelZeroIds.LivingRoom),
                [ChannelZeroPuzzleIds.WorkshopDrawer] = Physical(ChannelZeroIds.WorkshopRoom),
                [ChannelZeroPuzzleIds.TubeTester] = Physical(ChannelZeroIds.WorkshopRoom),
                [ChannelZeroPuzzleIds.FloorPlan] = Physical(ChannelZeroIds.WorkshopRoom),
                [ChannelZeroPuzzleIds.RepairJournal] = Physical(ChannelZeroIds.WorkshopRoom),
                [ChannelZeroPuzzleIds.Wiring] = Physical(ChannelZeroIds.WorkshopRoom),
                [ChannelZeroPuzzleIds.FoldingCrank] = Physical(ChannelZeroIds.WorkshopRoom),
                ["result:TubeTest"] = Physical(ChannelZeroIds.WorkshopRoom),
                ["flag:CrtRearOpen"] = Physical(ChannelZeroIds.LivingRoom),
                ["flag:ToolboxScrewdriverTaken"] = Physical(ChannelZeroIds.LivingRoom),
                ["flag:TubeCaseLooted"] = Physical(ChannelZeroIds.LivingRoom),
                ["flag:MedicalSuppliesTaken"] = Physical(ChannelZeroIds.LivingRoom),
                ["flag:CrtRepaired"] = Physical(ChannelZeroIds.LivingRoom),
                ["flag:JinwooInjured"] = Physical(ChannelZeroIds.LivingRoom),
                ["flag:JinwooTreated"] = Physical(ChannelZeroIds.LivingRoom),
                ["flag:Rug2749Solved"] = Physical(ChannelZeroIds.LivingRoom),
                ["flag:Lockbox8888Solved"] = Physical(ChannelZeroIds.LivingRoom),
                ["flag:TubeTestNormal"] = Physical(ChannelZeroIds.WorkshopRoom),
                ["flag:PartsDrawerSorted"] = Physical(ChannelZeroIds.WorkshopRoom),
                ["flag:FoldingCrank"] = Physical(ChannelZeroIds.WorkshopRoom),
                ["flag:FoldingCrankHeld"] = Physical(ChannelZeroIds.WorkshopRoom),
                ["flag:RecMasterComplete"] = Meta(),
                ["flag:CrtRearInspected"] = Knowledge(ChannelZeroIds.LivingRoom),
                ["flag:FloorPlanSeen"] = Knowledge(ChannelZeroIds.WorkshopRoom),
                ["flag:AllJournalPagesSeen"] = Knowledge(ChannelZeroIds.WorkshopRoom),
                ["flag:WiringSeen"] = Knowledge(ChannelZeroIds.WorkshopRoom),
                ["flag:KeyCutterSeen"] = Knowledge(ChannelZeroIds.WorkshopRoom),
                ["flag:MinaFirstSeen"] = Knowledge(ChannelZeroIds.LivingRoom),
                ["flag:CrtNoiseSeen"] = Knowledge(ChannelZeroIds.LivingRoom),
                ["flag:FamilyPhotoClueSeen"] = Knowledge(ChannelZeroIds.LivingRoom),
                ["flag:MinaAllErasConfirmed"] = Knowledge(ChannelZeroIds.LivingRoom),
                ["flag:AllFamilyPhotosSeen"] = Knowledge(ChannelZeroIds.LivingRoom),
                ["flag:CrtWiringSeen"] = Knowledge(ChannelZeroIds.LivingRoom),
                ["flag:Clock808Seen"] = Knowledge(ChannelZeroIds.LivingRoom),
                ["flag:WorkshopWiringSeen"] = Knowledge(ChannelZeroIds.WorkshopRoom),
                ["flag:Chapter1Complete"] = Meta(),
                ["flag:Chapter2Complete"] = Meta(),
                ["flag:LiveReference2001Seen"] = Knowledge(ChannelZeroIds.LivingRoom),
            };

        static ChannelZeroStateCatalog()
        {
            foreach (int year in new[] { 1961, 1981, 2001, 2021 })
            {
                descriptors["flag:FamilyPhoto" + year] = Knowledge(ChannelZeroIds.LivingRoom);
                descriptors["flag:Journal" + year] = Knowledge(ChannelZeroIds.WorkshopRoom);
            }
            foreach (string flag in new[]
            {
                Chapter1Flags.ServiceRequestSeen, Chapter1Flags.Photo2001BaselineSeen,
                Chapter1Flags.UnpluggedTVObserved, Chapter1Flags.SofaChildAppeared,
                Chapter1Flags.ChildSofaIntroSeen, Chapter1Flags.TVRearInspected,
                Chapter1Flags.ScrewdriverOwned, Chapter1Flags.ReplacementTubeOwned,
                Chapter1Flags.TVPhysicalRepairDone, Chapter1Flags.FirstSlip1961Done,
                Chapter1Flags.ReturnWiringSolved, Chapter1Flags.ReturnTubeSequenceSolved,
                Chapter1Flags.ReturnCircuitSolved, Chapter1Flags.Photo2001ChildCreated,
                Chapter1Flags.ManualDialUnlocked, Chapter1Flags.ChildTrace2749Known,
                Chapter1Flags.SignalStabilizerSolved, Chapter1Flags.ClockKeyOwned,
                Chapter1Flags.ClockKnotSolved, Chapter1Flags.WorkshopKeyOwned,
                Chapter1Flags.WorkshopKeyUsed,
            }) descriptors["flag:" + flag] = Committed(ChannelZeroIds.LivingRoom);
            foreach (string flag in new[]
            {
                Chapter1Flags.VaseBroken1961, Chapter1Flags.VasePreserved1961,
                Chapter1Flags.VaseCausalityLearned, Chapter1Flags.JinwooInjurySeen,
                Chapter1Flags.JinwooAmputationFutureSeen, Chapter1Flags.JinwooDisinfected,
                Chapter1Flags.JinwooBandaged, Chapter1Flags.JinwooHandPreserved,
            }) descriptors["flag:" + flag] = Causal(ChannelZeroIds.LivingRoom);
            foreach (string flag in new[]
            {
                Chapter1Flags.ChildTrace1961Seen, Chapter1Flags.ChildTrace1981Seen,
                Chapter1Flags.ChildTrace2001Seen, Chapter1Flags.ChildTrace2021Seen,
                Chapter1Flags.ClockKnotEntered,
            }) descriptors["flag:" + flag] = Knowledge(ChannelZeroIds.LivingRoom);
            descriptors["flag:" + Chapter1Flags.Chapter1Complete] = Meta();
            descriptors["flag:" + Chapter1Flags.LegacyMigrationNoticePending] = Meta();
        }

        public static StateDescriptor Resolve(string stateId, string currentRoomId)
        {
            if (!string.IsNullOrWhiteSpace(stateId) && descriptors.TryGetValue(stateId, out StateDescriptor descriptor))
                return descriptor;
            if (!string.IsNullOrWhiteSpace(stateId) && stateId.StartsWith("hint:", StringComparison.Ordinal))
                return Meta();
            return new StateDescriptor(StatePersistence.Physical, currentRoomId);
        }

        public static StateDescriptor ResolveLegacy(string stateId, string currentRoomId)
        {
            if (!string.IsNullOrWhiteSpace(stateId) && descriptors.TryGetValue(stateId, out StateDescriptor descriptor))
                return descriptor;
            if (!string.IsNullOrWhiteSpace(stateId) && stateId.StartsWith("hint:", StringComparison.Ordinal))
                return Meta();
            return new StateDescriptor(StatePersistence.Physical, InferLegacyRoom(stateId, currentRoomId));
        }

        private static StateDescriptor Knowledge(string roomId) =>
            new(StatePersistence.Knowledge, roomId);

        private static StateDescriptor Physical(string roomId) =>
            new(StatePersistence.Physical, roomId);

        private static StateDescriptor Committed(string roomId) =>
            new(StatePersistence.Committed, roomId);

        private static StateDescriptor Causal(string roomId) =>
            new(StatePersistence.CausalEvent, roomId, true);

        private static StateDescriptor Meta() => new(StatePersistence.Meta);

        private static string InferLegacyRoom(string stateId, string fallback)
        {
            if (string.IsNullOrWhiteSpace(stateId))
                return fallback ?? string.Empty;
            if (stateId.StartsWith("ch1.", StringComparison.Ordinal) || LivingPhysicalFlags.Contains(stateId))
                return ChannelZeroIds.LivingRoom;
            if (stateId.StartsWith("ch2.", StringComparison.Ordinal) ||
                stateId.StartsWith("result:", StringComparison.Ordinal) || WorkshopPhysicalFlags.Contains(stateId))
                return ChannelZeroIds.WorkshopRoom;
            if (stateId.StartsWith("prologue.", StringComparison.Ordinal))
                return ChannelZeroIds.EntryRoom;
            return fallback ?? string.Empty;
        }

        private static readonly HashSet<string> LivingPhysicalFlags = new(StringComparer.Ordinal)
        {
            "flag:CrtRearOpen", "flag:CrtRepaired", "flag:JinwooInjured", "flag:JinwooTreated",
            "flag:Rug2749Solved", "flag:Lockbox8888Solved",
        };

        private static readonly HashSet<string> WorkshopPhysicalFlags = new(StringComparer.Ordinal)
        {
            "flag:TubeTestNormal", "flag:FoldingCrank",
        };
    }
}

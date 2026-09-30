using System;
using System.Collections.Generic;
using System.Linq;

namespace ChannelZero.Runtime.Core
{
    public sealed class ChannelZeroInventoryService
    {
        private readonly ChannelZeroSessionState state;
        private readonly PuzzleDefinitionCatalog catalog;
        public ChannelZeroInventoryService(ChannelZeroSessionState state, PuzzleDefinitionCatalog catalog) { this.state = state; this.catalog = catalog; }
        public bool Select(string itemId)
        {
            if (!state.HasItem(itemId)) return false;
            state.selectedInventoryItemId = itemId;
            return true;
        }
        public bool HoldSelected()
        {
            if (string.IsNullOrWhiteSpace(state.selectedInventoryItemId)) return false;
            if (catalog.TryGetItem(state.selectedInventoryItemId, out InventoryItemDefinition item) && !item.canHold) return false;
            return state.TryHold(state.selectedInventoryItemId);
        }
        public string DisplayName(string itemId) => catalog.TryGetItem(itemId, out InventoryItemDefinition item) ? item.displayName : itemId;
        public string Description(string itemId) => catalog.TryGetItem(itemId, out InventoryItemDefinition item) ? item.description : string.Empty;
    }

    public sealed class ChannelZeroRoomNavigationService
    {
        private readonly ChannelZeroSessionState state;
        public ChannelZeroRoomNavigationService(ChannelZeroSessionState state) { this.state = state; }
        public bool CanEnterLiving => state.recordIds.Contains(ChannelZeroIds.PrologueServiceRequestCloseup) ||
            state.recordIds.Contains("PRO.ENTRY.MAIL.DOC.REQUEST");
        public bool CanEnterWorkshop => state.GetFlag("Chapter1Complete");
        public bool TryEnterLiving()
        {
            if (!CanEnterLiving) return false;
            state.MoveTo(ChannelZeroIds.LivingRoom, StoryChapter.Chapter1);
            state.Tune(ChannelEra.Year2001);
            return true;
        }
        public bool TryEnterWorkshop() { if (!CanEnterWorkshop) return false; state.MoveTo(ChannelZeroIds.WorkshopRoom, StoryChapter.Chapter2); return true; }
        public bool TryBack() => state.TryGoBack();
    }

    public sealed class ChannelZeroChapterProgressionService
    {
        private readonly ChannelZeroSessionState state;
        private readonly ChannelZeroInteractionCatalog catalog;
        private readonly IChannelZeroRecordingService recording;
        public ChannelZeroChapterProgressionService(ChannelZeroSessionState state,
            ChannelZeroInteractionCatalog catalog = null,
            IChannelZeroRecordingService recording = null)
        {
            this.state = state;
            this.catalog = catalog ?? ChannelZeroInteractionCatalog.LoadDefault();
            this.recording = recording;
        }
        public void Refresh()
        {
            foreach (ChapterCompletionDefinition chapter in catalog.Chapters)
            {
                bool puzzleStatesReady = (chapter.requiredPuzzleStates ?? Array.Empty<PuzzleStateRequirement>())
                    .All(requirement => state.GetPuzzleState(requirement.puzzleId) == requirement.stateId);
                bool flagsReady = (chapter.requiredFlags ?? Array.Empty<string>()).All(state.GetFlag);
                bool itemsReady = (chapter.requiredItems ?? Array.Empty<string>()).All(state.HasItem);
                state.SetFlag(chapter.completionFlag, puzzleStatesReady && flagsReady && itemsReady);
            }
        }

        public string DescribeChapterOneBlockers()
        {
            List<string> remaining = new();
            if (!state.GetFlag(Chapter1Flags.TVPhysicalRepairDone)) remaining.Add("TV 후면 수리");
            if (!state.GetFlag(Chapter1Flags.ReturnCircuitSolved)) remaining.Add("1961 귀환 회로");
            if (!state.GetFlag(Chapter1Flags.VaseCausalityLearned)) remaining.Add("화병 인과 확인");
            if (!state.GetFlag(Chapter1Flags.SignalStabilizerSolved)) remaining.Add("신호 안정기 2749");
            if (!state.GetFlag(Chapter1Flags.JinwooHandPreserved)) remaining.Add("진우의 오른손 보존");
            if (!state.GetFlag(Chapter1Flags.ClockKnotSolved)) remaining.Add("08:08 괘종시계");
            if (!state.GetFlag(Chapter1Flags.WorkshopKeyOwned)) remaining.Add("작업실 열쇠");

            return remaining.Count == 0
                ? "작업실 잠금 조건은 충족됐다. 문을 다시 확인해 보자."
                : $"작업실 잠금이 아직 풀리지 않았다. 남은 진행: {string.Join(", ", remaining)}";
        }

        private bool HasRecording(ChannelEra era) => recording?.HasRecording(ChannelZeroIds.LivingRoom, era)
            ?? ChannelZeroRecordingState.HasRecording(state, ChannelZeroIds.LivingRoom, era);
    }

    public static class ChannelZeroRecordingState
    {
        public static bool HasRecording(ChannelZeroSessionState state, string roomId, ChannelEra era) =>
            state?.timelineSnapshots != null && state.timelineSnapshots.Any(snapshot =>
                snapshot != null && snapshot.roomId == roomId && snapshot.era == era);
    }

    public sealed class ChannelZeroRewindPlan
    {
        public string RoomId { get; }
        public ChannelEra Era { get; }
        public bool HasCheckpoint { get; }
        public string Summary { get; }

        public ChannelZeroRewindPlan(string roomId, ChannelEra era, bool hasCheckpoint, string summary)
        {
            RoomId = roomId;
            Era = era;
            HasCheckpoint = hasCheckpoint;
            Summary = summary;
        }
    }

    public sealed class ChannelZeroTimelineService : IChannelZeroRecordingService
    {
        private readonly ChannelZeroSessionState state;
        private readonly ChannelZeroInventoryService inventory;
        private readonly ChannelZeroScenarioProgressionService scenario;
        private readonly PuzzleDefinitionCatalog puzzleCatalog;
        public ChannelZeroTimelineService(ChannelZeroSessionState state, ChannelZeroInventoryService inventory,
            PuzzleDefinitionCatalog puzzleCatalog = null)
        {
            this.state = state;
            this.inventory = inventory;
            this.puzzleCatalog = puzzleCatalog ?? LoadPuzzleCatalog();
            scenario = new ChannelZeroScenarioProgressionService(state);
        }

        public TimelineSnapshot Record()
        {
            state.timelineSnapshots ??= new List<TimelineSnapshot>();
            state.timelineSnapshots.RemoveAll(snapshot => snapshot.roomId == state.roomId && snapshot.era == state.era);
            TimelineSnapshot snapshot = Capture("REC:" + state.roomId + ":" + (int)state.era);
            state.timelineSnapshots.Add(snapshot);
            state.operation = ChannelOperation.Rec;
            return snapshot;
        }

        public TimelineSnapshot RecordCurrent() => Record();

        public bool HasRecording(string roomId, ChannelEra era) =>
            ChannelZeroRecordingState.HasRecording(state, roomId, era);

        public TimelineSnapshot Play()
        {
            state.operation = ChannelOperation.Play;
            return FindLatest();
        }

        public TimelineSnapshot LatestRecordForCurrentRoom() => FindLatest()?.Clone();

        public bool Load()
        {
            if (FindLatest() == null) return false;
            state.operation = ChannelOperation.Load;
            return true;
        }

        public bool Rewind()
        {
            TimelineSnapshot snapshot = FindExact();
            if (snapshot == null)
                ResetUnresolvedPhysicalStateToInitial();
            else
                ApplyPreservingKnowledge(snapshot);
            state.operation = ChannelOperation.Rew;
            return true;
        }

        public ChannelZeroRewindPlan PreviewRewind()
        {
            bool hasCheckpoint = FindExact() != null;
            string source = hasCheckpoint ? "최근 REC 체크포인트" : "퍼즐 초기 상태";
            return new ChannelZeroRewindPlan(state.roomId, state.era, hasCheckpoint,
                $"REW 미리보기 — {(int)state.era}년 {state.roomId}의 미해결 물리 상태를 {source}로 되돌립니다. 단서·완료 퍼즐·핵심 아이템은 유지됩니다.");
        }

        public bool Apply(ChannelZeroRewindPlan plan)
        {
            if (plan == null || plan.RoomId != state.roomId || plan.Era != state.era)
                return false;
            return Rewind();
        }

        public bool Hold()
        {
            bool held = inventory.HoldSelected();
            if (held) state.operation = ChannelOperation.Hold;
            return held;
        }

        private TimelineSnapshot Capture(string id)
        {
            TimelineSnapshot snapshot = new()
            {
                snapshotId = id,
                roomId = state.roomId,
                era = state.era,
                visualStateId = state.roomVisualStateId,
                scenarioStageId = scenario.CurrentStage.ToString(),
                selectedInventoryItemId = state.selectedInventoryItemId,
            };
            foreach (PuzzleStateEntry entry in state.puzzleStates.Where(IsRewindablePuzzleState))
                snapshot.puzzleStates.Add(entry.Clone());
            foreach (string itemId in state.inventoryItemIds.Where(itemId => ItemBelongsToRoom(itemId, state.roomId)))
                snapshot.inventoryItemIds.Add(itemId);
            return snapshot;
        }

        private TimelineSnapshot FindExact() => state.timelineSnapshots?
            .LastOrDefault(snapshot => snapshot.roomId == state.roomId && snapshot.era == state.era);

        private TimelineSnapshot FindLatest() => FindExact()
            ?? state.timelineSnapshots?.LastOrDefault(snapshot => snapshot.roomId == state.roomId);

        private void ApplyPreservingKnowledge(TimelineSnapshot snapshot)
        {
            IReadOnlyList<PuzzleDefinition> rewindable = RewindableDefinitions();
            HashSet<string> rewindableItems = RewindableItems(rewindable);
            state.puzzleStates.RemoveAll(entry => IsEntryForDefinitions(entry, rewindable));
            foreach (PuzzleStateEntry entry in snapshot.puzzleStates ?? new List<PuzzleStateEntry>())
                if (IsEntryForDefinitions(entry, rewindable))
                    state.SetPuzzleState(entry.puzzleId, entry.stateId, entry.persistence,
                        entry.roomId, entry.eraScoped, entry.era);

            string heldItemId = state.heldItemId;
            List<string> rewindItems = state.inventoryItemIds
                .Where(itemId => rewindableItems.Contains(itemId) && itemId != heldItemId).ToList();
            foreach (string itemId in rewindItems)
                state.RemoveItem(itemId);
            foreach (string itemId in snapshot.inventoryItemIds ?? new List<string>())
                if (rewindableItems.Contains(itemId))
                    state.AddItem(itemId);
            if (!string.IsNullOrWhiteSpace(heldItemId))
                state.AddItem(heldItemId);

            state.selectedInventoryItemId = !string.IsNullOrWhiteSpace(snapshot.selectedInventoryItemId) &&
                state.HasItem(snapshot.selectedInventoryItemId)
                    ? snapshot.selectedInventoryItemId
                    : string.Empty;
            state.roomVisualStateId = snapshot.visualStateId;
        }

        private void ResetUnresolvedPhysicalStateToInitial()
        {
            IReadOnlyList<PuzzleDefinition> rewindable = RewindableDefinitions();
            HashSet<string> rewindableItems = RewindableItems(rewindable);
            state.puzzleStates.RemoveAll(entry => IsEntryForDefinitions(entry, rewindable));
            string heldItemId = state.heldItemId;
            foreach (string itemId in state.inventoryItemIds
                         .Where(itemId => rewindableItems.Contains(itemId) && itemId != heldItemId).ToList())
                state.RemoveItem(itemId);
            state.selectedInventoryItemId = string.Empty;
            state.roomVisualStateId = ChannelZeroIds.DefaultVisualState;
        }

        private bool IsRewindablePuzzleState(PuzzleStateEntry entry) =>
            IsEntryForDefinitions(entry, RewindableDefinitions());

        private IReadOnlyList<PuzzleDefinition> RewindableDefinitions() => puzzleCatalog.Puzzles
            .Where(definition => definition.ActiveEra == state.era &&
                (string.IsNullOrWhiteSpace(definition.solvedFlag) || !state.GetFlag(definition.solvedFlag)) &&
                HotspotMatchesRoom(definition.hotspotId, state.roomId))
            .ToList();

        private bool IsEntryForDefinitions(PuzzleStateEntry entry,
            IReadOnlyList<PuzzleDefinition> definitions)
        {
            if (entry == null || entry.persistence != StatePersistence.Physical ||
                !string.Equals(entry.roomId, state.roomId, StringComparison.Ordinal))
                return false;
            return definitions.Any(definition =>
                string.Equals(entry.puzzleId, definition.RuntimePuzzleId, StringComparison.Ordinal) ||
                entry.puzzleId.StartsWith(definition.RuntimePuzzleId + ".", StringComparison.Ordinal));
        }

        private static HashSet<string> RewindableItems(IEnumerable<PuzzleDefinition> definitions)
        {
            HashSet<string> items = new(StringComparer.Ordinal);
            foreach (PuzzleDefinition definition in definitions)
            {
                foreach (string itemId in definition.lootItems ?? Array.Empty<string>())
                    items.Add(itemId);
                if (definition.RuntimePuzzleId == ChannelZeroPuzzleIds.JinwooHand)
                {
                    items.Add(ChannelZeroPuzzleIds.Disinfectant);
                    items.Add(ChannelZeroPuzzleIds.Bandage);
                }
                if (definition.RuntimePuzzleId == ChannelZeroPuzzleIds.CrtRear)
                    items.Add(ChannelZeroPuzzleIds.ReplacementTube);
            }
            return items;
        }

        private static bool HotspotMatchesRoom(string hotspotId, string roomId) =>
            roomId == ChannelZeroIds.LivingRoom
                ? hotspotId.StartsWith("Living_", StringComparison.Ordinal)
                : roomId == ChannelZeroIds.WorkshopRoom &&
                  hotspotId.StartsWith("Workshop_", StringComparison.Ordinal);

        private static PuzzleDefinitionCatalog LoadPuzzleCatalog()
        {
            return PuzzleDefinitionCatalog.LoadDefault();
        }

        private bool ItemBelongsToRoom(string itemId, string roomId)
        {
            string recordedOrigin = state.GetItemOriginRoom(itemId);
            if (!string.IsNullOrWhiteSpace(recordedOrigin))
                return recordedOrigin == roomId;

            // Save v1 did not track origin rooms. Keep its chapter 1/2 items rewindable.
            if (roomId == ChannelZeroIds.LivingRoom) return LivingItems.Contains(itemId);
            if (roomId == ChannelZeroIds.WorkshopRoom) return WorkshopItems.Contains(itemId);
            return false;
        }

        private static readonly HashSet<string> LivingItems = new()
        {
            ChannelZeroPuzzleIds.Screwdriver, ChannelZeroPuzzleIds.ReplacementTube,
            ChannelZeroPuzzleIds.Disinfectant, ChannelZeroPuzzleIds.Bandage, ChannelZeroPuzzleIds.MasterTape,
        };
        private static readonly HashSet<string> WorkshopItems = new()
        {
            ChannelZeroPuzzleIds.NormalTube, ChannelZeroPuzzleIds.FaultyTube, ChannelZeroIds.FoldingCrankItem,
        };
    }

    public sealed class ChannelZeroComparisonOverlay
    {
        public TimelineSnapshot Record { get; }
        public string CurrentRoomId { get; }
        public ChannelEra CurrentEra { get; }
        public IReadOnlyList<string> Differences { get; }

        public ChannelZeroComparisonOverlay(TimelineSnapshot record, string currentRoomId,
            ChannelEra currentEra, IReadOnlyList<string> differences)
        {
            Record = record;
            CurrentRoomId = currentRoomId;
            CurrentEra = currentEra;
            Differences = differences ?? Array.Empty<string>();
        }

        public string Summary => (Differences.Count == 0
            ? $"{(int)Record.era}년 기록과 현재 장면에서 확인된 차이가 없다."
            : $"{(int)Record.era}년 기록을 {(int)CurrentEra}년 현재 장면 위에 겹쳤다.\n" +
              string.Join("\n", Differences)) + "\n비교 전용 화면이며 현재 세계에 영향을 주지 않는다.";
    }

    public sealed class ChannelZeroComparisonOverlayService
    {
        private readonly ChannelZeroSessionState state;
        private readonly ChannelZeroTimelineService timeline;
        public ChannelZeroComparisonOverlay Current { get; private set; }
        public bool IsOpen => Current != null;

        public ChannelZeroComparisonOverlayService(ChannelZeroSessionState state,
            ChannelZeroTimelineService timeline)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
        }

        public bool OpenLatest()
        {
            TimelineSnapshot record = timeline.LatestRecordForCurrentRoom();
            if (record == null)
                return false;
            Current = new ChannelZeroComparisonOverlay(record, state.roomId, state.era,
                BuildDifferences(record));
            state.operation = ChannelOperation.Load;
            return true;
        }

        public void Close() => Current = null;

        private IReadOnlyList<string> BuildDifferences(TimelineSnapshot record)
        {
            List<string> differences = new();
            if (record.era != state.era)
                differences.Add($"연도: {(int)record.era} → {(int)state.era}");
            if (!string.Equals(record.visualStateId, state.roomVisualStateId, StringComparison.Ordinal))
                differences.Add("공간의 물리 상태가 다르다.");

            Dictionary<string, string> recorded = new(StringComparer.Ordinal);
            foreach (PuzzleStateEntry entry in record.puzzleStates ?? new List<PuzzleStateEntry>())
                if (entry != null && !string.IsNullOrWhiteSpace(entry.puzzleId))
                    recorded[entry.puzzleId] = entry.stateId;
            foreach (PuzzleStateEntry entry in state.puzzleStates.Where(entry =>
                         entry.persistence == StatePersistence.Physical && entry.roomId == state.roomId))
                if (!recorded.TryGetValue(entry.puzzleId, out string previous) || previous != entry.stateId)
                    differences.Add($"장치 변화: {entry.puzzleId}");

            HashSet<string> recordedItems = new(record.inventoryItemIds ?? new List<string>(), StringComparer.Ordinal);
            int itemDifference = state.inventoryItemIds.Count(item => !recordedItems.Contains(item));
            if (itemDifference > 0)
                differences.Add($"현재 추가 소지품: {itemDifference}개");
            return differences;
        }
    }

    public sealed class ChannelZeroHintService
    {
        private readonly ChannelZeroSessionState state;
        private readonly PuzzleDefinitionCatalog catalog;
        public ChannelZeroHintService(ChannelZeroSessionState state, PuzzleDefinitionCatalog catalog) { this.state = state; this.catalog = catalog; }
        public string Request(string closeupId)
        {
            if (!catalog.TryGetPuzzle(closeupId, out PuzzleDefinition definition) || definition.hints == null || definition.hints.Length == 0)
                return "이 퍼즐에는 아직 등록된 힌트가 없습니다.";
            string key = "hint:" + definition.id;
            int.TryParse(state.GetPuzzleState(key, "0"), out int level);
            level = Math.Min(level + 1, definition.hints.Length);
            state.SetPuzzleState(key, level.ToString(), StatePersistence.Meta);
            return $"힌트 {level}/{definition.hints.Length}: {definition.hints[level - 1]}";
        }
    }

    public sealed class ChannelZeroOperationService
    {
        private readonly ChannelZeroSessionState state;
        private readonly ChannelZeroTimelineService timeline;
        private readonly ChannelZeroInventoryService inventory;
        private readonly ChannelZeroChapterProgressionService progression;
        private readonly ChannelZeroComparisonOverlayService comparison;
        private readonly Dictionary<ChannelOperation, Func<string>> commands;
        private ChannelZeroRewindPlan pendingRewind;

        public ChannelZeroOperationService(ChannelZeroSessionState state,
            ChannelZeroTimelineService timeline, ChannelZeroInventoryService inventory,
            ChannelZeroChapterProgressionService progression,
            ChannelZeroComparisonOverlayService comparison = null)
        {
            this.state = state;
            this.timeline = timeline;
            this.inventory = inventory;
            this.progression = progression;
            this.comparison = comparison ?? new ChannelZeroComparisonOverlayService(state, timeline);
            commands = new Dictionary<ChannelOperation, Func<string>>
            {
                [ChannelOperation.Rec] = Record,
                [ChannelOperation.Play] = Play,
                [ChannelOperation.Load] = Load,
                [ChannelOperation.Rew] = Rewind,
                [ChannelOperation.Hold] = Hold,
            };
        }

        public string Execute(ChannelOperation operation)
        {
            if (operation != ChannelOperation.Rew)
                pendingRewind = null;
            if (commands.TryGetValue(operation, out Func<string> command))
            {
                string result = command();
                progression.Refresh();
                return result;
            }
            state.operation = operation;
            return "READY";
        }

        private string Record()
        {
            if (state.roomId == ChannelZeroIds.LivingRoom && !state.GetFlag("CrtRepaired"))
                return "REC 실패 — CRT 수리를 먼저 완료해야 합니다.";
            TimelineSnapshot snapshot = timeline.Record();
            return $"REC 완료 — {(int)snapshot.era}년 {snapshot.roomId} 상태 저장";
        }

        private string Play()
        {
            TimelineSnapshot snapshot = timeline.Play();
            return snapshot == null ? "PLAY 실패 — 현재 방의 기록이 없습니다." :
                $"PLAY — {(int)snapshot.era}년 기록 확인";
        }

        private string Load() => comparison.OpenLatest()
            ? "LOAD — 기록 비교 레이어가 준비되었습니다. REC 오브젝트를 선택하세요."
            : "LOAD 실패 — 불러올 기록이 없습니다.";

        private string Rewind()
        {
            if (pendingRewind == null || pendingRewind.RoomId != state.roomId ||
                pendingRewind.Era != state.era)
            {
                pendingRewind = timeline.PreviewRewind();
                return pendingRewind.Summary + " REW를 한 번 더 눌러 실행하세요.";
            }

            bool usedCheckpoint = pendingRewind.HasCheckpoint;
            bool applied = timeline.Apply(pendingRewind);
            pendingRewind = null;
            return applied
                ? usedCheckpoint
                    ? "REW 완료 — 미해결 물리 상태를 현재 시점의 REC 체크포인트로 되돌렸습니다."
                    : "REW 완료 — REC가 없어 미해결 물리 상태를 초기 상태로 되돌렸습니다."
                : "REW 취소 — 방이나 연도가 바뀌었습니다. 다시 미리보기를 확인하세요.";
        }

        private string Hold() => timeline.Hold()
            ? $"HOLD — {inventory.DisplayName(state.heldItemId)} 고정"
            : "HOLD 실패 — 먼저 인벤토리 아이템을 선택하세요.";
    }
}

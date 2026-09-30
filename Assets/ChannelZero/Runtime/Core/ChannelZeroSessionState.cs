using System;
using System.Collections.Generic;

namespace ChannelZero.Runtime.Core
{
    [Serializable]
    public sealed class PuzzleStateEntry
    {
        public string puzzleId;
        public string stateId;
        public StatePersistence persistence;
        public string roomId;
        public ChannelEra era;
        public bool eraScoped;

        public PuzzleStateEntry(string puzzleId, string stateId,
            StatePersistence persistence = StatePersistence.Physical,
            string roomId = "", ChannelEra era = ChannelEra.Year2001, bool eraScoped = false)
        {
            this.puzzleId = puzzleId;
            this.stateId = stateId;
            this.persistence = persistence;
            this.roomId = roomId ?? string.Empty;
            this.era = era;
            this.eraScoped = eraScoped;
        }

        public PuzzleStateEntry Clone() =>
            new(puzzleId, stateId, persistence, roomId, era, eraScoped);
    }

    [Serializable]
    public sealed class RoomHistoryEntry
    {
        public string roomId;
        public StoryChapter chapter;
        public ChannelEra era;
        public string visualStateId;

        public RoomHistoryEntry(string roomId, StoryChapter chapter, ChannelEra era, string visualStateId)
        {
            this.roomId = roomId;
            this.chapter = chapter;
            this.era = era;
            this.visualStateId = visualStateId;
        }
    }

    [Serializable]
    public sealed class InventoryOriginEntry
    {
        public string itemId;
        public string roomId;

        public InventoryOriginEntry(string itemId, string roomId)
        {
            this.itemId = itemId;
            this.roomId = roomId;
        }
    }

    [Serializable]
    public sealed class ChannelZeroSessionState
    {
        public const int CurrentSaveVersion = 4;

        public int saveVersion = CurrentSaveVersion;
        public StoryChapter chapter = StoryChapter.Prologue;
        public string roomId = ChannelZeroIds.EntryRoom;
        public ChannelEra era = ChannelEra.Year2001;
        public string roomVisualStateId = ChannelZeroIds.DefaultVisualState;
        public ChannelOperation operation = ChannelOperation.None;
        public DisappearingStairState disappearingStairState = DisappearingStairState.Hidden;
        public string heldItemId = string.Empty;
        public string selectedInventoryItemId = string.Empty;
        public string activeCloseupId = string.Empty;
        public string activeCloseupStateId = ChannelZeroIds.DefaultVisualState;
        public string closeupOriginRoomId = string.Empty;
        public ChannelEra closeupOriginEra = ChannelEra.Year2001;
        public string closeupOriginVisualStateId = ChannelZeroIds.DefaultVisualState;
        public bool closeupInputLocked;
        public List<string> visitedRoomIds = new();
        public List<string> observedHotspotIds = new();
        public List<string> recordIds = new();
        public List<string> seenTextIds = new();
        public List<string> inventoryItemIds = new();
        public List<InventoryOriginEntry> inventoryOrigins = new();
        public List<PuzzleStateEntry> puzzleStates = new();
        public List<RoomHistoryEntry> roomHistory = new();
        public List<TimelineSnapshot> timelineSnapshots = new();

        public bool CanGoBack => roomHistory != null && roomHistory.Count > 0;

        public static ChannelZeroSessionState CreateNew()
        {
            ChannelZeroSessionState state = new();
            state.MarkVisited(ChannelZeroIds.EntryRoom);
            return state;
        }

        public void MoveTo(string nextRoomId, StoryChapter nextChapter)
        {
            if (string.IsNullOrWhiteSpace(nextRoomId))
                throw new ArgumentException("Room ID is required.", nameof(nextRoomId));

            if (roomId != nextRoomId)
            {
                roomHistory ??= new List<RoomHistoryEntry>();
                roomHistory.Add(new RoomHistoryEntry(roomId, chapter, era, roomVisualStateId));
            }

            roomId = nextRoomId;
            chapter = nextChapter;
            roomVisualStateId = ChannelZeroIds.DefaultVisualState;
            MarkVisited(nextRoomId);
        }

        public bool TryGoBack()
        {
            if (!CanGoBack || !string.IsNullOrWhiteSpace(activeCloseupId))
                return false;

            int lastIndex = roomHistory.Count - 1;
            RoomHistoryEntry previous = roomHistory[lastIndex];
            roomHistory.RemoveAt(lastIndex);

            roomId = previous.roomId;
            chapter = previous.chapter;
            era = previous.era;
            roomVisualStateId = string.IsNullOrWhiteSpace(previous.visualStateId)
                ? ChannelZeroIds.DefaultVisualState
                : previous.visualStateId;
            return true;
        }

        public void Tune(ChannelEra nextEra)
        {
            era = nextEra;
            roomVisualStateId = ChannelZeroIds.DefaultVisualState;
        }

        public void SetVisualState(string stateId)
        {
            roomVisualStateId = string.IsNullOrWhiteSpace(stateId)
                ? ChannelZeroIds.DefaultVisualState
                : stateId;
        }

        public void Observe(string hotspotId)
        {
            AddUnique(observedHotspotIds, hotspotId);
        }

        public void AddItem(string itemId)
        {
            bool alreadyOwned = HasItem(itemId);
            AddUnique(inventoryItemIds, itemId);
            inventoryOrigins ??= new List<InventoryOriginEntry>();
            if (!alreadyOwned && !string.IsNullOrWhiteSpace(itemId) &&
                inventoryOrigins.Find(entry => entry.itemId == itemId) == null)
                inventoryOrigins.Add(new InventoryOriginEntry(itemId, roomId));
        }

        public bool HasItem(string itemId)
        {
            return !string.IsNullOrWhiteSpace(itemId) && inventoryItemIds.Contains(itemId);
        }

        public bool RemoveItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || !inventoryItemIds.Remove(itemId))
                return false;
            if (selectedInventoryItemId == itemId)
                selectedInventoryItemId = string.Empty;
            if (heldItemId == itemId)
                heldItemId = string.Empty;
            inventoryOrigins?.RemoveAll(entry => entry.itemId == itemId);
            return true;
        }

        public string GetItemOriginRoom(string itemId)
        {
            return inventoryOrigins?.Find(entry => entry.itemId == itemId)?.roomId ?? string.Empty;
        }

        public void SetFlag(string flagId, bool value = true)
        {
            SetPuzzleState("flag:" + flagId, value.ToString().ToLowerInvariant());
        }

        public bool GetFlag(string flagId)
        {
            return string.Equals(GetPuzzleState("flag:" + flagId, "false"), "true",
                StringComparison.OrdinalIgnoreCase);
        }

        public bool TryHold(string itemId)
        {
            if (!HasItem(itemId))
                return false;

            heldItemId = itemId;
            operation = ChannelOperation.Hold;
            return true;
        }

        public void SetPuzzleState(string puzzleId, string stateId)
        {
            StateDescriptor descriptor = ChannelZeroStateCatalog.Resolve(puzzleId, roomId);
            SetPuzzleState(puzzleId, stateId, descriptor.Persistence, descriptor.RoomId,
                descriptor.EraScoped);
        }

        public void SetPuzzleState(string puzzleId, string stateId, StatePersistence persistence,
            string scopeRoomId = "", bool eraScoped = false, ChannelEra? scopeEra = null)
        {
            if (string.IsNullOrWhiteSpace(puzzleId))
                return;

            PuzzleStateEntry entry = puzzleStates.Find(item => item.puzzleId == puzzleId);
            if (entry == null)
                puzzleStates.Add(new PuzzleStateEntry(puzzleId, stateId, persistence,
                    scopeRoomId, scopeEra ?? era, eraScoped));
            else
            {
                entry.stateId = stateId;
                entry.persistence = persistence;
                entry.roomId = scopeRoomId ?? string.Empty;
                entry.era = scopeEra ?? era;
                entry.eraScoped = eraScoped;
            }
        }

        public void EnterCloseup(string closeupId, string stateId = ChannelZeroIds.DefaultVisualState)
        {
            if (string.IsNullOrWhiteSpace(closeupId))
                throw new ArgumentException("Closeup ID is required.", nameof(closeupId));

            if (string.IsNullOrWhiteSpace(activeCloseupId))
            {
                closeupOriginRoomId = roomId;
                closeupOriginEra = era;
                closeupOriginVisualStateId = roomVisualStateId;
            }

            activeCloseupId = closeupId;
            activeCloseupStateId = string.IsNullOrWhiteSpace(stateId)
                ? ChannelZeroIds.DefaultVisualState
                : stateId;
            closeupInputLocked = false;
        }

        public bool TryExitCloseup()
        {
            if (closeupInputLocked)
                return false;

            if (!string.IsNullOrWhiteSpace(closeupOriginRoomId))
            {
                roomId = closeupOriginRoomId;
                era = closeupOriginEra;
                roomVisualStateId = string.IsNullOrWhiteSpace(closeupOriginVisualStateId)
                    ? ChannelZeroIds.DefaultVisualState
                    : closeupOriginVisualStateId;
            }

            activeCloseupId = string.Empty;
            activeCloseupStateId = ChannelZeroIds.DefaultVisualState;
            closeupOriginRoomId = string.Empty;
            closeupOriginVisualStateId = ChannelZeroIds.DefaultVisualState;
            closeupInputLocked = false;
            return true;
        }

        public void MarkRecordRead(string recordId)
        {
            AddUnique(recordIds, recordId);
        }

        public void MarkTextSeen(string textId)
        {
            AddUnique(seenTextIds, textId);
        }

        public bool HasSeenText(string textId)
        {
            return !string.IsNullOrWhiteSpace(textId) && seenTextIds.Contains(textId);
        }

        public void RewindPhysicalState(IEnumerable<PuzzleStateEntry> checkpointPuzzleStates)
        {
            puzzleStates.RemoveAll(entry => entry.persistence == StatePersistence.Physical &&
                (string.IsNullOrWhiteSpace(entry.roomId) || entry.roomId == roomId));
            if (checkpointPuzzleStates != null)
            {
                foreach (PuzzleStateEntry source in checkpointPuzzleStates)
                {
                    PuzzleStateEntry entry = source.Clone();
                    entry.persistence = StatePersistence.Physical;
                    if (string.IsNullOrWhiteSpace(entry.roomId))
                        entry.roomId = roomId;
                    SetPuzzleState(entry.puzzleId, entry.stateId, entry.persistence,
                        entry.roomId, entry.eraScoped, entry.era);
                }
            }
            roomVisualStateId = ChannelZeroIds.DefaultVisualState;
            operation = ChannelOperation.Rew;
        }

        public void ClearTransientInteraction()
        {
            activeCloseupId = string.Empty;
            activeCloseupStateId = ChannelZeroIds.DefaultVisualState;
            closeupOriginRoomId = string.Empty;
            closeupOriginVisualStateId = ChannelZeroIds.DefaultVisualState;
            closeupInputLocked = false;
            selectedInventoryItemId = string.Empty;
            puzzleStates?.RemoveAll(entry => entry.persistence == StatePersistence.Transient);
        }

        public string GetPuzzleState(string puzzleId, string fallback = "locked")
        {
            PuzzleStateEntry entry = puzzleStates.Find(item => item.puzzleId == puzzleId);
            return entry?.stateId ?? fallback;
        }

        public void ForeshadowDisappearingStair(bool lockPlanRead)
        {
            DisappearingStairState next = lockPlanRead
                ? DisappearingStairState.LocksKnown
                : DisappearingStairState.Foreshadowed;

            if (disappearingStairState < next)
                disappearingStairState = next;
        }

        public bool TryOpenDisappearingStair(bool unlockOrderKnown)
        {
            bool isLaterChapter = chapter >= StoryChapter.Chapter4;
            bool crankHeld = heldItemId == ChannelZeroIds.FoldingCrankItem;
            bool locksKnown = disappearingStairState >= DisappearingStairState.LocksKnown;

            if (!isLaterChapter || !unlockOrderKnown || !crankHeld || !locksKnown)
                return false;

            disappearingStairState = DisappearingStairState.Open;
            return true;
        }

        public void NormalizeAfterLoad()
        {
            visitedRoomIds ??= new List<string>();
            observedHotspotIds ??= new List<string>();
            recordIds ??= new List<string>();
            seenTextIds ??= new List<string>();
            inventoryItemIds ??= new List<string>();
            inventoryOrigins ??= new List<InventoryOriginEntry>();
            puzzleStates ??= new List<PuzzleStateEntry>();
            roomHistory ??= new List<RoomHistoryEntry>();
            timelineSnapshots ??= new List<TimelineSnapshot>();
            operation = ChannelOperation.None;
            ClearTransientInteraction();
            if (!string.IsNullOrWhiteSpace(selectedInventoryItemId) && !HasItem(selectedInventoryItemId))
                selectedInventoryItemId = string.Empty;
            roomId = string.IsNullOrWhiteSpace(roomId) ? ChannelZeroIds.EntryRoom : roomId;
            roomVisualStateId = string.IsNullOrWhiteSpace(roomVisualStateId)
                ? ChannelZeroIds.DefaultVisualState
                : roomVisualStateId;
            MarkVisited(roomId);
        }

        private void MarkVisited(string value)
        {
            AddUnique(visitedRoomIds, value);
        }

        private static void AddUnique(List<string> values, string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && !values.Contains(value))
                values.Add(value);
        }
    }
}

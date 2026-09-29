using System;
using System.Collections.Generic;

namespace ChannelZero.Runtime.Core
{
    public sealed class PuzzleActionView
    {
        public string actionId;
        public string label;
        public string labelKey;
        public bool interactable = true;

        public PuzzleActionView(string actionId, string label, bool interactable = true,
            string labelKey = "")
        {
            this.actionId = actionId;
            this.label = label;
            this.interactable = interactable;
            this.labelKey = labelKey;
        }
    }

    public sealed class PuzzleView
    {
        public string puzzleId;
        public string closeupId;
        public string title;
        public string body;
        public string titleKey;
        public string bodyKey;
        public string artworkStateId = ChannelZeroIds.DefaultVisualState;
        public bool completed;
        public bool acceptsInventoryItems;
        public List<PuzzleActionView> actions = new();
    }

    public sealed class PuzzleActionResult
    {
        public PuzzleView view;
        public string narrativeTrigger;
        public string feedbackText;
        public string requestedCloseupId;
        public bool stateChanged;
        public List<string> completedChapterIds = new();
    }

    public sealed class ChannelZeroPuzzleContext
    {
        public ChannelZeroSessionState State { get; }
        public IChannelZeroRecordingService Recording { get; }
        public ChannelEra Era => State.era;

        public ChannelZeroPuzzleContext(ChannelZeroSessionState state,
            IChannelZeroRecordingService recording = null)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Recording = recording;
        }

        public string GetState(string puzzleId, string fallback = "default") => State.GetPuzzleState(puzzleId, fallback);
        public void SetState(string puzzleId, string value) => State.SetPuzzleState(puzzleId, value,
            StatePersistence.Physical, State.roomId);
        public bool HasItem(string itemId) => State.HasItem(itemId);
        public void AddItem(string itemId) => State.AddItem(itemId);
        public bool RemoveItem(string itemId) => State.RemoveItem(itemId);
        public bool GetFlag(string flagId) => State.GetFlag(flagId);
        public void SetFlag(string flagId, bool value = true) => State.SetFlag(flagId, value);
    }

    public interface IChannelZeroRecordingService
    {
        TimelineSnapshot RecordCurrent();
        bool HasRecording(string roomId, ChannelEra era);
    }

    public interface IChannelZeroPuzzle
    {
        string PuzzleId { get; }
        string CloseupId { get; }
        PuzzleView BuildView(ChannelZeroPuzzleContext context);
        PuzzleActionResult Execute(string actionId, ChannelZeroPuzzleContext context);
    }

    public interface IInventoryItemPuzzle
    {
        PuzzleActionResult UseItem(string itemId, ChannelZeroPuzzleContext context);
    }

    public interface IChannelZeroPuzzleRegistry
    {
        bool TryGet(string closeupId, out IChannelZeroPuzzle puzzle);
        bool TryGetDefinition(string closeupId, out PuzzleDefinition definition);
    }

    public enum PuzzleInteractionMode
    {
        Missing,
        Active,
        CommonSystem,
        PassiveObservation,
        InactiveEra,
        PrerequisiteBlocked,
        ResultOnly,
    }

    public enum PuzzleLifecycleState
    {
        Locked,
        Available,
        InProgress,
        Solved,
    }

    public readonly struct PuzzleInteractionAccess
    {
        public PuzzleInteractionMode Mode { get; }
        public PuzzleDefinition Definition { get; }
        public string NarrativeTextId { get; }
        public string ResultStateId { get; }
        public PuzzleLifecycleState LifecycleState { get; }
        public bool ReviewUiEnabled { get; }
        public bool AcceptsPuzzleActions =>
            Mode == PuzzleInteractionMode.Active || Mode == PuzzleInteractionMode.CommonSystem;
        public bool OpensPuzzleUi =>
            AcceptsPuzzleActions ||
            (ReviewUiEnabled &&
             (Mode == PuzzleInteractionMode.PassiveObservation ||
              Mode == PuzzleInteractionMode.InactiveEra ||
              Mode == PuzzleInteractionMode.ResultOnly));

        public PuzzleInteractionAccess(PuzzleInteractionMode mode, PuzzleDefinition definition,
            string narrativeTextId = "", string resultStateId = "",
            PuzzleLifecycleState lifecycleState = PuzzleLifecycleState.Locked,
            bool reviewUiEnabled = false)
        {
            Mode = mode;
            Definition = definition;
            NarrativeTextId = narrativeTextId ?? string.Empty;
            ResultStateId = resultStateId ?? string.Empty;
            LifecycleState = lifecycleState;
            ReviewUiEnabled = reviewUiEnabled;
        }
    }
}

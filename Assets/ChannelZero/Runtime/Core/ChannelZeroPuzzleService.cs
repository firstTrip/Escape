using System;
using System.Collections.Generic;
using System.Linq;

namespace ChannelZero.Runtime.Core
{
    public sealed class ChannelZeroPuzzleStateCalculator
    {
        public PuzzleLifecycleState Evaluate(PuzzleDefinition definition, ChannelZeroSessionState state)
        {
            if (definition == null || state == null)
                return PuzzleLifecycleState.Locked;
            if (IsSolved(definition, state))
                return PuzzleLifecycleState.Solved;
            bool prerequisitesMet = (definition.prerequisiteFlags ?? Array.Empty<string>()).All(state.GetFlag);
            bool eraReady = IsMultiEraRule(definition.ruleType) || state.era == definition.ActiveEra;
            if (!prerequisitesMet || !eraReady)
                return PuzzleLifecycleState.Locked;
            bool started = state.puzzleStates.Any(entry =>
                (entry.puzzleId == definition.RuntimePuzzleId ||
                 entry.puzzleId.StartsWith(definition.RuntimePuzzleId + ".", StringComparison.Ordinal)) &&
                !string.IsNullOrWhiteSpace(entry.stateId) && entry.stateId != "default" && entry.stateId != "empty");
            return started ? PuzzleLifecycleState.InProgress : PuzzleLifecycleState.Available;
        }

        internal static bool IsMultiEraRule(string ruleType) => ruleType == "rec" ||
            ruleType == "vase_causality" || ruleType == "child_trace" ||
            ruleType == "jinwoo_causality";

        public bool IsSolved(PuzzleDefinition definition, ChannelZeroSessionState state)
        {
            if (!string.IsNullOrWhiteSpace(definition.solvedFlag) && state.GetFlag(definition.solvedFlag))
                return true;
            return definition.RuntimePuzzleId switch
            {
                ChannelZeroPuzzleIds.Toolbox => state.HasItem(ChannelZeroPuzzleIds.Screwdriver),
                ChannelZeroPuzzleIds.TubeCase => state.GetFlag("CrtRepaired"),
                ChannelZeroPuzzleIds.MedicalCabinet => state.GetFlag("JinwooTreated"),
                ChannelZeroPuzzleIds.WorkshopDrawer => state.GetFlag("TubeTestNormal"),
                _ => false,
            };
        }
    }

    public sealed class ChannelZeroPuzzleService
    {
        public const string NoActionMessage = "지금은 여기서 할 게 없다.";

        private readonly IChannelZeroPuzzleRegistry registry;
        private readonly ChannelZeroSessionState state;
        private readonly ChannelZeroHintService hints;
        private readonly IChannelZeroRecordingService recording;
        private readonly ChannelZeroChapterProgressionService progression;
        private readonly ChannelZeroPuzzleStateCalculator stateCalculator = new();

        public ChannelZeroPuzzleService(IChannelZeroPuzzleRegistry registry, ChannelZeroSessionState state,
            ChannelZeroHintService hints = null, IChannelZeroRecordingService recording = null,
            ChannelZeroChapterProgressionService progression = null)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.hints = hints;
            this.recording = recording;
            this.progression = progression ?? new ChannelZeroChapterProgressionService(state,
                recording: recording);
        }

        public bool TryOpen(string closeupId, out PuzzleView view)
        {
            view = null;
            PuzzleInteractionAccess access = EvaluateAccess(closeupId);
            if (!access.OpensPuzzleUi)
                return false;
            if (!registry.TryGet(closeupId, out IChannelZeroPuzzle puzzle))
                return false;
            view = puzzle.BuildView(Context());
            Decorate(view, !access.AcceptsPuzzleActions);
            return true;
        }

        public bool TryExecute(string closeupId, string actionId, out PuzzleActionResult result)
        {
            result = null;
            PuzzleInteractionAccess access = EvaluateAccess(closeupId);
            if (!access.AcceptsPuzzleActions)
                return false;
            if (!registry.TryGet(closeupId, out IChannelZeroPuzzle puzzle))
                return false;
            if (actionId == "system:hint")
            {
                PuzzleView hintView = puzzle.BuildView(Context());
                Decorate(hintView);
                result = new PuzzleActionResult { view = hintView, feedbackText = hints?.Request(closeupId), stateChanged = true };
                return true;
            }
            bool chapterOneWasComplete = state.GetFlag("Chapter1Complete");
            bool chapterTwoWasComplete = state.GetFlag("Chapter2Complete");
            result = puzzle.Execute(actionId, Context());
            RefreshProgression(result, chapterOneWasComplete, chapterTwoWasComplete);
            result.view ??= puzzle.BuildView(Context());
            Decorate(result.view);
            return true;
        }

        public bool TryUseItem(string closeupId, string itemId, out PuzzleActionResult result)
        {
            result = null;
            if (!EvaluateAccess(closeupId).AcceptsPuzzleActions || string.IsNullOrWhiteSpace(itemId)
                || !registry.TryGet(closeupId, out IChannelZeroPuzzle puzzle)
                || puzzle is not IInventoryItemPuzzle itemPuzzle)
                return false;

            bool chapterOneWasComplete = state.GetFlag("Chapter1Complete");
            bool chapterTwoWasComplete = state.GetFlag("Chapter2Complete");
            result = itemPuzzle.UseItem(itemId, Context());
            RefreshProgression(result, chapterOneWasComplete, chapterTwoWasComplete);
            result.view ??= puzzle.BuildView(Context());
            Decorate(result.view);
            return true;
        }

        public PuzzleInteractionAccess EvaluateAccess(string closeupId)
        {
            if (!registry.TryGetDefinition(closeupId, out PuzzleDefinition definition))
                return new PuzzleInteractionAccess(PuzzleInteractionMode.Missing, null);

            PuzzleLifecycleState lifecycle = stateCalculator.Evaluate(definition, state);
            bool solved = lifecycle == PuzzleLifecycleState.Solved;
            string resultState = definition.ResultState(state.era);
            bool reviewUiEnabled =
                (definition.ruleType == "era_evidence" || definition.allowInactiveEraReview) &&
                !definition.UsesTextOnlyReview(state.era);
            if (solved)
                return new PuzzleInteractionAccess(PuzzleInteractionMode.ResultOnly, definition,
                    definition.InactiveTextId(state.era), resultState, lifecycle, reviewUiEnabled);

            bool prerequisitesMet = (definition.prerequisiteFlags ?? Array.Empty<string>()).All(state.GetFlag);
            if (ChannelZeroPuzzleStateCalculator.IsMultiEraRule(definition.ruleType))
            {
                PuzzleInteractionMode recMode = prerequisitesMet
                    ? PuzzleInteractionMode.CommonSystem
                    : PuzzleInteractionMode.PrerequisiteBlocked;
                return new PuzzleInteractionAccess(recMode, definition,
                    definition.InactiveTextId(state.era), resultState, lifecycle, reviewUiEnabled);
            }

            if (state.era != definition.ActiveEra)
            {
                PuzzleInteractionMode mode = definition.ruleType == "era_evidence"
                    ? PuzzleInteractionMode.PassiveObservation
                    : PuzzleInteractionMode.InactiveEra;
                return new PuzzleInteractionAccess(mode, definition,
                    definition.InactiveTextId(state.era), resultState, lifecycle, reviewUiEnabled);
            }

            if (!prerequisitesMet)
                return new PuzzleInteractionAccess(PuzzleInteractionMode.PrerequisiteBlocked, definition,
                    definition.InactiveTextId(state.era), resultState, lifecycle, reviewUiEnabled);

            return new PuzzleInteractionAccess(PuzzleInteractionMode.Active, definition,
                lifecycleState: lifecycle, reviewUiEnabled: reviewUiEnabled);
        }

        public bool RecordPassiveObservation(string closeupId)
        {
            PuzzleInteractionAccess access = EvaluateAccess(closeupId);
            if (access.Mode != PuzzleInteractionMode.PassiveObservation ||
                !registry.TryGet(closeupId, out IChannelZeroPuzzle puzzle))
                return false;
            bool chapterOneWasComplete = state.GetFlag("Chapter1Complete");
            bool chapterTwoWasComplete = state.GetFlag("Chapter2Complete");
            PuzzleActionResult result = puzzle.Execute("observe", Context());
            RefreshProgression(result, chapterOneWasComplete, chapterTwoWasComplete);
            return result != null;
        }

        public IReadOnlyList<PuzzleDefinition> NewlyActiveDefinitions()
        {
            List<PuzzleDefinition> active = new();
            HashSet<string> seen = new(StringComparer.Ordinal);
            foreach (PuzzleDefinition definition in Definitions())
            {
                if (!seen.Add(definition.RuntimePuzzleId) || definition.ActiveEra != state.era)
                    continue;
                PuzzleInteractionAccess access = EvaluateAccess(definition.closeupId);
                if (access.Mode == PuzzleInteractionMode.Active)
                    active.Add(definition);
            }
            return active;
        }

        private IEnumerable<PuzzleDefinition> Definitions()
        {
            if (registry is ChannelZeroBuiltInPuzzleRegistry builtIn)
                return builtIn.Catalog.Puzzles;
            return Array.Empty<PuzzleDefinition>();
        }

        private void Decorate(PuzzleView view, bool reviewOnly = false)
        {
            if (view == null)
                return;

            if (reviewOnly)
            {
                view.actions.Clear();
                view.acceptsInventoryItems = false;
            }
            else
            {
                bool hasAvailableAction = view.actions.Exists(action =>
                    action.interactable && action.actionId != "system:hint");
                if (view.completed || (!hasAvailableAction && !view.acceptsInventoryItems))
                {
                    view.body = NoActionMessage;
                    view.bodyKey = "ui.puzzle.no_action";
                }

                if (!view.completed && hints != null)
                    view.actions.Add(new PuzzleActionView("system:hint", "힌트"));
            }

            view.titleKey = string.IsNullOrWhiteSpace(view.titleKey)
                ? $"puzzle.{view.puzzleId}.title"
                : view.titleKey;
            view.bodyKey = string.IsNullOrWhiteSpace(view.bodyKey)
                ? $"puzzle.{view.puzzleId}.body.{view.artworkStateId}"
                : view.bodyKey;
            foreach (PuzzleActionView action in view.actions)
                if (string.IsNullOrWhiteSpace(action.labelKey))
                    action.labelKey = $"puzzle.{view.puzzleId}.action.{action.actionId}";
        }

        private ChannelZeroPuzzleContext Context() => new(state, recording);

        private void RefreshProgression(PuzzleActionResult result, bool chapterOneWasComplete,
            bool chapterTwoWasComplete)
        {
            progression.Refresh();
            if (result == null)
                return;
            if (!chapterOneWasComplete && state.GetFlag("Chapter1Complete"))
                result.completedChapterIds.Add("CH1");
            if (!chapterTwoWasComplete && state.GetFlag("Chapter2Complete"))
                result.completedChapterIds.Add("CH2");
        }
    }
}

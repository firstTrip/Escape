using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChannelZero.Runtime.Core
{
    public enum InteractionActionType
    {
        OpenCloseup,
        PlayNarrative,
        EnterLivingRoom,
        EnterWorkshop,
        GoBack,
        NoAction,
    }

    [Serializable]
    public sealed class InteractionRouteDefinition
    {
        public string interactionId;
        public string action;
        public string closeupId;
        public string narrativeTarget;
        public string closeupNarrativeTarget;
        public string narrativeTrigger = "inspect";
        public string operation;
        public string unlockId;
        public string statusMessage;
        public bool playNarrativeBeforeAction;
        public bool documentOnly;

        public InteractionActionType ActionType => Enum.TryParse(action, true, out InteractionActionType value)
            ? value
            : InteractionActionType.NoAction;

        public bool Matches(ChannelOperation currentOperation) => string.IsNullOrWhiteSpace(operation) ||
            string.Equals(operation, currentOperation.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Serializable]
    public sealed class InteractionUnlockDefinition
    {
        public string interactionId;
        public string[] requiredFlags = Array.Empty<string>();
        public string[] requiredItemsAny = Array.Empty<string>();
        public string[] requiredRecordsAny = Array.Empty<string>();
        public int requiredEra;
        public string lockedMessage;
    }

    [Serializable]
    public sealed class PuzzleStateRequirement
    {
        public string puzzleId;
        public string stateId;
    }

    [Serializable]
    public sealed class ChapterCompletionDefinition
    {
        public string completionFlag;
        public PuzzleStateRequirement[] requiredPuzzleStates = Array.Empty<PuzzleStateRequirement>();
        public string[] requiredFlags = Array.Empty<string>();
        public string[] requiredItems = Array.Empty<string>();
    }

    [Serializable]
    internal sealed class InteractionDefinitionFile
    {
        public int schemaVersion;
        public List<InteractionRouteDefinition> routes = new();
        public List<InteractionUnlockDefinition> unlocks = new();
        public List<ChapterCompletionDefinition> chapters = new();
    }

    public sealed class ChannelZeroInteractionCatalog
    {
        private static ChannelZeroInteractionCatalog defaultCatalog;
        private readonly Dictionary<string, List<InteractionRouteDefinition>> routes;
        private readonly Dictionary<string, InteractionUnlockDefinition> unlocks;
        private readonly List<ChapterCompletionDefinition> chapters;

        private ChannelZeroInteractionCatalog(InteractionDefinitionFile file)
        {
            routes = file.routes.GroupBy(route => route.interactionId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
            unlocks = file.unlocks.ToDictionary(rule => rule.interactionId, StringComparer.Ordinal);
            chapters = file.chapters ?? new List<ChapterCompletionDefinition>();
        }

        public static ChannelZeroInteractionCatalog FromJson(string json)
        {
            InteractionDefinitionFile file = JsonUtility.FromJson<InteractionDefinitionFile>(json);
            if (file == null || file.schemaVersion != 1)
                throw new FormatException("Unsupported interaction definition JSON.");
            file.routes ??= new List<InteractionRouteDefinition>();
            file.unlocks ??= new List<InteractionUnlockDefinition>();
            file.chapters ??= new List<ChapterCompletionDefinition>();
            if (file.routes.Any(route => string.IsNullOrWhiteSpace(route.interactionId)))
                throw new FormatException("Interaction route ID is required.");
            if (file.routes.Any(route => !Enum.TryParse(route.action, true, out InteractionActionType _)))
                throw new FormatException("Unknown interaction action type.");
            if (file.routes.Any(route => route.ActionType == InteractionActionType.OpenCloseup &&
                string.IsNullOrWhiteSpace(route.closeupId)))
                throw new FormatException("OpenCloseup route requires a closeup ID.");
            if (file.routes.GroupBy(route => route.interactionId + "|" + (route.operation ?? string.Empty),
                    StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
                throw new FormatException("Duplicate interaction route variant.");
            if (file.unlocks.GroupBy(rule => rule.interactionId, StringComparer.Ordinal).Any(group => group.Count() > 1))
                throw new FormatException("Duplicate interaction unlock rule.");
            return new ChannelZeroInteractionCatalog(file);
        }

        public static ChannelZeroInteractionCatalog LoadDefault()
        {
            if (defaultCatalog != null)
                return defaultCatalog;
            TextAsset asset = Resources.Load<TextAsset>("ChannelZero/Data/interactions_ko-KR.v1");
            if (asset == null)
                throw new InvalidOperationException("Interaction definition resource is missing.");
            defaultCatalog = FromJson(asset.text);
            return defaultCatalog;
        }

        public bool TryResolve(string interactionId, ChannelOperation operation,
            out InteractionRouteDefinition route)
        {
            route = null;
            if (!routes.TryGetValue(interactionId, out List<InteractionRouteDefinition> candidates))
                return false;
            route = candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate.operation) &&
                candidate.Matches(operation)) ?? candidates.FirstOrDefault(candidate => string.IsNullOrWhiteSpace(candidate.operation));
            return route != null;
        }

        public bool TryGetUnlock(string interactionId, out InteractionUnlockDefinition definition) =>
            unlocks.TryGetValue(interactionId, out definition);

        public string NarrativeTargetForCloseup(string closeupId)
        {
            InteractionRouteDefinition route = routes.Values.SelectMany(value => value)
                .FirstOrDefault(candidate => candidate.closeupId == closeupId &&
                    (!string.IsNullOrWhiteSpace(candidate.closeupNarrativeTarget) ||
                     !string.IsNullOrWhiteSpace(candidate.narrativeTarget)));
            if (route == null)
                return string.Empty;
            return string.IsNullOrWhiteSpace(route.closeupNarrativeTarget)
                ? route.narrativeTarget
                : route.closeupNarrativeTarget;
        }

        public IReadOnlyList<ChapterCompletionDefinition> Chapters => chapters;
        public IReadOnlyCollection<InteractionRouteDefinition> Routes => routes.Values.SelectMany(value => value).ToArray();
    }

    public sealed class ChannelZeroInteractionRouter
    {
        private readonly ChannelZeroInteractionCatalog catalog;
        public ChannelZeroInteractionRouter(ChannelZeroInteractionCatalog catalog) =>
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

        public InteractionRouteDefinition Resolve(string interactionId, ChannelOperation operation)
        {
            if (catalog.TryResolve(interactionId, operation, out InteractionRouteDefinition route))
                return route;
            return new InteractionRouteDefinition
            {
                interactionId = interactionId,
                action = nameof(InteractionActionType.NoAction),
                narrativeTarget = interactionId,
                narrativeTrigger = "inspect",
                statusMessage = ChannelZeroPuzzleService.NoActionMessage,
            };
        }
    }
}

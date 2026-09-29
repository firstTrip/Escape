using System;
using System.Collections.Generic;
using ChannelZero.Runtime.Core;

namespace ChannelZero.Runtime.Presentation
{
    public interface IChannelZeroInteractionHandler
    {
        InteractionActionType ActionType { get; }
        void Execute(InteractionRouteDefinition route);
    }

    public sealed class ChannelZeroInteractionDispatcher
    {
        private readonly Dictionary<InteractionActionType, IChannelZeroInteractionHandler> handlers = new();

        public void Register(InteractionActionType actionType, Action<InteractionRouteDefinition> execute) =>
            handlers[actionType] = new DelegateInteractionHandler(actionType, execute);

        public bool Dispatch(InteractionRouteDefinition route)
        {
            if (route == null || !handlers.TryGetValue(route.ActionType, out IChannelZeroInteractionHandler handler))
                return false;
            handler.Execute(route);
            return true;
        }

        private sealed class DelegateInteractionHandler : IChannelZeroInteractionHandler
        {
            private readonly Action<InteractionRouteDefinition> execute;
            public InteractionActionType ActionType { get; }
            public DelegateInteractionHandler(InteractionActionType actionType,
                Action<InteractionRouteDefinition> execute)
            {
                ActionType = actionType;
                this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            }
            public void Execute(InteractionRouteDefinition route) => execute(route);
        }
    }
}

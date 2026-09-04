using Chatter.CQRS;
using Chatter.CQRS.Commands;
using Chatter.CQRS.Context;
using Chatter.CQRS.Events;
using Chatter.CQRS.Pipeline;
using Chatter.CQRS.Queries;

namespace Chatter.Aot.Smoke.Tests.Fakes;

public sealed class GeneratedPingCommand : ICommand
{
    public bool WasHandled { get; set; }
}

public sealed class GeneratedPingCommandHandler : IMessageHandler<GeneratedPingCommand>
{
    public Task Handle(GeneratedPingCommand message, IMessageHandlerContext context)
    {
        message.WasHandled = true;
        return Task.CompletedTask;
    }
}

public sealed class GeneratedPingEvent : IEvent
{
    public bool WasHandled { get; set; }
}

public sealed class GeneratedPingEventHandler : IMessageHandler<GeneratedPingEvent>
{
    public Task Handle(GeneratedPingEvent message, IMessageHandlerContext context)
    {
        message.WasHandled = true;
        return Task.CompletedTask;
    }
}

public sealed class GeneratedPingQuery : IQuery<string>
{
}

public sealed class GeneratedPingQueryHandler : IQueryHandler<GeneratedPingQuery, string>
{
    public Task<string> Handle(GeneratedPingQuery query, IQueryHandlerContext context) => Task.FromResult("generated-pong");
}

public sealed class GeneratedBehaviorPingCommand : ICommand
{
    public List<string> ExecutionOrder { get; } = new();
}

public sealed class GeneratedBehaviorPingCommandHandler : IMessageHandler<GeneratedBehaviorPingCommand>
{
    public Task Handle(GeneratedBehaviorPingCommand message, IMessageHandlerContext context)
    {
        message.ExecutionOrder.Add("handler");
        return Task.CompletedTask;
    }
}

[RegisterForAllCommands]
public sealed class GeneratedAllCommandsBehavior<TCommand> : ICommandBehavior<TCommand>
    where TCommand : ICommand
{
    public static readonly List<string> InvokedFor = new();

    public async Task Handle(TCommand message, IMessageHandlerContext messageHandlerContext, CommandHandlerDelegate next)
    {
        InvokedFor.Add(typeof(TCommand).Name);
        await next();
    }
}

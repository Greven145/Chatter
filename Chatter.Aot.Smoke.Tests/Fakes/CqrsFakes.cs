using Chatter.CQRS;
using Chatter.CQRS.Commands;
using Chatter.CQRS.Context;
using Chatter.CQRS.Events;
using Chatter.CQRS.Queries;

namespace Chatter.Aot.Smoke.Tests.Fakes;

public sealed class ScrutorPingCommand : ICommand
{
}

public sealed class ScrutorPingCommandHandler : IMessageHandler<ScrutorPingCommand>
{
    public Task Handle(ScrutorPingCommand message, IMessageHandlerContext context) => Task.CompletedTask;
}

public sealed class ScrutorPingQuery : IQuery<string>
{
}

public sealed class ScrutorPingQueryHandler : IQueryHandler<ScrutorPingQuery, string>
{
    public Task<string> Handle(ScrutorPingQuery query, IQueryHandlerContext context) => Task.FromResult("pong");
}

public sealed class ExplicitPingQuery : IQuery<string>
{
}

public sealed class ExplicitPingQueryHandler : IQueryHandler<ExplicitPingQuery, string>
{
    public Task<string> Handle(ExplicitPingQuery query, IQueryHandlerContext context) => Task.FromResult("pong");
}

public sealed class ExplicitPingCommand : ICommand
{
    public bool WasHandled { get; set; }
}

public sealed class ExplicitPingCommandHandler : IMessageHandler<ExplicitPingCommand>
{
    public Task Handle(ExplicitPingCommand message, IMessageHandlerContext context)
    {
        message.WasHandled = true;
        return Task.CompletedTask;
    }
}

public sealed class ExplicitPingEvent : IEvent
{
    public bool HandledByFirst { get; set; }
    public bool HandledBySecond { get; set; }
}

public sealed class ExplicitPingEventHandlerFirst : IMessageHandler<ExplicitPingEvent>
{
    public Task Handle(ExplicitPingEvent message, IMessageHandlerContext context)
    {
        message.HandledByFirst = true;
        return Task.CompletedTask;
    }
}

public sealed class ExplicitPingEventHandlerSecond : IMessageHandler<ExplicitPingEvent>
{
    public Task Handle(ExplicitPingEvent message, IMessageHandlerContext context)
    {
        message.HandledBySecond = true;
        return Task.CompletedTask;
    }
}

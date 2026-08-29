using Chatter.CQRS;
using Chatter.CQRS.Commands;
using Chatter.CQRS.Context;
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

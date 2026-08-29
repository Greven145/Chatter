using Chatter.CQRS;
using Chatter.CQRS.Commands;
using Chatter.CQRS.Context;
using Chatter.CQRS.Queries;

namespace Chatter.Aot.Smoke.Tests.Fakes;

// NOTE ON TEST ISOLATION: whole-program trimming analyzes reachability across the ENTIRE published
// binary, not per call-site. A type used in ANY explicit, closed generic DI registration
// (services.AddScoped<TService, TImplementation>()) anywhere in this project has its constructor's
// reflection metadata preserved for EVERY caller in the binary — including an unrelated Scrutor-scan
// test that resolves the same type. Empirically confirmed while building this suite: adding an
// explicit AddScoped<IQueryHandler<PingQuery,string>, PingQueryHandler>() call for the
// QueryDispatcher test flipped the (unrelated) Scrutor query-handler test from throwing to
// succeeding. To keep each red-baseline test's result attributable to the mechanism it names, a type
// discovered via Scrutor/assembly-scan below is NEVER ALSO referenced via an explicit generic DI
// registration anywhere else in this project. Types with an "Explicit" prefix are the deliberate
// exception, used only by the QueryDispatcher dispatch test.

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

// Used only by QueryDispatcherTests, which explicitly registers this handler via a closed generic DI
// call (services.AddScoped<IQueryHandler<...>, ...>()) to isolate the DISPATCHER's own dynamic +
// MakeGenericType behavior from the separate Scrutor-registration gap covered above.
public sealed class ExplicitPingQuery : IQuery<string>
{
}

public sealed class ExplicitPingQueryHandler : IQueryHandler<ExplicitPingQuery, string>
{
    public Task<string> Handle(ExplicitPingQuery query, IQueryHandlerContext context) => Task.FromResult("pong");
}

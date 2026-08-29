using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.CQRS;
using Chatter.CQRS.Queries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chatter.Aot.Smoke.Tests;

// Red baseline for #275/#276: CqrsExtensions.AddCommandHandlers/AddQueryHandlers drive handler
// registration via Scrutor's services.Scan() (Assembly.GetTypes() + open-generic interface
// matching). Empirically verified against the published, executed native binary (not just build-time
// analysis): Scrutor's scan DOES find and register the handler (a ServiceDescriptor for
// IMessageHandler<TCommand>/IQueryHandler<TQuery,TResult> mapping to the concrete handler type is
// present), but resolving it then THROWS, because the handler's own constructor lost its
// reflection-visible metadata under full trimming (nothing else in the program calls `new
// HandlerType()` or otherwise roots it, so ILLink strips the constructor's reflection metadata even
// though the type itself survives for Assembly.GetTypes() purposes). This differs from #275's
// original "silently returns wrong/empty list, no exception" characterization — that failure mode did
// not reproduce here; the actual failure is a loud DI resolution exception. Both are genuine, severe
// Native AOT blockers for this registration path; only the exact failure shape differs. When Phase 2
// lands an additive AOT-safe registration path, flip these assertions to expect successful resolution
// and drop [Trait("AotStatus","KnownGap")] in the same PR.
public class ScrutorHandlerRegistrationTests
{
    [Fact]
    [Trait("AotStatus", "KnownGap")]
    public void AddChatterCqrs_UnderNativeAot_ThrowsResolvingScrutorDiscoveredCommandHandler()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddChatterCqrs(configuration, typeof(ScrutorPingCommandHandler));

        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<InvalidOperationException>(
            () => provider.GetService<IMessageHandler<ScrutorPingCommand>>());
        Assert.Contains("suitable constructor", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(nameof(ScrutorPingCommandHandler), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AotStatus", "KnownGap")]
    public void AddChatterCqrs_UnderNativeAot_ThrowsResolvingScrutorDiscoveredQueryHandler()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddChatterCqrs(configuration, typeof(ScrutorPingQueryHandler));

        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<InvalidOperationException>(
            () => provider.GetService<IQueryHandler<ScrutorPingQuery, string>>());
        Assert.Contains("suitable constructor", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(nameof(ScrutorPingQueryHandler), ex.Message, StringComparison.Ordinal);
    }
}

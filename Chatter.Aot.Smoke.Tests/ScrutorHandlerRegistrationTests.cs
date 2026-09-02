using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.CQRS;
using Chatter.CQRS.Queries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chatter.Aot.Smoke.Tests;

public class ScrutorHandlerRegistrationTests
{
    // Constructor preservation under Native AOT is a whole-program decision, not scoped to the
    // registration path that requested it: this same published binary also reaches
    // Chatter.SourceGenerators-generated code that registers ScrutorPingCommandHandler/
    // ScrutorPingQueryHandler explicitly (SourceGeneratedRegistrationTests), which independently
    // gives ILC a reason to preserve their constructors everywhere, including here. Resolving a
    // Scrutor-scanned handler no longer throws in this binary as a result — not because the scan
    // itself became AOT-safe.
    [Fact]
    public void AddChatterCqrs_UnderNativeAot_ResolvesScrutorDiscoveredCommandHandlerWhenConstructorPreservedElsewhere()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddChatterCqrs(configuration, typeof(ScrutorPingCommandHandler));

        using var provider = services.BuildServiceProvider();

        var resolved = provider.GetService<IMessageHandler<ScrutorPingCommand>>();
        Assert.IsType<ScrutorPingCommandHandler>(resolved);
    }

    [Fact]
    public void AddChatterCqrs_UnderNativeAot_ResolvesScrutorDiscoveredQueryHandlerWhenConstructorPreservedElsewhere()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddChatterCqrs(configuration, typeof(ScrutorPingQueryHandler));

        using var provider = services.BuildServiceProvider();

        var resolved = provider.GetService<IQueryHandler<ScrutorPingQuery, string>>();
        Assert.IsType<ScrutorPingQueryHandler>(resolved);
    }
}

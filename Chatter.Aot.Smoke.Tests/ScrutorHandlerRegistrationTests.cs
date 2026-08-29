using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.CQRS;
using Chatter.CQRS.Queries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chatter.Aot.Smoke.Tests;

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

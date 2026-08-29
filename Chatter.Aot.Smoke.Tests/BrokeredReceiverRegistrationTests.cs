using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.MessageBrokers.Receiving;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chatter.Aot.Smoke.Tests;

public class BrokeredReceiverRegistrationTests
{
    [Fact]
    [Trait("AotStatus", "KnownGap")]
    public void AddMessageBrokers_UnderNativeAot_ThrowsResolvingScannedReceiver()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var chatterBuilder = services.AddChatterCqrs(configuration, typeof(ScrutorPongMessage));
        chatterBuilder.AddMessageBrokers(markerTypesForRequiredAssemblies: typeof(ScrutorPongMessage));

        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<InvalidOperationException>(
            () => provider.GetService<IBrokeredMessageReceiver<ScrutorPongMessage>>());
        Assert.Contains("suitable constructor", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(nameof(ScrutorPongMessage), ex.Message, StringComparison.Ordinal);
    }
}

using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.MessageBrokers.Receiving;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chatter.Aot.Smoke.Tests;

// ChatterMessageBrokerExtensions.AddAllReceivers finds [BrokeredMessage]-decorated receiver types via
// assemblies.GetTypes() (a raw reflection assembly scan, same trim-unsafe family as Scrutor).
// Empirically verified: the scan finds and registers ScrutorPongMessage's receiver
// (BrokeredMessageReceiver<ScrutorPongMessage> is mapped to IBrokeredMessageReceiver<ScrutorPongMessage>),
// but resolving it THROWS because the receiver's constructor lost its reflection-visible metadata
// under full trimming — same root mechanism as ScrutorHandlerRegistrationTests. See that file's
// comment for the full explanation of why this is a thrown exception, not a silent empty result.
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

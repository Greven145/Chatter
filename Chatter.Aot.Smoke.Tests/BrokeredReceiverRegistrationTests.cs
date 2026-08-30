using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.CQRS;
using Chatter.MessageBrokers.Receiving;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace Chatter.Aot.Smoke.Tests;

public class BrokeredReceiverRegistrationTests
{
    [Fact]
    [Trait("AotStatus", "KnownGap")]
    public void AddMessageBrokers_UnderNativeAot_ThrowsConstructingScannedReceiverHostedService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().Build();

        var chatterBuilder = services.AddChatterCqrs(configuration, typeof(ScrutorPongMessage));
        chatterBuilder.AddMessageBrokers(markerTypesForRequiredAssemblies: typeof(ScrutorPongMessage));

        using var provider = services.BuildServiceProvider();

        // The receiver-interface resolution itself now succeeds here — this binary also closes
        // BrokeredMessageReceiver<> safely elsewhere (see AddReceiver_UnderNativeAot_Constructs...
        // below), and constructor-preservation for an open generic type is a whole-program decision, not
        // scoped to one closed instantiation. The hosted-service factory below still fails reliably: Native
        // AOT's per-instantiation codegen has no other direct closure over
        // BrokeredMessageReceiverBackgroundService<ScrutorPongMessage> anywhere in this program to piggyback on.
        var ex = Assert.Throws<MissingMethodException>(
            () => provider.GetServices<IHostedService>().ToList());
        Assert.Contains(nameof(ScrutorPongMessage), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddReceiver_UnderNativeAot_ConstructsReceiverAndHostedServiceWithoutReflection()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().Build();

        // AssemblySourceFilter falls back to scanning EVERY loaded assembly (including this test assembly,
        // which has the [BrokeredMessage]-decorated ScrutorPongMessage) whenever no namespace selector is set —
        // WithMarkerTypes alone only ADDS an assembly to the explicit list, it does not restrict the scan. A
        // namespace selector that matches nothing here is what actually isolates this test from the
        // still-KnownGap scanned-receiver path exercised above.
        var chatterBuilder = services.AddChatterCqrs(configuration, typeof(IMessage));
        chatterBuilder.AddMessageBrokers(receiverHandlerSourceBuilder: b => b.WithMarkerTypes(typeof(IMessage)).WithNamespaceSelector("__no_such_namespace__"));
        services.AddReceiver<ExplicitPongMessage>(receiverPath: "aot-smoke-explicit-queue");

        using var provider = services.BuildServiceProvider();

        var receiver = provider.GetRequiredService<IBrokeredMessageReceiver<ExplicitPongMessage>>();
        Assert.NotNull(receiver);

        // Constructs every registered IHostedService, including the AddReceiver<TMessage> background service —
        // the exact factory this phase changed from Activator.CreateInstance to a direct `new`.
        var hostedServices = provider.GetServices<IHostedService>().ToList();
        Assert.NotEmpty(hostedServices);
    }
}

using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.MessageBrokers.Receiving;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace Chatter.Aot.Smoke.Tests;

public class AzureServiceBusReceiverRegistrationTests
{
    [Fact]
    public void AddQueueReceiver_UnderNativeAot_ConstructsReceiverAndHostedServiceWithoutReflection()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().Build();

        var chatterBuilder = services.AddChatterCqrsWithExplicitHandlers(configuration);
        chatterBuilder
            .AddMessageBrokersWithExplicitReceivers()
            .WithAotJsonSerialization(PingJsonContext.Default)
            .AddAzureServiceBus(asb => asb
                .WithConnectionString("Endpoint=sb://aot-smoke-unused.servicebus.windows.net/;SharedAccessKeyName=unused;SharedAccessKey=unused")
                .AddQueueReceiver<AsbPongCommand>("aot-smoke-asb-queue"));

        using var provider = services.BuildServiceProvider();

        var receiver = provider.GetRequiredService<IBrokeredMessageReceiver<AsbPongCommand>>();
        Assert.NotNull(receiver);

        var hostedServices = provider.GetServices<IHostedService>().ToList();
        Assert.NotEmpty(hostedServices);
    }
}

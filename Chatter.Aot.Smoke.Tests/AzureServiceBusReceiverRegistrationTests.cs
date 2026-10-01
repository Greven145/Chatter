using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.MessageBrokers.Receiving;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace Chatter.Aot.Smoke.Tests;

public class AzureServiceBusReceiverRegistrationTests
{
    [Fact]
    public void AddQueueReceiver_UnderNativeAot_ConstructsReceiverAndHostedServiceWithoutReflection()
    {
        var chatterBuilder = AotHostFactory.NewAotHost(out var services);
        chatterBuilder
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

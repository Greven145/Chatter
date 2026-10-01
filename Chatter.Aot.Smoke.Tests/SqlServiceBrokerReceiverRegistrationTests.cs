using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.MessageBrokers.Receiving;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace Chatter.Aot.Smoke.Tests;

public class SqlServiceBrokerReceiverRegistrationTests
{
    [Fact]
    public void AddQueueReceiver_UnderNativeAot_ConstructsReceiverAndHostedServiceWithoutReflection()
    {
        var chatterBuilder = AotHostFactory.NewAotHost(out var services);
        chatterBuilder
            .AddSqlServiceBroker(ssb => ssb
                .AddSqlServiceBrokerOptions(connectionString: "Server=aot-smoke-unused;Database=aot-smoke;")
                .AddQueueReceiver<SqlServiceBrokerPongMessage>("aot-smoke-ssb-queue"));

        using var provider = services.BuildServiceProvider();

        var receiver = provider.GetRequiredService<IBrokeredMessageReceiver<SqlServiceBrokerPongMessage>>();
        Assert.NotNull(receiver);

        var hostedServices = provider.GetServices<IHostedService>().ToList();
        Assert.NotEmpty(hostedServices);
    }
}

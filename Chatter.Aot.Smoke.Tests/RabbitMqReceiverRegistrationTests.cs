using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.MessageBrokers.Receiving;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace Chatter.Aot.Smoke.Tests;

public class RabbitMqReceiverRegistrationTests
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
            .AddRabbitMq(rmq => rmq
                .AddRabbitMqOptions(hostName: "aot-smoke-unused-host")
                .AddQueueReceiver<RabbitMqPongMessage>("aot-smoke-rabbitmq-queue"));

        using var provider = services.BuildServiceProvider();

        var receiver = provider.GetRequiredService<IBrokeredMessageReceiver<RabbitMqPongMessage>>();
        Assert.NotNull(receiver);

        var hostedServices = provider.GetServices<IHostedService>().ToList();
        Assert.NotEmpty(hostedServices);
    }
}

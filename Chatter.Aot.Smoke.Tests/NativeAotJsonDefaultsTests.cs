using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.MessageBrokers;
using Chatter.MessageBrokers.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Chatter.Aot.Smoke.Tests;

public class NativeAotJsonDefaultsTests
{
    [Fact]
    public void DynamicCode_IsUnsupported_UnderNativeAot()
        => Assert.False(RuntimeFeature.IsDynamicCodeSupported);

    [Fact]
    public void JsonBodyConverter_WithoutOptions_ThrowsActionableErrorUnderNativeAot()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => new JsonBodyConverter());

        Assert.Contains("WithAotJsonSerialization", ex.Message);
    }

    [Fact]
    public void RabbitMqBodyConverter_ResolvedFromDi_RoundTripsThroughRegisteredAotOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().Build();

        services.AddChatterCqrsWithExplicitHandlers(configuration)
            .AddMessageBrokersWithExplicitReceivers()
            .WithAotJsonSerialization(PingJsonContext.Default)
            .AddRabbitMq(rmq => rmq.AddRabbitMqOptions(hostName: "aot-smoke-unused-host"));

        using var provider = services.BuildServiceProvider();
        var converter = provider.GetServices<Chatter.MessageBrokers.IBrokeredMessageBodyConverter>()
            .OfType<RabbitMqBodyConverter>()
            .Single();

        var bytes = converter.Convert(new PingPublicDto { Name = "ping", Status = PingResultStatus.Closed });
        var roundTripped = converter.Convert<PingPublicDto>(bytes);

        Assert.Equal("ping", roundTripped.Name);
        Assert.Equal(PingResultStatus.Closed, roundTripped.Status);
    }
}

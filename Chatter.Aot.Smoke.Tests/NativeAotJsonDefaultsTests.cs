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
    // Adversarial-review finding: InboundBrokeredMessage's `bodyConverter ?? new JsonBodyConverter()`
    // fallback throws for a null bodyConverter. This path is already pinned as always-throwing by
    // WhenConstructing.MustThrowNullReferenceWhenBodyConverterIsNull (a pre-existing, unrelated typo:
    // the ctor dereferences the still-null parameter, not the just-assigned property, one line after
    // the fallback assignment). Under Native AOT the throw fires one line earlier, from the fallback's
    // own JsonBodyConverter() ctor, as InvalidOperationException instead of NullReferenceException --
    // not a new failure mode for a call pattern that was already broken on every platform.
    [Fact]
    public void InboundBrokeredMessage_WithNullBodyConverter_ThrowsActionableErrorUnderNativeAot()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new Chatter.MessageBrokers.Receiving.InboundBrokeredMessage(
                "message-id", new byte[] { 1 }, new System.Collections.Generic.Dictionary<string, object>(), "receiver-path", null));

        Assert.Contains("WithAotJsonSerialization", ex.Message);
    }
}

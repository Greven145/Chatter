using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.MessageBrokers;
using Chatter.MessageBrokers.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Chatter.Aot.Smoke.Tests;

public class ConverterGetTypeInfoTests
{
    private static JsonBodyConverter NewJsonBodyConverter()
        => new(ChatterJson.CreateAotOptions(PingJsonContext.Default));

    private static RabbitMqBodyConverter ResolveRabbitMqBodyConverter()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().Build();

        services.AddChatterCqrsWithExplicitHandlers(configuration)
            .AddMessageBrokersWithExplicitReceivers()
            .WithAotJsonSerialization(PingJsonContext.Default)
            .AddRabbitMq(rmq => rmq.AddRabbitMqOptions(hostName: "aot-smoke-unused-host"));

        var provider = services.BuildServiceProvider();
        return provider.GetServices<IBrokeredMessageBodyConverter>()
            .OfType<RabbitMqBodyConverter>()
            .Single();
    }

    [Fact]
    public void JsonBodyConverter_UnderNativeAot_SerializesRuntimeTypeBehindObjectReference()
    {
        var sut = NewJsonBodyConverter();
        object body = new PingPublicDto { Name = "runtime", Status = PingResultStatus.Closed };

        var json = sut.Stringify(body);
        var roundTripped = sut.Convert<PingPublicDto>(sut.GetBytes(json));

        Assert.Equal("runtime", roundTripped.Name);
        Assert.Equal(PingResultStatus.Closed, roundTripped.Status);
    }

    [Fact]
    public void JsonBodyConverter_UnderNativeAot_ThrowsNotSupportedForBodyTypeMissingFromContext()
    {
        var sut = NewJsonBodyConverter();

        Assert.Throws<NotSupportedException>(() => sut.Stringify((object)new PingUndeclaredDto { Name = "x" }));
        Assert.Throws<NotSupportedException>(() => sut.Convert<PingUndeclaredDto>(sut.GetBytes("{\"Name\":\"x\"}")));
    }

    [Fact]
    public void RabbitMqBodyConverter_UnderNativeAot_StringifiesNullBodyAsJsonNull()
    {
        var sut = ResolveRabbitMqBodyConverter();

        Assert.Equal("null", sut.Stringify((object?)null));
    }

    [Fact]
    public void RabbitMqBodyConverter_UnderNativeAot_ThrowsNotSupportedForBodyTypeMissingFromContext()
    {
        var sut = ResolveRabbitMqBodyConverter();

        Assert.Throws<NotSupportedException>(() => sut.Stringify((object)new PingUndeclaredDto { Name = "x" }));
    }
}

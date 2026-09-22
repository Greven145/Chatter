using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.MessageBrokers;
using Chatter.MessageBrokers.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text.Json;
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
    // Adversarial-review finding: MaterializingObjectConverter.Write now calls options.GetTypeInfo(runtimeType)
    // instead of the JsonSerializerOptions-based overload. Both throw identically for an undeclared runtime
    // type (verified against the pre-change overload in a throwaway probe) -- this is the pre-existing,
    // already-documented open-world limit on ChatterMessageBrokerJsonContext, not a change in blast radius.
    [Fact]
    public void MaterializingObjectConverter_UnderNativeAot_SerializesEveryDeclaredLeafType()
    {
        var options = ChatterJson.CreateAotOptions(PingJsonContext.Default);
        var context = new Dictionary<string, object>
        {
            ["a"] = 1L,
            ["b"] = 2.5d,
            ["c"] = System.DateTime.UtcNow,
            ["d"] = "text",
            ["e"] = true,
            ["f"] = 3,
            ["g"] = System.TimeSpan.FromSeconds(1),
            ["h"] = System.Guid.NewGuid(),
            ["i"] = 4UL,
        };

        var json = JsonSerializer.Serialize(context, options);
        var roundTripped = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);

        Assert.NotNull(roundTripped);
        Assert.Equal(context.Count, roundTripped!.Count);
    }

    [Fact]
    public void MaterializingObjectConverter_UnderNativeAot_ThrowsForUndeclaredLeafType()
    {
        var options = ChatterJson.CreateAotOptions(PingJsonContext.Default);
        var context = new Dictionary<string, object>
        {
            ["undeclared"] = new PingUndeclaredDto { Name = "x" },
        };

        Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(context, options));
    }

}

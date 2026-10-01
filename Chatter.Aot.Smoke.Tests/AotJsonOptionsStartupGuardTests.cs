using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.MessageBrokers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;

namespace Chatter.Aot.Smoke.Tests;

public class AotJsonOptionsStartupGuardTests
{
    [Fact]
    public void RabbitMq_WithoutAotJsonSerialization_ThrowsAtStartupNotFirstDelivery()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().Build();

        services.AddChatterCqrsWithExplicitHandlers(configuration)
            .AddMessageBrokersWithExplicitReceivers()
            .AddRabbitMq(rmq => rmq.AddRabbitMqOptions(hostName: "aot-smoke-unused-host"));

        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<InvalidOperationException>(() => provider.GetServices<IHostedService>().ToList());
        Assert.Contains("WithAotJsonSerialization", ex.Message);
    }

    [Fact]
    public void RabbitMq_WithAotJsonSerialization_StartsCleanly()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().Build();

        services.AddChatterCqrsWithExplicitHandlers(configuration)
            .AddMessageBrokersWithExplicitReceivers()
            .WithAotJsonSerialization(PingJsonContext.Default)
            .AddRabbitMq(rmq => rmq.AddRabbitMqOptions(hostName: "aot-smoke-unused-host"));

        using var provider = services.BuildServiceProvider();

        var hostedServices = provider.GetServices<IHostedService>().ToList();
        Assert.NotEmpty(hostedServices);
    }

    [Fact]
    public void SqlServiceBroker_WithoutAotJsonSerialization_ThrowsAtStartupNotFirstDelivery()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().Build();

        services.AddChatterCqrsWithExplicitHandlers(configuration)
            .AddMessageBrokersWithExplicitReceivers()
            .AddSqlServiceBroker(ssb => ssb.AddSqlServiceBrokerOptions(connectionString: "Server=aot-smoke-unused;Database=aot-smoke;"));

        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<InvalidOperationException>(() => provider.GetServices<IHostedService>().ToList());
        Assert.Contains("WithAotJsonSerialization", ex.Message);
    }

    [Fact]
    public void SqlServiceBroker_WithAotJsonSerialization_StartsCleanly()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().Build();

        services.AddChatterCqrsWithExplicitHandlers(configuration)
            .AddMessageBrokersWithExplicitReceivers()
            .WithAotJsonSerialization(PingJsonContext.Default)
            .AddSqlServiceBroker(ssb => ssb.AddSqlServiceBrokerOptions(connectionString: "Server=aot-smoke-unused;Database=aot-smoke;"));

        using var provider = services.BuildServiceProvider();

        var hostedServices = provider.GetServices<IHostedService>().ToList();
        Assert.NotEmpty(hostedServices);
    }

    // BodyConverterFactory enumerates every registered IBrokeredMessageBodyConverter up front
    // (BodyConverterFactory.InitProviderLookup), constructing the core JsonBodyConverter/TextPlainBodyConverter
    // regardless of which MessageBodyType a consumer actually selects. A non-JSON MessageBodyType does not
    // exempt a host from needing WithAotJsonSerialization.
    [Fact]
    public void RabbitMq_WithNonJsonMessageBodyType_StillThrowsAtStartup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().Build();

        services.AddChatterCqrsWithExplicitHandlers(configuration)
            .AddMessageBrokersWithExplicitReceivers()
            .AddRabbitMq(rmq => rmq.AddRabbitMqOptions(hostName: "aot-smoke-unused-host", messageBodyType: "text/plain"));

        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<InvalidOperationException>(() => provider.GetServices<IHostedService>().ToList());
        Assert.Contains("WithAotJsonSerialization", ex.Message);
    }

    // The guard lives on AddCoreMessageBrokerServices, called by every broker module (RabbitMQ,
    // SqlServiceBroker, and any other, e.g. Azure Service Bus, which has no AOT-published smoke coverage
    // of its own yet -- see #429) through AddMessageBrokers/AddMessageBrokersWithExplicitReceivers. Proven
    // here with no broker module registered at all, confirming the fix is not broker-specific.
    [Fact]
    public void CoreMessageBrokers_WithoutAotJsonSerialization_ThrowsAtStartupRegardlessOfBroker()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().Build();

        services.AddChatterCqrsWithExplicitHandlers(configuration)
            .AddMessageBrokersWithExplicitReceivers();

        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<InvalidOperationException>(() => provider.GetServices<IHostedService>().ToList());
        Assert.Contains("WithAotJsonSerialization", ex.Message);
    }
}

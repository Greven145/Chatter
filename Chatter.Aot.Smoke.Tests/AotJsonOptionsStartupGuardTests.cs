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
}

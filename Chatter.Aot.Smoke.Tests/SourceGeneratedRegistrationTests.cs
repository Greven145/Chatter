using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.CQRS;
using Chatter.CQRS.Queries;
using Chatter.CQRS.SourceGenerated;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chatter.Aot.Smoke.Tests;

public class SourceGeneratedRegistrationTests
{
    [Fact]
    public async Task GeneratedHandlerRegistration_UnderNativeAot_DispatchesToDiscoveredCommandHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().Build();

        services.AddChatterCqrsWithExplicitHandlers(configuration);
        GeneratedHandlerRegistration.RegisterAll(services);

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IMessageDispatcher>();

        var command = new GeneratedPingCommand();
        await dispatcher.Dispatch(command);

        Assert.True(command.WasHandled);
    }

    [Fact]
    public async Task GeneratedHandlerRegistration_UnderNativeAot_DispatchesToDiscoveredEventHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().Build();

        services.AddChatterCqrsWithExplicitHandlers(configuration);
        GeneratedHandlerRegistration.RegisterAll(services);

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IMessageDispatcher>();

        var evt = new GeneratedPingEvent();
        await dispatcher.Dispatch(evt);

        Assert.True(evt.WasHandled);
    }

    [Fact]
    public async Task GeneratedHandlerRegistration_UnderNativeAot_DispatchesToDiscoveredQueryHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().Build();

        services.AddChatterCqrsWithExplicitHandlers(configuration);
        GeneratedHandlerRegistration.RegisterAll(services);

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IQueryDispatcher>();
        IQuery<string> query = new GeneratedPingQuery();

        var result = await dispatcher.Query(query);

        Assert.Equal("generated-pong", result);
    }

    [Fact]
    public async Task GeneratedAllCommandsBehaviorRegistration_UnderNativeAot_ExecutesInPipelineAroundHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().Build();

        services.AddChatterCqrsWithExplicitHandlers(configuration);
        services.AddCommandHandler<GeneratedBehaviorPingCommand, GeneratedBehaviorPingCommandHandler>();
        GeneratedAllCommandsBehaviorRegistration.RegisterAll(services);

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IMessageDispatcher>();

        var command = new GeneratedBehaviorPingCommand();
        await dispatcher.Dispatch(command);

        Assert.Equal(["handler"], command.ExecutionOrder);
        Assert.Contains(nameof(GeneratedBehaviorPingCommand), GeneratedAllCommandsBehavior<GeneratedBehaviorPingCommand>.InvokedFor);
    }
}

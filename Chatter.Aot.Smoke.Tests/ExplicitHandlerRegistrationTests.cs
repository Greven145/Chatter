using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.CQRS;
using Chatter.CQRS.Queries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chatter.Aot.Smoke.Tests;

public class ExplicitHandlerRegistrationTests
{
    [Fact]
    public async Task AddCommandHandler_UnderNativeAot_DispatchesToRegisteredHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().Build();

        services.AddChatterCqrsWithExplicitHandlers(configuration);
        services.AddCommandHandler<ExplicitPingCommand, ExplicitPingCommandHandler>();

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IMessageDispatcher>();

        var command = new ExplicitPingCommand();
        await dispatcher.Dispatch(command);

        Assert.True(command.WasHandled);
    }

    [Fact]
    public async Task AddEventHandler_UnderNativeAot_DispatchesToAllRegisteredHandlers()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().Build();

        services.AddChatterCqrsWithExplicitHandlers(configuration);
        services.AddEventHandler<ExplicitPingEvent, ExplicitPingEventHandlerFirst>();
        services.AddEventHandler<ExplicitPingEvent, ExplicitPingEventHandlerSecond>();

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IMessageDispatcher>();

        var evt = new ExplicitPingEvent();
        await dispatcher.Dispatch(evt);

        Assert.True(evt.HandledByFirst);
        Assert.True(evt.HandledBySecond);
    }

    [Fact]
    public async Task AddQueryHandler_UnderNativeAot_DispatchesToRegisteredHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().Build();

        services.AddChatterCqrsWithExplicitHandlers(configuration);
        services.AddQueryHandler<ExplicitPingQuery, string, ExplicitPingQueryHandler>();

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IQueryDispatcher>();
        IQuery<string> query = new ExplicitPingQuery();

        var result = await dispatcher.Query(query);

        Assert.Equal("pong", result);
    }
}

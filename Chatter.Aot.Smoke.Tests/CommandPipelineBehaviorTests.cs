using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.CQRS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chatter.Aot.Smoke.Tests;

public class CommandPipelineBehaviorTests
{
    [Fact]
    public async Task AddCommandBehavior_UnderNativeAot_ExecutesInPipelineAroundHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().Build();

        services.AddChatterCqrsWithExplicitHandlers(configuration);
        services.AddCommandHandler<BehaviorPingCommand, BehaviorPingCommandHandler>();
        services.AddCommandBehavior<BehaviorPingCommand, BehaviorPingCommandBehavior>();

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IMessageDispatcher>();

        var command = new BehaviorPingCommand();
        await dispatcher.Dispatch(command);

        Assert.Equal(["behavior-before", "handler", "behavior-after"], command.ExecutionOrder);
    }
}

using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.CQRS.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Chatter.Aot.Smoke.Tests;

public class QueryDispatcherTests
{
    [Fact]
    public async Task NonGenericQueryOverload_UnderNativeAot_DispatchesSuccessfully()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IQueryHandler<ExplicitPingQuery, string>, ExplicitPingQueryHandler>();
        services.AddInMemoryQueryDispatcher();

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IQueryDispatcher>();
        IQuery<string> query = new ExplicitPingQuery();

        var result = await dispatcher.Query(query);

        Assert.Equal("pong", result);
    }
}

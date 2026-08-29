using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.CQRS.Queries;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Extensions.DependencyInjection;

namespace Chatter.Aot.Smoke.Tests;

public class QueryDispatcherTests
{
    [Fact]
    [Trait("AotStatus", "KnownGap")]
    public async Task NonGenericQueryOverload_UnderNativeAot_ThrowsRuntimeBinderException()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IQueryHandler<ExplicitPingQuery, string>, ExplicitPingQueryHandler>();
        services.AddInMemoryQueryDispatcher();

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IQueryDispatcher>();
        IQuery<string> query = new ExplicitPingQuery();

        await Assert.ThrowsAsync<RuntimeBinderException>(() => dispatcher.Query(query));
    }
}

using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.CQRS.Queries;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Extensions.DependencyInjection;

namespace Chatter.Aot.Smoke.Tests;

// QueryDispatcher.cs's non-generic Query<TResult>(IQuery<TResult>[, ctx]) overload (~lines 27-44)
// uses `dynamic` + Type.MakeGenericType to resolve and invoke the handler.
// This is a hard Native-AOT-throw site, confirmed here: under the published,
// executed native binary the `dynamic` call-site binder (Microsoft.CSharp.RuntimeBinder) cannot
// resolve the concrete handler's Handle method via late-bound reflection, since the required
// reflection metadata was trimmed. The handler IS correctly registered and resolvable (this test
// registers it explicitly, not via Scrutor) — dispatch itself is what fails.
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

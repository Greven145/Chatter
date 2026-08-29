using BenchmarkDotNet.Attributes;
using Chatter.CQRS.Context;
using Chatter.CQRS.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Chatter.Benchmarks;

// Baseline for #275 Phase 1 (QueryDispatcher's dynamic + MakeGenericType overload is a planned
// internal-only rewrite target). Captures both overloads so Phase 1's "after" numbers can be diffed
// against the CURRENT non-generic dynamic-dispatch path specifically, not just dispatch in general.
[MemoryDiagnoser]
public class QueryDispatcherBenchmarks
{
    private IQueryDispatcher _dispatcher = null!;
    private BenchmarkQuery _query = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IQueryHandler<BenchmarkQuery, string>, BenchmarkQueryHandler>();
        services.AddInMemoryQueryDispatcher();

        var provider = services.BuildServiceProvider();
        _dispatcher = provider.GetRequiredService<IQueryDispatcher>();
        _query = new BenchmarkQuery();
    }

    // The Query<TResult>(IQuery<TResult>) overload: `dynamic` + Type.MakeGenericType per call.
    [Benchmark(Baseline = true)]
    public Task<string> NonGenericDynamicDispatch() => _dispatcher.Query(_query);

    // The Query<TQuery, TResult>(TQuery) overload: fully statically typed, no dynamic/reflection.
    [Benchmark]
    public Task<string> GenericDispatch() => _dispatcher.Query<BenchmarkQuery, string>(_query);

    // Public: the dynamic call-site binder inside QueryDispatcher.Query (a different assembly)
    // cannot resolve members on a private/internal target type.
    public sealed class BenchmarkQuery : IQuery<string>
    {
    }

    public sealed class BenchmarkQueryHandler : IQueryHandler<BenchmarkQuery, string>
    {
        public Task<string> Handle(BenchmarkQuery query, IQueryHandlerContext context) => Task.FromResult("pong");
    }
}

using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.MessageBrokers.Receiving;
using Chatter.SqlChangeFeed;
using Chatter.SqlChangeFeed.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace Chatter.Aot.Smoke.Tests;

public class SqlChangeFeedReceiverRegistrationTests
{
    [Fact]
    public void AddSqlChangeFeed_UnderNativeAot_ConstructsReceiverAndHostedServiceWithoutReflection()
    {
        var chatterBuilder = AotHostFactory.NewAotHost(out var services);
        chatterBuilder
            .AddSqlChangeFeed<ChangeFeedPongRow>(
                connectionString: "Server=aot-smoke-unused;Database=aot-smoke;",
                databaseName: null,
                tableName: "aot-smoke-table");

        using var provider = services.BuildServiceProvider();

        var receiver = provider.GetRequiredService<IBrokeredMessageReceiver<ProcessChangeFeedCommand<ChangeFeedPongRow>>>();
        Assert.NotNull(receiver);

        // ProcessChangeFeedCommandViaChatter defaults to true, which swaps in the internal
        // ChangeFeedReceiver<TRowChangeData> override instead of the generic BrokeredMessageReceiver<TMessage>
        // shape the other modules' receivers use. ChangeFeedReceiver is internal to Chatter.SqlChangeFeed, so
        // it cannot be named directly from this assembly — assert on the resolved type's name instead. Accepted
        // tradeoff: a rename of ChangeFeedReceiver silently breaks this string match rather than failing to
        // compile; there's no InternalsVisibleTo grant to this assembly, so a compile-time check isn't available.
        Assert.Equal("ChangeFeedReceiver`1", receiver.GetType().Name);

        var hostedServices = provider.GetServices<IHostedService>().ToList();
        Assert.NotEmpty(hostedServices);
    }
}

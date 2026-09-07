using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.MessageBrokers;
using Chatter.MessageBrokers.Reliability.Outbox;
using Chatter.MessageBrokers.Sending;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Chatter.Aot.Smoke.Tests;

public class OutboxAotJsonDualPathTests
{
    [Fact]
    public async Task InMemoryBrokeredMessageOutbox_UnderNativeAot_SerializesAndReplaysMessageContextThroughInjectedAotOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().Build();

        var chatterBuilder = services.AddChatterCqrsWithExplicitHandlers(configuration);
        chatterBuilder.AddMessageBrokersWithExplicitReceivers();
        chatterBuilder.WithAotJsonSerialization(PingJsonContext.Default);

        using var provider = services.BuildServiceProvider();
        var outbox = provider.GetRequiredService<IBrokeredMessageOutbox>();
        var aotOptions = ChatterJson.CreateAotOptions(PingJsonContext.Default);

        var sentAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var messageContext = new Dictionary<string, object>
        {
            [MessageContext.ContentType] = "application/json",
            ["SentAtUtc"] = sentAt,
        };
        var bodyConverter = new JsonBodyConverter(aotOptions);
        var outbound = new OutboundBrokeredMessage("aot-outbox-1", System.Text.Encoding.UTF8.GetBytes("{}"), messageContext, "dest", bodyConverter);

        await outbox.SendToOutbox(outbound, null, TestContext.Current.CancellationToken);

        var pending = (await ((IPollableOutboxStore)outbox).GetUnprocessedMessagesFromOutbox(TestContext.Current.CancellationToken)).Single(m => m.MessageId == "aot-outbox-1");

        // Exercises MaterializePersistedContext's AOT-mode branch directly — the deserialize-side half of the
        // same dual-path SendToOutbox's serialize just proved, through the identical injected AOT options.
        var replayed = MessageContext.MaterializePersistedContext(pending.MessageContext, aotOptions);

        Assert.Equal("application/json", replayed[MessageContext.ContentType]);
        Assert.Equal(sentAt, replayed["SentAtUtc"]);
    }
}

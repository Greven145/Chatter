using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.MessageBrokers;
using Chatter.MessageBrokers.Context;
using Chatter.MessageBrokers.Routing.Options;
using Chatter.MessageBrokers.Routing.Slips;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;

namespace Chatter.Aot.Smoke.Tests;

public class RoutingSlipAotTests
{
    private static MessageBrokerContext NewContext(IDictionary<string, object> applicationProperties, JsonSerializerOptions aotOptions)
        => new("message-id", new byte[] { 1 }, applicationProperties, "receiver-path", CancellationToken.None, new JsonBodyConverter(aotOptions));

    [Fact]
    public void RoutingSlip_UnderNativeAot_AttachAndDetachRoundTrips()
    {
        var aotOptions = ChatterJson.CreateAotOptions(PingJsonContext.Default);

        var slip = RoutingSlipBuilder.NewRoutingSlip(Guid.NewGuid())
            .WithRoute("first")
            .WithRoute("second")
            .Build();
        slip.RouteToNextStep();
        slip.Attachments["note"] = "aot-smoke";

        var send = new SendOptions();
        send.WithRoutingSlip(slip, aotOptions);
        Assert.True(send.MessageContext.TryGetValue(MessageContext.RoutingSlip, out _));

        var serializeContext = new Dictionary<string, object>();
        var serializeSut = NewContext(serializeContext, aotOptions);
        serializeSut.BrokeredMessage.WithRoutingSlip(slip, aotOptions);

        var deserializeContext = new Dictionary<string, object>
        {
            [MessageContext.RoutingSlip] = serializeContext[MessageContext.RoutingSlip],
        };
        var deserializeSut = NewContext(deserializeContext, aotOptions);

        var found = deserializeSut.TryGetRoutingSlip(out var roundTripped, aotOptions);

        Assert.True(found);
        Assert.Equal(slip.Id, roundTripped.Id);
        Assert.Single(roundTripped.Route);
        Assert.Equal("second", roundTripped.Route[0].DestinationPath);
        Assert.Single(roundTripped.Visited);
        Assert.Equal("first", roundTripped.Visited[0].DestinationPath);
        Assert.Equal("aot-smoke", roundTripped.Attachments["note"]);
    }

    [Fact]
    public void RoutingSlip_UnderNativeAot_WithoutOptionsThrowsActionableError()
    {
        var context = new Dictionary<string, object>();
        var message = new MessageBrokerContext("message-id", new byte[] { 1 }, context, "receiver-path",
            CancellationToken.None, new JsonBodyConverter(ChatterJson.CreateAotOptions(PingJsonContext.Default)));

        var slip = RoutingSlipBuilder.NewRoutingSlip(Guid.NewGuid()).WithRoute("only").Build();

        Assert.Throws<InvalidOperationException>(() => message.BrokeredMessage.WithRoutingSlip(slip));
    }
}

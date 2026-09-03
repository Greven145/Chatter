using Chatter.CQRS;
using Chatter.CQRS.Commands;
using Chatter.CQRS.Context;
using System.Text.Json.Serialization;

namespace Chatter.Samples.RabbitMq;

// Source-generated type metadata for PingSent, combined with Chatter's own envelope-shape context
// via ChatterJson.CreateAotOptions / WithAotJsonSerialization - the AOT-safe alternative to the
// default ChatterJson.Options reflection path, required for message-body (de)serialization to
// work under Native AOT.
[JsonSerializable(typeof(PingSent))]
internal sealed partial class PingSentJsonContext : JsonSerializerContext;

public static class PingQueue
{
    public const string Name = "chatter-samples-ping";
    public const string DeadLetterName = "chatter-samples-ping-deadletter";
}

public sealed class PingSent : ICommand
{
    public required string Payload { get; init; }
}

// Signals a real round trip: PingSent published directly to RabbitMQ via IBrokeredMessageSender
// -> consumed back by this same process's queue receiver -> handled here.
public sealed class PingReceived
{
    public readonly TaskCompletionSource<string> Received = new();
}

public sealed class PingSentHandler(PingReceived signal) : IMessageHandler<PingSent>
{
    public Task Handle(PingSent message, IMessageHandlerContext context)
    {
        Console.WriteLine($"[received] PingSent: payload={message.Payload}");
        signal.Received.TrySetResult(message.Payload);
        return Task.CompletedTask;
    }
}

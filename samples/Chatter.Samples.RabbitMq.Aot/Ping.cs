using Chatter.CQRS;
using Chatter.CQRS.Commands;
using Chatter.CQRS.Context;
using System.Text.Json.Serialization;

namespace Chatter.Samples.RabbitMq.Aot;

public static class PingQueue
{
    public const string Name = "chatter-samples-ping-aot";
    public const string DeadLetterName = "chatter-samples-ping-aot-deadletter";
}

public sealed class PingSent : ICommand
{
    public required string Payload { get; init; }
}

// Source-generated type metadata for PingSent - the AOT-safe replacement for
// ChatterJson.Options' reflection-based DefaultJsonTypeInfoResolver. Combined with Chatter's own
// envelope-shape context inside ChatterJson.CreateAotOptions (wired in via
// WithAotJsonSerialization below), this is what lets the RabbitMQ transport (de)serialize the
// message body without any runtime reflection - required for a clean Native AOT publish, not
// just for JIT correctness.
[JsonSerializable(typeof(PingSent))]
internal sealed partial class PingSentJsonContext : JsonSerializerContext;

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

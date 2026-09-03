using Chatter.MessageBrokers.RabbitMQ;
using Chatter.MessageBrokers.Sending;
using Chatter.Samples.RabbitMq.Aot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// This sample compiles ONLY Chatter's AOT-safe registration surface - no AddChatterCqrs
// (assembly scanning), no AddMessageBrokers' scanning overload, no runtime mode switch. Unlike
// Chatter.Samples.RabbitMq (which compiles both paths into one binary so its reflection-based
// `scan` mode's genuinely trim-unsafe calls still show up in a `dotnet publish` warning count,
// even though only its `explicit` mode is meant to run under AOT), this project's
// `dotnet publish -c Release -r linux-x64` is verified warning-free: nothing trim-unsafe is
// compiled in at all, so there's nothing for the analyzer to flag.
//
// What a consumer does differently here versus the default reflection-based path:
//   1. AddChatterCqrsWithExplicitHandlers instead of AddChatterCqrs - no assembly scan.
//   2. AddCommandHandler<TCommand, THandler>() per handler instead of relying on scanning to
//      discover IMessageHandler<> implementations.
//   3. AddQueueReceiver<TMessage> for the broker receiver - already AOT-safe either way (it
//      delegates to the generic Services.AddReceiver<TMessage>, confirmed by reading
//      RabbitMqOptionsBuilder.cs), so no change needed there versus the default path.
//   4. WithAotJsonSerialization(consumerContext) with a [JsonSerializable]-decorated
//      JsonSerializerContext for every message DTO - required for message-body (de)serialization
//      to work under Native AOT at all, not just to silence a warning. Without it, the receiver
//      throws at runtime the moment it tries to deserialize a real message (verified directly
//      while building Chatter.Samples.RabbitMq's explicit mode - see that project's PR history).
var amqpUri = Environment.GetEnvironmentVariable("CHATTER_SAMPLES_AMQP_URI") ?? "amqp://guest:guest@localhost:5672/";

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<PingReceived>();

var chatterBuilder = builder.Services.AddChatterCqrsWithExplicitHandlers(builder.Configuration);
chatterBuilder.Services.AddCommandHandler<PingSent, PingSentHandler>();
chatterBuilder
    .AddMessageBrokers()
    .WithAotJsonSerialization(PingSentJsonContext.Default)
    .AddRabbitMq(rmq => rmq
        .AddRabbitMqOptions(uri: amqpUri)
        .AddQueueReceiver<PingSent>(PingQueue.Name, deadLetterQueuePath: PingQueue.DeadLetterName));

using var host = builder.Build();
await host.StartAsync();

Console.WriteLine("[startup] explicit-only AOT sample");

var signal = host.Services.GetRequiredService<PingReceived>();

// Give the receiver's background service a moment to establish its RabbitMQ consumer before
// publishing, so the round trip below is a genuine receive, not a race against startup.
await Task.Delay(TimeSpan.FromSeconds(2));

using (var scope = host.Services.CreateScope())
{
    var dispatcher = scope.ServiceProvider.GetRequiredService<IBrokeredMessageDispatcher>();
    var payload = $"hello from the explicit-only AOT sample at {DateTimeOffset.UtcNow:O}";
    Console.WriteLine($"[publish] PingSent: payload={payload}");
    await dispatcher.Send(new PingSent { Payload = payload }, PingQueue.Name);
}

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
await using var registration = timeout.Token.Register(() => signal.Received.TrySetCanceled());

try
{
    var received = await signal.Received.Task;
    Console.WriteLine($"[success] round trip complete, payload={received}");
}
catch (TaskCanceledException)
{
    Console.Error.WriteLine("[failure] timed out waiting for the round trip to complete");
    Environment.ExitCode = 1;
}

// Let the receiver finish acknowledging the just-handled delivery before the host tears down its
// connection - stopping immediately races the in-flight ack against connection shutdown.
await Task.Delay(TimeSpan.FromSeconds(1));
await host.StopAsync();

using Chatter.MessageBrokers.RabbitMQ;
using Chatter.MessageBrokers.Sending;
using Chatter.Samples.RabbitMq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Demonstrates a real round trip against RabbitMQ (see ../docker-compose.yml) under two
// registration paths, selected by the first argument:
//   explicit (default) - Chatter's AOT-safe, reflection-free registration API. This is the mode
//     published and run as a Native AOT binary.
//   scan               - Chatter's default, reflection-based assembly-scanning registration.
//     Run under the JIT (`dotnet run`) only; scanning is not AOT-safe.
var mode = args.Length > 0 ? args[0] : "explicit";
var amqpUri = Environment.GetEnvironmentVariable("CHATTER_SAMPLES_AMQP_URI") ?? "amqp://guest:guest@localhost:5672/";

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<PingReceived>();

if (mode == "scan")
{
    builder.Services
        .AddChatterCqrs(builder.Configuration, typeof(PingSentHandler))
        .AddMessageBrokers()
        .AddRabbitMq(rmq => rmq
            .AddRabbitMqOptions(uri: amqpUri)
            .AddQueueReceiver<PingSent>(PingQueue.Name, deadLetterQueuePath: PingQueue.DeadLetterName));
}
else
{
    var chatterBuilder = builder.Services.AddChatterCqrsWithExplicitHandlers(builder.Configuration);
    chatterBuilder.Services.AddCommandHandler<PingSent, PingSentHandler>();
    chatterBuilder
        .AddMessageBrokers()
        .WithAotJsonSerialization(PingSentJsonContext.Default)
        .AddRabbitMq(rmq => rmq
            .AddRabbitMqOptions(uri: amqpUri)
            .AddQueueReceiver<PingSent>(PingQueue.Name, deadLetterQueuePath: PingQueue.DeadLetterName));
}

using var host = builder.Build();
await host.StartAsync();

Console.WriteLine($"[startup] mode={mode}");

var signal = host.Services.GetRequiredService<PingReceived>();

// Give the receiver's background service a moment to establish its RabbitMQ consumer before
// publishing, so the round trip below is a genuine receive, not a race against startup.
await Task.Delay(TimeSpan.FromSeconds(2));

using (var scope = host.Services.CreateScope())
{
    var dispatcher = scope.ServiceProvider.GetRequiredService<IBrokeredMessageDispatcher>();
    var payload = $"hello from {mode} mode at {DateTimeOffset.UtcNow:O}";
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

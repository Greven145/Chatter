using Chatter.CQRS;
using Chatter.CQRS.Queries;
using Chatter.Samples.Cqrs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Demonstrates Chatter.CQRS's standard, reflection-based assembly-scanning registration —
// the default path most consumers use. See Chatter.Samples.RabbitMq for the AOT-safe explicit
// registration alternative alongside real broker traffic.
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<OrderLedger>();
builder.Services.AddChatterCqrs(
    builder.Configuration,
    pipelineBuilder: pipeline => pipeline.WithBehavior<LoggingCommandBehavior<CreateOrder>>(),
    messageHandlerSourceBuilder: source => source.WithMarkerTypes(typeof(CreateOrderHandler)));

using var host = builder.Build();
await host.StartAsync();

var dispatcher = host.Services.GetRequiredService<IMessageDispatcher>();
var queries = host.Services.GetRequiredService<IQueryDispatcher>();

await dispatcher.Dispatch(new CreateOrder { CustomerId = "customer-1", Total = 42.50m });

var total = await queries.Query(new GetOrderTotal { CustomerId = "customer-1" });
Console.WriteLine($"[query] GetOrderTotal(customer-1) = {total:C}");

await host.StopAsync();

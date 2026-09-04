using Chatter.CQRS;
using Chatter.CQRS.Queries;
using Chatter.CQRS.SourceGenerated;
using Chatter.Samples.Cqrs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Demonstrates Chatter.CQRS's source-generated registration: referencing Chatter.CQRS at all
// pulls in its embedded analyzer, which discovers every IMessageHandler<>/IQueryHandler<,>
// implementation in this compilation at build time (no marker attribute needed) and every
// generic command-behavior type marked [RegisterForAllCommands] - LoggingCommandBehavior<T>
// below - and emits explicit, reflection-free registration calls for each into
// GeneratedHandlerRegistration.RegisterAll / GeneratedAllCommandsBehaviorRegistration.RegisterAll.
// Same AOT safety as writing AddCommandHandler<T,H>() by hand (see Chatter.Samples.RabbitMq.Aot),
// with zero per-type registration code to maintain.
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<OrderLedger>();

var chatterBuilder = builder.Services.AddChatterCqrsWithExplicitHandlers(builder.Configuration);
GeneratedHandlerRegistration.RegisterAll(chatterBuilder.Services);
GeneratedAllCommandsBehaviorRegistration.RegisterAll(chatterBuilder.Services);

using var host = builder.Build();
await host.StartAsync();

var dispatcher = host.Services.GetRequiredService<IMessageDispatcher>();
var queries = host.Services.GetRequiredService<IQueryDispatcher>();

await dispatcher.Dispatch(new CreateOrder { CustomerId = "customer-1", Total = 42.50m });

var total = await queries.Query(new GetOrderTotal { CustomerId = "customer-1" });
Console.WriteLine($"[query] GetOrderTotal(customer-1) = {total:C}");

await host.StopAsync();

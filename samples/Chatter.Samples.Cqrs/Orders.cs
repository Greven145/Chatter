using Chatter.CQRS;
using Chatter.CQRS.Commands;
using Chatter.CQRS.Context;
using Chatter.CQRS.Events;
using Chatter.CQRS.Pipeline;
using Chatter.CQRS.Queries;

namespace Chatter.Samples.Cqrs;

public sealed class CreateOrder : ICommand
{
    public required string CustomerId { get; init; }
    public required decimal Total { get; init; }
}

public sealed class GetOrderTotal : IQuery<decimal>
{
    public required string CustomerId { get; init; }
}

public sealed class OrderCreated : IEvent
{
    public required string CustomerId { get; init; }
    public required decimal Total { get; init; }
}

// A minimal in-memory read model store standing in for a real persistence layer, so the
// query handler has something real to read that the command handler actually wrote.
public sealed class OrderLedger
{
    private readonly Dictionary<string, decimal> _totalsByCustomer = new();

    public void Record(string customerId, decimal total) => _totalsByCustomer[customerId] = total;

    public decimal TotalFor(string customerId) => _totalsByCustomer.GetValueOrDefault(customerId);
}

public sealed class CreateOrderHandler(OrderLedger ledger, IMessageDispatcher dispatcher) : IMessageHandler<CreateOrder>
{
    public async Task Handle(CreateOrder message, IMessageHandlerContext context)
    {
        ledger.Record(message.CustomerId, message.Total);
        await dispatcher.Dispatch(new OrderCreated { CustomerId = message.CustomerId, Total = message.Total }, context);
    }
}

public sealed class GetOrderTotalHandler(OrderLedger ledger) : IQueryHandler<GetOrderTotal, decimal>
{
    public Task<decimal> Handle(GetOrderTotal message, IQueryHandlerContext context)
        => Task.FromResult(ledger.TotalFor(message.CustomerId));
}

public sealed class OrderCreatedHandler : IMessageHandler<OrderCreated>
{
    public Task Handle(OrderCreated message, IMessageHandlerContext context)
    {
        Console.WriteLine($"[event] OrderCreated: customer={message.CustomerId} total={message.Total:C}");
        return Task.CompletedTask;
    }
}

// A cross-cutting pipeline behavior wrapped around every ICommand dispatch, registered via the
// default reflection-based CommandPipelineBuilder.WithBehavior — this sample demonstrates the
// library's standard (non-AOT-safe) registration surface; see Chatter.Samples.RabbitMq for the
// AOT-safe explicit alternative. Closed-generic behavior registration (WithBehavior<T>() for one
// specific command) requires the behavior itself to be generic over the command type — the
// library resolves ICommandBehavior<> by reflecting on the behavior type's own generic arguments.
public sealed class LoggingCommandBehavior<TCommand> : ICommandBehavior<TCommand> where TCommand : ICommand
{
    public async Task Handle(TCommand message, IMessageHandlerContext messageHandlerContext, CommandHandlerDelegate next)
    {
        Console.WriteLine($"[behavior] Handling {typeof(TCommand).Name}");
        await next();
        Console.WriteLine($"[behavior] Handled {typeof(TCommand).Name}");
    }
}

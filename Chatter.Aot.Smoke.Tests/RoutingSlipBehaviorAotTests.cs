using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.CQRS;
using Chatter.CQRS.Commands;
using Chatter.CQRS.Context;
using Chatter.MessageBrokers;
using Chatter.MessageBrokers.Context;
using Chatter.MessageBrokers.Receiving;
using Chatter.MessageBrokers.Routing.Options;
using Chatter.MessageBrokers.Routing.Slips;
using Chatter.MessageBrokers.Sending;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chatter.Aot.Smoke.Tests;

public sealed class RoutingSlipBehaviorCommand : ICommand
{
}

internal sealed class RecordingBrokeredMessageDispatcher : IBrokeredMessageDispatcher
{
    public string LastDestination { get; private set; }

    public Task Send<TMessage>(TMessage message, string destinationPath, TransactionContext transactionContext = null, SendOptions options = null) where TMessage : ICommand
    {
        LastDestination = destinationPath;
        return Task.CompletedTask;
    }

    public Task Send<TMessage>(TMessage message, TransactionContext transactionContext = null, SendOptions options = null) where TMessage : ICommand
        => throw new NotImplementedException();

    public Task Send<TMessage>(TMessage message, string destinationPath, IMessageHandlerContext messageHandlerContext, SendOptions options = null) where TMessage : ICommand
        => throw new NotImplementedException();

    public Task Send<TMessage>(TMessage message, IMessageHandlerContext messageHandlerContext, SendOptions options = null) where TMessage : ICommand
        => throw new NotImplementedException();

    public Task Publish<TMessage>(TMessage message, string destinationPath, TransactionContext transactionContext = null, PublishOptions options = null) where TMessage : CQRS.Events.IEvent
        => throw new NotImplementedException();

    public Task Publish<TMessage>(TMessage message, TransactionContext transactionContext = null, PublishOptions options = null) where TMessage : CQRS.Events.IEvent
        => throw new NotImplementedException();

    public Task Publish<TMessage>(IEnumerable<TMessage> messages, TransactionContext transactionContext = null, PublishOptions options = null) where TMessage : CQRS.Events.IEvent
        => throw new NotImplementedException();

    public Task Publish<TMessage>(TMessage message, string destinationPath, IMessageHandlerContext messageHandlerContext, PublishOptions options = null) where TMessage : CQRS.Events.IEvent
        => throw new NotImplementedException();

    public Task Publish<TMessage>(TMessage message, IMessageHandlerContext messageHandlerContext, PublishOptions options = null) where TMessage : CQRS.Events.IEvent
        => throw new NotImplementedException();

    public Task Publish<TMessage>(IEnumerable<TMessage> messages, IMessageHandlerContext messageHandlerContext, PublishOptions options = null) where TMessage : CQRS.Events.IEvent
        => throw new NotImplementedException();

    public Task Forward(InboundBrokeredMessage inboundBrokeredMessage, string forwardDestination, TransactionContext transactionContext)
        => throw new NotImplementedException();

    public Task Forward(string forwardDestination, IMessageBrokerContext context)
        => throw new NotImplementedException();
}

public class RoutingSlipBehaviorAotTests
{
    [Fact]
    public async Task RoutingSlipBehavior_UnderNativeAot_SendsToNextDestinationWithoutExplicitOptions()
    {
        // jsonOptions is only ever supplied to RoutingSlipBehavior's own constructor (the real,
        // DI-resolved production path) -- never passed explicitly to TryGetRoutingSlip/Send, proving the
        // ambient-to-DI threading actually closes the chain the adversarial review found broken.
        var aotOptions = ChatterJson.CreateAotOptions(PingJsonContext.Default);

        var slip = RoutingSlipBuilder.NewRoutingSlip(Guid.NewGuid())
            .WithRoute("next-destination")
            .Build();

        var sendOptions = new SendOptions();
        sendOptions.WithRoutingSlip(slip, aotOptions);
        var serializedSlip = (string)sendOptions.MessageContext[MessageContext.RoutingSlip];

        var messageContext = new Dictionary<string, object>
        {
            [MessageContext.RoutingSlip] = serializedSlip,
        };
        var bodyConverter = new JsonBodyConverter(aotOptions);
        var context = new MessageBrokerContext("message-id", new byte[] { 1 }, messageContext, "receiver-path",
            CancellationToken.None, bodyConverter);

        var dispatcher = new RecordingBrokeredMessageDispatcher();
        context.Container.Include<IExternalDispatcher>(dispatcher);

        var behavior = new RoutingSlipBehavior<RoutingSlipBehaviorCommand>(
            NullLogger<RoutingSlipBehavior<RoutingSlipBehaviorCommand>>.Instance, aotOptions);

        var nextCalled = false;
        Task Next()
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        await behavior.Handle(new RoutingSlipBehaviorCommand(), context, Next);

        Assert.True(nextCalled);
        Assert.Equal("next-destination", dispatcher.LastDestination);
    }
}
